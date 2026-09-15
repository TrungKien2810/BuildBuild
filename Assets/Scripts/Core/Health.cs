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

        /// <summary>Tham so: sat thuong bi chan, Health cua ke danh (null neu nguon khong ro).</summary>
        public event Action<float, Health> Blocked;

        void Awake() => ResetTo(maxHealth);

        public void ResetTo(float value)
        {
            maxHealth = Mathf.Max(1f, value);
            Current = maxHealth;
        }

        /// <param name="source">Health cua ke danh — can cho hieu ung phan don; dan/FX khong ro nguon thi de null.</param>
        public void TakeDamage(float amount, Health source = null)
        {
            if (IsDead || amount <= 0f) return;
            if (Invulnerable)
            {
                Blocked?.Invoke(amount, source);
                return;
            }

            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(amount);

            if (Current <= 0f) Died?.Invoke();
        }
    }
}
