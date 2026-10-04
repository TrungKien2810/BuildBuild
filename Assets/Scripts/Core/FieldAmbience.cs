using System.Collections.Generic;
using UnityEngine;

namespace BuildBuild.Core
{
    /// <summary>
    /// Lop dong nen san co tran wave (nguoi dung duyet 2026-10-03): dan chim se bay ngang mep rung trai sang phai, vai
    /// con buom luon thap tren co. Tuc co lay la `BackdropLoop` dat san trong scene. Nam trong nen wave nen tat cung
    /// nen khi sang Chinh Phat. SceneBuilder dien moi toa do.
    /// </summary>
    public sealed class FieldAmbience : MonoBehaviour
    {
        [Header("Dan chim bay ngang mep rung, trai sang phai")]
        [SerializeField] Sprite[] birdFrames;
        [SerializeField] float birdFps = 10f;
        [SerializeField] Vector2 birdBandY;       // world y thap / cao cua dan
        [SerializeField] Vector2 birdGap = new Vector2(15f, 30f);
        [SerializeField] float left;
        [SerializeField] float right;

        [Header("Buom luon trong mot khung (world)")]
        [SerializeField] Sprite[] butterflyFrames;
        [SerializeField] float butterflyFps = 12f;
        [SerializeField] int butterflyCount = 2;
        [SerializeField] Rect butterflyArea;

        [SerializeField] int order;

        const float Pixel = 0.02f;   // nen ve tren luoi 2 px

        readonly List<Transform> _birds = new List<Transform>();
        readonly List<float> _birdSpeeds = new List<float>();
        readonly List<Transform> _butterflies = new List<Transform>();
        float _nextFlock;

        void OnEnable() => _nextFlock = Time.time + Random.Range(3f, 8f);

        void Start()
        {
            for (int i = 0; i < butterflyCount; i++) _butterflies.Add(Spawn("Butterfly", butterflyFrames, butterflyFps));
        }

        void Update()
        {
            for (int i = _birds.Count - 1; i >= 0; i--)
            {
                var p = _birds[i].position;
                p.x += _birdSpeeds[i] * Time.deltaTime;
                if (p.x > right + 0.6f)
                {
                    Destroy(_birds[i].gameObject);
                    _birds.RemoveAt(i);
                    _birdSpeeds.RemoveAt(i);
                    continue;
                }
                _birds[i].position = p;
                // Nhap nho len xuong + ghim luoi 2 px: con chau dung con, khong nhoe.
                float bob = Mathf.Sin(Time.time * 2.5f + i) * 0.05f;
                _birds[i].GetChild(0).position = new Vector3(Snap(p.x), Snap(p.y + bob), p.z);
            }
            if (_birds.Count == 0 && Time.time >= _nextFlock) SpawnFlock();

            // Buom troi theo nhieu Perlin rieng tung con: luon cham, doi huong muot, khong ra khoi khung.
            for (int i = 0; i < _butterflies.Count; i++)
            {
                float t = Time.time * 0.12f;
                float x = Mathf.Lerp(butterflyArea.xMin, butterflyArea.xMax, Mathf.PerlinNoise(t, i * 7.3f));
                float y = Mathf.Lerp(butterflyArea.yMin, butterflyArea.yMax, Mathf.PerlinNoise(i * 3.1f, t));
                y += Mathf.Sin(Time.time * 3f + i) * 0.04f;
                _butterflies[i].GetChild(0).position = new Vector3(Snap(x), Snap(y), 0f);
            }
        }

        void SpawnFlock()
        {
            int count = Random.Range(1, 4);
            float y = Random.Range(birdBandY.x, birdBandY.y);
            for (int i = 0; i < count; i++)
            {
                var bird = Spawn("Bird", birdFrames, birdFps);
                bird.position = new Vector3(left - 0.4f - i * 0.6f - Random.value * 0.3f, y + Random.Range(-0.15f, 0.15f), 0f);
                _birds.Add(bird);
                _birdSpeeds.Add(Random.Range(2.2f, 2.8f));
            }
            _nextFlock = Time.time + Random.Range(birdGap.x, birdGap.y);
        }

        Transform Spawn(string name, Sprite[] frames, float fps)
        {
            var root = new GameObject(name).transform;
            root.SetParent(transform, false);
            var body = new GameObject("Body", typeof(SpriteRenderer));
            body.transform.SetParent(root, false);
            var sr = body.GetComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            sr.sortingOrder = order;
            body.AddComponent<BackdropLoop>().Setup(frames, fps);
            return root;
        }

        static float Snap(float v) => Mathf.Round(v / Pixel) * Pixel;
    }
}
