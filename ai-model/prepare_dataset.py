"""Audit images and persist deterministic grouped 70/15/15 manifests, without copying images."""
from __future__ import annotations

import argparse
import csv
import json
import random
from collections import Counter, defaultdict
from pathlib import Path

from data import CLASS_NAMES, FOLDERS, image_fingerprints


def make_groups(records: list[dict], near_distance: int = 4, explicit: dict | None = None) -> list[list[dict]]:
    parents = list(range(len(records)))

    def find(i):
        while parents[i] != i:
            parents[i] = parents[parents[i]]
            i = parents[i]
        return i

    def join(i, j):
        parents[find(j)] = find(i)

    for i, left in enumerate(records):
        for j in range(i):
            right = records[j]
            same_pixels = left["sha256_pixels"] == right["sha256_pixels"]
            if same_pixels and left["label"] != right["label"]:
                raise ValueError(f"Conflicting labels for identical image: {left['path']} / {right['path']}")
            same_group = explicit and explicit.get(left["path"]) is not None and explicit.get(left["path"]) == explicit.get(right["path"])
            near = left["label"] == right["label"] and (left["dhash"] ^ right["dhash"]).bit_count() <= near_distance
            if same_pixels or same_group or near:
                join(i, j)
    clusters = defaultdict(list)
    for i, record in enumerate(records):
        clusters[find(i)].append(record)
    return list(clusters.values())


def assign_splits(groups: list[list[dict]], seed: int) -> list[dict]:
    """Greedily balance class deficits, keeping whole (possibly cross-label) groups together."""
    totals = Counter(int(row["label"]) for group in groups for row in group)
    counts = {split: Counter() for split in ("train", "validation", "test")}
    ratios = {"train": .70, "validation": .15, "test": .15}
    ordered = list(groups)
    random.Random(seed).shuffle(ordered)
    ordered.sort(key=len, reverse=True)
    rows = []
    for index, group in enumerate(ordered):
        composition = Counter(int(row["label"]) for row in group)
        # Relative unfilled capacity; large groups placed first to limit fraction drift.
        def deficit(split):
            return sum(n * (ratios[split] * totals[label] - counts[split][label]) /
                       max(1, ratios[split] * totals[label]) for label, n in composition.items())
        split = max(ratios, key=deficit)
        counts[split].update(composition)
        for record in group:
            rows.append({**record, "group_id": f"group-{index:05d}", "split": split})
    for split in ratios:
        if any(counts[split][label] == 0 for label in totals):
            raise ValueError("Grouping leaves a class absent from a split. Review groups or collect more objects.")
    return sorted(rows, key=lambda row: row["path"])


def prepare(root: Path, output: Path, seed=42, near_distance=4, groups_csv: Path | None = None):
    records, invalid = [], []
    for label, folder in enumerate(FOLDERS):
        if not (root / folder).is_dir():
            raise ValueError(f"Missing class directory: {root / folder}")
        for path in sorted((root / folder).iterdir()):
            if not path.is_file() or path.suffix.lower() not in (".jpg", ".jpeg", ".png", ".webp"):
                continue
            try:
                digest, dhash, dimensions = image_fingerprints(path)
                records.append({"path": path.relative_to(root).as_posix(), "label": label,
                                "class_name": CLASS_NAMES[label], "sha256_pixels": digest, "dhash": dhash,
                                "width": dimensions[0], "height": dimensions[1]})
            except Exception as exc:
                invalid.append({"path": str(path), "error": str(exc)})
    if invalid:
        raise ValueError(f"Invalid images must be reviewed before splitting: {invalid}")
    explicit = None
    if groups_csv:
        with groups_csv.open(encoding="utf-8-sig", newline="") as stream:
            explicit = {row["path"]: row["group_id"] for row in csv.DictReader(stream)}
        if set(explicit) != {row["path"] for row in records}:
            raise ValueError("groups CSV must cover all image paths exactly")
    groups = make_groups(records, near_distance, explicit)
    rows = assign_splits(groups, seed)
    output.mkdir(parents=True, exist_ok=True)
    with (output / "splits.csv").open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader(); writer.writerows(rows)
    duplicate_groups = [group for group in groups if len(group) > 1]
    audit = {"dataset_root": str(root.resolve()), "seed": seed, "class_names": CLASS_NAMES,
             "total": len(records), "class_counts": dict(Counter(row["class_name"] for row in records)),
             "split_counts": {split: dict(Counter(row["class_name"] for row in rows if row["split"] == split))
                              for split in ("train", "validation", "test")},
             "group_count": len(groups), "largest_group": max(map(len, groups)),
             "exact_duplicate_excess": len(records) - len({row["sha256_pixels"] for row in records}),
             "near_hash_distance": near_distance, "multi_image_groups": [[r["path"] for r in g] for g in duplicate_groups],
             "explicit_object_session_groups": bool(explicit), "invalid_images": invalid,
             "limitations": "No object/session metadata supplied. dHash grouping is heuristic, not proof of object-disjoint splits."
             if not explicit else "Object/session grouping supplied by user; verify its correctness."}
    (output / "dataset_audit.json").write_text(json.dumps(audit, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({k: v for k, v in audit.items() if k != "multi_image_groups"}, ensure_ascii=False, indent=2))
    return audit


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dataset", type=Path, default=Path(__file__).parent.parent / "Dataset/dataset-four-classes")
    parser.add_argument("--output", type=Path, default=Path(__file__).parent / "reports-organic")
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--near-distance", type=int, choices=range(0, 9), default=4)
    parser.add_argument("--groups-csv", type=Path)
    args = parser.parse_args()
    prepare(args.dataset, args.output, args.seed, args.near_distance, args.groups_csv)
