using System;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Hinh MAC DINH cua hieu ung trang thai (`Resources/StatusLibrary.asset`): nguon nao chua co asset rieng (khong
    /// truyen `fx` vao `StatusEffects.Apply`) thi ve FX chung o day.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Status Library")]
    public class StatusLibrary : ScriptableObject
    {
        [Serializable]
        public struct Visual
        {
            public StatusId id;
            [Tooltip("FX lap bam than (o chan unit) suot luc hieu ung con. Trong = khong ve.")]
            public Sprite[] fxFrames;
        }

        public Visual[] visuals;

        static StatusLibrary _instance;
        static bool _loaded;

        /// <summary>FX bam than cua <paramref name="id"/>; null neu chua cau hinh.</summary>
        public static Sprite[] FxOf(StatusId id)
        {
            if (!_loaded)
            {
                _loaded = true;
                _instance = Resources.Load<StatusLibrary>("StatusLibrary");
            }
            if (_instance == null || _instance.visuals == null) return null;
            foreach (var visual in _instance.visuals)
                if (visual.id == id) return visual.fxFrames;
            return null;
        }
    }
}
