using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSortQuest
{
    [Serializable] public sealed class RecognitionCategory
    {
        public string class_name, display_name, guidance;
        public bool game_supported;
    }
    public static class RecognitionInfo
    {
        [Serializable] sealed class Catalog { public RecognitionCategory[] classes; }
        static RecognitionCategory[] cached;
        public static IReadOnlyList<RecognitionCategory> All
        {
            get
            {
                if (cached == null)
                {
                    var asset = Resources.Load<TextAsset>("RecognitionCatalog");
                    if (asset == null) throw new InvalidOperationException("Missing RecognitionCatalog resource");
                    cached = JsonUtility.FromJson<Catalog>(asset.text).classes;
                }
                return Array.AsReadOnly(cached);
            }
        }
        public static bool TryGet(string label, out RecognitionCategory category)
        {
            foreach (var entry in All)
                if (string.Equals(label, entry.class_name, StringComparison.OrdinalIgnoreCase))
                { category = entry; return true; }
            category = null; return false;
        }
    }
}
