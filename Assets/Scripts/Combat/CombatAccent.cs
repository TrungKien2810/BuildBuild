using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Lop accent dung chung cho feedback don trung: material alpha-only tu `Resources/Shaders/CombatAccent`
    /// va cache mesh silhouette theo sprite. Tach khoi `HitSpark` de burst va silhouette khong phu thuoc
    /// nhau, va de chi con MOT cho tha cache khi doi scene.
    /// </summary>
    public static class CombatAccent
    {
        static Material _material;
        static readonly Dictionary<Sprite, Mesh> Silhouettes = new Dictionary<Sprite, Mesh>();

        public static Material Material
        {
            get
            {
                if (_material == null)
                    _material = new Material(Resources.Load<Shader>("Shaders/CombatAccent"));
                return _material;
            }
        }

        /// <summary>Mesh alpha cua sprite (giu UV de cat dung o atlas), cache theo sprite.</summary>
        public static Mesh Silhouette(Sprite sprite)
        {
            if (Silhouettes.TryGetValue(sprite, out var mesh) && mesh != null) return mesh;
            var points = sprite.vertices;
            var vertices = new Vector3[points.Length];
            var colors = new Color[points.Length];
            for (int i = 0; i < points.Length; i++) { vertices[i] = points[i]; colors[i] = Color.white; }
            var source = sprite.triangles;
            var triangles = new int[source.Length];
            for (int i = 0; i < source.Length; i++) triangles[i] = source[i];
            mesh = new Mesh { name = "Hit silhouette " + sprite.name, vertices = vertices,
                uv = sprite.uv, colors = colors, triangles = triangles };
            mesh.RecalculateBounds();
            Silhouettes[sprite] = mesh;
            return mesh;
        }

        /// <summary>
        /// Cache giu tham chieu Sprite cua tran cu nen atlas khong unload duoc khi doi scene
        /// (Battle -> Doi quan -> Town). Tha cache luc scene unload; mesh tao luc chay phai Destroy tay.
        /// </summary>
        public static void Release()
        {
            foreach (var mesh in Silhouettes.Values) if (mesh != null) Object.Destroy(mesh);
            Silhouettes.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            // Domain reload co the dang tat: dang ky lai mot lan, khong de chong handler.
            Silhouettes.Clear();
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        static void OnSceneUnloaded(Scene scene) => Release();
    }
}
