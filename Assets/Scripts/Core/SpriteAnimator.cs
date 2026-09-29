using System;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Core
{
    /// <summary>
    /// Chay animation bang cach doi `SpriteRenderer.sprite` theo nhip co dinh.
    ///
    /// Vi sao khong dung Animator Controller: bo art hien tai la cac PNG frame roi, va moi unit deu
    /// dung dung 5 action cung cau truc. Mot `.controller` + 5 `.anim` cho MOI unit la 6 asset phai
    /// dung bang Editor, trong khi thu can o day chi la "phat day sprite nay o 9 FPS". Khi nao can
    /// blend/state machine that (hero co skill) thi hay chuyen sang Animator.
    ///
    /// Lat huong bang `flipX`: quy uoc du an la model ve nhan vat QUAY TRAI, nen quai di sang trai
    /// giu flipX = false.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnimator : MonoBehaviour
    {
        [SerializeField] SpriteAnimationSet set;

        SpriteRenderer _renderer;
        SpriteAnimationClip _clip;
        float _timer;
        int _frame;
        bool _finished;

        /// <summary>Ban ra khi mot clip khong loop chay het frame cuoi.</summary>
        public event Action<string> ClipFinished;

        /// <summary>Ban ra khi clip toi `hitFrame` cua no — luc vu khi cham, khong phai luc giơ tay hay thu ve.</summary>
        public event Action<string> HitFrame;

        public string CurrentClip => _clip?.name;
        public bool IsFinished => _finished;

        /// <summary>
        /// Dung khung hinh hien tai tai cho (khong reset frame). Dung cho hieu ung khoa nhu dong bang:
        /// quai dung yen ma chan van buoc thi doc ra nhu game dung hinh, khong phai bi khoa.
        /// </summary>
        public bool Paused { get; set; }

        /// <summary>He so nhip phat (1 = 9 FPS cua bo art). Buff toc danh tang theo de animation khop nhip ban.</summary>
        public float Speed { get; set; } = 1f;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (set != null) Play("idle");
        }

        public void Bind(SpriteAnimationSet animationSet)
        {
            set = animationSet;
            _clip = null;
            Play("idle");
        }

        /// <summary>
        /// Doi bo animation ma giu clip dang chay (cung ten, cung frame) — bien hinh giua don danh thi don do chay tiep
        /// bang art moi va `ClipFinished` van ban dung luc.
        /// </summary>
        public void Swap(SpriteAnimationSet animationSet)
        {
            set = animationSet;
            var next = _clip != null ? set.Get(_clip.name) : null;
            if (next == null || !next.IsValid)
            {
                _clip = null;
                Play("idle");
                return;
            }
            _clip = next;
            _frame = Mathf.Min(_frame, next.frames.Length - 1);
            _renderer.sprite = next.frames[_frame];
        }

        /// <summary>Doi clip. Goi lai dung clip dang chay thi khong lam gi (tru khi `restart`).</summary>
        public void Play(string clipName, bool restart = false)
        {
            if (set == null) return;
            Play(set.Get(clipName), restart);
        }

        /// <summary>Phat mot clip khong nam trong bo (vd. bien hinh). Ten clip la ten `ClipFinished` ban ra.</summary>
        public void Play(SpriteAnimationClip next, bool restart = false)
        {
            if (next == null || !next.IsValid) return;
            if (_clip == next && !restart) return;

            _clip = next;
            _frame = 0;
            _timer = 0f;
            _finished = false;
            _renderer.sprite = _clip.frames[0];
            if (_clip.hitFrame == 1) HitFrame?.Invoke(_clip.name);
        }

        void Update()
        {
            if (_clip == null || _finished || Paused || set == null) return;
            if (set.framesPerSecond <= 0f) return;

            _timer += Time.deltaTime * Speed;
            float frameDuration = 1f / set.framesPerSecond;
            while (_timer >= frameDuration)
            {
                _timer -= frameDuration;
                Advance();
                if (_finished) return;
            }
        }

        void Advance()
        {
            int next = _frame + 1;
            if (next >= _clip.frames.Length)
            {
                if (!_clip.loop)
                {
                    _finished = true;
                    ClipFinished?.Invoke(_clip.name);
                    return;
                }
                next = 0;
            }

            _frame = next;
            _renderer.sprite = _clip.frames[_frame];
            if (_frame + 1 == _clip.hitFrame) HitFrame?.Invoke(_clip.name);
        }
    }
}
