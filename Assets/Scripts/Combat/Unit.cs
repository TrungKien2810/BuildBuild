using System;
using System.Collections.Generic;
using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>Phe cua mot unit. Quai luon la `Npc`; hero/linh mac dinh `Player`, hero/linh cua lanh chua Chinh Phat la `Npc`.</summary>
    public enum Faction { Player, Npc }

    /// <summary>
    /// Lop nen chung cua moi thu tren san an don va bi khong che: `Enemy`, `Hero` (ca linh), `Castle`. Kit cua hero
    /// nham "doi thu cua phe minh" qua `Hostiles(Side)` thay vi `Enemy.Alive` — nho vay hero NPC trong phu ban
    /// Chinh Phat (GDD muc 8) la HERO THAT, chieu nao cung chay duoc ca hai chieu (nguoi dung chot 2026-10-01).
    ///
    /// Hieu ung trang thai giong nhau giua quai va hero (an ky, troi, lam cham, Long Uy, pha giap, Xich co han, keo,
    /// hat tung) nam o day; choang / dong bang / day lui moi lop tu ve theo kieu cua minh.
    /// </summary>
    public abstract class Unit : MonoBehaviour
    {
        /// <summary>Moi unit dang bat trong scene (ca lau dai). Dung `Hostiles` de lay doi thu.</summary>
        public static readonly List<Unit> All = new List<Unit>();

        /// <summary>Ban ra NGAY LUC unit (quai hoac hero) het mau, truoc animation death.</summary>
        public static event Action<Unit> Killed;

        /// <summary>Tren dau unit (toa do LOCAL, da nhan scale than) — cho dat FX an ky.</summary>
        protected static readonly Vector3 MarkOffset = new Vector3(0f, 0.65f, 0f);

        public Faction Side { get; set; }

        public abstract Health Health { get; }
        public abstract bool IsDying { get; }

        /// <summary>Nua be ngang than tren mat dat (world).</summary>
        public abstract float BodyRadius { get; }

        /// <summary>Chieu cao than (world), tinh tu chan.</summary>
        public abstract float BodyHeight { get; }

        /// <summary>Giua than (world) — cho dan nham vao.</summary>
        public virtual Vector3 BodyCenter => transform.position + new Vector3(0f, BodyHeight * 0.5f, 0f);

        /// <summary>
        /// Diem tren mat dat ma ke danh dung o <paramref name="from"/> do tam toi (truoc mep than). Unit thuong la chan;
        /// lau dai la mat tuong ngang hang voi ke danh.
        /// </summary>
        public virtual Vector3 AimPoint(Vector3 from) => transform.position;

        public virtual bool IsBoss => false;
        public virtual bool IsElite => false;
        public virtual bool FliesHigh => false;

        /// <summary>Quai can chien bi unit phe ta giu cho (`Blocker`). Hero khong bi giu.</summary>
        public virtual bool CanBeHeld => false;

        /// <summary>Unit phe ta dang GIU unit nay; null = khong ai giu.</summary>
        public virtual Blocker Holder => null;

        /// <summary>Hero / linh unit nay dang danh; null = khong danh ai (hoac dang danh lau dai).</summary>
        public virtual Hero EngagedHero => null;

        /// <summary><paramref name="hero"/> co nham / danh trung duoc unit nay khong (quai bay cao: chi ban xa).</summary>
        public virtual bool TargetableBy(Hero hero) => true;

        public bool IsHostileTo(Faction side) => Side != side;

        public abstract bool IsStunned { get; }
        public abstract void ApplyStun(float seconds, Sprite[] frames = null);
        public abstract void ApplyFreeze(float seconds);
        public abstract void PushBack(float direction);

        /// <summary>Khong bi troi / keo / hat tung (quai bay cao, Long Giap, lau dai).</summary>
        protected virtual bool Unmovable => false;

        /// <summary>Bi keo / hat tung: bo cho vay, bo day lui dang chay.</summary>
        protected virtual void OnDisplaced() { }

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        protected static void RaiseKilled(Unit unit) => Killed?.Invoke(unit);

        // ---------------------------------------------------------------- doi thu

        /// <summary>Moi unit KHAC phe <paramref name="side"/> — duyet bang foreach khong cap phat; `ToArray` khi vong lap co the giet unit.</summary>
        public static HostileSet Hostiles(Faction side) => new HostileSet(side);

        public readonly struct HostileSet
        {
            readonly Faction _side;
            public HostileSet(Faction side) => _side = side;
            public Enumerator GetEnumerator() => new Enumerator(_side);

            public Unit[] ToArray()
            {
                var list = new List<Unit>();
                foreach (var unit in this) list.Add(unit);
                return list.ToArray();
            }

            public struct Enumerator
            {
                readonly Faction _side;
                int _index;
                public Enumerator(Faction side)
                {
                    _side = side;
                    _index = -1;
                }
                public Unit Current => All[_index];
                public bool MoveNext()
                {
                    while (++_index < All.Count)
                    {
                        var unit = All[_index];
                        if (unit != null && unit.Side != _side) return true;
                    }
                    return false;
                }
            }
        }

        // ---------------------------------------------------------------- an ky

        UnityEngine.Object _markedBy;
        float _markedUntil;
        GameObject _markFx;
        SpriteRenderer _markRenderer;

        /// <summary>An ky cua <paramref name="marker"/> trong <paramref name="seconds"/> giay; an lai thi dem lai tu dau.</summary>
        public void ApplyMark(UnityEngine.Object marker, float seconds, Sprite[] frames)
        {
            if (IsDying || seconds <= 0f) return;
            _markedBy = marker;
            _markedUntil = Time.time + seconds;
            if (_markFx != null) Destroy(_markFx);
            _markFx = OneShotFx.SpawnLoop(transform, MarkOffset, frames, seconds, SortOrder.ForUnit(this) + 1);
            _markRenderer = _markFx != null ? _markFx.GetComponent<SpriteRenderer>() : null;
        }

        public bool IsMarkedBy(UnityEngine.Object marker) =>
            marker != null && _markedBy == marker && Time.time < _markedUntil;

        // ---------------------------------------------------------------- troi

        float _rootedUntil;
        GameObject _rootFx;
        SpriteRenderer _rootRenderer;

        /// <summary>Bi troi (Quat Xich): dung im, van danh.</summary>
        public bool IsRooted => Time.time < _rootedUntil;

        /// <summary>Troi <paramref name="seconds"/> giay (Quat Xich): khong di, khong bi day, van danh. Chong nhau lay han dai hon.</summary>
        public void ApplyRoot(float seconds, Sprite[] frames = null)
        {
            if (IsDying || seconds <= 0f || Unmovable) return;
            if (Time.time + seconds <= _rootedUntil) return;
            _rootedUntil = Time.time + seconds;
            // Vong xich quanh chan, lap toi het troi; troi moi dai hon thi dung lai vong cho khop han.
            if (_rootFx != null) Destroy(_rootFx);
            _rootFx = OneShotFx.SpawnLoop(transform, Vector3.zero, frames, seconds, SortOrder.ForUnit(this) + 1);
            _rootRenderer = _rootFx != null ? _rootFx.GetComponent<SpriteRenderer>() : null;
        }

        // ---------------------------------------------------------------- bi cuon (khoa don)

        float _disarmedUntil;

        /// <summary>Bi cuon trong Xoay Nuoc (Ngu Nhan): dung yen khong ra don, van bi danh.</summary>
        public bool IsDisarmed => Time.time < _disarmedUntil;

        /// <summary>Khoa don <paramref name="seconds"/> giay. Vung goi MOI frame voi han ngan — ra khoi vung la danh lai.</summary>
        public void ApplyDisarm(float seconds)
        {
            if (IsDying || seconds <= 0f) return;
            _disarmedUntil = Mathf.Max(_disarmedUntil, Time.time + seconds);
        }

        float _tangleUntil;
        float _tangleMiss;

        /// <summary>Day Roi (Tien Det To): `missChance` don cua unit nay truot trong `seconds` giay — trung lai thi lam moi.</summary>
        public void ApplyTangle(float missChance, float seconds)
        {
            if (IsDying || seconds <= 0f || missChance <= 0f) return;
            _tangleMiss = missChance;
            _tangleUntil = Time.time + seconds;
        }

        /// <summary>Tung xuc xac truot cho MOT don (goi luc don cham).</summary>
        public bool RollMiss() => Time.time < _tangleUntil && UnityEngine.Random.value < _tangleMiss;

        // ---------------------------------------------------------------- he so toc (nhanh Tru, plan muc B1.4)

        float _slowUntil;
        float _slowPercent;
        float _hasteUntil;
        float _hastePercent;

        /// <summary>Dang bi lam cham (va khong mien).</summary>
        public bool IsSlowed => Time.time < _slowUntil && !SlowImmune;

        /// <summary>Mien moi lam cham (boss Noi Gian). Bat len thi slow dang chay cung mat tac dung.</summary>
        public bool SlowImmune { get; set; }

        /// <summary>He so nhan toc chay: (1 - cham) x (1 + tang toc).</summary>
        protected float SpeedFactor
        {
            get
            {
                float factor = 1f;
                if (IsSlowed) factor *= 1f - _slowPercent;
                if (Time.time < _hasteUntil) factor *= 1f + _hastePercent;
                return factor;
            }
        }

        /// <summary>
        /// Lam cham <paramref name="percent"/> (0..1) trong <paramref name="seconds"/> giay (phao Bang). Hai nguon chong
        /// nhau thi lay muc MANH hon va thoi han DAI hon — khong cong don.
        /// </summary>
        public void ApplySlow(float percent, float seconds)
        {
            if (IsDying || SlowImmune || percent <= 0f || seconds <= 0f) return;
            bool active = Time.time < _slowUntil;
            _slowPercent = active ? Mathf.Max(_slowPercent, Mathf.Clamp01(percent)) : Mathf.Clamp01(percent);
            _slowUntil = Mathf.Max(_slowUntil, Time.time + seconds);
        }

        /// <summary>Tang toc chay <paramref name="percent"/> trong <paramref name="seconds"/> giay (boss Tieng Thet). Khong cong don.</summary>
        public void ApplyHaste(float percent, float seconds)
        {
            if (IsDying || percent <= 0f || seconds <= 0f) return;
            _hastePercent = Mathf.Max(Time.time < _hasteUntil ? _hastePercent : 0f, percent);
            _hasteUntil = Mathf.Max(_hasteUntil, Time.time + seconds);
        }

        // ---------------------------------------------------------------- nhan them sat thuong

        float _aweWeaken, _aweVulnerability, _aweUntil;
        float _armorBreak, _armorBrokenUntil;
        float _chainedUntil;
        float _chainBonus;

        /// <summary>Phan sat thuong bi Long Uy giam luc nay (0 = khong).</summary>
        protected float AweWeaken => Time.time < _aweUntil ? _aweWeaken : 0f;

        /// <summary>Xich co han tu Nem Moc; 0 = khong.</summary>
        protected float TimedChainBonus => Time.time < _chainedUntil ? _chainBonus : 0f;

        /// <summary>Tong phan nhan them sat thuong tu pha giap + Long Uy (Xich do lop con cong them).</summary>
        protected float StatusVulnerability =>
            (Time.time < _armorBrokenUntil ? _armorBreak : 0f) + (Time.time < _aweUntil ? _aweVulnerability : 0f);

        /// <summary>
        /// Long Uy (Wyrmspear): dang trong tam thi giam cong <paramref name="weaken"/> va nhan them <paramref name="vulnerability"/>
        /// sat thuong. Nguon goi MOI frame, het han ngay sau (0,2s) — ra khoi tam la mat. Hai nguon lay muc cao hon.
        /// </summary>
        public void ApplyAwe(float weaken, float vulnerability)
        {
            if (IsDying) return;
            bool active = Time.time < _aweUntil;
            _aweWeaken = active ? Mathf.Max(_aweWeaken, weaken) : weaken;
            _aweVulnerability = active ? Mathf.Max(_aweVulnerability, vulnerability) : vulnerability;
            _aweUntil = Time.time + 0.2f;
        }

        /// <summary>Pha giap (Dia Long Chan): nhan them <paramref name="bonus"/> sat thuong tu moi nguon. Khong cong don.</summary>
        public void ApplyArmorBreak(float bonus, float seconds)
        {
            if (IsDying || bonus <= 0f || seconds <= 0f) return;
            _armorBreak = Time.time < _armorBrokenUntil ? Mathf.Max(_armorBreak, bonus) : bonus;
            _armorBrokenUntil = Mathf.Max(_armorBrokenUntil, Time.time + seconds);
        }

        /// <summary>Xich co thoi han (Nem Moc): nhan them <paramref name="bonus"/> sat thuong ke ca khi khong bi giu.</summary>
        public void ApplyChain(float bonus, float seconds)
        {
            if (IsDying || bonus <= 0f || seconds <= 0f) return;
            bool active = Time.time < _chainedUntil;
            _chainBonus = active ? Mathf.Max(_chainBonus, bonus) : bonus;
            _chainedUntil = Mathf.Max(_chainedUntil, Time.time + seconds);
        }

        /// <summary>Cac hieu ung trang thai dang chay tren unit (bang thong tin hero). Lop con them hieu ung rieng.</summary>
        public virtual void CollectEffects(List<UnitEffect> into)
        {
            float now = Time.time;
            if (_markedBy != null && now < _markedUntil) into.Add(new UnitEffect(EffectKind.Mark, "Ấn ký", _markedUntil));
            if (IsRooted) into.Add(new UnitEffect(EffectKind.Root, "Trói", _rootedUntil));
            if (IsSlowed) into.Add(new UnitEffect(EffectKind.Slow, $"Chậm {Percent(_slowPercent)}", _slowUntil));
            if (now < _hasteUntil) into.Add(new UnitEffect(EffectKind.Haste, $"Tăng tốc {Percent(_hastePercent)}", _hasteUntil));
            if (now < _armorBrokenUntil) into.Add(new UnitEffect(EffectKind.ArmorBreak, $"Phá giáp +{Percent(_armorBreak)}", _armorBrokenUntil));
            if (now < _aweUntil) into.Add(new UnitEffect(EffectKind.Awe, $"Long Uy -{Percent(_aweWeaken)} công"));
            if (now < _chainedUntil) into.Add(new UnitEffect(EffectKind.Chain, $"Xích +{Percent(_chainBonus)}", _chainedUntil));
        }

        protected static string Percent(float fraction) => Mathf.RoundToInt(fraction * 100f) + "%";

        // ---------------------------------------------------------------- keo, hat tung

        float _pullUntil;
        Vector3 _pullTo;
        float _launchStart, _launchUntil, _launchHeight;
        Vector3 _launchGround;

        /// <summary>
        /// Bi KEO ve <paramref name="destination"/> trong <paramref name="seconds"/> giay (Nem Moc). Bo cho vay dang giu:
        /// toi noi, quai tu nhan cho moi o unit gan nhat nhu binh thuong.
        /// </summary>
        public void PullTo(Vector3 destination, float seconds)
        {
            if (IsDying || Unmovable) return;
            OnDisplaced();
            _pullTo = new Vector3(destination.x, destination.y, transform.position.z);
            _pullUntil = Time.time + Mathf.Max(0.01f, seconds);
        }

        /// <summary>Dang bi hat tung (cot da Bach Dia Long): khong di, khong danh, ve o lop Air.</summary>
        public bool IsLaunched => _launchUntil > 0f;

        /// <summary>
        /// Hat tung cao <paramref name="height"/> (world) trong <paramref name="seconds"/> giay roi roi ve dung cho cu:
        /// bo cho vay, khong di khong danh. Boss khong bi nhac len. Dang bay thi khong hat lai.
        /// </summary>
        public void KnockUp(float height, float seconds)
        {
            if (IsDying || IsBoss || IsLaunched || Unmovable || seconds <= 0f) return;
            OnDisplaced();
            _pullUntil = 0f;
            _launchGround = transform.position;
            _launchHeight = height;
            _launchStart = Time.time;
            _launchUntil = Time.time + seconds;
        }

        /// <summary>Dang bay thi dat do cao theo parabol va tra ve true; cham dat thi ve cho cu.</summary>
        protected bool UpdateLaunch()
        {
            if (!IsLaunched) return false;
            float u = (Time.time - _launchStart) / (_launchUntil - _launchStart);
            if (u >= 1f)
            {
                Land();
                return false;
            }
            transform.position = _launchGround + new Vector3(0f, 4f * _launchHeight * u * (1f - u), 0f);
            return true;
        }

        protected void Land()
        {
            transform.position = _launchGround;
            _launchUntil = 0f;
        }

        /// <summary>Dang bi keo thi truot mot nhip ve dich va tra ve true — frame nay khong di, khong danh.</summary>
        protected bool UpdatePull()
        {
            if (_pullUntil <= 0f) return false;
            float left = _pullUntil - Time.time;
            if (left <= Time.deltaTime)
            {
                transform.position = _pullTo;
                _pullUntil = 0f;
                return false;
            }
            transform.position += (_pullTo - transform.position) * (Time.deltaTime / left);
            return true;
        }

        // ---------------------------------------------------------------- vong doi

        /// <summary>Xoa moi trang thai chung — goi o dau `Initialise` (unit dung lai tu prefab / pool).</summary>
        protected void ResetStatus()
        {
            _markedBy = null;
            _markedUntil = 0f;
            _rootedUntil = 0f;
            _chainedUntil = 0f;
            _aweUntil = 0f;
            _armorBrokenUntil = 0f;
            _pullUntil = 0f;
            _launchUntil = 0f;
            _disarmedUntil = 0f;
        }

        /// <summary>An Duc (Luu Bi): go troi / cham / pha giap / Xich co han / Long Uy.</summary>
        protected void ClearDebuffs()
        {
            _rootedUntil = 0f;
            _slowUntil = 0f;
            _chainedUntil = 0f;
            _aweUntil = 0f;
            _armorBrokenUntil = 0f;
            if (_rootFx != null) Destroy(_rootFx);
        }

        /// <summary>Go FX an ky + troi (unit chet).</summary>
        protected void ClearStatusFx()
        {
            if (_markFx != null) Destroy(_markFx);
            if (_rootFx != null) Destroy(_rootFx);
        }

        /// <summary>FX bam than ve ngay tren than — goi sau khi dat `sortingOrder` cua than.</summary>
        protected void SortStatusFx(int bodyOrder)
        {
            if (_markRenderer != null) _markRenderer.sortingOrder = bodyOrder + 1;
            if (_rootRenderer != null) _rootRenderer.sortingOrder = bodyOrder + 1;
        }
    }
}
