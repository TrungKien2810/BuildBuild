using System;
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
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Castle : MonoBehaviour
    {
        [SerializeField] SpriteRenderer body;

        [Header("Sprite theo muc mau")]
        [SerializeField] Sprite intact;
        [SerializeField] Sprite damaged1;   // ~60% HP
        [SerializeField] Sprite damaged2;   // ~25% HP

        [Header("Nguong doi sprite")]
        [SerializeField, Range(0f, 1f)] float damaged1Below = 0.6f;
        [SerializeField, Range(0f, 1f)] float damaged2Below = 0.25f;

        Health _health;

        public Health Health => _health;
        public event Action Destroyed;

        void Awake()
        {
            _health = GetComponent<Health>();
            _health.Damaged += _ => RefreshSprite();
            _health.Died += OnDied;
        }

        // Start, khong phai Awake: Health.Awake moi dat mau day, ma thu tu Awake giua hai component
        // khong duoc dam bao — doc Fraction truoc do la 0 va lau dai hien sprite sap ngay tu dau.
        void Start() => RefreshSprite();

        /// <summary>Hoi day mau truoc tran moi — thua thi choi lai tu wave 1 voi lau dai nguyen ven.</summary>
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
