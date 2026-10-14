using System.Collections.Generic;
using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bong chan — LOP RUNTIME DUNG CHUNG, khong bake vao sprite actor (art-style.md §7: actor khong ve blob shadow;
    /// §8: chi lop "environment" duoc phep co bong ngan canh cung).
    ///
    /// `Unit` tu gan component nay trong `OnEnable` (xem `Unit.GroundShadow`); thanh tri tat roi tu ve dai bong
    /// o chan bang `SpawnBaseBand`. Bong la CON cua unit nen xac bi xoa thi bong mat theo, nhung con nam duoi goc
    /// da nhan `Battlefield.UnitScale` (hero Warden con phinh than theo stack) nen moi frame chia lai scale.
    ///
    /// Hinh: elip det sinh bang code luc chay, vien cung khong khua rang cua, PPU 50 nen 1 texel = 2 px man hinh
    /// (cung luoi voi art unit). Be rong lay tu `Unit.BodyRadius` — quai to tu co bong to.
    /// </summary>
    [RequireComponent(typeof(Unit))]
    public class UnitShadow : MonoBehaviour
    {
        /// <summary>1 texel bong = 2 px man hinh, dung bang luoi art unit.</summary>
        const float PixelsPerUnit = 50f;

        /// <summary>Bong rong bang 2,3 lan ban kinh than: cho chan cham dat hep hon cho phinh to nhat cua sprite.</summary>
        const float WidthRatio = 2.3f;

        const int MinTexels = 10, MaxTexels = 70;

        /// <summary>Nhac nhe khoi hang chan de khong de len vet dat / vong an ky cung hang y.</summary>
        const float Lift = 0.02f;

        /// <summary>Bay lo lung: nho hon ~20% va nhat hon (alpha x0,7) de doc ra "khong cham dat".</summary>
        const float FlyScale = 0.8f, FlyAlpha = 0.7f;

        /// <summary>Dung mot mau cho ca bong chan lan dai chan thanh (art-style.md: 2 bac alpha cung, khong gradient mem).</summary>
        static readonly Color32 Core = new Color32(43, 28, 23, 128);   // #2B1C17 · alpha 0,50
        static readonly Color32 Ring = new Color32(43, 28, 23, 87);    // alpha 0,34
        static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        /// <summary>Cache theo be rong — moi co than chi sinh mot texture cho ca tran.</summary>
        static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();
        static Sprite _band;

        Unit _unit;
        SpriteRenderer _renderer;
        int _texels = -1;

        void Awake() => _unit = GetComponent<Unit>();

        /// <summary>Gan bong cho <paramref name="unit"/>; `Unit.OnEnable` goi, goi lai nhieu lan cung khong sao.</summary>
        public static void Attach(Unit unit)
        {
            if (unit == null || unit.GetComponent<UnitShadow>() != null) return;
            unit.gameObject.AddComponent<UnitShadow>();
        }

        void LateUpdate()
        {
            if (_unit == null || !_unit.GroundShadow) return;
            float radius = _unit.BodyRadius;
            if (radius <= 0f) return;   // than chua do xong (hoac la thanh tri, da tat GroundShadow)

            bool flying = _unit.FliesHigh;
            int texels = Texels(radius * (flying ? FlyScale : 1f));
            if (_renderer == null)
            {
                var go = new GameObject("Shadow");
                go.transform.SetParent(transform, false);
                _renderer = go.AddComponent<SpriteRenderer>();
                _renderer.sortingOrder = SortOrder.GroundDecal + 1;   // tren nen/vet dat, duoi moi unit
            }
            if (texels != _texels)
            {
                _texels = texels;
                _renderer.sprite = SpriteFor(texels);
            }

            var t = _renderer.transform;
            t.position = _unit.ShadowAnchor + Vector3.up * Lift;
            // Con nam duoi goc da nhan UnitScale (hero Warden phinh theo stack): bu lai de bong dung co man hinh.
            float parent = _unit.transform.lossyScale.x;
            t.localScale = Vector3.one / (parent > 0.0001f ? parent : 1f);
            _renderer.color = flying ? new Color(1f, 1f, 1f, FlyAlpha) : Color.white;
        }

        /// <summary>Be rong bong: `clamp(round(banKinh x 2,3 x 50 / 2) x 2, 10, 70)` art-px — so chan de hai nua doi xung.</summary>
        static int Texels(float radius)
        {
            int w = Mathf.RoundToInt(radius * WidthRatio * PixelsPerUnit / 2f) * 2;
            return Mathf.Clamp(w, MinTexels, MaxTexels);
        }

        /// <summary>Elip det: loi alpha 0,50 (khoang cach chuan hoa^2 &lt; 0,45), vien 0,34, ngoai cung trong suot.</summary>
        static Sprite SpriteFor(int w)
        {
            if (Cache.TryGetValue(w, out var cached)) return cached;
            int h = Mathf.Max(4, w / 3);
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x + 0.5f) / w * 2f - 1f;
                    float ny = (y + 0.5f) / h * 2f - 1f;
                    float d = nx * nx + ny * ny;
                    pixels[y * w + x] = d < 0.45f ? Core : d < 1f ? Ring : Clear;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            Cache[w] = Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            return Cache[w];
        }

        /// <summary>
        /// Dai bong o chan mot khoi TINH (thanh tri): thanh ngang canh cung, 2 hang tren/dong alpha, cao 4 texel.
        /// Nguoi goi giu renderer va tu dat vi tri/be rong theo sprite cua cap dang dung.
        /// </summary>
        public static SpriteRenderer SpawnBaseBand(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = SortOrder.GroundDecal + 1;   // tren Body cua thanh (SortOrder.Castle), duoi unit
            renderer.sprite = Band();
            return renderer;
        }

        /// <summary>Dai chan 4x4 texel: nua tren la loi, nua duoi la vien — cung 2 bac alpha voi bong chan unit.</summary>
        static Sprite Band()
        {
            if (_band != null) return _band;
            const int w = 4, h = 4;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = y >= h / 2 ? Core : Ring;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            _band = Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            return _band;
        }
    }
}
