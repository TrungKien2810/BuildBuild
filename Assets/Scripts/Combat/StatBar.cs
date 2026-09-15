using System;
using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Thanh chi so ve tren dau hero: MAU (do, luon co — hero an don duoc nen phai doc duoc con bao
    /// nhieu) va MANA (cyan, chi hero co chieu lon). Cung mot khung, khac mau + khac do cao.
    ///
    /// Chrome TAM: chua co art thanh trong `Assets/Art/Generated/UI/` nen day dung sprite 1x1 to mau,
    /// giong cach `FrostZone` tu ve dia placeholder. Co art that thi thay hai sprite o `Pixel()`,
    /// phan con lai giu nguyen.
    /// </summary>
    public class StatBar : MonoBehaviour
    {
        // Do cao lay tu hitbox cua hero (`Hero.BodyHeight`) — DeathKnight cao gap doi Frostweaver. Mau cach dinh
        // dau mot chut, mana tren mau. Moi so o day la toa do LOCAL cua hero (da nhan `Battlefield.UnitScale`).
        const float HealthGap = 0.04f;
        const float ManaGap = 0.09f;
        static readonly Color Background = new Color(0.08f, 0.10f, 0.18f, 0.85f);
        static readonly Color HealthFill = new Color(0.85f, 0.25f, 0.25f);
        static readonly Color ManaFill = new Color(0.45f, 0.85f, 1f);      // cung ho mau voi frost FX

        const float Width = 0.32f;
        const float Height = 0.035f;
        const int Order = SortOrder.Overlay;

        static Sprite _centre;
        static Sprite _left;

        Func<float> _fraction;
        Transform _fill;

        public static void AttachHealth(Hero hero) =>
            Attach(hero, "HealthBar", HealthGap, HealthFill, () => hero.Health.Fraction);

        public static void AttachMana(Hero hero) =>
            Attach(hero, "ManaBar", ManaGap, ManaFill, () => hero.ManaFraction);

        static void Attach(Hero hero, string name, float gap, Color fill, Func<float> fraction)
        {
            var go = new GameObject(name);
            go.transform.SetParent(hero.transform, worldPositionStays: false);
            go.transform.localPosition = new Vector3(0f, hero.BodyHeight / hero.transform.lossyScale.y + gap, 0f);
            go.AddComponent<StatBar>().Build(fill, fraction);
        }

        void Build(Color fill, Func<float> fraction)
        {
            _fraction = fraction;
            Piece("Background", Pixel(centred: true), Background, Vector3.zero, new Vector3(Width, Height, 1f));

            // Pivot trai => scale x la be rong that, thanh day tu trai sang phai. Dat o mep trai cua nen.
            _fill = Piece("Fill", Pixel(centred: false), fill,
                new Vector3(-Width * 0.5f, 0f, 0f), new Vector3(0f, Height, 1f));
        }

        Transform Piece(string name, Sprite sprite, Color color, Vector3 localPosition, Vector3 localScale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = Order;
            return go.transform;
        }

        void Update()
        {
            if (_fraction == null || _fill == null) return;
            _fill.localScale = new Vector3(Width * _fraction(), Height, 1f);
        }

        // pixelsPerUnit = 1 => sprite rong dung 1 unit, nen localScale doc thang ra don vi the gioi.
        static Sprite Pixel(bool centred)
        {
            if (centred && _centre != null) return _centre;
            if (!centred && _left != null) return _left;

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            var pivot = centred ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), pivot, 1f);
            if (centred) _centre = sprite; else _left = sprite;
            return sprite;
        }
    }
}
