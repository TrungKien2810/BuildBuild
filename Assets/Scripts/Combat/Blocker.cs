using System.Collections.Generic;
using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Luat CHAN QUAI (GAME_DESIGN.md muc 2, chot 2026-09-13): moi unit phe ta tren san — hero, linh xuong, ky si,
    /// linh trieu hoi — GIU toi da `MaxHeld` quai (mac dinh 3, Chainwarden 4, doc tu `HeroDefinition.maxHeld`).
    ///   - Moi quai bi giu dung o mot CHO rieng canh unit, lech nhau theo truc y (xem `SlotPosition`);
    ///   - quai thu MaxHeld+1 tro di khong bi giu, di tiep ve lau dai (`Enemy.EngageHero` bo qua unit het cho);
    ///   - quai bi giu chet -> con GAN NHAT chua bi ai giu va dang trong tam nhin cua no vao the cho. Bao ngay
    ///     luc het mau (`Unit.Killed`), khong cho het animation death;
    ///   - unit chan chet -> tha het quai no dang giu.
    ///
    /// `Hero` tu gan component nay trong `Initialise`. Quai TU nhan cho (`Enemy.HoldBy`); lop nay chi giu so cho
    /// va chu dong goi quai vao the cho khi co con chet.
    ///
    /// Cho tu nha khi quai bi xoa / da chet / da bo sang unit khac — khong can quai bao lai.
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public class Blocker : MonoBehaviour
    {
        /// <summary>Moi unit chan dang tren san (ke ca con dang chay animation death — loc bang `IsDown`).</summary>
        public static readonly List<Blocker> Active = new List<Blocker>();

        public const int DefaultMaxHeld = 3;

        /// <summary>
        /// Goc tren MAT DAT giua hai cho canh nhau. 70 do: tren vong vay (~0,6 unit) hai cho cach nhau hon
        /// `Battlefield.PersonalSpace`, khong thi luc tach doi hinh day quai ra khoi cho lien tuc.
        /// 3 cho -> 0, ±70 (chinh dien, cheo tren, cheo duoi); 4 cho -> ±35, ±105 (hai cho ngoai hoi lui ra sau).
        /// </summary>
        const float SlotStep = 70f;

        Hero _hero;
        Health _health;
        Enemy[] _held = new Enemy[DefaultMaxHeld];

        public Hero Unit => _hero;
        public int MaxHeld => _held.Length;
        public bool IsDown => _hero == null || _hero.IsDying;

        /// <summary>Quai dang bi unit nay giu mang Xich: nhan them bay nhieu phan sat thuong (Chainwarden 0,2). 0 = khong.</summary>
        public float HeldDamageBonus { get; private set; }

        void Awake()
        {
            _hero = GetComponent<Hero>();
            _health = GetComponent<Health>();
            _health.Died += ReleaseAll;
        }

        void OnEnable()
        {
            Active.Add(this);
            BuildBuild.Combat.Unit.Killed += OnEnemyKilled;
        }

        void OnDisable()
        {
            Active.Remove(this);
            BuildBuild.Combat.Unit.Killed -= OnEnemyKilled;
            ReleaseAll();
        }

        void OnDestroy()
        {
            if (_health != null) _health.Died -= ReleaseAll;
        }

        /// <summary>Goi tu `Hero.Initialise` (moi lan spawn lai). Doi so cho thi tha het quai dang giu.</summary>
        public void Initialise(int maxHeld, float heldDamageBonus = 0f)
        {
            HeldDamageBonus = Mathf.Max(0f, heldDamageBonus);
            maxHeld = Mathf.Max(1, maxHeld);
            if (_held.Length != maxHeld) _held = new Enemy[maxHeld];
            else ReleaseAll();
        }

        /// <summary>So quai DANG bi unit nay giu (cho cho Gizmos va nguoi test doc).</summary>
        public int HeldCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _held.Length; i++)
                    if (!SlotFree(i)) count++;
                return count;
            }
        }

        public bool Holds(Unit enemy) => enemy != null && System.Array.IndexOf(_held, enemy) >= 0;

        /// <summary>Con cho cho <paramref name="enemy"/> (hoac no dang giu san mot cho).</summary>
        public bool HasFreeSlot(Unit enemy)
        {
            if (IsDown) return false;
            for (int i = 0; i < _held.Length; i++)
                if (_held[i] == enemy || SlotFree(i)) return true;
            return false;
        }

        /// <summary>
        /// Giu cho TRONG GAN <paramref name="enemy"/> nhat (phia <paramref name="side"/>) cho no; tra ve chi so cho,
        /// -1 neu het cho. Gan nhat chu khong lay cho dau tien: quai den tu cheo duoi khong phai vong len chinh dien.
        /// </summary>
        public int ClaimSlot(Enemy enemy, float side, float reach)
        {
            if (IsDown) return -1;
            for (int i = 0; i < _held.Length; i++)
                if (_held[i] == enemy) return i;

            int best = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < _held.Length; i++)
            {
                if (!SlotFree(i)) continue;
                float distance = Battlefield.GroundDistance(SlotPosition(i, side, reach), enemy.transform.position);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = i;
            }
            if (best >= 0) _held[best] = enemy;
            return best;
        }

        /// <summary>Quai tu bo cho (bo sang unit khac, bi day qua xa...).</summary>
        public void Release(Enemy enemy)
        {
            for (int i = 0; i < _held.Length; i++)
                if (_held[i] == enemy) _held[i] = null;
        }

        /// <summary>
        /// Vi tri cho <paramref name="slot"/> tren vong vay quanh unit, phia <paramref name="side"/> (+1 phai, -1 trai).
        /// Ban kinh vong = nua than unit + 0,8 tam danh cua quai: quai dung vao cho la cham duoc unit.
        /// </summary>
        public Vector3 SlotPosition(int slot, float side, float reach)
        {
            float angle = (slot - (_held.Length - 1) * 0.5f) * SlotStep * Mathf.Deg2Rad;
            float ring = _hero.BodyRadius + reach * 0.8f;
            return transform.position + new Vector3(side * Mathf.Cos(angle) * ring,
                Mathf.Sin(angle) * ring * Battlefield.DepthSquash, 0f);
        }

        // Cho tu nha khi quai chet/bi xoa hoac da bo sang unit khac — khong can quai bao lai.
        bool SlotFree(int i) => _held[i] == null || _held[i].IsDying || _held[i].Holder != this;

        void ReleaseAll() => System.Array.Clear(_held, 0, _held.Length);

        /// <summary>
        /// Quai dang bi giu vua het mau: nha cho va goi con GAN NHAT chua bi ai giu, dang nhin thay unit nay
        /// (trong `heroAggroRange` cua chinh no), vao the. Quai ban xa khong vao cho (`Enemy.CanBeHeld`).
        /// </summary>
        void OnEnemyKilled(BuildBuild.Combat.Unit dead)
        {
            int slot = System.Array.IndexOf(_held, dead);
            if (slot < 0) return;
            _held[slot] = null;
            if (IsDown) return;

            Enemy best = null;
            float bestSqr = float.PositiveInfinity;
            foreach (var enemy in Enemy.Alive)
            {
                if (enemy == null || enemy.IsDying || !enemy.CanBeHeld || enemy.Holder != null || !enemy.IsHostileTo(Unit.Side)) continue;
                float aggro = enemy.Definition.heroAggroRange;
                float sqr = (enemy.AimPoint(transform.position) - transform.position).sqrMagnitude;
                if (sqr > aggro * aggro || sqr >= bestSqr) continue;
                bestSqr = sqr;
                best = enemy;
            }
            if (best != null) best.HoldBy(this);
        }

        // Scene view: vong tron = cho trong / cho da co quai, duong ke toi quai dang giu.
        void OnDrawGizmosSelected()
        {
            if (_hero == null || _held == null) return;
            for (int i = 0; i < _held.Length; i++)
            {
                bool taken = !SlotFree(i);
                Gizmos.color = taken ? Color.red : Color.green;
                Gizmos.DrawWireSphere(SlotPosition(i, 1f, 0.4f), 0.08f);
                if (taken) Gizmos.DrawLine(transform.position, _held[i].transform.position);
            }
        }
    }
}
