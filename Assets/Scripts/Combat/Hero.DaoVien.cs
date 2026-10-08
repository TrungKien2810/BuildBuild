using System.Collections;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Phan cua `Hero` cho bo Dao Vien (`DaoVienBrother`): gan component Luu Bi / Quan Vu / Truong Phi, va nhung thu ba
    /// component do can cham vao ben trong `Hero` — nam xuong cho keo day (Dong Sinh Cong Tu), giai trang thai (An Duc),
    /// khieu khich co han (Duong Duong Kieu Hong).
    /// </summary>
    public partial class Hero
    {
        DaoVienBrother _brother;
        float _tauntUntil;
        float _tauntRadius;

        bool Taunting => Time.time < _tauntUntil;

        /// <summary>He so toc hoi mana: Ket Nghia 3/3 + Nhan Duc cua Luu Bi gan do.</summary>
        float ManaRate => DaoVienBrother.ManaRate(this, _brother);

        void InitialiseBrother()
        {
            _tauntUntil = 0f;
            _brother = null;
            if (_definition.daoVien == null) return;
            switch (_definition.swornBrother)
            {
                case SwornBrother.LuuBi: _brother = Ensure<LuuBi>(); break;
                case SwornBrother.QuanVu: _brother = Ensure<QuanVu>(); break;
                case SwornBrother.TruongPhi: _brother = Ensure<TruongPhi>(); break;
            }
            if (_brother != null) _brother.Initialise(this, _definition.daoVien);
        }

        T Ensure<T>() where T : Component
        {
            var component = GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        /// <summary>Quai trong <paramref name="radius"/> bo unit khac vao cho hero nay trong <paramref name="seconds"/> giay (`Taunter`).</summary>
        public void Taunt(float radius, float seconds)
        {
            if (_dying) return;
            _tauntRadius = radius;
            _tauntUntil = Time.time + seconds;
        }

        /// <summary>An Duc: go moi debuff (xem `ClearDebuffs`).</summary>
        public void Cleanse()
        {
            if (_dying) return;
            ClearDebuffs();
        }

        /// <summary>
        /// Dong Sinh Cong Tu: goi tu `OnDied` thay cho chet. Nam im <paramref name="delay"/> giay (0 mau, moi don bi bo qua),
        /// roi day voi <paramref name="health"/> ti le mau toi da + bat tu <paramref name="invulnerable"/> giay.
        /// </summary>
        public void BeginRevive(float delay, float health, float invulnerable, Sprite[] silk)
        {
            StartCoroutine(Revive(delay, health, invulnerable, silk));
        }

        IEnumerator Revive(float delay, float health, float invulnerable, Sprite[] silk)
        {
            _health.Invulnerable = true;
            _casting = false;
            _charging = false;
            Status.Apply(StatusId.Stun, 0f, delay);     // khong di, khong danh, animation dung hinh
            if (animator != null) animator.Play("hurt", restart: true);
            OneShotFx.Spawn(transform.position, silk, false, SortOrder.ForUnit(this) + 1, Battlefield.UnitScale);
            yield return new WaitForSeconds(delay);
            if (_dying) yield break;
            _health.Revive(_health.Max * health);
            yield return new WaitForSeconds(invulnerable);
            if (!_dying && !_petrified) _health.Invulnerable = false;
        }
    }
}
