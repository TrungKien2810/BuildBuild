using System;
using System.Collections;
using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Mot the ket qua gacha: vien theo do hiem, icon unit, ten, va ghi chu MOI / +manh. GachaScreen goi `Show` de
    /// nap du lieu, `Deal` de the up hien ra, roi `Reveal` de lat — SR/SSR co nhip nin (rung + hao quang) truoc khi lat.
    /// </summary>
    public class GachaCardView : MonoBehaviour
    {
        static readonly Color BackTint = new Color(0.3f, 0.3f, 0.36f);
        static readonly Color GlowSR = new Color(0.78f, 0.42f, 1f);
        static readonly Color GlowSSR = new Color(1f, 0.82f, 0.25f);

        [SerializeField] Image frame;
        [SerializeField] Image icon;
        [SerializeField] Text nameText;
        [SerializeField] Text noteText;

        [Header("Vien theo do hiem")]
        [SerializeField] Sprite frameN;
        [SerializeField] Sprite frameR;
        [SerializeField] Sprite frameSR;
        [SerializeField] Sprite frameSSR;

        Rarity _rarity;
        Image _glow;
        Image _flash;

        public Rarity Rarity => _rarity;

        public void Show(GachaResult result)
        {
            var unit = result.unit;
            _rarity = unit.rarity;
            frame.sprite = unit.rarity switch
            {
                Rarity.SSR => frameSSR,
                Rarity.SR => frameSR,
                Rarity.R => frameR,
                _ => frameN,
            };
            icon.sprite = unit.icon;
            icon.enabled = unit.icon != null;
            nameText.text = $"{unit.displayName}\n{unit.rarity}";
            noteText.text = result.isNew ? "MỚI"
                : result.canStarUp ? $"+{result.shardsGained} mảnh · ĐỦ NÂNG SAO"
                : $"+{result.shardsGained} mảnh";
        }

        /// <summary>The up, chua hien ra (scale 0). Goi sau `Show`.</summary>
        public void FaceDown()
        {
            SetFace(false);
            transform.localScale = Vector3.zero;
        }

        /// <summary>The up bat ra cho minh (pop co nay nhe).</summary>
        public IEnumerator Deal(Func<float> dt)
        {
            Sfx.Play("gacha-deal");
            for (float t = 0f; t < 1f; t += dt() / 0.14f)
            {
                transform.localScale = Vector3.one * BackOut(t);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        /// <summary>Lat the. `dt` tra ve buoc thoi gian da nhan toc do (cham de tua nhanh).</summary>
        public IEnumerator Reveal(Func<float> dt, Action onRare)
        {
            bool rare = _rarity >= Rarity.SR;
            Color glowColor = _rarity == Rarity.SSR ? GlowSSR : GlowSR;

            // Nin: the rung manh dan, hao quang loe len sau the — bao truoc do hiem truoc khi lat.
            if (rare)
            {
                EnsureGlow(glowColor);
                Sfx.Play("gacha-charge");
                float hold = _rarity == Rarity.SSR ? 0.9f : 0.5f;
                for (float t = 0f; t < 1f; t += dt() / hold)
                {
                    float shake = 6f * t;
                    transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 70f) * shake);
                    transform.localScale = Vector3.one * (1f + 0.12f * t);
                    _glow.color = WithAlpha(glowColor, 0.85f * t);
                    _glow.transform.localScale = Vector3.one * (1f + 0.25f * t);
                    yield return null;
                }
                transform.localRotation = Quaternion.identity;
            }

            // Lat: ep ngang ve 0, doi sang mat truoc, mo ra lai.
            float baseScale = transform.localScale.y;
            for (float t = 0f; t < 1f; t += dt() / 0.09f)
            {
                transform.localScale = new Vector3(baseScale * (1f - t), baseScale, 1f);
                yield return null;
            }
            SetFace(true);
            if (rare)
            {
                onRare?.Invoke();
                EnsureFlash();
            }
            Sfx.Play(_rarity == Rarity.SSR ? "gacha-ssr" : rare ? "gacha-rare" : "gacha-card");
            for (float t = 0f; t < 1f; t += dt() / 0.09f)
            {
                transform.localScale = new Vector3(baseScale * t, baseScale, 1f);
                yield return null;
            }

            if (!rare)
            {
                transform.localScale = Vector3.one;
                yield break;
            }

            // Bung: chop trang + nay to roi ve 1; hao quang o lai dap nhe (Update).
            float punch = _rarity == Rarity.SSR ? 1.35f : 1.22f;
            for (float t = 0f; t < 1f; t += dt() / 0.35f)
            {
                transform.localScale = Vector3.one * Mathf.Lerp(punch, 1f, 1f - (1f - t) * (1f - t));
                _flash.color = WithAlpha(Color.white, 0.9f * (1f - t));
                yield return null;
            }
            transform.localScale = Vector3.one;
            _flash.color = WithAlpha(Color.white, 0f);
        }

        void Update()
        {
            if (_glow == null) return;
            // Hao quang la anh em cua the (ve phia sau), nam ngoai GridLayout nen bam theo the moi frame.
            var card = (RectTransform)transform;
            var glow = _glow.rectTransform;
            glow.anchorMin = card.anchorMin;
            glow.anchorMax = card.anchorMax;
            glow.pivot = card.pivot;
            glow.anchoredPosition = card.anchoredPosition;
            glow.sizeDelta = card.sizeDelta;
            if (transform.localRotation == Quaternion.identity)
            {
                // Het nin: dap nhe quanh 1,2x.
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
                glow.localScale = Vector3.one * (1.18f + 0.06f * pulse) * transform.localScale.y;
                var c = _glow.color;
                if (transform.localScale.x >= 0.999f) _glow.color = WithAlpha(c, 0.55f + 0.3f * pulse);
            }
        }

        void OnDestroy()
        {
            if (_glow != null) Destroy(_glow.gameObject);
        }

        void SetFace(bool up)
        {
            frame.enabled = up;
            icon.gameObject.SetActive(up);
            nameText.gameObject.SetActive(up);
            noteText.gameObject.SetActive(up);
            GetComponent<Image>().color = up ? Color.white : BackTint;
        }

        void EnsureGlow(Color color)
        {
            if (_glow != null) return;
            // Dung chinh vien do hiem tint mau lam hao quang; dat dau cha de ve sau moi the (khong de len the ben canh).
            var go = new GameObject("Glow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            go.transform.SetParent(transform.parent, false);
            go.transform.SetAsFirstSibling();
            _glow = go.GetComponent<Image>();
            _glow.sprite = frame.sprite;
            _glow.raycastTarget = false;
            _glow.color = WithAlpha(color, 0f);
        }

        void EnsureFlash()
        {
            if (_flash != null) return;
            var go = new GameObject("Flash", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _flash = go.GetComponent<Image>();
            _flash.raycastTarget = false;
        }

        static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        static float BackOut(float t)
        {
            t = Mathf.Clamp01(t) - 1f;
            return 1f + t * t * (2.7f * t + 1.7f);
        }
    }
}
