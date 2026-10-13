"""Verify the package import used by the backend handoff instructions."""
import importlib.util
from pathlib import Path
import sys


def test_backend_package_import():
    base = Path(__file__).parents[1]
    spec = importlib.util.spec_from_file_location("ai_model", base / "__init__.py",
                                                submodule_search_locations=[str(base)])
    module = importlib.util.module_from_spec(spec)
    sys.modules["ai_model"] = module
    try:
        spec.loader.exec_module(module)
        assert callable(module.predict_image)
        assert module.predict_image.__module__ == "ai_model.inference"
        assert sys.modules["ai_model.inference"].CLASS_NAMES == ("Plastic", "Metal", "Paper")
    finally:
        for name in list(sys.modules):
            if name == "ai_model" or name.startswith("ai_model."):
                del sys.modules[name]
