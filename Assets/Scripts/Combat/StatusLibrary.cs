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
            [Tooltip("Be ngang THAT cua FX tren san, tinh bang world. 0 = giu co goc cua art " +
                     "(sprite 128 px o 100 PPU = 1,28 world, roi nhan them scale cua unit).")]
            public float size;
        }

        public Visual[] visuals;

        static StatusLibrary _instance;
        static bool _loaded;

        static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _instance = Resources.Load<StatusLibrary>("StatusLibrary");
        }

        /// <summary>Ca muc cau hinh cua <paramref name="id"/> (art + co). Mac dinh rong neu chua cau hinh.</summary>
        public static Visual VisualOf(StatusId id)
        {
            Load();
            if (_instance == null || _instance.visuals == null) return default;
            foreach (var visual in _instance.visuals)
                if (visual.id == id) return visual;
            return default;
        }

        /// <summary>FX bam than cua <paramref name="id"/>; null neu chua cau hinh.</summary>
        public static Sprite[] FxOf(StatusId id) => VisualOf(id).fxFrames;

        /// <summary>Be ngang world mong muon cua FX (0 = giu co goc art).</summary>
        public static float SizeOf(StatusId id) => VisualOf(id).size;
    }
}
