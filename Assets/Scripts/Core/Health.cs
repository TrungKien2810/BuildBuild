using System;
using UnityEngine;

namespace BuildBuild.Core
{
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

        /// <summary>
        /// Khien ao (Den Canh cua Vespera): an sat thuong TRUOC mau, sau giap. Het khien thi ban `ShieldBroken`; het han
        /// thi nguoi dat khien goi `ClearShield`.
        /// </summary>
        public float Shield { get; private set; }

        public event Action ShieldBroken;

        /// <summary>Cast lai khong cong don — lay luong lon hon, tuc la lam moi khien.</summary>
        public void AddShield(float amount) => Shield = Mathf.Max(Shield, amount);

        public void ClearShield() => Shield = 0f;

        void Awake() => ResetTo(maxHealth);

        /// <summary>Hoi mau, khong vuot `Max`; da chet thi khong hoi.</summary>
        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f || Current >= maxHealth) return;
            float before = Current;
            Current = Mathf.Min(maxHealth, Current + amount);
            Healed?.Invoke(Current - before);
        }

        public void ResetTo(float value)
        {
            maxHealth = Mathf.Max(1f, value);
            Current = maxHealth;
            Shield = 0f;
        }

        /// <summary>Doi mau toi da, giu nguyen TI LE mau hien tai (Long Giap cua Wyrmspear +60% giua tran).</summary>
        public void SetMax(float value)
        {
            float fraction = Fraction;
            maxHealth = Mathf.Max(1f, value);
            if (!IsDead) Current = Mathf.Max(1f, maxHealth * fraction);
        }

        /// <param name="source">Health cua ke danh — can cho hieu ung phan don; dan/FX khong ro nguon thi de null.</param>
        /// <param name="ignoreArmor">Bo qua `Armor` (Xuyen Long cua Wyrmspear). Vulnerability van tinh.</param>
        public void TakeDamage(float amount, Health source = null, bool ignoreArmor = false)
        {
            if (IsDead || amount <= 0f) return;
            amount *= 1f + Mathf.Max(0f, Vulnerability);

            if (Invulnerable)
            {
                Blocked?.Invoke(amount, source);
                return;
            }

            if (!ignoreArmor) amount *= 1f - Mathf.Clamp01(Armor);
            amount *= 1f - Mathf.Clamp01(Guard);
            if (Shield > 0f)
            {
                float absorbed = Mathf.Min(Shield, amount);
                Shield -= absorbed;
                amount -= absorbed;
                if (Shield <= 0f) ShieldBroken?.Invoke();
                // Khien an tron don: bao `Blocked` cho feedback "Chan", khong tru mau.
                if (amount <= 0f)
                {
                    Blocked?.Invoke(absorbed, source);
                    return;
                }
            }
            float before = Current;
            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(amount);
            if (source != null) source.DamageDealt?.Invoke(before - Current);

            if (Current <= 0f) Died?.Invoke();
        }
    }
}
