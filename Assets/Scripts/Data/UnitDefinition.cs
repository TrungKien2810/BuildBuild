using UnityEngine;

namespace BuildBuild.Data
{
    public enum UnitCategory { Hero, Cannon }
    public enum Rarity { N, R, SR, SSR }

    /// <summary>
    /// Mot hero hoac mot phao co the quay ra tu gacha. Do hiem gan CO DINH vao unit (chot 2026-09-11):
    /// cac hero hien tai deu N, rieng Phap su trieu hoi o bac cao hon.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Unit Definition", fileName = "Unit_")]
    public class UnitDefinition : ScriptableObject
    {
        [Tooltip("Khoa luu trong save.json — doi id la mat quyen so huu cu cua nguoi choi.")]
        public string id;
        public string displayName;
        public UnitCategory category;
        public Rarity rarity = Rarity.N;
        public Sprite icon;
    }
}
