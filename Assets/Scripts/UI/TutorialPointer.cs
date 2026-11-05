using UnityEngine;

namespace BuildBuild.UI
{
    /// <summary>
    /// Cho nut dang duoc huong dan "tho" nhe de mat nguoi choi roi dung cho — lop chi diem cua day huong
    /// dan tan thu (`TipPanel` goi, `Meta/Tutorial` quyet buoc nao).
    ///
    /// Vi sao Nhap nhay chinh cai nut chu khong ve vong sang quanh no: project nay khong co sprite vong
    /// highlight, va mot vong ve bang code se lech khoi nut that moi lan doi kich thuoc/anchor. Nut that
    /// thi luon dung cho, luon dung kich thuoc, va khong phai them asset nao.
    ///
    /// Lop thuong (khong phai MonoBehaviour): no khong co gi de dat vao scene, va `TipPanel` da chay
    /// `Update` moi frame — them mot component chi de tick la them mot thu phai noi day o `SceneBuilder`.
    /// </summary>
    public class TutorialPointer
    {
        /// <summary>Nhip tho: 1,1 vong/giay — du cham de khong giat, du nhanh de thay la "bam toi day".</summary>
        const float PulseHz = 1.1f;
        const float PeakScale = 1.08f;

        Transform _target;
        Vector3 _restScale = Vector3.one;

        public void PointAt(Transform target)
        {
            if (_target == target) return;
            Release();
            if (target == null) return;
            _target = target;
            // Giu lai scale goc: co nut duoc dat scale khac 1 trong scene, tra ve 1 la hong bo cuc cua no.
            _restScale = target.localScale;
        }

        /// <summary>Tra nut ve nguyen trang va thoi nhap nhay. Goi duoc nhieu lan.</summary>
        public void Release()
        {
            // `!=` cua Unity da tinh ca truong hop doi tuong da bi huy khi doi scene.
            if (_target != null) _target.localScale = _restScale;
            _target = null;
        }

        public void Tick()
        {
            if (_target == null) return;
            // unscaledTime: the huong dan van tho khi game dang tam dung (timeScale = 0).
            float wave = (Mathf.Sin(Time.unscaledTime * PulseHz * Mathf.PI * 2f) + 1f) * 0.5f;
            _target.localScale = _restScale * Mathf.Lerp(1f, PeakScale, wave);
        }
    }
}
