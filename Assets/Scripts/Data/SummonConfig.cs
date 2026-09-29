using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Thong so chung cua viec tha top linh trieu hoi (GAME_DESIGN.md muc 2, IDEAS #1). Linh KHONG ton tai nguyen: chi
    /// vong hoi chieu rieng tung loai (`SoldierDefinition.summonCooldown`) quyet dinh luc nao keo tha duoc
    /// (nguoi dung 2026-09-26).
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Summon Config", fileName = "SummonConfig")]
    public class SummonConfig : ScriptableObject
    {
        [Header("Tha top")]
        [Tooltip("Khoang cach theo truc y (tren man) giua hai con trong mot top — lech nhau de khong dung chong.")]
        public float squadSpacing = 0.4f;
        [Tooltip("FX bung len duoi chan moi con vua ra san. Trong = ve placeholder bang code (SummonBurst).")]
        public Sprite[] spawnFxFrames;
    }
}
