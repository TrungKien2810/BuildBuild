using System;
using UnityEngine;

namespace BuildBuild.Core
{
    /// <summary>
    /// Tua nhanh + tam dung cua tran (GDD muc 10.1). Truoc day chi `AdminPanel` (phim F1) cham duoc
    /// `Time.timeScale` — nguoi choi thuong khong co cach nao tua nhanh hay dung lai giua wave.
    ///
    /// Trang thai nam o LOP TINH chu khong o `BattleHud`: bang chon hero du bi cua `HeroBoard` cung ghi
    /// `Time.timeScale` (no luu `_resumeScale` roi tra lai), nen hai noi phai nhin cung mot nguon.
    ///
    /// Muc tua CHI co hieu luc khi dang danh (`SetBattle`, do `WaveManager` goi theo `IsFighting`): ngoai tran
    /// `Time.timeScale` phai la 1. Truoc day x2/x3 theo nguoi choi ra ca sanh lan scene khac nen hero idle
    /// quay nhanh gap doi (nguoi dung 2026-10-21).
    /// </summary>
    public static class GameSpeed
    {
        static readonly float[] Steps = { 1f, 2f, 3f };
        static int _index;
        static bool _paused;
        static bool _inBattle;

        public static float Value => Steps[_index];
        public static string Label => _paused ? "Paused" : $"x{Steps[_index]:0.#}";
        public static bool Paused => _paused;

        /// <summary>Trang thai doi (nhan nut tua / tam dung) — nut tren HUD cap nhat chu.</summary>
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _index = 0;
            _paused = false;
            _inBattle = false;
            Changed = null;
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Sang scene khac thi bo tam dung + bo trang thai trong tran, nhung GIU muc tua (nguoi choi da chon x2
        /// thi lan vao tran sau van x2). Scene moi chua danh nen `Time.timeScale` ve 1.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneLoad()
        {
            _paused = false;
            _inBattle = false;
            Apply();
        }

        /// <summary>Trong tran hay khong — `WaveManager` goi moi lan `IsFighting` doi.</summary>
        public static void SetBattle(bool active)
        {
            if (_inBattle == active) return;
            _inBattle = active;
            Apply();
        }

        public static void Cycle() => SetIndex((_index + 1) % Steps.Length);

        public static void SetIndex(int index)
        {
            _index = Mathf.Clamp(index, 0, Steps.Length - 1);
            Apply();
            Changed?.Invoke();
        }

        public static void Pause()
        {
            _paused = true;
            Apply();
            Changed?.Invoke();
        }

        public static void Resume()
        {
            _paused = false;
            Apply();
            Changed?.Invoke();
        }

        static void Apply() => Time.timeScale = _paused ? 0f : _inBattle ? Value : 1f;
    }
}
