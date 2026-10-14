using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Ve FX cua lop chieu "dat tren lan" cho KHOP vung danh that.
    ///
    /// Vi sao can: tu 2026-10-10 cac vung chieu nam ngang (dai hoa Bach Loc, song quet, luoi quet, non phun)
    /// phu HET be sau cua san (`Battlefield.InLaneBand` / `InLaneColumn`) vi quai spawn o do cao bat ky.
    /// Ve MOT tam o hang cua hero thi nguoi choi tuong chieu chi an dung hang do — hinh phai noi dung dieu
    /// gameplay dang lam.
    ///
    /// HAI cach ve, chon theo HINH DANG ART — **khong bao gio KEO GIAN art**:
    ///   - <see cref="SpawnRows"/> / <see cref="SpawnRowsOnce"/>: art lap duoc (mang, vung, dai, lua, net,
    ///     gai) -> mot tam o MOI hang hero, giu nguyen ti le hinh.
    ///   - <see cref="SpawnLane"/> / <see cref="SpawnLaneLoop"/>: art la MOT VAT THE co hinh rieng (rong muc,
    ///     huou, non phun, cay gay) -> MOT tam o co goc.
    ///
    /// BAI HOC 2026-10-10 (nguoi dung bao "ngu but, hong lien cung loi"): ban dau o day co `SpawnStretched`
    /// keo truc Y cho phu be sau san. Art FX cua du an ve tren o VUONG 1,28-2,56 world con be sau san la 11
    /// world -> phai keo **4,3x-17,2x**, dung bang vet nhoe keo doc ca man hinh nguoi dung nhin thay. Muon
    /// phu be sau thi ve NHIEU TAM hoac de nguyen co goc.
    /// </summary>
    public static class LaneFx
    {
        /// <summary>Ve <paramref name="frames"/> lap o MOI hang hero quanh <paramref name="x"/> (giu nguyen ti le hinh).
        /// Tra ve cac FX da tao de nguoi goi xoa khi het (rong neu khong co art).</summary>
        /// <param name="yOffset">Lech doc them cho tung tam — art co pivot TAM thi nang len cho nam dung mat dat.</param>
        public static GameObject[] SpawnRows(Transform parent, float x, Sprite[] frames, float seconds, int order,
            Sprite[] intro = null, float scale = 1f, float yOffset = 0f)
        {
            if (frames == null || frames.Length == 0) return System.Array.Empty<GameObject>();
            var made = new GameObject[Battlefield.LaneRows];
            for (int row = 0; row < Battlefield.LaneRows; row++)
            {
                var at = new Vector3(x, Battlefield.LaneRowCenter(row) + yOffset, 0f);
                var local = parent != null ? parent.InverseTransformPoint(at) : at;
                made[row] = OneShotFx.SpawnLoop(parent, local, frames, seconds, order, intro, scale);
            }
            return made;
        }

        /// <summary>Nhu <see cref="SpawnRows"/> nhung chay MOT lan (vu no: hoa no, mam thu hoach, luoi lua).</summary>
        public static void SpawnRowsOnce(float x, Sprite[] frames, bool flipX, int order, float scale = 1f, float yOffset = 0f)
        {
            if (frames == null || frames.Length == 0) return;
            for (int row = 0; row < Battlefield.LaneRows; row++)
                OneShotFx.Spawn(new Vector3(x, Battlefield.LaneRowCenter(row) + yOffset, 0f), frames, flipX, order, scale);
        }

        /// <summary>Ve MOT tam o co goc cho vat the co hinh dang rieng (rong muc, huou, non phun, cay gay).</summary>
        public static void SpawnLane(Vector3 at, Sprite[] frames, bool flipX, int order, float scale = 1f)
        {
            OneShotFx.Spawn(at, frames, flipX, order, scale);
        }

        /// <summary>Ban lap cua <see cref="SpawnLane"/> (vat the song suot khoang thoi gian).</summary>
        public static GameObject SpawnLaneLoop(Transform parent, Vector3 localOffset, Sprite[] frames, float seconds,
            int order, float scale = 1f)
        {
            return OneShotFx.SpawnLoop(parent, localOffset, frames, seconds, order, scale: scale);
        }
    }
}
