using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// So bay len roi mo dan. Chu TextMesh + font pixel cua game (PixelFont), co bong den phia
    /// sau cho doc duoc tren nen sang. Tu huy khi het thoi gian.
    /// </summary>
    public class FloatingNumber : MonoBehaviour
    {
        /// <summary>Cao mot dong chu theo toa do the gioi — dung de xep chong nhieu so.</summary>
        public const float LineHeight = 0.22f;

        const float Lifetime = 0.8f;
        const float Rise = 0.45f;
        const float PopScale = 1.35f;
        const float PopSeconds = 0.1f;
        const float JitterX = 0.08f;
        const int FontSize = 50;   // boi so cua PixelFont.DesignPx (10)
        // TextMesh: 1 dong cao ~ characterSize * fontSize / 10 unit.
        const float CharacterSize = LineHeight * 10f / FontSize;
        static readonly Vector3 ShadowOffset = new Vector3(0.015f, -0.015f, 0f);

        static Font _font;

        TextMesh _text;
        TextMesh _shadow;
        Color _color;
        Vector3 _start;
        float _age;

        public static void Spawn(Vector3 position, string text, Color color)
        {
            if (_font == null) _font = PixelFont.Asset;

            var go = new GameObject("FloatingNumber");
            var number = go.AddComponent<FloatingNumber>();
            number._color = color;
            number._start = position + new Vector3(UnityEngine.Random.Range(-JitterX, JitterX), 0f, 0f);
            go.transform.position = number._start;

            number._shadow = Label(go.transform, text, ShadowOffset, SortOrder.Overlay + 1);
            number._text = Label(go.transform, text, Vector3.zero, SortOrder.Overlay + 2);
            number.Apply(1f);
        }

        static TextMesh Label(Transform parent, string text, Vector3 offset, int order)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = offset;

            var mesh = go.AddComponent<TextMesh>();
            mesh.font = _font;
            mesh.text = text;
            mesh.fontSize = FontSize;
            mesh.characterSize = CharacterSize;
            mesh.fontStyle = FontStyle.Bold;
            mesh.anchor = TextAnchor.LowerCenter;
            mesh.alignment = TextAlignment.Center;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _font.material;
            renderer.sortingOrder = order;
            return mesh;
        }

        void Update()
        {
            _age += Time.deltaTime;
            if (_age >= Lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float t = _age / Lifetime;
            // Ease-out: vot len nhanh roi cham lai, doc nhu bi "bat" ra khoi than.
            transform.position = _start + Vector3.up * (Rise * (1f - (1f - t) * (1f - t)));
            float pop = _age < PopSeconds ? Mathf.Lerp(PopScale, 1f, _age / PopSeconds) : 1f;
            transform.localScale = Vector3.one * pop;
            // Giu ro 60% dau, mo het o 40% cuoi.
            Apply(t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f);
        }

        void Apply(float alpha)
        {
            _text.color = new Color(_color.r, _color.g, _color.b, alpha);
            _shadow.color = new Color(0f, 0f, 0f, alpha * 0.8f);
        }
    }
}
