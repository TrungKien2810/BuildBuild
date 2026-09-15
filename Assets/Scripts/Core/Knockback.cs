using UnityEngine;

namespace BuildBuild.Core
{
    /// <summary>
    /// Day lui ngan khi an don (luc giat hurt). Ngan (0,1 s) va co khoang MIEN 0,6 s sau moi lan day: bi
    /// danh don dap thi chi lui mot nhip roi dung vung danh tiep, khong bi day lien tuc den mat luot danh.
    /// Chi day theo x — khong day unit ra ngoai dai dat theo chieu sau.
    /// </summary>
    public class Knockback
    {
        const float Distance = 0.25f;
        const float Duration = 0.1f;
        const float Immunity = 0.6f;

        float _until;
        float _immuneUntil;
        float _direction;

        /// <param name="direction">Dau cua x: +1 day sang phai, -1 sang trai.</param>
        public void Trigger(float direction)
        {
            if (Time.time < _immuneUntil) return;
            _direction = Mathf.Sign(direction);
            _until = Time.time + Duration;
            _immuneUntil = Time.time + Immunity;
        }

        /// <summary>Dang bi day thi dich <paramref name="target"/> va tra ve true — nguoi goi bo qua di/danh frame nay.</summary>
        public bool Step(Transform target)
        {
            if (Time.time >= _until) return false;
            target.position += new Vector3(_direction * (Distance / Duration) * Time.deltaTime, 0f, 0f);
            return true;
        }

        public void Reset()
        {
            _until = 0f;
            _immuneUntil = 0f;
        }
    }
}
