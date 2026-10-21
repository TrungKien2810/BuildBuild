using System;
using UnityEngine;
using BuildBuild.Combat;

namespace BuildBuild.Core
{
    public enum DamageKind { Direct, Periodic, Reflected, Linked }
    /// <summary>
    /// Mau cua bat ky thu gi an duoc sat thuong: quai, hero, lau dai.
    /// Tach rieng khoi controller de lau dai va quai dung chung mot duong sat thuong.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] float maxHealth = 100f;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;
        public float Fraction => maxHealth <= 0f ? 0f : Mathf.Clamp01(Current / maxHealth);

        /// <summary>Sat thuong THUC TE da an vao (sau khi chan tru), goi ca khi don chi lam sut mau.</summary>
        public event Action<float> Damaged;
        // Presentation for accepted direct hits only; periodic/reflected/linked damage stays quiet.
        public event Action<Vector2> Impacted;
        public event Action Died;

        /// <summary>Mien sat thuong (vd. Cragfist Hoa Da). Don van toi — bao qua `Blocked` — nhung khong tru mau.</summary>
        public bool Invulnerable { get; set; }

        /// <summary>
        /// Phan sat thuong NHAN THEM tu moi nguon (0,2 = +20%). Trang thai Xich cua Chainwarden (GDD muc 3) — `Enemy`
        /// dat lai moi frame. Nhan ngay dau `TakeDamage` nen don cua hero, linh, phao deu huong.
        /// </summary>
        public float Vulnerability { get; set; }

        /// <summary>Tham so: sat thuong bi chan, Health cua ke danh (null neu nguon khong ro).</summary>
        public event Action<float, Health> Blocked;

        /// <summary>0..1 — phan sat thuong bi giap an mat truoc khi tru mau (tinh anh 0,15, boss 0,3).</summary>
        public float Armor { get; set; }

        /// <summary>
        /// 0..1 — phan sat thuong GIAM THEM tu buff (vung Cam Co cua Banneret), nhan sau giap. Tach khoi `Armor` vi
        /// Chainwarden / Wyrmspear ghi de `Armor` khi bien hinh.
        /// </summary>
        public float Guard { get; set; }

        /// <summary>
        /// Ban ra tren Health cua KE DANH (tham so `source` cua `TakeDamage`) voi luong mau THAT da tru o nan nhan —
        /// cho hut mau (Chainwarden Cuong No). Don bi chan / mien sat thuong khong tinh.
        /// </summary>
        public event Action<float> DamageDealt;

        /// <summary>Ban ra khi duoc hoi mau (luong thuc te da cong).</summary>
        public event Action<float> Healed;

        /// <summary>Thong bao chung de thong ke wave nhan mau that, khien da an va nguon don.</summary>
        public static event Action<Health, Health, float, float, Health> AnyDamaged;
        public static event Action<Health, Health, float> AnyHealed;
        public static event Action<Health, Health, float, Faction?> DirectDamage;
        public static event Action<Health, Health, DamageKind> KilledBy;
        public Func<float, Health, float> AdjustDirectHit { get; set; }

        public void SpendBlood(float fraction)
        {
            if (!IsDead) Current -= Mathf.Min(Current - 1f, Mathf.Floor(Current * fraction));
        }

        /// <summary>
        /// Khien ao (Den Canh cua Vespera): an sat thuong TRUOC mau, sau giap. Het khien thi ban `ShieldBroken`; het han
        /// thi nguoi dat khien goi `ClearShield`.
        /// </summary>
        public float Shield { get; private set; }

        /// <summary>
        /// Chan tron mot don TRUOC khi tinh giap/khien (Toan Phong Con cua Wukong bat dan quai ban xa): tra ve true = don bi
        /// chan, bao `Blocked`, khong tru mau. null = khong chan gi.
        /// </summary>
        public Func<float, Health, bool> Deflects { get; set; }

        public event Action ShieldBroken;

        /// <summary>
        /// Bo QUA im lang don tu nguon nay (quai bay cao: can chien khong voi toi) — khong tru mau, khong bao `Blocked`.
        /// null = nhan moi don.
        /// </summary>
        public Func<Health, bool> IgnoresHitFrom { get; set; }

        /// <summary>Nguoi dung khien dang giu (de ghi cong "Hoi mau" khi khien an don) — null neu khong ro.</summary>
        Health _shieldCaster;

        /// <summary>Cast lai khong cong don — lay luong lon hon, tuc la lam moi khien.</summary>
        public void AddShield(float amount, Health caster = null)
        {
            if (amount > Shield) _shieldCaster = caster;
            Shield = Mathf.Max(Shield, amount);
        }

