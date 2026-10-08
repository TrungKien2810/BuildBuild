using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Ban do Do Tham cua Chinh Phat (GAME_DESIGN.md muc 8): thanh minh giua ban do phu suong, bam Do Tham -> vong quet lan
    /// ra, lo 3 thanh NPC De / Ngang / Kho. Bam thanh -> bang xem truoc (lanh chua, luc chien, doi hinh 3x3, linh, phao, mau
    /// thanh, thuong) -> Tan cong = vao chuan bi Chinh Phat voi dung doi hinh do (`WaveManager.EnterConquest`). Mo tu nut
    /// "Pho ban" tren HUD sanh. Luat + so o `ConquestMap`.
    /// </summary>
    public class ConquestMapScreen : MonoBehaviour
    {
        const float ScanSeconds = 0.9f;

        [SerializeField] WaveManager waves;
        [SerializeField] Economy economy;
        [SerializeField] WaveTuning tuning;
        [SerializeField] EquipmentConfig equipment;
        [Tooltip("Pool gacha — tra pham N/R/SR/SSR cua unit cho luc chien.")]
        [SerializeField] GachaConfig gacha;
        [SerializeField] BattleHud hud;
        [SerializeField] CannonBoard cannonBoard;
        [SerializeField] HeroDefinition[] heroes;
        [SerializeField] CannonDefinition[] cannons;
        [SerializeField] SoldierDefinition[] soldiers;

        [Header("Ban do")]
        [SerializeField] GameObject root;
        [SerializeField] RectTransform map;
        [SerializeField] GameObject fog;
        [SerializeField] RectTransform scanRing;
        [SerializeField] Button scanButton;
        [SerializeField] Text scanLabel;
        [SerializeField] Text infoText;
        [SerializeField] Button closeButton;
        [SerializeField] Button[] tokens;
        [SerializeField] Text[] tokenLabels;
        [Tooltip("Token thanh NPC theo do kho: De / Ngang / Kho.")]
        [SerializeField] Sprite[] tierTokens;

        [Header("Bang xem thanh")]
        [SerializeField] GameObject preview;
        [SerializeField] Image lordPortrait;
        [SerializeField] Text lordName;
        [SerializeField] Text powerText;
        [SerializeField] Image[] heroCells;
        [SerializeField] Image[] soldierIcons;
        [SerializeField] Image[] cannonIcons;
        [SerializeField] Text castleText;
        [SerializeField] Text rewardText;
        [SerializeField] Button attackButton;
        [SerializeField] Text attackLabel;
        [SerializeField] Button previewClose;
        [SerializeField] Sprite[] lordPortraits;

        ConquestTarget _selected;
        float _scanStarted = -1f;
        float _refreshAt;

        void Start()
        {
            scanButton.onClick.AddListener(Scan);
            closeButton.onClick.AddListener(Close);
            previewClose.onClick.AddListener(() => preview.SetActive(false));
            attackButton.onClick.AddListener(Attack);
            for (int i = 0; i < tokens.Length; i++)
            {
                int index = i;
                tokens[i].onClick.AddListener(() => Select(index));
            }
            scanRing.GetComponent<Image>().sprite = RingSprite();
            scanRing.gameObject.SetActive(false);
            root.SetActive(false);
        }

        // Vong quet ve bang code (vanh vang 3 px tren o 96, loc Point) — chua co asset rieng cho hieu ung nay.
        static Sprite RingSprite()
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var gold = new Color(1f, 0.85f, 0.4f, 1f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f));
                    texture.SetPixel(x, y, d <= size / 2f && d >= size / 2f - 3f ? gold : Color.clear);
                }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        public void Open()
        {
            if (waves.IsFighting) return;
            root.SetActive(true);
            Sfx.Play("ui-open");
            preview.SetActive(false);
            _scanStarted = -1f;
            Refresh();
        }

        void Close()
        {
            root.SetActive(false);
            hud.Refresh();
        }

        void Update()
        {
            if (!root.activeSelf) return;
            if (_scanStarted >= 0f)
            {
                // Vong quet lan tu thanh minh ra het ban do roi moi lo thanh NPC.
                float t = Mathf.Clamp01((Time.unscaledTime - _scanStarted) / ScanSeconds);
                float size = Mathf.Lerp(0f, map.rect.width * 1.2f, t);
                scanRing.sizeDelta = new Vector2(size, size);
                scanRing.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f - t);
                if (t >= 1f)
                {
                    _scanStarted = -1f;
                    scanRing.gameObject.SetActive(false);
                    Refresh();
                }
            }
            if (Time.unscaledTime >= _refreshAt)
            {
                _refreshAt = Time.unscaledTime + 1f;
                RefreshScan();
            }
        }

        void Refresh()
        {
            var targets = ConquestMap.Targets;
            bool revealed = targets.Count > 0 && _scanStarted < 0f;
            fog.SetActive(!revealed);
            for (int i = 0; i < tokens.Length; i++)
            {
                bool show = revealed && i < targets.Count;
                tokens[i].gameObject.SetActive(show);
                if (!show) continue;
                var target = targets[i];
                var rect = (RectTransform)tokens[i].transform;
                var spot = ConquestMap.SpotFor(target, i);
                rect.anchoredPosition = new Vector2((spot.x - 0.5f) * map.rect.width, (spot.y - 0.5f) * map.rect.height);
                var image = tokens[i].GetComponent<Image>();
                image.sprite = tierTokens[target.tier];
                image.color = target.done ? new Color(0.45f, 0.45f, 0.45f) : Color.white;
                tokenLabels[i].text = target.done
                    ? $"{ConquestMap.TierNames[target.tier]} · đã đánh"
                    : $"{ConquestMap.TierNames[target.tier]} · LC {target.power:N0}";
                tokenLabels[i].color = TierColor(target);
            }
            RefreshScan();
        }

        void RefreshScan()
        {
            int cost = ConquestMap.ScanCost(tuning);
            float wait = ConquestMap.SecondsToFreeScan;
            scanLabel.text = cost <= 0 ? "Do Thám" : $"Quét ngay · {cost:N0} vàng";
            scanButton.interactable = _scanStarted < 0f && (cost <= 0 || economy.Gold >= cost);
            string free = cost > 0 ? $" · quét miễn phí sau {Mathf.CeilToInt(wait / 60f)} phút" : "";
            infoText.text = $"Lượt: {ConquestMap.AttacksLeft}/{ConquestMap.AttacksPerDay} · Chuỗi thắng: {ConquestMap.Streak} · " +
                            $"Lực chiến: {Mathf.RoundToInt(MyPower):N0}{free}";
        }

        float MyPower => ConquestMap.PlayerPower(heroes, cannons, cannonBoard.MountCount, soldiers, equipment, gacha);

        void Scan()
        {
            int cost = ConquestMap.ScanCost(tuning);
            if (cost > 0 && !economy.TrySpend(cost)) return;
            ConquestMap.Scan(heroes, cannons, cannonBoard.MountCount, soldiers, equipment, gacha);
            preview.SetActive(false);
            _scanStarted = Time.unscaledTime;
            scanRing.gameObject.SetActive(true);
            Refresh();
        }

        void Select(int index)
        {
            if (index >= ConquestMap.Targets.Count) return;
            _selected = ConquestMap.Targets[index];
            var target = _selected;
            preview.SetActive(true);

            lordPortrait.sprite = lordPortraits[target.lord % lordPortraits.Length];
            lordName.text = $"{ConquestMap.LordNames[target.lord % ConquestMap.LordNames.Length]} · {ConquestMap.TierNames[target.tier]}";
            float mine = Mathf.Max(1f, MyPower);
            powerText.text = $"Lực chiến {target.power:N0}  (mình {Mathf.RoundToInt(mine):N0})";
            powerText.color = TierColor(target);

            for (int i = 0; i < heroCells.Length; i++)
            {
                var hero = i < target.heroes.Count ? ConquestMap.Find(heroes, target.heroes[i]) : null;
                SetIcon(heroCells[i], hero != null ? Portrait(hero) : null);
            }
            for (int i = 0; i < soldierIcons.Length; i++)
                SetIcon(soldierIcons[i], i < target.soldiers.Count ? Portrait(ConquestMap.Find(soldiers, target.soldiers[i])) : null);
            for (int i = 0; i < cannonIcons.Length; i++)
            {
                var cannon = i < target.cannons.Count ? ConquestMap.Find(cannons, target.cannons[i]) : null;
                SetIcon(cannonIcons[i], cannon != null ? cannon.icon : null);
            }
            string reserves = target.reserves.Count > 0 ? $" · Tiếp viện {target.reserves.Count} hero" : "";
            castleText.text = $"Máu thành ×{target.castleScale:0.00} · Pháo lv {target.cannonLevel} · Hero {target.stars}★{reserves}";
            string shards = target.shards > 0 ? $" · {target.shards} mảnh" : "";
            rewardText.text = $"Thưởng: {ConquestMap.Gold(target, tuning):N0} vàng · cơ hội trang bị{shards}";

            bool can = ConquestMap.CanAttack(target);
            attackButton.interactable = can;
            attackLabel.text = target.done ? "Đã đánh" : ConquestMap.AttacksLeft <= 0 ? "Hết lượt hôm nay" : "Tấn công";
        }

        void Attack()
        {
            if (!ConquestMap.CanAttack(_selected)) return;
            root.SetActive(false);
            waves.EnterConquest(_selected);
            hud.Refresh();
        }

        static Sprite Portrait(HeroDefinition hero)
        {
            if (hero == null) return null;
            if (hero.avatar != null) return hero.avatar;
            return hero.animations != null && hero.animations.idle.IsValid ? hero.animations.idle.frames[0] : null;
        }

        static void SetIcon(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
            image.preserveAspect = true;
        }

        static Color TierColor(ConquestTarget target) =>
            target.done ? new Color(0.7f, 0.7f, 0.7f)
            : target.tier == 0 ? new Color(0.55f, 0.95f, 0.5f)
            : target.tier == 1 ? new Color(1f, 0.86f, 0.35f)
            : new Color(1f, 0.45f, 0.4f);
    }
}
