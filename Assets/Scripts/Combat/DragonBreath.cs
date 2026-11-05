using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Long Viêm cua Dau Rong Da (`CannonDefinition.dragonKit` khac null — xem `DragonCannonKit`). Gan tu
    /// `Cannon.Initialise` giong `Hero` gan `DragonRider`: `Cannon` van giu "ban the nao" (phe, level, sat thuong,
    /// nong), con bo luat phun nam o day.
    ///
    /// Vong lap: co quai trong tam -> phun non `breathSeconds` giay (sat thuong theo nhip, moi quai trung bi Thiêu Đốt)
    /// -> nghi `restSeconds` -> phun tiep. Trung quai thi day Long Nộ; thanh day thi ngat dot phun, lay hoi roi
    /// phong Long Tức (mot lan 3D moi quai + dat chay). Quai chet luc dang chay vi phao nay thi no lan (Hỏa Bạo).
    ///
    /// KHONG ra don theo `Cannon` cu (cooldown + clip attack + projectile): no la kieu ra don thu 5, `Cannon.Update`
    /// thoat som khi co component nay.
    /// </summary>
    public sealed class DragonBreath : MonoBehaviour
    {
        enum Phase { Rest, Breath, Inhale, Ult }

        /// <summary>Mot nan nhan dang chay do phao nay va luot no lan cua no (0 = bi non/thuong truc tiep dot).</summary>
        struct BurnMark
        {
            public Unit unit;
            public int step;
        }

        const float BreathFrameSeconds = 1f / 9f;   // cung nhip 9 FPS voi `SpriteAnimationSet`
        const float SfxGap = 0.42f;
        /// <summary>Tran so doan luong lua (moi doan 1 tam art) — du phu tam danh dai nhat ma khong sinh vo han.</summary>
        const int MaxFlameSegments = 8;

        Cannon _cannon;
        CannonDefinition _definition;
        DragonCannonKit _kit;
        Faction _side;
        SpriteAnimator _animator;
        SpriteRenderer _body;

        Phase _phase;
        float _phaseLeft;
        float _tickLeft;
        float _rage;
        float _lastHitAt = -99f;
        float _sfxAt;

        readonly List<SpriteRenderer> _flameSegments = new List<SpriteRenderer>();
        Sprite[] _flameFrames;
        float _flameTimer;
        float _flameScale = 1f;
        float _flameReach;
        bool _flameOn;
        int _flameFrame;

        readonly List<BurnMark> _burning = new List<BurnMark>();
        readonly List<Unit> _scratch = new List<Unit>();

        /// <summary>Thanh Long Nộ 0..max — HUD khong doc (phao khong co thanh rieng), de day cho debug/kit sau nay.</summary>
        public float Rage => _rage;

        public void Initialise(Cannon cannon, CannonDefinition definition, DragonCannonKit kit, Faction side)
        {
            _cannon = cannon;
            _definition = definition;
            _kit = kit;
            _side = side;
            _animator = GetComponent<SpriteAnimator>();
            _body = GetComponent<SpriteRenderer>();
            _phase = Phase.Rest;
            _phaseLeft = kit.restSeconds * 0.5f;   // vao tran la phun som, khong dung nghi het mot nhip
            if (_animator != null)
            {
                _animator.ClipFinished -= OnClipFinished;
                _animator.ClipFinished += OnClipFinished;
            }
        }

        void OnDestroy()
        {
            if (_animator != null) _animator.ClipFinished -= OnClipFinished;
            for (int i = 0; i < _flameSegments.Count; i++)
                if (_flameSegments[i] != null) Destroy(_flameSegments[i].gameObject);
            _flameSegments.Clear();
        }

        bool FacesRight => _body != null && _body.flipX;

        Vector3 Origin => _cannon != null ? _cannon.MuzzlePoint() : transform.position;

        float Damage => _cannon != null ? _cannon.Damage : 0f;

        int Tier
        {
            get
            {
                var thresholds = _kit.rageThresholds;
                int tier = 0;
                for (int i = 0; thresholds != null && i < thresholds.Length; i++)
                    if (_rage >= thresholds[i]) tier = i + 1;
                return Mathf.Clamp(tier, 0, 2);
            }
        }

        float DamageFactor => Pick(_kit.rageDamage, Tier, 1f);

        float Reach => _definition.range * Pick(_kit.rageRange, Tier, 1f);

        static float Pick(float[] values, int index, float fallback) =>
            values != null && index < values.Length ? values[index] : fallback;

        /// <summary>Goi moi frame tu `Cannon.Update` (Cannon da thoat khoi duong cooldown cua no).</summary>
        public void Tick()
        {
            if (_kit == null) return;
            float dt = Time.deltaTime;

            if (_phase == Phase.Rest || _phase == Phase.Breath) DecayRage(dt);
            CheckBurnDeaths();

            switch (_phase)
            {
                case Phase.Rest:
                    _phaseLeft -= dt;
                    if (_phaseLeft <= 0f && HasTarget(Reach)) StartBreath();
                    break;

                case Phase.Breath:
                    _phaseLeft -= dt;
                    Breathe(dt);
                    if (_rage >= _kit.rageMax && HasTarget(Reach)) StartInhale();
                    else if (_phaseLeft <= 0f) EndBreath();
                    break;

                case Phase.Inhale:
                    _phaseLeft -= dt;
                    if (_phaseLeft <= 0f) StartUlt();
                    break;

                case Phase.Ult:
                    _phaseLeft -= dt;
                    AnimateFlame(dt);
                    if (_phaseLeft <= 0f) EndUlt();
                    break;
            }
        }

        // ---------------------------------------------------------------- phun thuong

        void StartBreath()
        {
            _phase = Phase.Breath;
            _phaseLeft = _kit.breathSeconds;
            _tickLeft = 0f;
            Play("attack");
            ShowFlame(Tier, Reach);
        }

        void EndBreath()
        {
            _phase = Phase.Rest;
            _phaseLeft = _kit.restSeconds;
            HideFlame();
            Play("idle");
        }

        void Breathe(float dt)
        {
            AnimateFlame(dt);
            ShowFlame(Tier, Reach);   // Long Nộ len muc thi doi bo frame lua

            if (Time.time - _sfxAt >= SfxGap && !string.IsNullOrEmpty(_kit.breathSfx))
            {
                _sfxAt = Time.time;
                Sfx.Play(_kit.breathSfx);
            }

            _tickLeft -= dt;
            if (_tickLeft > 0f) return;
            _tickLeft += _kit.tickSeconds;

            float dps = Damage * DamageFactor;
            if (Scorch(Origin, Reach, _kit.coneHalfAngle, dps * _kit.tickSeconds, 0) <= 0) return;
            _lastHitAt = Time.time;

            _rage = Mathf.Min(_kit.rageMax, _rage + _kit.rageGainPerSecond * _kit.tickSeconds);
        }

        void DecayRage(float dt)
        {
            if (_rage <= 0f || Time.time - _lastHitAt <= _kit.rageDecayDelay) return;
            _rage = Mathf.Max(0f, _rage - _kit.rageDecayPerSecond * dt);
        }

        // ---------------------------------------------------------------- Long Tức

        void StartInhale()
        {
            _phase = Phase.Inhale;
            _phaseLeft = _kit.ultInhaleSeconds;
            HideFlame();
            Play("skill", restart: true);
        }

        void StartUlt()
        {
            _phase = Phase.Ult;
            _phaseLeft = _kit.ultSeconds;
            float reach = _definition.range * _kit.ultRangeScale;
            ShowFlame(2, reach, _kit.breathUlt, _kit.ultBreathScale);

            // Moi quai an DUNG MOT lan 3D cho ca luong (khong an theo tick).
            float damage = Damage * _kit.ultPower;
            Scorch(Origin, reach, _kit.coneHalfAngle, damage, 0);

            // Dat chay: rai doc dai phun, sat thuong do trang thai Chay tick (xem `HazardPool`).
            float dps = Damage * _kit.ultGroundShare;
            Vector3 dir = FacesRight ? Vector3.right : Vector3.left;
            int pools = Mathf.Max(1, _kit.ultGroundPools);
            for (int i = 1; i <= pools; i++)
            {
                var at = Origin + dir * (reach * i / pools);
                HazardPool.Spawn(at, _kit.groundFrames, _kit.ultGroundSeconds, _kit.ultGroundRadius,
                    StatusId.Burn, dps, _kit.burnSeconds, side: _side);
            }

            Sfx.Play(_kit.breathSfx);
        }

        void EndUlt()
        {
            _rage = 0f;
            _phase = Phase.Rest;
            _phaseLeft = _kit.restSeconds;
            HideFlame();
            Play("idle");
        }

        // ---------------------------------------------------------------- trung don

        /// <summary>
        /// Gây sat thuong + Thiêu Đốt cho moi quai trong non. Tra ve so quai trung.
        /// <paramref name="damage"/> la sat thuong cua CA lan goi (mot tick DPS, hoac ca luong Long Tức).
        /// </summary>
        int Scorch(Vector3 origin, float reach, float halfAngle, float damage, int chainStep)
        {
            if (damage <= 0f) return 0;
            Vector3 dir = FacesRight ? Vector3.right : Vector3.left;
            _scratch.Clear();
            foreach (var enemy in Unit.Hostiles(_side))
            {
                if (enemy == null || enemy.IsDying) continue;
                var to = enemy.AimPoint(origin) - origin;
                to.z = 0f;
                if (to.magnitude > reach + enemy.BodyRadius) continue;
                if (to.sqrMagnitude > 0.0001f && Vector3.Angle(dir, to) > halfAngle) continue;
                _scratch.Add(enemy);
            }

            int hit = 0;
            foreach (var enemy in _scratch)
            {
                if (enemy == null || enemy.IsDying) continue;
                hit++;
                float before = enemy.Health.Current;
                enemy.Health.TakeDamage(damage * Cannon.BigTargetFactor(_definition, enemy), side: _side);
                WaveStats.RecordCannonDamage(_definition, _side, before - enemy.Health.Current);
                enemy.Status.Apply(StatusId.Burn, Damage * _kit.burnShare, _kit.burnSeconds, side: _side,
                    fx: _kit.burnFrames);
                RememberBurn(enemy, chainStep);
            }
            _scratch.Clear();
            return hit;
        }

        // ---------------------------------------------------------------- Hỏa Bạo

        void RememberBurn(Unit unit, int step)
        {
            for (int i = 0; i < _burning.Count; i++)
                if (_burning[i].unit == unit) return;   // da co: khong nang luot, giu luot cua lan dau
            _burning.Add(new BurnMark { unit = unit, step = step });
        }

        /// <summary>
        /// Quai chet luc dang chay vi phao nay thi no lan sang quai gan do, roi chuyen luot sang chung.
        /// Duyet MOI frame thay vi nghe `Health.Died` cua tung con: mot cho go su kien, khong ro ri khi unit bi huy.
        /// </summary>
        void CheckBurnDeaths()
        {
            if (_burning.Count == 0) return;
            for (int i = _burning.Count - 1; i >= 0; i--)
            {
                var mark = _burning[i];
                if (mark.unit != null && !mark.unit.IsDying)
                {
                    // Het chay ma con song: bo dau, de chet VE SAU (khong con chay) khong no lan.
                    if (!mark.unit.Status.Has(StatusId.Burn)) _burning.RemoveAt(i);
                    continue;
                }
                _burning.RemoveAt(i);
                if (mark.unit == null) continue;
                Burst(mark.unit.transform.position, mark.step);
            }
        }

        void Burst(Vector3 at, int step)
        {
            if (step >= _kit.burstChainMax) return;
            float power = Damage * _kit.burstPower * Mathf.Pow(_kit.burstFalloff, step);
            OneShotFx.Spawn(at + Vector3.up * 0.4f, _kit.burstFrames, false, SortOrder.Air, Battlefield.UnitScale);

            var hits = new List<Unit>();
            foreach (var enemy in Unit.Hostiles(_side))
            {
                if (enemy == null || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.AimPoint(at), at) - enemy.BodyRadius > _kit.burstRadius) continue;
                hits.Add(enemy);
            }
            foreach (var enemy in hits)
            {
                if (enemy == null || enemy.IsDying) continue;
                float before = enemy.Health.Current;
                enemy.Health.TakeDamage(power * Cannon.BigTargetFactor(_definition, enemy), side: _side);
                WaveStats.RecordCannonDamage(_definition, _side, before - enemy.Health.Current);
                enemy.Status.Apply(StatusId.Burn, Damage * _kit.burnShare, _kit.burnSeconds, side: _side,
                    fx: _kit.burnFrames);
                RememberBurn(enemy, step + 1);
            }
        }

        // ---------------------------------------------------------------- hinh

        /// <summary>Co quai nao trong non toi <paramref name="reach"/> khong (dung `range` tho, khong tinh quai da chet).</summary>
        bool HasTarget(float reach)
        {
            Vector3 dir = FacesRight ? Vector3.right : Vector3.left;
            Vector3 origin = Origin;
            foreach (var enemy in Unit.Hostiles(_side))
            {
                if (enemy == null || enemy.IsDying) continue;
                var to = enemy.AimPoint(origin) - origin;
                to.z = 0f;
                if (to.magnitude > reach + enemy.BodyRadius) continue;
                if (to.sqrMagnitude > 0.0001f && Vector3.Angle(dir, to) > _kit.coneHalfAngle) continue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Bat luong lua: bo frame theo muc Long Nộ (0..2) hoac Long Tức, va <paramref name="reach"/> = do dai luong
        /// phai phu. Luong ve 1:1 (1 art px = 1 px man) nen KHONG keo gian mot tam cho du tam: cat thanh NHIEU doan
        /// noi duoi, moi doan lech pha frame — vua du dai bang tam danh, vua giu nguyen luoi pixel.
        /// </summary>
        void ShowFlame(int tier, float reach, Sprite[] overrideFrames = null, float scale = 1f)
        {
            var frames = overrideFrames != null ? overrideFrames
                : tier >= 2 ? _kit.breathHigh
                : tier >= 1 ? _kit.breathMid
                : _kit.breathLow;
            if (frames == null || frames.Length == 0) return;
            if (_flameFrames != frames)
            {
                _flameFrames = frames;
                _flameFrame = 0;
                _flameTimer = 0f;
            }
            _flameScale = scale;
            _flameReach = reach;
            _flameOn = true;
            RefreshFlame();
        }

        /// <summary>Dat lai tung doan luong lua: vi tri theo nong + huong lat, frame lech pha de lua "chay" ra xa.</summary>
        void RefreshFlame()
        {
            if (!_flameOn || _flameFrames == null || _flameFrames.Length == 0) return;
            float length = Mathf.Max(0.01f, _flameFrames[0].bounds.size.x * _flameScale);
            int needed = Mathf.Clamp(Mathf.CeilToInt(_flameReach / length), 1, MaxFlameSegments);
            while (_flameSegments.Count < needed)
            {
                // KHONG gan vao be phao: be/than da nhan `displayScale` x co be (0,72) nen luong lua se bi thu nho
                // theo. Dat o goc scene va tu dat vi tri world -> 1 art px = 1 px man dung nhu art ve.
                var go = new GameObject("Flame");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = SortOrder.Air;
                _flameSegments.Add(sr);
            }

            Vector3 dir = FacesRight ? Vector3.right : Vector3.left;
            Vector3 origin = Origin;
            for (int i = 0; i < _flameSegments.Count; i++)
            {
                var segment = _flameSegments[i];
                bool on = i < needed;
                if (segment.gameObject.activeSelf != on) segment.gameObject.SetActive(on);
                if (!on) continue;
                segment.sprite = _flameFrames[(_flameFrame + i) % _flameFrames.Length];
                segment.flipX = FacesRight;
                segment.transform.localScale = Vector3.one * _flameScale;
                segment.transform.position = origin + dir * (length * (i + 0.5f));
            }
        }

        void AnimateFlame(float dt)
        {
            if (!_flameOn || _flameFrames == null || _flameFrames.Length == 0) return;
            _flameTimer += dt;
            while (_flameTimer >= BreathFrameSeconds)
            {
                _flameTimer -= BreathFrameSeconds;
                _flameFrame = (_flameFrame + 1) % _flameFrames.Length;
            }
            RefreshFlame();
        }

        void HideFlame()
        {
            _flameOn = false;
            for (int k = 0; k < _flameSegments.Count; k++)
                if (_flameSegments[k] != null) _flameSegments[k].gameObject.SetActive(false);
        }
        void Play(string clip, bool restart = false)
        {
            if (_animator == null) return;
            _animator.Play(clip, restart);
        }

        void OnClipFinished(string clip)
        {
            // Clip `attack` cua phao la clip MOT lan (6 frame ~0,7 s) con dot phun dai 2,5 s: chay lai de than con
            // "dang phun" suot dot, khong dung hinh o frame cuoi.
            if (clip == "attack" && _phase == Phase.Breath)
            {
                Play("attack", restart: true);
                return;
            }
            // Clip `skill` (than lay hoi + phong) chay xong ma Long Tức van con thi than ve idle; luong lua van chay
            // toi het `ultSeconds` — khong cat luong theo clip.
            if (clip != "skill") return;
            if (_phase == Phase.Inhale || _phase == Phase.Ult) return;
            Play("idle");
        }
    }
}
