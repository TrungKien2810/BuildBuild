using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// So cua bo chieu Chainwarden (Cai nguc moc xich, SR — GAME_DESIGN.md muc 3, chot 2026-09-24). Noi tai Cai Nguc
    /// (giu 4 quai + Xich) nam o `HeroDefinition.maxHeld` / `heldDamageBonus`; lop nay giu hai chieu con lai.
    /// Moi so la gia tri KHOI DIEM Claude de xuat, CHUA can bang — chinh trong Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Warden Definition", fileName = "Warden_")]
    public class WardenDefinition : ScriptableObject
    {
        [Header("Chieu thuong — Quat Xich (cooldown an)")]
        public float sweepCooldown = 10f;
        [Tooltip("Ban kinh tren MAT DAT tinh tu chan Chainwarden toi mep than quai.")]
        public float sweepRadius = 1f;
        [Tooltip("Sat thuong THAP — tac dung chinh la troi.")]
        public float sweepDamage = 10f;
        [Tooltip("Troi: quai dung im nhung van danh. Tinh anh/boss bi troi mot nua.")]
        public float rootDuration = 1.5f;
        [Tooltip("FX quay xich quanh minh. Trong = khong co FX (art chua co).")]
        public Sprite[] sweepFrames;

        [Header("Chieu lon — Nem Moc (thanh mana cua Hero, SkillKind.HookThrow)")]
        [Tooltip("Tam nem (tren man, tinh tu chan Chainwarden).")]
        public float hookRange = 6f;
        [Tooltip("Giay keo quai tu cho no ve sat Chainwarden.")]
        public float hookPullTime = 0.3f;
        [Tooltip("Choang sau khi bi keo. Boss bi choang mot nua.")]
        public float hookStun = 0.8f;
        [Tooltip("Xich tu moc: quai bi keo nhan them sat thuong nay trong `hookChainDuration` giay, ke ca khi khong bi giu.")]
        public float hookChainBonus = 0.2f;
        public float hookChainDuration = 4f;
        [Tooltip("FX moc tren quai bi keo. Art `hook-fx` DANG CHO DUYET — trong thi khong co FX.")]
        public Sprite[] hookFrames;
        [Tooltip("Sao choang lap tren dau quai bi keo (dung chung Enemy/stunned-status).")]
        public Sprite[] stunFrames;
    }
}
