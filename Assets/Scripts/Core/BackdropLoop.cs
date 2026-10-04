using UnityEngine;

namespace BuildBuild.Core
{
    /// <summary>Vat trang tri nen chay lap mot day frame (co bay, khoi trai, qua) — lech pha ngau nhien de cac ban khong nhip nhau.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BackdropLoop : MonoBehaviour
    {
        [SerializeField] Sprite[] frames;
        [SerializeField] float framesPerSecond = 9f;

        SpriteRenderer _renderer;
        float _phase = -1f;

        public void Setup(Sprite[] sequence, float fps)
        {
            frames = sequence;
            framesPerSecond = fps;
        }

        void Update()
        {
            if (frames == null || frames.Length == 0) return;
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            if (_phase < 0f) _phase = Random.value * frames.Length;
            _renderer.sprite = frames[(int)(Time.time * framesPerSecond + _phase) % frames.Length];
        }
    }
}
