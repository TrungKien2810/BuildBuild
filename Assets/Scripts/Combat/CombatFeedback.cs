using System;
using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Cho nguoi choi THAY don danh trung: chop mau tren than + so bay len tren dau (-20 an don, +15 hoi / hut mau,
    /// "Chan" khi mien sat thuong). Chi nghe event cua `Health` — moi nguon (hero, linh, phao, boss, hut mau
    /// Chainwarden, hoi mau Vespera) tu dong co feedback, khong nguon nao phai goi gi them.
    ///
    /// Chop mau ghi vao `_Color` cua shader qua MaterialPropertyBlock, KHONG ghi `renderer.color`: kenh do da co
    /// Enemy / EnemyExtras / Hero dat tint dong bang, choang, cham, cuong no moi frame — ghi chung thi de nhau.
    /// `_Color` nhan them len tint do (Sprite-Lit-Default: vertex color * _Color * unity_SpriteColor).
    /// </summary>
    public class CombatFeedback : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");

        const float FlashSeconds = 0.12f;
        static readonly Color HurtFlash = new Color(1f, 0.3f, 0.3f);
        static readonly Color HealFlash = new Color(0.55f, 1f, 0.55f);

        // Quai an don: vang nhat (don cua ta). Phe ta an don: do (dang mat mau). Hoi / hut mau: xanh la.
        static readonly Color EnemyHurtText = new Color(1f, 0.93f, 0.55f);
        static readonly Color AllyHurtText = new Color(1f, 0.35f, 0.3f);
        static readonly Color HealText = new Color(0.45f, 1f, 0.45f);
        static readonly Color BlockedText = new Color(0.8f, 0.8f, 0.85f);

        Health _health;
        SpriteRenderer _renderer;
        MaterialPropertyBlock _block;
        Func<float> _bodyHeight;
        bool _ally;
        float _flashUntil;

        // Gom cac don trong CUNG mot frame thanh mot so (Loc Xich trung 5 quai -> hut mau 5 lan cung frame).
        float _pendingDamage;
        float _pendingHeal;
        bool _pendingBlocked;

        /// <param name="ally">Phe ta (hero, linh) — so an don mau do; quai thi mau vang.</param>
        /// <param name="bodyHeight">Chieu cao than theo toa do the gioi — so hien ngay tren thanh mau.</param>
        public void Init(bool ally, Func<float> bodyHeight)
        {
            _ally = ally;
            _bodyHeight = bodyHeight;
        }

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _health = GetComponent<Health>();
            _block = new MaterialPropertyBlock();
            _health.Damaged += OnDamaged;
            _health.Healed += OnHealed;
            _health.Blocked += OnBlocked;
        }

        void OnDestroy()
        {
            if (_health == null) return;
            _health.Damaged -= OnDamaged;
            _health.Healed -= OnHealed;
            _health.Blocked -= OnBlocked;
        }

        void OnDamaged(float amount)
        {
            _pendingDamage += amount;
            Flash(HurtFlash);
        }

        void OnHealed(float amount)
        {
            _pendingHeal += amount;
            // Dang chop do vi vua an don thi giu do: an don doc quan trong hon hoi mau.
            if (Time.time >= _flashUntil) Flash(HealFlash);
        }

        void OnBlocked(float amount, Health source) => _pendingBlocked = true;

        void Flash(Color color)
        {
            _flashUntil = Time.time + FlashSeconds;
            SetTint(color);
        }

        void SetTint(Color color)
        {
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(ColorId, color);
            _renderer.SetPropertyBlock(_block);
        }

        void LateUpdate()
        {
            if (_flashUntil > 0f && Time.time >= _flashUntil)
            {
                _flashUntil = 0f;
                SetTint(Color.white);
            }

            if (_pendingDamage <= 0f && _pendingHeal <= 0f && !_pendingBlocked) return;

            float top = transform.position.y + (_bodyHeight != null ? _bodyHeight() : 0.5f);
            var head = new Vector3(transform.position.x, top, 0f);
            // Hai so cung luc (vua an don vua hut mau) thi xep chong, khong de len nhau.
            float stack = 0f;
            if (_pendingDamage > 0f)
            {
                FloatingNumber.Spawn(head, "-" + Round(_pendingDamage), _ally ? AllyHurtText : EnemyHurtText);
                stack += FloatingNumber.LineHeight;
            }
            if (_pendingHeal > 0f)
            {
                FloatingNumber.Spawn(head + Vector3.up * stack, "+" + Round(_pendingHeal), HealText);
                stack += FloatingNumber.LineHeight;
            }
            if (_pendingBlocked) FloatingNumber.Spawn(head + Vector3.up * stack, "Block", BlockedText);

            _pendingDamage = 0f;
            _pendingHeal = 0f;
            _pendingBlocked = false;
        }

        static int Round(float amount) => Mathf.Max(1, Mathf.RoundToInt(amount));
    }
}
