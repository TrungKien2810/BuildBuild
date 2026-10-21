using System.Collections.Generic;
using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>Short directional impact geometry. No damage, physics, or body animation changes.</summary>
    public sealed class HitSpark : MonoBehaviour
    {
        const int Limit = 48;
        const float Lifetime = 0.16f;
        static readonly Stack<HitSpark> Pool = new Stack<HitSpark>();
        static Mesh _mesh;
        static int _active;
        static readonly int ColorId = Shader.PropertyToID("_Color");
        MeshRenderer _renderer;
        MaterialPropertyBlock _properties;
        float _age, _size;
        bool _counted;

        public static void Spawn(Vector3 point, Vector2 direction, float bodyHeight)
        {
            if (_active >= Limit || !CombatFeedback.PresentationOn) return;
            HitSpark spark = null;
            while (Pool.Count > 0 && spark == null) spark = Pool.Pop();
            if (spark == null)
            {
                var go = new GameObject("HitSpark");
                spark = go.AddComponent<HitSpark>();
                go.AddComponent<MeshFilter>().sharedMesh = Geometry();
                spark._renderer = go.AddComponent<MeshRenderer>();
                spark._renderer.sharedMaterial = CombatAccent.Material;
                spark._renderer.sortingOrder = SortOrder.Air + 1;
                spark._properties = new MaterialPropertyBlock();
            }
            spark._age = 0f;
            spark._size = Mathf.Clamp(bodyHeight * 0.42f, 0.24f, 0.62f);
            spark.transform.SetPositionAndRotation(point, Quaternion.Euler(0f, 0f,
                direction.sqrMagnitude > 0.001f ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg : 0f));
            spark.transform.localScale = Vector3.one * spark._size;
            spark._properties.SetColor(ColorId, Color.white);
            spark._renderer.SetPropertyBlock(spark._properties);
            spark.gameObject.SetActive(true);
            spark._counted = true;
            _active++;
        }

        void Update()
        {
            if (Time.timeScale <= 0f) return;
            _age += Time.unscaledDeltaTime;
            if (_age >= Lifetime)
            {
                gameObject.SetActive(false);
                Pool.Push(this);
                return;
            }
            float t = _age / Lifetime;
            transform.localScale = Vector3.one * (_size * (1f + t * 0.5f));
            _properties.SetColor(ColorId, new Color(1f, 1f, 1f, 1f - t * t));
            _renderer.SetPropertyBlock(_properties);
        }

        void OnDisable()
        {
            if (!_counted) return;
            _active--;
            _counted = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPool() { Pool.Clear(); _active = 0; }

        // White impact burst only. No colored trail or slash behind the target.
        static Mesh Geometry()
        {
            if (_mesh != null) return _mesh;
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
            {
                int start = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                colors.Add(color); colors.Add(color); colors.Add(color);
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            }
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI / 8f;
                float b = (i + 1) * Mathf.PI / 8f;
                float ra = i % 2 == 0 ? (i % 4 == 0 ? 0.9f : 0.6f) : 0.28f;
                float rb = (i + 1) % 2 == 0 ? ((i + 1) % 4 == 0 ? 0.9f : 0.6f) : 0.28f;
                Triangle(Vector2.zero, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * ra,
                    new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * rb, Color.white);
            }
            _mesh = new Mesh { name = "Combat impact star" };
            _mesh.SetVertices(vertices); _mesh.SetColors(colors);
            _mesh.SetUVs(0, new List<Vector2>(new Vector2[vertices.Count]));
            _mesh.SetTriangles(triangles, 0);
            _mesh.RecalculateBounds();
            return _mesh;
        }
    }
}
