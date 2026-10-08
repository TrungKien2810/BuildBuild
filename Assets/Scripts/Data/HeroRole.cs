using System;
using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Vai tro hero — chi de PHAN LOAI (icon tren the, bo loc man Doi quan), KHONG doi hanh vi tran: can chien / danh xa
    /// van doc tu `range` (`Hero.IsRangedUnit`). Them vai moi o CUOI enum: asset luu enum bang so.
    /// `None` = chua gan (linh, unit trieu hoi) -> khong hien icon.
    /// </summary>
    public enum HeroRole { None, Tank, Melee, Archer, Mage, Support, Healer }

    /// <summary>
    /// Ten + icon cua tung vai, nguon duy nhat cho moi man UI (the, chi tiet, bo loc). SceneBuilder tao asset
    /// `Assets/Data/HeroRoleCatalog.asset`; doi ten / icon thi sua asset do, khong chep sang tung man.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Hero Role Catalog", fileName = "HeroRoleCatalog")]
    public class HeroRoleCatalog : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public HeroRole role;
            public string displayName;
            public Sprite icon;
        }

        [Tooltip("Thu tu o day la thu tu nut tren bo loc.")]
        public Entry[] entries;

        public Entry Find(HeroRole role) => role == HeroRole.None ? null : Array.Find(entries, e => e.role == role);
        public Sprite IconOf(HeroRole role) => Find(role)?.icon;
        public string NameOf(HeroRole role) => Find(role)?.displayName ?? "";
    }
}
