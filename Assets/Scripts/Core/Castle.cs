using System;
using System.Collections;
using BuildBuild.Combat;
using BuildBuild.Meta;
using UnityEngine;

namespace BuildBuild.Core
{
    /// <summary>
    /// Lau dai: muc tieu cuoi cung cua quai va la dieu kien THUA (GAME_DESIGN.md muc 1 + 2).
    /// MVP: 3 tang co dinh, HP 2000. Nang cap lau dai la pham vi mo rong.
    ///
    /// Ba sprite theo muc mau da co san o Assets/Art/Generated/Castle/.
    /// No ky thuat da biet (ghi trong HANDOFF): `castle-damaged1` chi khac ban lanh 1,1% pixel nen
    /// o kich thuoc game gan nhu khong doc ra la da mat mau — dung tin vao rieng sprite de bao mau,
    /// van can thanh mau tren HUD.
    ///
    /// Thanh tri 3 cap (GAME_DESIGN.md muc 2, code 2026-10-05): moi cap mot bo 3 sprite + mau + vi tri cac be phao, do
    /// SceneBuilder dien tu `Castle/mounts-tiers.json`. Cap doc tu save (`SaveData.castleTier`); nang o sanh giua wave.
    /// Ba cap dat cung VACH PHAI (goc GameObject) + cung DAY nen quai va 9 o hero khong phai doi.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Castle : Unit
    {
        [SerializeField] SpriteRenderer body;
        [SerializeField] SpriteRenderer[] pedestals;
        [SerializeField] float pedestalDrop;

        [Header("Sprite theo muc mau")]
        [SerializeField] Sprite intact;
        [SerializeField] Sprite damaged1;   // ~60% HP
        [SerializeField] Sprite damaged2;   // ~25% HP

        [Serializable]
        public class Tier
        {
            public string name;
            public float maxHealth = 2000f;
            [Tooltip("Gia vang nang TU cap truoc len cap nay (cap 1 = 0).")]
            public int cost;
            [Tooltip("Phai da thang wave nay moi nang len cap nay (0 = khong dieu kien).")]
            public int unlockWave;
            public Sprite intact;
            public Sprite damaged1;
            public Sprite damaged2;
            [Tooltip("Vi tri + scale cua Body (local theo goc Castle o vach phai).")]
            public Vector3 bodyPosition;
            public float bodyScale = 1f;
            [Tooltip("Vi tri local cac be phao, tu tren xuong — so phan tu = so be cua cap.")]
            public Vector3[] mounts;
            [Tooltip("Scale tung be (cung thu tu `mounts`) — phao co theo be rong mat be cua cap nay.")]
            public float[] mountScales;
        }

        [Header("Cap thanh tri (rong = giu sprite + mau dat san)")]
        [SerializeField] Tier[] tiers;

        [Header("Nguong doi sprite")]
        [SerializeField, Range(0f, 1f)] float damaged1Below = 0.6f;
        [SerializeField, Range(0f, 1f)] float damaged2Below = 0.25f;

        Health _health;
        SpriteRenderer _baseShadow;

        public override Health Health => _health;
        public override bool IsDying => _health == null || _health.IsDead;

        // Lau dai la `Unit` de hero chu dong tien danh (Chinh Phat) nham duoc thanh dich. Tuong la mot duong thang dung
        // tai x cua lau dai (cung quy uoc quai dang dung): khong be ngang, khong bi khong che, khong bi day / keo.
        public override float BodyRadius => 0f;

        // Bong chan thanh do dai rieng o duoi ve (rong bang ca sprite, khong theo ban kinh than).
        public override bool GroundShadow => false;
        public override float BodyHeight => WallAimHeight;
        public override Vector3 AimPoint(Vector3 from) => new Vector3(transform.position.x, from.y, from.z);
        public override bool IsStunned => false;
        public override void ApplyStun(float seconds, Sprite[] frames = null) { }
        public override void ApplyFreeze(float seconds) { }
        public override void PushBack(float direction) { }
        protected override bool Unmovable => true;
        // Chuan bi Chinh Phat: thanh NPC da dung tren san nhung chua danh — hero can chien (duoi moi khoang cach) khong
        // duoc lao sang danh truoc khi bam bat dau.
        public override bool TargetableBy(Hero hero) => WaveManager.Fighting;

        /// <summary>Dan nham vao mat tuong cao chung nay x2 (giua than) tinh tu hang cua ke ban.</summary>
        const float WallAimHeight = 1.5f;
        public event Action Destroyed;
        public event Action TierChanged;

        /// <summary>Cap hien tai, 1..TierCount (0 neu scene cu chua co bang cap).</summary>
        public int TierLevel { get; private set; }
        public int TierCount => tiers != null ? tiers.Length : 0;
        public Tier CurrentTier => TierLevel > 0 ? tiers[TierLevel - 1] : null;
        /// <summary>Cap ke tiep, null neu da toi da.</summary>
        public Tier NextTier => TierLevel > 0 && TierLevel < TierCount ? tiers[TierLevel] : null;

        void Awake()
        {
            _health = GetComponent<Health>();
            _health.Damaged += _ => RefreshSprite();
            _health.Died += OnDied;
            _baseShadow = UnitShadow.SpawnBaseBand(transform, "BaseShadow");
            ApplyTier(SaveSystem.Data.castleTier);
        }

        /// <summary>Doi sang cap <paramref name="level"/>: sprite, vi tri Body, mau day. Be phao do `CannonBoard` dat theo.</summary>
        public void ApplyTier(int level)
        {
            if (TierCount == 0) return;
            TierLevel = Mathf.Clamp(level, 1, TierCount);
            var tier = CurrentTier;
            intact = tier.intact;
            damaged1 = tier.damaged1;
            damaged2 = tier.damaged2;
            if (body != null)
            {
                body.transform.localPosition = tier.bodyPosition;
                body.transform.localScale = Vector3.one * tier.bodyScale;
            }
            // Mat be va chan phao cung lay tu tier.mounts; khong giu nam be tinh khi cap chi co ba/bon phao.
            if (pedestals != null)
                for (int i = 0; i < pedestals.Length; i++)
                {
                    bool active = i < tier.mounts.Length;
                    if (active) pedestals[i].transform.localPosition = tier.mounts[i] + Vector3.down * pedestalDrop;
                    pedestals[i].gameObject.SetActive(active);
                }
            _health.ResetTo(tier.maxHealth);
            RefreshSprite();
            RefreshBaseShadow();
            TierChanged?.Invoke();
        }

        /// <summary>
        /// Dai bong o chan thanh: chi trai dung doan CHAM DAT cua sprite (dinh luoi tight mesh nam sat day), khong
        /// trai het be ngang sprite — khoi dung cao hon hay le trong suot ma co bong thi doc ra mot net ke lo ra ngoai.
        /// Nam ngay duoi day, lech ve duoi-phai (sang tu tren-trai, art-style.md §7–8). Goi lai moi khi doi cap
        /// hoac lat thanh (ban sao thanh NPC o Chinh Phat).
        /// </summary>
        public void RefreshBaseShadow()
        {
            if (_baseShadow == null || body == null || body.sprite == null) return;
            var band = _baseShadow.sprite.bounds.size;
            if (band.x <= 0f) return;
            const float texel = 0.02f;                   // 1 texel bong = 2 px man hinh
            var t = body.transform;
            float bottom = float.MaxValue, left = float.MaxValue, right = float.MinValue;
            float flip = body.flipX ? -1f : 1f;                // flipX lat quanh pivot, dinh mesh thi khong
            foreach (var v in body.sprite.vertices) bottom = Mathf.Min(bottom, t.TransformPoint(new Vector3(v.x * flip, v.y)).y);
            foreach (var v in body.sprite.vertices)
            {
                var w = t.TransformPoint(new Vector3(v.x * flip, v.y));
                if (w.y > bottom + 4f * texel) continue;  // chi dinh cua doan cham dat
                left = Mathf.Min(left, w.x);
                right = Mathf.Max(right, w.x);
            }
            if (right <= left) return;
            _baseShadow.transform.position = new Vector3((left + right) / 2f + 2f * texel, bottom - band.y / 2f + texel, 0f);
            _baseShadow.transform.localScale = new Vector3((right - left) / band.x, 1f, 1f);
        }

        /// <summary>Ly do chua nang duoc (null = nang duoc). Kiem tien o nguoi goi.</summary>
        public string UpgradeBlocker()
        {
            var next = NextTier;
            if (next == null) return "Fence Maxed";
            int best = SaveSystem.Data.nextWave - 1;
            if (best < next.unlockWave) return $"Win Wave {next.unlockWave} to Unlock";
            return null;
        }

        /// <summary>Dang chay hieu ung nang cap (rung -> doi sprite -> nay ve).</summary>
        public bool IsUpgrading { get; private set; }

        /// <summary>Len cap ke tiep va luu save ngay; doi hinh qua hieu ung ~0,9 s. Nguoi goi da kiem `UpgradeBlocker` + tru vang.</summary>
        public void Upgrade()
        {
            if (NextTier == null || IsUpgrading) return;
            SaveSystem.Data.castleTier = TierLevel + 1;
            SaveSystem.Save();
            if (body == null) ApplyTier(SaveSystem.Data.castleTier);
            else StartCoroutine(UpgradeRoutine(SaveSystem.Data.castleTier));
        }

        static readonly Color UpgradeGlow = new Color(1f, 0.92f, 0.62f);

        /// <summary>Thanh cu rung manh dan + nga vang, doi sang cap moi o dinh, thanh moi nay 1,06 -> 1 va nhat vang ve trang.</summary>
        IEnumerator UpgradeRoutine(int level)
        {
            IsUpgrading = true;
            var t = body.transform;
            Vector3 basePos = t.localPosition;
            const float shake = 0.45f;
            for (float e = 0f; e < shake; e += Time.deltaTime)
            {
                float k = e / shake;
                t.localPosition = basePos + (Vector3)UnityEngine.Random.insideUnitCircle * (0.08f * k);
                body.color = Color.Lerp(Color.white, UpgradeGlow, k);
                yield return null;
            }

            ApplyTier(level);
            basePos = t.localPosition;
            float baseScale = t.localScale.x;
            const float settle = 0.45f;
            for (float e = 0f; e < settle; e += Time.deltaTime)
            {
                float k = e / settle;
                t.localScale = Vector3.one * (baseScale * Mathf.Lerp(1.06f, 1f, k * (2f - k)));
                body.color = Color.Lerp(UpgradeGlow, Color.white, k);
                yield return null;
            }
            t.localPosition = basePos;
            t.localScale = Vector3.one * baseScale;
            body.color = Color.white;
            IsUpgrading = false;
        }

        // Start, khong phai Awake: Health.Awake moi dat mau day, ma thu tu Awake giua hai component
        // khong duoc dam bao — doc Fraction truoc do la 0 va lau dai hien sprite sap ngay tu dau.
        void Start() => RefreshSprite();

        /// <summary>Hoi day mau — WaveManager goi ngay khi mot wave ket thuc (thang lan thua).</summary>
        public void Repair()
        {
            _health.ResetTo(_health.Max);
            RefreshSprite();
        }

        void RefreshSprite()
        {
            if (body == null) return;

            float f = _health.Fraction;
            if (f <= damaged2Below && damaged2 != null) body.sprite = damaged2;
            else if (f <= damaged1Below && damaged1 != null) body.sprite = damaged1;
            else if (intact != null) body.sprite = intact;
        }

        void OnDied()
        {
            RefreshSprite();
            Destroyed?.Invoke();
        }
    }
}
