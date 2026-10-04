using System;
using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo chieu rieng cua Wyrmspear (Long Ky Binh, SSR — GAME_DESIGN.md muc 3, chot 2026-09-27). `Hero` tu gan component
    /// nay khi `HeroDefinition.dragonRider` co gia tri, giong `Warden` cua Chainwarden.
    ///   GD1 Ky Si:
    ///   - NOI TAI Nghich Lan: moi don cua Wyrmspear vao boss/tinh anh cong them % mau toi da muc tieu (`Strike`);
    ///   - CHIEU THUONG Xuyen Long (cooldown AN): dam xuyen mot duong ngang, BO QUA GIAP;
    ///   - CHIEU LON (thanh mana, `SkillKind.DragonRider`) lan dau = Long Hoa: bien hinh Long Nhan (het wave moi ve lai —
    ///     `HeroBoard` dung lai hero moi wave) + goi Bach Dia Long troi len phia truoc.
    ///   GD2 Long Nhan + Bach Dia Long:
    ///   - Long Giap: +mau toi da, giam sat thuong nhan, mien choang/hat van;
    ///   - Long Uy: +cong; quai trong tam giam cong, boss/tinh anh trong tam nhan them sat thuong tu moi nguon;
    ///   - Long No: Wyrmspear HOAC rong chiu don -> +1 tang (cach nhau toi thieu `rageStackInterval`, toi da 7), moi tang
    ///     + giam sat thuong nhan + cong + HUT MAU (nguoi dung them 2026-09-28); im don `rageDecayDelay` giay thi roi 1
    ///     tang moi `rageDecayInterval`. Ca hai cung huong;
    ///   - Long Khe: rong huong `pactShare` (mot nua) Long Giap + Long Uy;
    ///   - Chieu thuong 1 Thien Long Lien Thuong (nguoi): 5 nhat mot muc tieu, moi nhat an Nghich Lan, nhat cuoi xuyen duong;
    ///   - Chieu thuong 2 Dia Long Chan (rong): gai da quanh rong — sat thuong vung, choang, pha giap;
    ///   - Chieu lon Thien Dia Long Giang: Long Nhan lao vao quai MAU CAO NHAT (dat nat); ket lieu quai thuong/tinh anh
    ///     duoi 20%, boss x2; ha duoc muc tieu thi hoan mana. Cot da trang + song chan la phan cua DIA LONG (nguoi dung
    ///     2026-09-28), troi cung luc o CUM QUAI DONG NHAT trong tam danh cua rong (khong theo muc tieu Long Nhan) va
    ///     hat tung quai trong cot.
    /// Rong bi ha chui xuong dat (clip death), `dragonRespawn` giay sau troi lai. Wyrmspear chet thi rong chui theo.
    /// Don cua chieu thuong ra theo THOI GIAN tinh tu dau clip (frame / FPS bo animation) — clip than choi qua
    /// `Hero.PlayAction` nen khong co HitFrame rieng.
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public class DragonRider : MonoBehaviour
    {
        /// <summary>Canvas FX 128 px o 100 PPU, nhan UnitScale: do cao mot o tren man (world).</summary>
        const float FxCell = 1.28f * Battlefield.UnitScale;

        /// <summary>
        /// Mui giao luc ra duong xuyen, art-px tu chan, x AM = phia truoc (art quay trai): Xuyen Long GD1 (o 4-5
        /// `skill-xuyenlong-body`, dat 97) va nhat cuoi Lien Thuong (o 7 `skill-lienthuong-body`, dat 101).
        /// </summary>
        static readonly Vector2 PierceTip = new Vector2(-44f, 28f);
        static readonly Vector2 LanceFinalTip = new Vector2(-63f, 36f);

        /// <summary>`skill-xuyenlong-fx`: dai sang cua o than (3) tam hang 58, o dau mui (1-2) hang 68 (o 128, y xuong).</summary>
        const float BeamBodyRow = 58f, BeamHeadRow = 68f;
        /// <summary>Dai sang o than dai 113 px -> buoc 108 cho cac doan chong mep, khong lo khe.</summary>
        const float BeamStepPx = 108f;

        /// <summary>
        /// Lien Thuong: mui giao o ba nhat (o 2 ngang / o 4 dam len / o 6 dam xuong cua `skill-lienthuong-body`) va dau mui
        /// ten + DUOI vet cua vet dam tuong ung (`lienthuong-fx` 1 ngang / 3 len / 2 xuong). Art-px tinh tu tam o 128, y huong
        /// LEN; than neo chan (x 64, dat 101), FX neo giua (64, 64). Art quay TRAI. Duoi vet dat trung mui giao -> ca vet
        /// phong ra PHIA TRUOC giao (neo dau mui ten thi duoi vet ~100 px de len than — nguoi dung loai 2026-09-28).
        /// </summary>
        static readonly (Vector2 tip, int fx, Vector2 fxHead, Vector2 fxTail)[] LanceThrusts =
        {
            (new Vector2(-53f, 37f), 0, new Vector2(-48f, -1f), new Vector2(48f, 1f)),
            (new Vector2(-53f, 72f), 2, new Vector2(-48f, 17f), new Vector2(47f, -10f)),
            (new Vector2(-60f, 2f), 1, new Vector2(-51f, -13f), new Vector2(42f, 11f)),
        };

        /// <summary>Thien Dia Long Giang: dinh bay cao bao nhieu (world) va roi vao dau duong bay (0..1).</summary>
        const float DescentHeight = 1.8f;
        const float DescentPeakAt = 0.4f;

        static readonly Color RageLabelColor = new Color(1f, 0.82f, 0.4f);

        Hero _hero;
        DragonRiderDefinition _definition;
        Vector3 _home;
        bool _transformed;
        Hero _dragon;
        float _dragonBackAt;
        float _pierceCooldown, _lanceCooldown, _quakeCooldown;

        int _stacksAtLastHit;
        float _lastHitAt = float.NegativeInfinity;
        float _lastStackAt = float.NegativeInfinity;

        // Duong bay Long Giang: tu cho dung -> diem cham dat (quai mau cao nhat luc bat dau cast).
        bool _flying;
        float _flightStart;
        Vector3 _flightFrom, _flightTo, _flightGround;
        bool _descentLocked;
        Vector3 _descentPoint;

        readonly List<(float time, Action action)> _pending = new List<(float, Action)>();
        TextMesh _rageLabel;

        public Hero Rider => _hero;
        public bool Transformed => _transformed;

        /// <summary>So tang Long No luc nay (da tru phan roi vi im don).</summary>
        public int RageStacks
        {
            get
            {
                if (_stacksAtLastHit <= 0) return 0;
                float quiet = Time.time - _lastHitAt - _definition.rageDecayDelay;
                if (quiet < 0f) return _stacksAtLastHit;
                int lost = 1 + Mathf.FloorToInt(quiet / Mathf.Max(0.01f, _definition.rageDecayInterval));
                return Mathf.Max(0, _stacksAtLastHit - lost);
            }
        }

        float FramesPerSecond
        {
            get
            {
                var set = _hero.Definition.animations;
                return set != null && set.framesPerSecond > 0f ? set.framesPerSecond : 9f;
            }
        }

        public void Initialise(Hero hero, DragonRiderDefinition definition)
        {
            _hero = hero;
            _definition = definition;
            _home = hero.transform.position;
            _transformed = false;
            _pierceCooldown = definition.pierceCooldown * 0.5f;
            _pending.Clear();
            _hero.Health.Damaged -= OnUnitDamaged;
            _hero.Health.Damaged += OnUnitDamaged;
            _hero.Health.DamageDealt -= OnUnitDamageDealt;
            _hero.Health.DamageDealt += OnUnitDamageDealt;
            _hero.Health.Died -= OnRiderDied;
            _hero.Health.Died += OnRiderDied;
        }

        // ------------------------------------------------------------------ Hero hoi vao

        /// <summary>Long Nhan dam qua dau rong (`dragonmanRange`); rong va GD1 giu tam goc.</summary>
        public float AttackRange(Hero unit, float baseRange) =>
            unit == _hero && _transformed ? _definition.dragonmanRange : baseRange;

        /// <summary>Long Uy (+cong, rong mot nua) + Long No (+cong moi tang, ca hai du).</summary>
        public void Multipliers(Hero unit, ref float damage)
        {
            float share = Share(unit);
            if (share <= 0f) return;
            damage *= 1f + _definition.aweDamageBonus * share + RageStacks * _definition.rageDamagePerStack;
        }

        /// <summary>Long Nhan dang bay trong Thien Dia Long Giang: `Hero` khong kep vao san va ve o lop Air.</summary>
        public bool Airborne(Hero unit) => _flying && unit == _hero;

        /// <summary>Long Giap: mien choang / hat van — chi chinh Long Nhan.</summary>
        public bool Unshakable(Hero unit) => _transformed && unit == _hero;

        /// <summary>1 = Wyrmspear da bien hinh, `pactShare` = rong, 0 = chua bien hinh / unit khac.</summary>
        float Share(Hero unit)
        {
            if (!_transformed || unit == null) return 0f;
            if (unit == _hero) return 1f;
            return unit == _dragon ? _definition.pactShare : 0f;
        }

        /// <summary>Don thuong cua Wyrmspear (dam truc tiep, khong dan). Nghich Lan cong % mau toi da boss/tinh anh.</summary>
        public void Strike(Unit target, float damage)
        {
            if (target == null || target.IsDying) return;
            target.Health.TakeDamage(damage + ReverseScale(target), _hero.Health);
            _hero.OnHitsLanded(1);
        }

        /// <summary>Tran = mot nua don thuong hien tai: % mau boss khong duoc lan at level / trang bi cua chinh Wyrmspear.</summary>
        float ReverseScale(Unit enemy) =>
            _hero.PassiveUnlocked && (enemy.IsBoss || enemy.IsElite)
                ? Mathf.Min(ConquestMatch.PercentBase(enemy.Health.Max) * _definition.reverseScalePercent, _hero.AttackDamage * 0.5f) : 0f;

        /// <summary>Goi tu `Hero.FinishCast` khi thanh mana day: lan dau Long Hoa, tu do ve sau Thien Dia Long Giang.</summary>
        public void CastUltimate()
        {
            if (_hero == null || _hero.IsDying) return;
            if (!_transformed) Transform();
            else Descent();
        }

        // ------------------------------------------------------------------ vong lap

        void Update()
        {
            if (_definition == null || _hero == null || _hero.IsDying) return;

            if (_flying) UpdateFlight();

            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                if (Time.time < _pending[i].time) continue;
                var action = _pending[i].action;
                _pending.RemoveAt(i);
                action();
            }

            if (!_transformed)
            {
                _pierceCooldown -= Time.deltaTime;
                if (_pierceCooldown <= 0f && _hero.MinorUnlocked) TryPierce();
                return;
            }

            UpdateArmor();
            ApplyAwe(_hero, 1f);
            UpdateDragon();
            UpdateRageLabel();

            _lanceCooldown -= Time.deltaTime;
            if (_lanceCooldown <= 0f && _hero.MinorUnlocked) TryLance();
        }

        /// <summary>Long Giap + Long No -> `Health.Armor`, dat lai moi frame vi so tang doi theo thoi gian.</summary>
        void UpdateArmor()
        {
            float rage = RageStacks * _definition.rageReductionPerStack;
            _hero.Health.Armor = _definition.armorReduction + rage;
            if (DragonUp) _dragon.Health.Armor = _definition.armorReduction * _definition.pactShare + rage;
        }

        /// <summary>Long Uy quanh <paramref name="unit"/>: moi frame lam moi trang thai tren quai trong tam.</summary>
        void ApplyAwe(Hero unit, float share)
        {
            var position = unit.transform.position;
            foreach (var enemy in Unit.Hostiles(_hero.Side))
            {
                if (enemy == null || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.AimPoint(position), position) - enemy.BodyRadius > _definition.aweRadius) continue;
                float vulnerability = enemy.IsBoss || enemy.IsElite ? _definition.aweVulnerability * share : 0f;
                enemy.ApplyAwe(_definition.aweWeaken * share, vulnerability);
            }
        }

        bool DragonUp => _dragon != null && !_dragon.IsDying;

        void UpdateDragon()
        {
            if (DragonUp)
            {
                ApplyAwe(_dragon, _definition.pactShare);
                _quakeCooldown -= Time.deltaTime;
                if (_quakeCooldown <= 0f && _hero.SecondMinorUnlocked) TryQuake();
                return;
            }

            // Bi ha: chui xuong dat (clip death tu xoa object), dem gio troi lai.
            if (_dragonBackAt <= 0f) _dragonBackAt = Time.time + _definition.dragonRespawn;
            else if (Time.time >= _dragonBackAt) SpawnDragon();
        }

        // ------------------------------------------------------------------ Long No

        void OnUnitDamaged(float amount)
        {
            if (!_transformed || amount <= 0f) return;
            int stacks = RageStacks;
            if (stacks < _definition.rageMaxStacks && Time.time - _lastStackAt >= _definition.rageStackInterval)
            {
                stacks++;
                _lastStackAt = Time.time;
            }
            _stacksAtLastHit = stacks;
            _lastHitAt = Time.time;
        }

        void OnDragonDamaged(float amount)
        {
            if (DragonUp) OnUnitDamaged(amount);
        }

        void OnUnitDamageDealt(float amount) => Lifesteal(_hero, amount);

        void OnDragonDamageDealt(float amount)
        {
            if (DragonUp) Lifesteal(_dragon, amount);
        }

        /// <summary>Hut mau theo tang Long No: moi unit hoi tu chinh sat thuong no gay ra.</summary>
        void Lifesteal(Hero unit, float amount)
        {
            if (!_transformed || amount <= 0f || unit == null || unit.IsDying) return;
            float steal = RageStacks * _definition.rageLifestealPerStack;
            if (steal > 0f) unit.Health.Heal(amount * steal, _hero.Health);
        }

        void UpdateRageLabel()
        {
            if (_rageLabel == null)
            {
                var go = new GameObject("RageStacks");
                go.transform.SetParent(transform, false);
                _rageLabel = go.AddComponent<TextMesh>();
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _rageLabel.font = font;
                _rageLabel.fontSize = 48;
                _rageLabel.characterSize = FloatingNumber.LineHeight * 10f / 48f / Battlefield.UnitScale;
                _rageLabel.fontStyle = FontStyle.Bold;
                _rageLabel.anchor = TextAnchor.LowerCenter;
                _rageLabel.color = RageLabelColor;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = font.material;
                renderer.sortingOrder = SortOrder.Overlay;
            }
            // Tren thanh mau/mana (StatBar dat o dinh dau).
            _rageLabel.transform.localPosition = new Vector3(0f, _hero.BodyHeight / Battlefield.UnitScale + 0.2f, 0f);
            int stacks = RageStacks;
            _rageLabel.text = stacks > 0 ? $"Nộ {stacks}/{_definition.rageMaxStacks}" : "";
        }

        // ------------------------------------------------------------------ GD1: Xuyen Long

        /// <summary>Quai gan nhat trong tam danh hien tai cua Wyrmspear; null = chua giap tran.</summary>
        Unit EngagedTarget()
        {
            Unit best = null;
            float bestReach = _hero.CurrentAttackRange;
            foreach (var enemy in Unit.Hostiles(_hero.Side))
            {
                if (enemy == null || enemy.IsDying) continue;
                float reach = Battlefield.GroundDistance(enemy.AimPoint(transform.position), transform.position) - enemy.BodyRadius;
                if (reach > bestReach) continue;
                bestReach = reach;
                best = enemy;
            }
            return best;
        }

        void TryPierce()
        {
            var target = EngagedTarget();
            if (target == null) return;
            float facing = target.transform.position.x >= transform.position.x ? 1f : -1f;
            if (!_hero.PlayAction(_definition.pierceBody)) return;
            SetFacing(facing);
            _pierceCooldown = _definition.pierceCooldown;
            Schedule(_definition.pierceBody.hitFrame, () =>
                PierceLine(facing, PierceTip, _hero.AttackDamage * _definition.piercePower, ignoreArmor: true, reverseScale: true));
        }

        /// <summary>Moi quai tren duong ngang tu chan theo <paramref name="facing"/>, dai `pierceLength`, an don mot lan.</summary>
        void PierceLine(float facing, Vector2 tip, float damage, bool ignoreArmor, bool reverseScale)
        {
            float length = _definition.pierceLength;
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                var offset = enemy.AimPoint(transform.position) - transform.position;
                float forward = offset.x * facing;
                if (forward < -enemy.BodyRadius || forward > length + enemy.BodyRadius) continue;
                if (Mathf.Abs(offset.y) > _definition.pierceHalfWidth + enemy.BodyRadius * Battlefield.DepthSquash) continue;
                enemy.Health.TakeDamage(damage + (reverseScale ? ReverseScale(enemy) : 0f), _hero.Health, ignoreArmor);
            }
            SpawnBeam(facing, tip, length);
        }

        /// <summary>
        /// Duong xuyen ghep tu art 4 o: o 3 (doan than) lat lien tiep tu mui giao, o 1-2 (dau luong) o cuoi, o 4 tan.
        /// Art ve chi TRAI: nhin phai thi lat.
        /// </summary>
        void SpawnBeam(float facing, Vector2 tip, float length)
        {
            var fx = _definition.pierceFx;
            if (fx == null || fx.Length < 4) return;
            bool flip = facing > 0f;
            // Dai sang than nam dung do cao mui giao; o dau mui ha 10 px so voi o than -> nang len cho lien mach.
            var start = transform.position + Art(tip, facing) + new Vector3(0f, (BeamBodyRow - 64f) * ArtPixel, 0f);
            var headLift = new Vector3(0f, (BeamHeadRow - BeamBodyRow) * ArtPixel, 0f);
            float segment = BeamStepPx * ArtPixel;
            int count = Mathf.Max(1, Mathf.CeilToInt(length / segment));
            var body = new[] { fx[2], fx[2], fx[2], fx[3] };
            for (int i = 0; i < count - 1; i++)
                OneShotFx.Spawn(start + new Vector3(facing * segment * (i + 0.5f), 0f, 0f), body, flip, SortOrder.Air,
                    Battlefield.UnitScale);
            // Duoi o dau mui (x 113) cach mep truoc doan than cuoi 5 px o buoc 108 -> lui 8 px cho chong mep.
            var headAt = start + new Vector3(facing * (segment * (count - 0.5f) - 8f * ArtPixel), 0f, 0f);
            OneShotFx.Spawn(headAt + headLift, new[] { fx[0], fx[1], fx[0], fx[1] }, flip, SortOrder.Air, Battlefield.UnitScale);
            // O tan (4) cung hang voi o than: chay o do cao than, sau 4 o dau mui.
            _pending.Add((Time.time + 4f / FramesPerSecond, () =>
                OneShotFx.Spawn(headAt, new[] { fx[3] }, flip, SortOrder.Air, Battlefield.UnitScale)));
        }

        /// <summary>Mot art-px (o 100 PPU, nhan UnitScale) tren man — world.</summary>
        static float ArtPixel => Battlefield.UnitScale / 100f;

        /// <summary>Diem art-px (x AM = phia truoc vi art quay trai, y huong len) -> offset world tu chan theo huong nhin.</summary>
        static Vector3 Art(Vector2 point, float facing) => new Vector3(-point.x * facing, point.y, 0f) * ArtPixel;

        // ------------------------------------------------------------------ Long Hoa

        void Transform()
        {
            _transformed = true;
            if (_definition.dragonmanAnimations != null) _hero.SwapAnimations(_definition.dragonmanAnimations);
            _hero.Health.SetMax(_hero.MaxHealth * (1f + _definition.armorHealthBonus));
            _stacksAtLastHit = 0;
            _lanceCooldown = _definition.lanceCooldown * 0.5f;
            SpawnCrack(transform.position);
            SpawnDragon();
        }

        void SpawnDragon()
        {
            _dragonBackAt = 0f;
            if (_definition.dragon == null || _definition.unitPrefab == null) return;

            var position = Battlefield.Clamp(transform.position + new Vector3(_definition.dragonForward, 0f, 0f), 0.3f,
                clampRight: true);
            var dragon = Instantiate(_definition.unitPrefab, position, Quaternion.identity);
            dragon.name = "BachDiaLong";
            dragon.Initialise(_definition.dragon);
            dragon.MakeDragonMount(this, _home + new Vector3(_definition.dragonForward, 0f, 0f));
            dragon.InheritProgression(_hero);
            dragon.Health.ResetTo(dragon.MaxHealth * (1f + _definition.armorHealthBonus * _definition.pactShare));
            dragon.Health.Damaged += OnDragonDamaged;
            dragon.Health.DamageDealt += OnDragonDamageDealt;
            // Thien Dia Long Giang la chieu chung hai con -> rong hien cung thanh mana cua Wyrmspear.
            StatBar.AttachMana(dragon, _hero);
            _dragon = dragon;
            _quakeCooldown = _definition.quakeCooldown * 0.5f;
            dragon.PlayAction(_definition.emerge);
            SpawnCrack(position);
        }

        void SpawnCrack(Vector3 ground)
        {
            // Duong dat cua art o 70% chieu cao o, pivot giua -> nang tam len 0,2 o.
            OneShotFx.Spawn(ground + new Vector3(0f, FxCell * 0.2f, 0f), _definition.crackFx, false, SortOrder.Air - 1,
                Battlefield.UnitScale);
        }

        void OnRiderDied()
        {
            _pending.Clear();
            // Chet giua khong trung -> roi xuong duong bay de clip death nam tren dat.
            if (_flying)
            {
                _flying = false;
                transform.position = _flightGround;
            }
            if (_rageLabel != null) _rageLabel.text = "";
            // Chu chet thi rong chui xuong dat theo (chay clip death, tu xoa).
            if (DragonUp) _dragon.Health.TakeDamage(_dragon.Health.Current * 100f, _dragon.Health, ignoreArmor: true);
        }

        void OnDestroy()
        {
            if (_hero != null)
            {
                _hero.Health.Damaged -= OnUnitDamaged;
                _hero.Health.DamageDealt -= OnUnitDamageDealt;
                _hero.Health.Died -= OnRiderDied;
            }
            // Dung lai doi hinh dau wave xoa Wyrmspear -> xoa luon rong.
            if (_dragon != null) Destroy(_dragon.gameObject);
        }

        // ------------------------------------------------------------------ GD2: Thien Long Lien Thuong

        void TryLance()
        {
            var target = EngagedTarget();
            if (target == null) return;
            float facing = target.transform.position.x >= transform.position.x ? 1f : -1f;
            if (!_hero.PlayAction(_definition.lanceBody)) return;
            SetFacing(facing);
            _lanceCooldown = _definition.lanceCooldown;

            var hits = _definition.lanceHitFrames;
            for (int i = 0; i < hits.Length; i++)
            {
                bool last = i == hits.Length - 1;
                int variant = i % 3;
                Schedule(hits[i], () =>
                {
                    if (last)
                    {
                        PierceLine(facing, LanceFinalTip, _hero.AttackDamage * _definition.lanceFinalPower, ignoreArmor: false,
                            reverseScale: true);
                        return;
                    }
                    if (target == null || target.IsDying) return;
                    target.Health.TakeDamage(_hero.AttackDamage * _definition.lancePower + ReverseScale(target), _hero.Health);
                    SpawnLanceFx(facing, variant);
                });
            }
        }

        /// <summary>Vet dam phong ra tu MUI GIAO (cung cho duong xuyen nhat cuoi ban ra), khong o muc tieu.</summary>
        void SpawnLanceFx(float facing, int variant)
        {
            var fx = _definition.lanceFx;
            if (fx == null || fx.Length < 4) return;
            var thrust = LanceThrusts[variant];
            var tip = Art(thrust.tip, facing);
            var centre = tip - Art(thrust.fxTail, facing);
            var head = centre + Art(thrust.fxHead, facing);
            OneShotFx.Spawn(transform.position + centre, new[] { fx[thrust.fx] }, facing > 0f, SortOrder.Air,
                Battlefield.UnitScale);
            // Tia trung (o 4) no o dau mui ten vet dam ngay sau vet dam.
            _pending.Add((Time.time + 1f / FramesPerSecond, () =>
                OneShotFx.Spawn(transform.position + head, new[] { fx[3] }, facing > 0f, SortOrder.Air, Battlefield.UnitScale)));
        }

        // ------------------------------------------------------------------ GD2: Dia Long Chan

        bool InQuake(Unit enemy) =>
            Battlefield.GroundDistance(enemy.AimPoint(_dragon.transform.position), _dragon.transform.position) - enemy.BodyRadius
            <= _definition.quakeRadius;

        void TryQuake()
        {
            bool any = false;
            foreach (var enemy in Unit.Hostiles(_hero.Side))
                if (enemy != null && !enemy.IsDying && InQuake(enemy)) { any = true; break; }
            if (!any || !_dragon.PlayAction(_definition.quakeBody)) return;
            _quakeCooldown = _definition.quakeCooldown;

            var dragon = _dragon;
            Schedule(_definition.quakeBody.hitFrame, () =>
            {
                if (dragon == null || dragon.IsDying) return;
                float damage = dragon.AttackDamage * _definition.quakePower;
                foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
                {
                    if (enemy == null || enemy.IsDying || !InQuake(enemy)) continue;
                    bool tough = enemy.IsBoss || enemy.IsElite;
                    enemy.ApplyStun(tough ? _definition.quakeStun * 0.5f : _definition.quakeStun, _definition.stunFrames);
                    enemy.ApplyArmorBreak(_definition.armorBreak, _definition.armorBreakDuration);
                    enemy.Health.TakeDamage(damage, dragon.Health);
                }
                // Chan gai o 75% chieu cao o art -> nang tam len 0,25 o.
                OneShotFx.Spawn(dragon.transform.position + new Vector3(0f, FxCell * 0.25f, 0f), _definition.quakeFx, false,
                    SortOrder.Air, Battlefield.UnitScale);
            });
        }

        // ------------------------------------------------------------------ GD2: Thien Dia Long Giang

        /// <summary>
        /// Goi tu `Hero.BeginCast` luc clip than Long Giang vua chay (chi khi da bien hinh). Chon muc tieu = quai MAU CAO
        /// NHAT trong `descentRange`, roi bay theo clip: o 1 thu nguoi tai cho, o 2-3 bat len + dang canh, o 4-5 lao xuong,
        /// cham dat dung o `descentHitFrame` tai muc tieu. Clip ve moi o cung mot duong dat nen do cao do code dat.
        /// </summary>
        public void BeginUltimate()
        {
            _descentLocked = false;
            if (!_transformed) return;
            Unit target = null;
            float best = -1f;
            float rangeSqr = _definition.descentRange * _definition.descentRange;
            foreach (var enemy in Unit.Hostiles(_hero.Side))
            {
                if (enemy == null || enemy.IsDying) continue;
                if ((enemy.AimPoint(transform.position) - transform.position).sqrMagnitude > rangeSqr) continue;
                if (enemy.Health.Current <= best) continue;
                best = enemy.Health.Current;
                target = enemy;
            }
            if (target == null) return;

            _descentLocked = true;
            _descentPoint = target.transform.position;
            _flying = true;
            _flightStart = Time.time;
            _flightFrom = transform.position;
            _flightTo = Battlefield.Clamp(_descentPoint, _hero.BodyRadius, clampRight: true);
            _flightGround = _flightFrom;
            SetFacing(_flightTo.x >= _flightFrom.x ? 1f : -1f);
            // Dia long cam dau xuong dat cung nhip: het clip dung luc Long Nhan cham dat, cot da troi len.
            if (DragonUp) _dragon.PlayAction(_definition.descentDragon);
        }

        /// <summary>Vi tri theo o clip: tu dau o 2 toi dau o cham dat. Duong dat lerp, do cao len nhanh roi lao xuong.</summary>
        void UpdateFlight()
        {
            float frames = (Time.time - _flightStart) * FramesPerSecond;
            float span = Mathf.Max(1f, _definition.descentHitFrame - 2);
            float u = Mathf.Clamp01((frames - 1f) / span);
            _flightGround = Vector3.Lerp(_flightFrom, _flightTo, Mathf.SmoothStep(0f, 1f, u));
            float rise = u < DescentPeakAt ? 1f - u / DescentPeakAt : (u - DescentPeakAt) / (1f - DescentPeakAt);
            transform.position = _flightGround + new Vector3(0f, DescentHeight * (1f - rise * rise), 0f);
            if (u >= 1f)
            {
                _flying = false;
                transform.position = _flightTo;
            }
        }

        /// <summary>
        /// Ra o frame `descentHitFrame` cua clip than (Long Nhan cam giao, vua cham dat tai muc tieu da chon o `BeginUltimate`):
        /// dat nat tai cho cham dat, moi quai trong `descentRadius` an don. Dia long con song thi cung luc do cot da + song
        /// chan troi o cum quai cua rong (`Pillar`).
        /// </summary>
        void Descent()
        {
            if (!_descentLocked) return;
            _descentLocked = false;
            if (_flying)
            {
                _flying = false;
                transform.position = _flightTo;
            }

            var centre = _flightTo;
            SpawnCrack(centre);

            float baseDamage = _hero.AttackDamage * _definition.descentPower;
            bool killed = false;
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.AimPoint(centre), centre) - enemy.BodyRadius > _definition.descentRadius)
                    continue;
                float damage = baseDamage + ConquestMatch.PercentBase(enemy.Health.Max) * _definition.descentMaxHealthPercent;
                if (enemy.IsBoss) damage *= _definition.bossMultiplier;
                enemy.Health.TakeDamage(damage, _hero.Health);
                // Ket lieu quai thuong / tinh anh con duoi nguong sau don.
                if (!enemy.IsBoss && !enemy.Health.IsDead && enemy.Health.Fraction < _definition.executeThreshold)
                    enemy.Health.TakeDamage(enemy.Health.Max * 100f, _hero.Health, ignoreArmor: true);
                if (enemy.Health.IsDead) killed = true;
            }
            if (killed) _hero.AddMana(_definition.killManaRefund);
            if (DragonUp) Pillar();
        }

        /// <summary>
        /// Cot da trang cua dia long: tam = quai co NHIEU quai khac trong `descentRadius` nhat, chon trong vung hoat dong
        /// cua rong (tam danh + day xich `leashRange`) — chi tam danh can chien 0,7 thi luc rong chua ap sat quai la mat cot
        /// (nguoi dung chon 2026-09-29). Hat tung moi quai con song trong `descentRadius` quanh tam. Khong co quai thi khong troi.
        /// </summary>
        void Pillar()
        {
            float reach = _dragon.CurrentAttackRange + _dragon.Definition.leashRange;
            Unit best = null;
            int bestCount = 0;
            foreach (var enemy in Unit.Hostiles(_hero.Side))
            {
                if (enemy == null || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.AimPoint(_dragon.transform.position), _dragon.transform.position) - enemy.BodyRadius > reach)
                    continue;
                int count = 0;
                foreach (var other in Unit.Hostiles(_hero.Side))
                    if (other != null && !other.IsDying && InPillar(other, enemy.transform.position)) count++;
                if (count <= bestCount) continue;
                bestCount = count;
                best = enemy;
            }
            if (best == null) return;

            var centre = best.transform.position;
            OneShotFx.Spawn(centre + new Vector3(0f, FxCell * 0.35f, 0f), _definition.pillarFx, false, SortOrder.Air,
                Battlefield.UnitScale);
            OneShotFx.Spawn(centre + new Vector3(0f, FxCell * 0.2f, 0f), _definition.shockwaveFx, false, SortOrder.Air - 1,
                Battlefield.UnitScale);
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
                if (enemy != null && !enemy.IsDying && InPillar(enemy, centre))
                    enemy.KnockUp(_definition.pillarKnockUpHeight, _definition.pillarKnockUpDuration);
        }

        bool InPillar(Unit enemy, Vector3 centre) =>
            Battlefield.GroundDistance(enemy.AimPoint(centre), centre) - enemy.BodyRadius <= _definition.descentRadius;

        // ------------------------------------------------------------------ tien ich

        /// <summary>Hen <paramref name="action"/> luc clip vua bat dau chay toi frame <paramref name="frame"/> (tinh tu 1).</summary>
        void Schedule(int frame, Action action)
        {
            float delay = Mathf.Max(0, frame - 1) / FramesPerSecond;
            _pending.Add((Time.time + delay, action));
        }

        // Art quay trai: flipX = nhin phai.
        void SetFacing(float facing)
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.flipX = facing > 0f;
        }
    }
}
