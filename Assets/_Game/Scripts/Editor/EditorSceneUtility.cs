using UnityEditor;
using UnityEngine;

namespace ClashOfDragons.EditorTools
{
    /// <summary>
    /// Shared helpers for the Editor scene-setup tools (ArenaBuilder, CharacterBuilder, HudBuilder,
    /// GameManagerBuilder, ...). Keeps scene construction idempotent: re-running a builder updates
    /// existing objects/assets instead of duplicating them.
    /// </summary>
    internal static class EditorSceneUtility
    {
        private static Font cachedFont;

        public static Transform GetOrCreateRoot(string name)
        {
            GameObject root = GameObject.Find(name);
            if (root == null)
            {
                root = new GameObject(name);
            }
            return root.transform;
        }

        public static Material GetOrCreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            return material;
        }

        /// <summary>Finds or creates a plain child GameObject (regular Transform) — for 3D objects.</summary>
        public static GameObject FindOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>Finds or creates a UI child GameObject (RectTransform) — for anything under a Canvas.</summary>
        public static GameObject FindOrCreateUiChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Unity's built-in legacy UI font (the modern replacement for the old Arial.ttf).</summary>
        public static Font GetDefaultFont()
        {
            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return cachedFont;
        }
    }
}
