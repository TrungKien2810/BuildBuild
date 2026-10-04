using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Nen chung cua bo Dao Vien (GAME_DESIGN.md muc 3, `DaoVienDefinition`) — `Hero` gan `LuuBi` / `QuanVu` / `TruongPhi`
    /// theo `HeroDefinition.swornBrother`. Noi tai chung Ket Nghia (passiveStar): tu 2/3 anh em (khac nhau, cung phe, con
    /// song — ke ca hero chi vien) tren san thi nguoi co noi tai +cong +mau toi da; du 3/3 them +toc hoi mana.
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public abstract class DaoVienBrother : MonoBehaviour
    {
        protected const float FramesPerSecond = 9f;   // cung nhip 110 ms/frame voi SpriteAnimationSet

        protected Hero _hero;
        protected DaoVienDefinition _definition;
        /// <summary>He so mau Ket Nghia dang ap len `Health.Max` (doi khi anh em vao / ra san).</summary>
        float _bondHealth;
        /// <summary>Sprite FX dang bay (song dao, rong...) — hero bi huy giua chung thi coroutine chet, phai tu don.</summary>
        readonly List<GameObject> _fx = new List<GameObject>();

        public virtual void Initialise(Hero hero, DaoVienDefinition definition)
        {
            _hero = hero;
            _definition = definition;
            _bondHealth = 1f;
        }

        protected virtual void OnDestroy()
        {
            foreach (var fx in _fx)
                if (fx != null) Destroy(fx);
        }

        protected virtual void Update()
        {
            if (_definition == null || _hero == null || _hero.IsDying) return;
            float bond = Bonded(2) ? 1f + _definition.bondHealth : 1f;
            if (Mathf.Approximately(bond, _bondHealth)) return;
            _bondHealth = bond;
            _hero.Health.SetMax(_hero.MaxHealth * bond);    // giu ti le mau
        }

        // ---------------------------------------------------------------- Ket Nghia

        /// <summary>So anh em khac nhau dang song cung phe voi <paramref name="hero"/>, tinh ca chinh no neu la anh em.</summary>
        static int BondCount(Hero hero)
        {
            int mask = 0;
            foreach (var other in Hero.Alive)
            {
                if (other == null || other.IsDying || other.Side != hero.Side) continue;
                var definition = other.Definition;
                if (definition == null || definition.daoVien == null || definition.swornBrother == SwornBrother.None) continue;
                mask |= 1 << (int)definition.swornBrother;
            }
            int count = 0;
            for (; mask != 0; mask &= mask - 1) count++;
            return count;
        }

        bool Bonded(int count) => _hero.PassiveUnlocked && BondCount(_hero) >= count;

        /// <summary>He so cong: Ket Nghia (+ Uy cua Quan Vu). Goi tu `Hero.AuraMultipliers`.</summary>
        public virtual void Multipliers(ref float damage)
        {
            if (Bonded(2)) damage *= 1f + _definition.bondDamage;
        }

        /// <summary>He so toc hoi mana cua <paramref name="hero"/>: Ket Nghia 3/3 (anh em) + Nhan Duc cua moi Luu Bi gan do.</summary>
        public static float ManaRate(Hero hero, DaoVienBrother own)
        {
            float rate = 1f;
            if (own != null && own.Bonded(3)) rate += own._definition.bondManaRate;
            foreach (var luuBi in LuuBi.Active) rate += luuBi.VirtueFor(hero, own != null);
            return rate;
        }

        /// <summary>Dong Sinh Cong Tu: mot Luu Bi con song keo <paramref name="dying"/> day thay vi chet. Goi tu `Hero.OnDied`.</summary>
        public static bool TryRevive(Hero dying)
        {
            foreach (var luuBi in LuuBi.Active)
                if (luuBi.TryRevive(dying)) return true;
            return false;
        }

        // ---------------------------------------------------------------- bo chieu tung nguoi

        /// <summary>Don thuong rieng (Quan Vu chem quat). false = `Hero` ra don nhu thuong.</summary>
        public virtual bool Strike(Unit target, float damage) => false;

        /// <summary>Chieu xin, goi tu `Hero.FinishCast` o `hitFrame` cua clip `skill`.</summary>
        public abstract void CastUltimate();

        /// <summary>Mien choang (Tuy Tuu cua Truong Phi).</summary>
        public virtual bool StunImmune => false;

        /// <summary>Hieu ung rieng cua bo chieu (tang Uy, Tuy Tuu) cho bang thong tin hero.</summary>
        public virtual void CollectEffects(List<UnitEffect> into) { }

        // ---------------------------------------------------------------- chung

        protected float Side => _hero.FacesRight ? 1f : -1f;

        /// <summary>Quai tren mat dat truoc mat, toi <paramref name="distance"/> theo huong nhin, lech sau toi da <paramref name="halfDepth"/>.</summary>
        protected bool EnemyAhead(float distance, float halfDepth)
        {
            foreach (var enemy in Unit.Hostiles(_hero.Side))
            {
                if (enemy == null || enemy.IsDying || enemy.FliesHigh) continue;
                var d = enemy.transform.position - transform.position;
                if (d.x * Side > -enemy.BodyRadius && d.x * Side <= distance + enemy.BodyRadius && Mathf.Abs(d.y) <= halfDepth)
                    return true;
            }
            return false;
        }

        /// <summary>Phat clip than khong ra don; tra ve thoi diem toi o `hitFrame` (-1 = dang ban, chua phat duoc).</summary>
        protected float Play(SpriteAnimationClip clip)
        {
            if (!_hero.PlayAction(clip)) return -1f;
            return Time.time + Mathf.Max(0, clip.hitFrame - 1) / FramesPerSecond;
        }

        /// <summary>Gay sat thuong; tra ve true neu don nay ha quai.</summary>
        protected bool Hit(Unit enemy, float damage)
        {
            if (enemy.Health.IsDead) return false;
            enemy.Health.TakeDamage(damage, _hero.Health);
            return enemy.Health.IsDead;
        }

        protected SpriteRenderer Sprite(string name, Sprite[] frames, Vector3 position, float scale = 1f)
        {
            if (frames == null || frames.Length == 0) return null;
            var go = new GameObject(name);
            _fx.RemoveAll(fx => fx == null);
            _fx.Add(go);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * (Battlefield.UnitScale * scale);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingOrder = SortOrder.Air;
            return renderer;
        }

        protected static void Animate(SpriteRenderer renderer, Sprite[] frames, float t) =>
            renderer.sprite = frames[(int)(t * FramesPerSecond) % frames.Length];
    }
}
