using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Mot loai LINH TRIEU HOI (GAME_DESIGN.md muc 2, IDEAS #1): cho o bai dat sau thanh, day vong hoi chieu + du tai
    /// nguyen thi keo tha ra san, sinh NGUYEN TOP `squadSize` con quanh diem tha. Het wave thi don sach.
    ///
    /// Ke thua `HeroDefinition`: tren san moi con linh la mot `Hero` nhu linh xuong cua Bonecaller — cung mau,
    /// animation, luat di chuyen (can chien duoi quai, tam xa co day xich tinh tu diem tha) va luat chan quai
    /// (`Blocker`, giu 3). Lop nay chi them phan TRIEU HOI.
    ///
    /// Moi so la gia tri KHOI DIEM Claude de xuat, CHUA can bang — chinh trong Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Soldier Definition", fileName = "Soldier_")]
    public class SoldierDefinition : HeroDefinition
    {
        [Header("Trieu hoi")]
        [Tooltip("So con sinh ra moi lan keo tha.")]
        [Min(1)] public int squadSize = 3;
        [Tooltip("Giay de vong hoi chieu tren dau day lai sau moi lan trieu hoi.")]
        public float summonCooldown = 15f;
        [Tooltip("Tai nguyen trieu hoi tru moi lan keo tha (xem `SummonConfig`).")]
        public int summonCost = 30;
    }
}
