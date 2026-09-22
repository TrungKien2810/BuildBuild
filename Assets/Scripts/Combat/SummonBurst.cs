using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Hieu ung xuat hien khi mot don vi vua duoc trieu hoi (dung cho Ky si tu than): quang toi bung
    /// len duoi chan roi tan trong <see cref="Duration"/> giay. Co art that (<paramref name="frames"/>)
    /// thi chay qua `OneShotFx` (tan dan da ve san trong art, khong loop); khong co thi ve placeholder
    /// bang code, cung kieu `FrostZone.Placeholder`.
    /// </summary>
    public class SummonBurst : MonoBehaviour
    {
        const float Duration = 0.9f;

        /// <summary>
        /// Art quang trieu hoi chi co 4 frame — chay o nhip chung 9 fps thi het trong 0,44 giay, chua kip nhin.
        /// 4,5 fps keo thanh ~0,9 giay, bang <see cref="Duration"/> cua ban placeholder.
        /// </summary>
        const float BurstFps = 4.5f;
        static Sprite _placeholder;

        SpriteRenderer _renderer;
        float _remaining;

        public static void Spawn(Vector3 position, float radius, Sprite[] frames = null)
        {
            if (frames != null && frames.Length > 0)
            {
                OneShotFx.Spawn(position, frames, false, SortOrder.GroundDecal, fps: BurstFps);
                return;
            }

            var go = new GameObject("SummonBurst");
            go.transform.position = position;
            go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = SortOrder.GroundDecal;
            renderer.sprite = Placeholder();

            var burst = go.AddComponent<SummonBurst>();
            burst._renderer = renderer;
            burst._remaining = Duration;
        }

        void Update()
        {
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            // Tan dan ve cuoi Duration thay vi bien mat dot ngot.
            var color = _renderer.color;
            color.a = Mathf.Clamp01(_remaining / Duration);
            _renderer.color = color;
        }

        static Sprite Placeholder()
        {
            if (_placeholder != null) return _placeholder;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var centre = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre) / (size * 0.5f);
                float alpha = d > 1f ? 0f : d > 0.7f ? 0.9f : 0.5f;   // vien dam, long trong
                texture.SetPixel(x, y, new Color(0.55f, 0.2f, 0.75f, alpha));   // tim toi, dong bo phap thuat ky si tu than
            }
            texture.Apply();

            _placeholder = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _placeholder;
        }
    }
}
