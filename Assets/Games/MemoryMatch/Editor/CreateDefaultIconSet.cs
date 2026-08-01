#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TapArena.MemoryMatch.EditorTools
{
    /// <summary>
    /// One-click creation of a placeholder CardIconSet using the Okabe-Ito
    /// palette (colorblind-safe, per SRS §8 accessibility requirement).
    /// Swap `icon` on each face for real art later — no code changes needed.
    /// </summary>
    public static class CreateDefaultIconSet
    {
        private static readonly (string id, Color color)[] DefaultFaces =
        {
            ("orange",       new Color(0.902f, 0.624f, 0.000f)),
            ("sky-blue",     new Color(0.337f, 0.706f, 0.914f)),
            ("bluish-green", new Color(0.000f, 0.620f, 0.451f)),
            ("yellow",       new Color(0.941f, 0.894f, 0.259f)),
            ("blue",         new Color(0.000f, 0.447f, 0.698f)),
            ("vermillion",   new Color(0.835f, 0.369f, 0.000f)),
        };

        [MenuItem("TapArena/Memory Match/Create Placeholder Icon Set")]
        public static void CreateAsset()
        {
            var set = ScriptableObject.CreateInstance<CardIconSetSO>();
            foreach (var (id, color) in DefaultFaces)
            {
                set.faces.Add(new CardFace { faceId = id, icon = null, placeholderColor = color });
            }

            const string dir = "Assets/Games/MemoryMatch/Resources";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/Games/MemoryMatch", "Resources");

            string path = $"{dir}/DefaultCardIconSet.asset";
            AssetDatabase.CreateAsset(set, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = set;
            Debug.Log($"Created placeholder CardIconSet at {path} with {set.faces.Count} colorblind-safe faces.");
        }
    }
}
#endif
