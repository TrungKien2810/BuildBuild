using System.Collections;
using System.Linq;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo Khoi Loi Su (`KhoiLoiSuKit`), gan vao `Hero` khi `HeroDefinition.khoiLoiSu` co. Chu khong tu danh:
    /// don thuong cua chu = lenh roi (`puppet`) dam muc tieu cua chinh roi.
    ///   - Nhat Khoi Dong Hanh (passiveStar): trien khai dung 1 roi MocKhoi truoc mat (mau rieng, chan toi da 3 theo
    ///     luat phe ta); roi chi quanh chu trong `controlRadius` (leashRange nho); roi vo thi sua `repairSeconds`
    ///     roi tai trien khai; chu chet thi roi bien mat. Body chu luon giu roi gap sau lung (art co dinh).
    ///   - Quet Moc (minorStar): roi quet vong truoc mat, vung `sweepRadius` ×`sweepPower`.
    ///   - Ho Chu (secondMinorStar): chu bi ap sat → roi thu ve canh chu, gat 1 dich lui `guardShove` (boss mien hat)
    ///     roi tro lai diem cu; tam mat diem chan cu trong luc roi di.
    ///   - Khoi Vuong Giang Tran (chieu xin, mana): doi art roi sang bo DaiKhoi (`grandSet`) + khien, nen song chan
    ///     dong vung `grandRadius` ×`grandPower` + choang `grandStun`; trong `grandDuration` moi 2s nen them 1 nhip;
    ///     het gio ve roi thuong (giu mau hien tai). Can roi dang song.
    /// Don gian hoa co ghi nhan: Ho Chu dich chuyen roi (khong di bo that); dang lon khong doi don thuong roi thanh
    /// quet that ma dung nhip nen lap + khien; chi so roi = cap/sao/trang bi cua chu qua `InheritProgression`.
    /// So CHUA can.
    ///
    /// Roi la `Hero` day du nen dong tac danh / quet lay tu BO CUA CHINH ROI (`puppet.animations`, `grandSet` — roi
    /// quet `MocKhoi/quet-moc`, dai khoi nen `DaiKhoi/slam`); ba clip trang thai khong nam trong bo nao (trien khai /
    /// ho chu / bung khop) khai o `KhoiLoiSuKit`. Dai khoi di bo bang art walk rieng (`Assets/Art/Generated/DaiKhoi/walk`,
    /// gap tu ban guide 2026-10-11 — builder tu nap); neu thieu folder do thi `SceneBuilder` lui ve idle.
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public class KhoiLoiSu : MonoBehaviour
    {
        Hero _hero;
        KhoiLoiSuKit _kit;
        Hero _puppet;
        float _repairAt, _sweepReady, _guardReady, _grandUntil, _grandTick;
        Vector3 _puppetPost;
        bool _hasPost;

        public void Initialise(Hero hero, KhoiLoiSuKit kit)
        {
            _hero = hero;
            _kit = kit;
            _puppet = null;
            _repairAt = _sweepReady = _guardReady = _grandUntil = _grandTick = 0f;
            _hasPost = false;
        }

        void OnDisable()
        {
            if (_puppet != null && !_puppet.IsDying) Destroy(_puppet.gameObject);
            _puppet = null;
        }

        void Update()
        {
            if (_kit == null || _hero == null || _hero.IsDying || !WaveManager.Fighting) return;
            if (_puppet == null || _puppet.IsDying)
            {
                // Roi vua vo (con tham chieu, ke ca object da Destroy): hen gio sua.
                if (!ReferenceEquals(_puppet, null)) _repairAt = Time.time + _kit.repairSeconds;
                _puppet = null;
                if (_hero.PassiveUnlocked && Time.time >= _repairAt && _hero.PlayAction(_kit.deployCast))
                    Deploy();
                return;
            }
            if (_hero.Casting || _hero.IsStunned || _hero.IsDisarmed) return;
            if (_hero.MinorUnlocked && Time.time >= _sweepReady && _hero.PlayAction(_kit.sweepCast))
            {
                _sweepReady = Time.time + _kit.sweepCooldown;
                StartCoroutine(Sweep());
                return;
            }
            if (_hero.SecondMinorUnlocked && Time.time >= _guardReady && MasterPressured(out var presser)
                && _hero.PlayAction(_kit.guardCast))
            {
                _guardReady = Time.time + _kit.guardCooldown;
                StartCoroutine(Guard(presser));
            }
        }

        void Deploy()
        {
            if (_kit.unitPrefab == null || _kit.puppet == null) return;
            var at = _hero.transform.position + new Vector3(_hero.FacesRight ? 1f : -1f, 0f, 0f);
            var unit = Instantiate(_kit.unitPrefab, at, Quaternion.identity);
            unit.Initialise(_kit.puppet);
            unit.InheritProgression(_hero);
            unit.MakeSummon(null, 0f);
            _puppet = unit;
            _puppetPost = at;
            _hasPost = true;
            _repairAt = 0f;
            // Roi cu vo giua luc dang dai khoi thi han dai khoi con treo: roi moi phai la roi thuong (khong nen
            // nhip dai khoi, `PuppetSet` khong tra bo DaiKhoi cho than Moc khoi).
            _grandUntil = _grandTick = 0f;
            _puppet.PlayAction(_kit.puppetDeploy);
            OneShotFx.Spawn(at, _kit.cable, false, SortOrder.Air, Battlefield.UnitScale);
        }

        /// <summary>
        /// Chieu cuoi chi co tac dung khi roi dang song va co dich trong tam roi — `Hero` giu mana day cho toi luc do
        /// (truoc day chu van tung chieu khi roi dang sua / chua co dich: mat sach mana ma khong co dai khoi).
        /// </summary>
        public bool CanUltimate => _puppet != null && !_puppet.IsDying && _puppet.AttackTargetInRange() != null;

        /// <summary>
        /// Don thuong cua chu: roi dam muc tieu cua chinh roi — MOT hit duy nhat, ra o frame cham cua clip dam cua roi.
        /// Roi dang ban dong tac khac (quet, ho chu) thi lenh bi bo, khong co sat thuong "khong hinh".
        /// </summary>
        public void Strike()
        {
            if (_puppet == null || _puppet.IsDying) return;
            var target = _puppet.AttackTargetInRange();
            if (target == null) return;
            var clip = PuppetSet.attack;
            if (!_puppet.PlayAction(clip)) return;
            StartCoroutine(PuppetHit(target, _puppet.HitDelay(clip)));
        }

        IEnumerator PuppetHit(Unit target, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_hero.IsDying || _puppet == null || _puppet.IsDying || target == null || target.IsDying) yield break;
            target.Health.TakeDamage(_puppet.AttackDamage, _puppet.Health);
            OneShotFx.Spawn(target.BodyCenter, _kit.impact, !_puppet.FacesRight, SortOrder.Air, Battlefield.UnitScale);
            _hero.OnHitsLanded(1);
        }

        /// <summary>
        /// Bo animation ROI dang mang. Roi doi bo khi bung khop (`CastUltimate` → `SwapAnimations`), nen moi clip dong
        /// tac cua roi phai lay tu day: lay clip cua bo goc giua luc dang hoa lon la keo art Moc khoi nho len than dai khoi.
        /// </summary>
        SpriteAnimationSet PuppetSet => Time.time < _grandUntil ? _kit.grandSet : _kit.puppet.animations;

        IEnumerator Sweep()
        {
            // Roi quet la dong tac cua CHINH ROI (MocKhoi/quet-moc), khong phai clip ra lenh cua chu — truoc day chu
            // lenh xong ma roi dung hinh.
            _puppet.PlayAction(PuppetSet.skill);
            yield return new WaitForSeconds(_hero.HitDelay(_kit.sweepCast));
            if (_hero.IsDying || _puppet == null || _puppet.IsDying) yield break;
            var at = _puppet.transform.position;
            float facing = _puppet.FacesRight ? 1f : -1f;
            OneShotFx.Spawn(at + new Vector3(facing * 0.6f, 0.4f, 0f), _kit.impact,
                !_puppet.FacesRight, SortOrder.Air, Battlefield.UnitScale);
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.AimPoint(at), at) > _kit.sweepRadius + enemy.BodyRadius) continue;
                enemy.Health.TakeDamage(_puppet.AttackDamage * _kit.sweepPower, _puppet.Health);
            }
            _hero.OnHitsLanded(1);
        }

        bool MasterPressured(out Unit presser)
        {
            presser = null;
            if (_puppet == null || _puppet.IsDying) return false;
            float best = _kit.guardRadius;
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying || enemy.IsBoss) continue;
                float d = Battlefield.GroundDistance(enemy.transform.position, _hero.transform.position) - enemy.BodyRadius;
                if (d < best) { best = d; presser = enemy; }
            }
            return presser != null;
        }

        IEnumerator Guard(Unit presser)
        {
            yield return new WaitForSeconds(_hero.HitDelay(_kit.guardCast));
            if (_hero.IsDying || _puppet == null || _puppet.IsDying) yield break;
            if (_hasPost) _puppetPost = _puppet.transform.position;
            _puppet.transform.position = _hero.transform.position
                + new Vector3(_hero.FacesRight ? 0.8f : -0.8f, 0f, 0f);
            _puppet.PlayAction(_kit.puppetParry);
            OneShotFx.Spawn(_puppet.transform.position, _kit.guardDust, false,
                SortOrder.GroundDecal + 1, Battlefield.UnitScale);
            if (presser != null && !presser.IsDying)
            {
                float facing = _hero.FacesRight ? 1f : -1f;
                var to = Battlefield.Clamp(presser.transform.position + new Vector3(facing * _kit.guardShove, 0f, 0f),
                    presser.BodyRadius, clampRight: true);
                presser.PullTo(to, 0.2f);
                presser.Health.TakeDamage(_puppet.AttackDamage, _puppet.Health);
            }
            yield return new WaitForSeconds(0.6f);
            if (_puppet != null && !_puppet.IsDying && _hasPost)
            {
                _puppet.transform.position = _puppetPost;
                OneShotFx.Spawn(_puppetPost, _kit.guardDust, false, SortOrder.GroundDecal + 1, Battlefield.UnitScale);
            }
            _hero.OnHitsLanded(1);
        }

        /// <summary>Khoi Vuong Giang Tran, goi o frame cham cua clip cast. Can roi dang song + co dich trong tam roi.</summary>
        public void CastUltimate()
        {
            if (!CanUltimate) return;
            _puppet.SwapAnimations(_kit.grandSet);
            // Bung khop phai NHIN THAY duoc: nhip nen dau tien doi clip bien hinh chay het, khong cat ngang no.
            _puppet.PlayAction(_kit.grandTransform);
            _puppet.Health.AddShield(_puppet.Health.Max * 0.3f, _hero.Health);
            _grandUntil = Time.time + _kit.grandDuration;
            _grandTick = Time.time + TransformSeconds();
        }

        /// <summary>Do dai clip bung khop theo nhip cua bo dai khoi (0 khi thieu art thi nen ngay).</summary>
        float TransformSeconds() =>
            _kit.grandSet != null && _kit.grandSet.framesPerSecond > 0f && _kit.grandTransform.IsValid
                ? _kit.grandTransform.frames.Length / _kit.grandSet.framesPerSecond
                : 0f;

        void SlamGrand()
        {
            if (_puppet == null || _puppet.IsDying) return;
            _puppet.PlayAction(PuppetSet.skill);
            var at = _puppet.transform.position;
            OneShotFx.Spawn(at, _kit.grandWave, false, SortOrder.GroundDecal + 1, Battlefield.UnitScale * 2f);
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.AimPoint(at), at) > _kit.grandRadius + enemy.BodyRadius) continue;
                enemy.Health.TakeDamage(_puppet.AttackDamage * _kit.grandPower, _puppet.Health);
                if (!enemy.IsBoss)
                    enemy.ApplyStun(enemy.IsElite ? _kit.grandStun * 0.5f : _kit.grandStun, _hero.Definition.stunFrames);
            }
            _hero.OnHitsLanded(1);
        }

        void LateUpdate()
        {
            if (_kit == null || _puppet == null || _puppet.IsDying) return;
            if (Time.time < _grandUntil)
            {
                if (Time.time >= _grandTick) { _grandTick = Time.time + GrandTickSeconds; SlamGrand(); }
            }
            else if (_grandTick > 0f)
            {
                _grandTick = 0f;
                _puppet.SwapAnimations(null);
            }
        }

        /// <summary>Nhip nen them khi dang o dang dai khoi (kit: moi 2 s mot nhip).</summary>
        const float GrandTickSeconds = 2f;
    }
}