        public void ClearShield() { Shield = 0f; _shieldCaster = null; }

        /// <summary>Khong nhan hoi mau toi moc nay (Thieu Hon cua Dreadwyrm).</summary>
        public float NoHealUntil { get; set; }
        public bool HealBlocked => Time.time < NoHealUntil;

        void Awake() => ResetTo(maxHealth);

        /// <summary>Hoi mau, khong vuot `Max`; da chet thi khong hoi.</summary>
        public void Heal(float amount, Health healer = null)
        {
            if (IsDead || amount <= 0f || Current >= maxHealth || HealBlocked) return;
            float before = Current;
            Current = Mathf.Min(maxHealth, Current + amount);
            float healed = Current - before;
            Healed?.Invoke(healed);
            AnyHealed?.Invoke(this, healer, healed);
        }

        public void ResetTo(float value)
        {
            maxHealth = Mathf.Max(1f, value);
            Current = maxHealth;
            Shield = 0f;
            _shieldCaster = null;
        }

        /// <summary>Keo day tu 0 mau (Dong Sinh Cong Tu cua Luu Bi): dat mau hien tai, giu mau toi da.</summary>
        public void Revive(float value) => Current = Mathf.Clamp(value, 1f, maxHealth);

        /// <summary>Doi mau toi da, giu nguyen TI LE mau hien tai (Long Giap cua Wyrmspear +60% giua tran).</summary>
        public void SetMax(float value)
        {
            float fraction = Fraction;
            maxHealth = Mathf.Max(1f, value);
            if (!IsDead) Current = Mathf.Max(1f, maxHealth * fraction);
        }

        /// <param name="source">Health cua ke danh — can cho hieu ung phan don; dan/FX khong ro nguon thi de null.</param>
        /// <param name="ignoreArmor">Bo qua `Armor` (Xuyen Long cua Wyrmspear). Vulnerability van tinh.</param>
        public void TakeDamage(float amount, Health source = null, bool ignoreArmor = false, DamageKind kind = DamageKind.Direct, Faction? side = null,
            Vector2? impactDirection = null)
        {
            if (IsDead || amount <= 0f) return;
            if (IgnoresHitFrom != null && IgnoresHitFrom(source)) return;
            amount *= 1f + Mathf.Max(0f, Vulnerability);

            if (Deflects != null && Deflects(amount, source))
            {
                Blocked?.Invoke(amount, source);
                return;
            }

            if (Invulnerable)
            {
                Blocked?.Invoke(amount, source);
                return;
            }

            if (kind == DamageKind.Direct && AdjustDirectHit != null) amount = AdjustDirectHit(amount, source);

            if (!ignoreArmor) amount *= 1f - Mathf.Clamp01(Armor);
            amount *= 1f - Mathf.Clamp01(Guard);
            float shieldAbsorbed = 0f;
            Health shieldCaster = _shieldCaster;
            if (Shield > 0f)
            {
                float absorbed = Mathf.Min(Shield, amount);
                shieldAbsorbed = absorbed;
                Shield -= absorbed;
                amount -= absorbed;
                if (Shield <= 0f) { _shieldCaster = null; ShieldBroken?.Invoke(); }
                // Khien an tron don: bao `Blocked` cho feedback "Chan", khong tru mau.
                if (amount <= 0f)
                {
                    AnyDamaged?.Invoke(this, source, 0f, shieldAbsorbed, shieldCaster);
                    if (kind == DamageKind.Direct) DirectDamage?.Invoke(this, source, 0f, side);
                    Blocked?.Invoke(absorbed, source);
                    return;
                }
            }
            float before = Current;
            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(amount);
            if (kind == DamageKind.Direct && source != this && before > Current)
                Impacted?.Invoke(impactDirection ?? (source != null ? (Vector2)(transform.position - source.transform.position) : Vector2.zero));
            // Tu tru mau (het han, rong tan) truyen chinh minh lam nguon de thong ke loai ra — khong tinh la hut mau.
            if (source != null && source != this && kind != DamageKind.Linked) source.DamageDealt?.Invoke(before - Current);
            AnyDamaged?.Invoke(this, source, before - Current, shieldAbsorbed, shieldCaster);
            if (kind == DamageKind.Direct) DirectDamage?.Invoke(this, source, before - Current, side);

            if (Current <= 0f)
            {
                KilledBy?.Invoke(this, source, kind);
                Died?.Invoke();
            }
        }
    }
}
