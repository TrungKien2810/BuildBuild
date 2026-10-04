using System.Collections.Generic;
using System.Linq;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo chieu Thuat Si Ket Hon (`SoulbinderKit`), gan vao `Hero` khi `HeroDefinition.soulbinder` co:
    ///   - Chuyen Ket (passiveStar): dich lien ket chet thi day chuyen sang dich gan nhat trong vung chon.
    ///   - Tam Hon Ket (minorStar): noi toi da 3 dich; sat thuong truc tiep phe ta lan `minorShare` mau that mat sang dich khac.
    ///   - Giat Hon (secondMinorStar): choang cac dich dang noi, moi hon gay 1,5x len goc.
    ///   - Dai Hon Khe (chieu xin, mana): noi toi da 5 dich, lan `pactShare`; Hon Chan 2x vao goc.
    /// Sat thuong lan ban ra `DamageKind.Linked` nen khong lan tiep.
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public class Soulbinder : MonoBehaviour
    {
        Hero _owner;
        SoulbinderKit _kit;
        readonly List<Unit> _members = new List<Unit>();
        readonly Dictionary<Unit, GameObject> _markers = new Dictionary<Unit, GameObject>();
        Vector3 _centre;
        Unit _root, _castTarget;
        float _until, _share, _beamAt, _linkReady, _ripReady, _linkAt = -1f, _ripAt = -1f;
        bool Great { get; set; }
        bool Valid(Unit u) => u != null && !u.IsDying && !u.Health.IsDead && u.IsHostileTo(_owner.Side);
        int Count => Time.time < _until ? _members.Count(Valid) : 0;

        public void Initialise(Hero owner, SoulbinderKit kit)
        {
            _owner = owner;
            _kit = kit;
            _linkReady = _ripReady = 0f;
            _linkAt = _ripAt = -1f;
            Clear();
        }

        void OnEnable() { Health.DirectDamage += OnDirect; Health.KilledBy += OnKilled; }
        void OnDisable() { Health.DirectDamage -= OnDirect; Health.KilledBy -= OnKilled; Clear(); }

        Unit[] Select(Unit root, int count) => Unit.Hostiles(_owner.Side).ToArray()
            .Where(u => Valid(u) && u.TargetableBy(_owner)
                && Battlefield.GroundDistance(u.AimPoint(_owner.transform.position), _owner.transform.position) <= _owner.CurrentAttackRange + u.BodyRadius
                && Battlefield.GroundDistance(u.transform.position, root.transform.position) <= _kit.linkRadius)
            .OrderBy(u => u == root ? -1f : Battlefield.GroundDistance(u.transform.position, root.transform.position))
            .Take(count).ToArray();

        void Connect(Unit root, bool great)
        {
            if (Great && !great && Time.time < _until) return;
            Clear();
            Great = great;
            _root = root;
            _centre = root.transform.position;
            _until = Time.time + (great ? _kit.pactDuration : _kit.minorDuration);
            _share = great ? _kit.pactShare : _kit.minorShare;
            foreach (var unit in Select(root, great ? 5 : 3)) Add(unit);
        }

        void Add(Unit unit)
        {
            _members.Add(unit);
            _markers[unit] = OneShotFx.SpawnLoop(unit.transform,
                new Vector3(0, unit.BodyHeight / Battlefield.UnitScale), Great ? _kit.pactMarker : _kit.marker,
                Mathf.Max(0.01f, _until - Time.time), SortOrder.Air);
        }

        void Remove(Unit unit)
        {
            if (_markers.TryGetValue(unit, out var marker) && marker != null) Destroy(marker);
            _markers.Remove(unit);
            _members.Remove(unit);
        }

        void Clear()
        {
            foreach (var unit in _members.ToArray())
            {
                if (unit != null) OneShotFx.Spawn(unit.BodyCenter, _kit?.fade, false, SortOrder.Air, Battlefield.UnitScale);
                Remove(unit);
            }
            Great = false;
        }

        void Update()
        {
            if (_kit == null || _owner == null || _owner.IsDying || _owner.Health.IsDead || !WaveManager.Fighting)
            { Clear(); return; }
            UpdateCasts();
            if (Time.time >= _until) { Clear(); return; }
            foreach (var unit in _members.ToArray()) if (!Valid(unit)) Remove(unit);
            if (!Valid(_root)) _root = _members.OrderByDescending(u => u.Health.Current).FirstOrDefault();
            if (_root == null || Time.time < _beamAt) return;
            _beamAt = Time.time + 0.18f;
            foreach (var unit in _members)
                if (unit != _root) OneShotFx.SpawnBeam(_root.BodyCenter, unit.BodyCenter, _kit.segment,
                    SortOrder.Air, Battlefield.UnitScale * 0.5f);
        }

        void UpdateCasts()
        {
            if (_linkAt > 0f && Time.time >= _linkAt)
            {
                _linkAt = -1f;
                if (Valid(_castTarget)) Connect(_castTarget, false);
            }
            if (_ripAt > 0f && Time.time >= _ripAt)
            {
                _ripAt = -1f;
                Rip(_owner.AttackDamage);
            }
            if (_owner.Casting || _owner.IsStunned || _owner.IsDisarmed) return;
            var target = _owner.AttackTargetInRange();
            if (target == null) return;
            if (_owner.SecondMinorUnlocked && Time.time >= _ripReady && Count >= 2 && _owner.PlayAction(_kit.rip))
            {
                _ripReady = Time.time + _kit.ripCooldown;
                _ripAt = Time.time + _owner.HitDelay(_kit.rip);
                return;
            }
            if (_owner.MinorUnlocked && Time.time >= _linkReady && !(Great && Time.time < _until)
                && Select(target, 3).Length >= 2 && _owner.PlayAction(_kit.link))
            {
                _linkReady = Time.time + _kit.linkCooldown;
                _castTarget = target;
                _linkAt = Time.time + _owner.HitDelay(_kit.link);
            }
        }

        void OnDirect(Health victim, Health source, float amount, Faction? side)
        {
            if (_owner == null || _owner.IsDying || _owner.Health.IsDead || Time.time >= _until || amount <= 0f) return;
            var struck = victim.GetComponent<Unit>();
            if (struck == null || !_members.Contains(struck) || !struck.IsHostileTo(_owner.Side)) return;
            var attacker = source != null ? source.GetComponent<Unit>() : null;
            var attackSide = attacker != null ? (Faction?)attacker.Side : side;
            if (attackSide != _owner.Side) return;
            foreach (var other in _members.ToArray())
                if (other != struck && Valid(other)) other.Health.TakeDamage(amount * _share, source, kind: DamageKind.Linked);
        }

        void OnKilled(Health dead, Health source, DamageKind kind)
        {
            if (_owner == null || _owner.IsDying || !_owner.PassiveUnlocked || Time.time >= _until) return;
            var unit = dead.GetComponent<Unit>();
            if (unit == null || !_members.Contains(unit) || !unit.IsHostileTo(_owner.Side)) return;
            Remove(unit);
            var next = Unit.Hostiles(_owner.Side).ToArray().Where(u => Valid(u) && !_members.Contains(u) && u.TargetableBy(_owner)
                && Battlefield.GroundDistance(u.transform.position, _centre) <= _kit.linkRadius
                && Battlefield.GroundDistance(u.transform.position, _owner.transform.position) <= _owner.CurrentAttackRange + u.BodyRadius)
                .OrderBy(u => Battlefield.GroundDistance(u.transform.position, unit.transform.position)).FirstOrDefault();
            if (next == null) return;
            OneShotFx.Spawn(next.BodyCenter, _kit.transfer, false, SortOrder.Air, Battlefield.UnitScale);
            Add(next);
        }

        void Rip(float damage)
        {
            var members = _members.Where(Valid).ToArray();
            if (members.Length < 2) return;
            if (!Valid(_root)) _root = members.OrderByDescending(u => u.Health.Current).First();
            foreach (var unit in members)
            {
                unit.ApplyStun(unit.IsBoss || unit.IsElite ? 0.5f : 1f);
                TravellingHeroFx.Spawn(unit.BodyCenter, _root.BodyCenter, _kit.fragment, 0.25f);
            }
            OneShotFx.Spawn(_root.BodyCenter, _kit.ripHit, false, SortOrder.Air, Battlefield.UnitScale);
            _root.Health.TakeDamage(damage * 1.5f * members.Length, _owner.Health);
        }

        /// <summary>Dai Hon Khe, goi o frame cham cua clip cast.</summary>
        public void CastUltimate()
        {
            var target = _owner.AttackTargetInRange();
            if (target == null) return;
            Connect(target, true);
            OneShotFx.Spawn(target.BodyCenter, _kit.pactHit, false, SortOrder.Air, Battlefield.UnitScale);
            target.Health.TakeDamage(_owner.AttackDamage * 2f, _owner.Health);
        }
    }
}
