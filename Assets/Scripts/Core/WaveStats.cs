using System;
using System.Collections.Generic;
using BuildBuild.Combat;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Core
{
    /// <summary>
    /// Gom dong gop cua TUNG PHE trong mot wave / tran Chinh Phat va giu ban chup sau khi ket thuc. Wave thuong phe NPC la
    /// quai (khong phai `Hero`) nen bang phe NPC rong; Chinh Phat thi bang phe dich xem duoc (`ContributionPanel`).
    /// </summary>
    public class WaveStats : MonoBehaviour
    {
        [SerializeField] WaveManager waves;
        // Chi so mang = (int)Faction. Moi phe mot tu dien rieng: hero NPC dung chung HeroDefinition voi hero minh.
        readonly Dictionary<UnityEngine.Object, Contribution>[] _current =
            { new Dictionary<UnityEngine.Object, Contribution>(), new Dictionary<UnityEngine.Object, Contribution>() };
        readonly List<Contribution>[] _snapshot = { new List<Contribution>(), new List<Contribution>() };
        bool _recording;

        public int SnapshotWave { get; private set; }
        /// <summary>Ban chup la cua tran Chinh Phat chu khong phai wave.</summary>
        public bool SnapshotConquest { get; private set; }
        public IReadOnlyList<Contribution> SnapshotOf(Faction side) => _snapshot[(int)side];
        public static WaveStats Active { get; private set; }

        void Awake()
        {
            Active = this;
            Health.AnyDamaged += OnDamaged;
            Health.AnyHealed += OnHealed;
        }

        void OnDestroy()
        {
            Health.AnyDamaged -= OnDamaged;
            Health.AnyHealed -= OnHealed;
            if (Active == this) Active = null;
        }

        void OnEnable()
        {
            if (waves == null) return;
            waves.WaveStarted += Begin;
            waves.WaveCleared += FinishCleared;
            waves.Defeated += FinishDefeated;
        }

        void OnDisable()
        {
            if (waves == null) return;
            waves.WaveStarted -= Begin;
            waves.WaveCleared -= FinishCleared;
            waves.Defeated -= FinishDefeated;
        }

        void Begin(int wave)
        {
            foreach (var side in _current) side.Clear();
            _recording = true;
            SnapshotWave = wave;
            SnapshotConquest = waves.LastWasConquest;
        }

        void FinishCleared(int wave, int gold) => Finish(wave);
        void FinishDefeated(int wave) => Finish(wave);

        void Finish(int wave)
        {
            _recording = false;
            SnapshotWave = wave;
            for (int i = 0; i < _snapshot.Length; i++)
            {
                _snapshot[i].Clear();
                _snapshot[i].AddRange(_current[i].Values);
            }
        }

        void OnDamaged(Health victim, Health source, float hpLost, float shieldAbsorbed, Health shieldCaster)
        {
            if (!_recording) return;
            // Sat thuong: unit cua mot phe danh trung bat cu thu gi phe kia (quai, hero / linh / thanh doi phuong).
            var attacker = Owner(source);
            var victimSide = SideOf(victim);
            if (hpLost > 0f && attacker != null && victimSide != null && victimSide != attacker.Side) Get(attacker).damage += hpLost;

            // Do don: an don tu phe kia (hoac khong ro nguon), khong tinh tu gay thuong / dong minh.
            var defender = Owner(victim);
            if (defender == null || source == victim || SideOf(source) == defender.Side) return;
            Get(defender).tank += hpLost + shieldAbsorbed;
            var caster = Owner(shieldCaster);
            if (shieldAbsorbed > 0f && caster != null && caster.Side == defender.Side) Get(caster).heal += shieldAbsorbed;
        }

        void OnHealed(Health target, Health healer, float amount)
        {
            if (!_recording || amount <= 0f) return;
            var patient = Owner(target);
            var medic = Owner(healer);
            if (patient != null && medic != null && patient.Side == medic.Side) Get(medic).heal += amount;
        }

        static Hero Owner(Health health)
        {
            if (health == null) return null;
            var hero = health.GetComponent<Hero>();
            return hero != null ? hero.ContributionOwner : null;
        }

        static Faction? SideOf(Health health)
        {
            var unit = health != null ? health.GetComponent<Unit>() : null;
            return unit != null ? unit.Side : (Faction?)null;
        }

        Contribution Get(Hero hero)
        {
            var side = _current[(int)hero.Side];
            var definition = hero.Definition;
            if (!side.TryGetValue(definition, out var contribution))
            {
                contribution = new Contribution { key = definition, name = definition.displayName, icon = definition.avatar };
                side.Add(definition, contribution);
            }
            return contribution;
        }

        public static void RecordCannonDamage(CannonDefinition definition, Faction faction, float amount)
        {
            if (Active == null || !Active._recording || definition == null || amount <= 0f) return;
            var side = Active._current[(int)faction];
            if (!side.TryGetValue(definition, out var contribution))
            {
                contribution = new Contribution { key = definition, name = definition.displayName, icon = definition.icon };
                side.Add(definition, contribution);
            }
            contribution.damage += amount;
        }

        [Serializable]
        public class Contribution
        {
            public UnityEngine.Object key;
            public string name;
            public Sprite icon;
            public float damage;
            public float tank;
            public float heal;
        }
    }
}
