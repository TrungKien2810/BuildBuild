using System.Collections.Generic;
using BuildBuild.Combat;
using BuildBuild.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Bang thong tin hero: trong luc DANG DANH, cham vao than mot hero tren san (ca hero phe dich o Chinh Phat, ca linh
    /// trieu hoi) thi hien chi so luc nay va hieu ung dang dinh (icon + so tang + giay con lai). Ngoai wave cham o hero van
    /// la go / chon hero nhu cu (`HeroBoard`) nen bang chi bat khi `WaveManager.Fighting`.
    ///
    /// Hero khong co collider nen khong di qua Physics2DRaycaster: tu do diem cham trong khung than (`BodyRadius`,
    /// `BodyHeight`). Cham trung UI (nut, bang) thi bo qua. Bang khong che man, tran van chay; hero chet / het wave thi dong.
    /// </summary>
    public class HeroInfoPanel : MonoBehaviour
    {
        const float RefreshInterval = 0.2f;
        /// <summary>Noi rong khung cham quanh than (world) — sprite nho, ngon tay to.</summary>
        const float TapPadding = 0.2f;

        [SerializeField] GameObject root;
        [SerializeField] Image avatar;
        [SerializeField] Text nameText;
        [SerializeField] Image healthFill;
        [SerializeField] Text healthText;
        [Tooltip("O chi so dau: icon doi theo hero danh / hero hoi mau (giong man Doi quan).")]
        [SerializeField] Image powerIcon;
        [SerializeField] Sprite damageIcon;
        [SerializeField] Sprite healIcon;
        [SerializeField] Text powerText;
        [SerializeField] Text rateText;
        [SerializeField] Text rangeText;
        [SerializeField] Text guardText;
        [SerializeField] Text emptyText;
        [SerializeField] RectTransform effectContent;
        [SerializeField] GameObject effectTemplate;
        [SerializeField] Button closeButton;
        [Tooltip("Icon theo `EffectKind` (dung thu tu enum). O trong thi o hieu ung chi co chu.")]
        [SerializeField] Sprite[] effectIcons;

        readonly List<UnitEffect> _effects = new List<UnitEffect>();
        readonly List<GameObject> _cells = new List<GameObject>();
        static readonly List<RaycastResult> Hits = new List<RaycastResult>();
        Hero _hero;
        float _nextRefresh;

        void Awake()
        {
            closeButton.onClick.AddListener(Hide);
            root.SetActive(false);
        }

        void Update()
        {
            var pointer = Pointer.current;
            if (WaveManager.Fighting && pointer != null && pointer.press.wasPressedThisFrame)
            {
                var screen = pointer.position.ReadValue();
                var hero = OverUi(screen) ? null : HeroAt(screen);
                if (hero != null) Show(hero);
            }

            if (!root.activeSelf) return;
            if (_hero == null || _hero.IsDying || !WaveManager.Fighting) Hide();
            else if (Time.unscaledTime >= _nextRefresh) Refresh();
        }

        void Show(Hero hero)
        {
            _hero = hero;
            var definition = hero.Definition;
            avatar.sprite = definition.avatar;
            avatar.enabled = definition.avatar != null;
            nameText.text = hero.Stars > 0 ? $"{definition.displayName}  {hero.Stars}★" : definition.displayName;
            root.SetActive(true);
            Sfx.Play("ui-open");
            Refresh();
        }

        public void Hide()
        {
            _hero = null;
            root.SetActive(false);
        }

        void Refresh()
        {
            _nextRefresh = Time.unscaledTime + RefreshInterval;
            var health = _hero.Health;
            healthFill.fillAmount = health.Fraction;
            healthText.text = health.Shield > 0f
                ? $"{Number(health.Current)} / {Number(_hero.MaxHealth)}  (+{Number(health.Shield)})"
                : $"{Number(health.Current)} / {Number(_hero.MaxHealth)}";

            powerIcon.sprite = _hero.Definition.healsAllies ? healIcon : damageIcon;
            powerText.text = Number(_hero.AttackDamage);
            rateText.text = _hero.AttacksPerSecond.ToString("0.00", Vi) + "/s";
            rangeText.text = _hero.CurrentAttackRange.ToString("0.0", Vi);
            guardText.text = Mathf.RoundToInt(health.Guard * 100f) + "%";

            _effects.Clear();
            _hero.CollectEffects(_effects);
            foreach (var cell in _cells) Destroy(cell);
            _cells.Clear();
            emptyText.gameObject.SetActive(_effects.Count == 0);
            foreach (var effect in _effects) AddCell(effect);
        }

        void AddCell(UnitEffect effect)
        {
            var cell = Instantiate(effectTemplate, effectContent);
            cell.SetActive(true);
            _cells.Add(cell);
            var icon = cell.transform.Find("Icon").GetComponent<Image>();
            icon.sprite = effect.icon != null ? effect.icon : IconOf(effect.kind);
            icon.enabled = icon.sprite != null;
            var stacks = cell.transform.Find("Stacks").GetComponent<Text>();
            stacks.text = effect.stacks > 0 ? effect.stacks.ToString() : "";
            var time = cell.transform.Find("Time").GetComponent<Text>();
            time.text = effect.secondsLeft > 0f ? Mathf.CeilToInt(effect.secondsLeft) + "s" : "";
            cell.transform.Find("Label").GetComponent<Text>().text = effect.label;
        }

        Sprite IconOf(EffectKind kind) =>
            effectIcons != null && (int)kind < effectIcons.Length ? effectIcons[(int)kind] : null;

        static bool OverUi(Vector2 screen)
        {
            var events = EventSystem.current;
            if (events == null) return false;
            events.RaycastAll(new PointerEventData(events) { position = screen }, Hits);
            foreach (var hit in Hits)
                if (hit.module is GraphicRaycaster) return true;
            return false;
        }

        /// <summary>Hero co khung than chua diem cham; nhieu con chong nhau thi lay con co tam than gan nhat.</summary>
        static Hero HeroAt(Vector2 screen)
        {
            var camera = Camera.main;
            if (camera == null) return null;
            Vector2 point = camera.ScreenToWorldPoint(screen);
            Hero best = null;
            float bestSqr = float.PositiveInfinity;
            foreach (var hero in Hero.Alive)
            {
                if (hero == null || hero.IsDying || hero.Definition == null) continue;
                var feet = hero.transform.position;
                if (Mathf.Abs(point.x - feet.x) > hero.BodyRadius + TapPadding) continue;
                if (point.y < feet.y - TapPadding || point.y > feet.y + hero.BodyHeight + TapPadding) continue;
                float sqr = (point - (Vector2)hero.BodyCenter).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = hero;
                }
            }
            return best;
        }

        /// <summary>Kieu so Viet: 950, 12,3k, 1,2M — cung cach `ContributionPanel`.</summary>
        static string Number(float value)
        {
            if (value < 1000f) return Mathf.RoundToInt(value).ToString();
            if (value < 1000000f) return (value / 1000f).ToString("0.0", Vi) + "k";
            return (value / 1000000f).ToString("0.0", Vi) + "M";
        }

        static readonly System.Globalization.CultureInfo Vi = new System.Globalization.CultureInfo("vi-VN");
    }
}
