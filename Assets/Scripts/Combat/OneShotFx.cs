using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// FX khong gay sat thuong — nguoi goi tu tinh sat thuong, FX chi de nguoi choi thay chuyen vua xay ra.
    /// Hai kieu: chay MOT lan roi tu huy (vet chem Ironblade, vong an ky Bonecaller), hoac LAP VONG toi
    /// mot moc thoi gian va bam theo cha (dau an ky tren dau quai).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class OneShotFx : MonoBehaviour
    {
        const float FramesPerSecond = 9f;   // cung nhip 110ms/frame voi SpriteAnimationSet va Projectile

        SpriteRenderer _renderer;
        Sprite[] _frames;
        float _timer;
        int _frame;
        float _loopUntil;   // 0 = chay mot lan
        Sprite[] _loopFrames;   // co intro: phan lap sau khi intro chay xong mot lan

        /// <param name="scale">Art ve theo co than unit (vet chem) thi truyen `Battlefield.UnitScale`; art ve dung ban kinh gameplay thi 1.</param>
        public static void Spawn(Vector3 position, Sprite[] frames, bool flipX, int sortingOrder, float scale = 1f)
        {
            var fx = Create(frames, sortingOrder);
            if (fx == null) return;
            fx.transform.position = position;
            fx.transform.localScale = Vector3.one * scale;
            fx._renderer.flipX = flipX;
        }

        /// <summary>
        /// Lap vong <paramref name="seconds"/> giay, gan vao <paramref name="parent"/> (cha bi xoa thi FX mat theo).
        /// <paramref name="intro"/> (tuy chon) chay MOT lan truoc phan lap — vd. vo da moc len roi dung yen.
        /// </summary>
        public static GameObject SpawnLoop(Transform parent, Vector3 localOffset, Sprite[] frames, float seconds, int sortingOrder,
            Sprite[] intro = null)
        {
            bool hasIntro = intro != null && intro.Length > 0;
            var fx = Create(hasIntro ? intro : frames, sortingOrder);
            if (fx == null) return null;
            if (hasIntro && frames != null && frames.Length > 0) fx._loopFrames = frames;
            fx.transform.SetParent(parent, false);
            fx.transform.localPosition = localOffset;
            fx._loopUntil = Time.time + seconds;
            return fx.gameObject;
        }

        static OneShotFx Create(Sprite[] frames, int sortingOrder)
        {
            if (frames == null || frames.Length == 0) return null;

            var go = new GameObject("Fx");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.sprite = frames[0];

            var fx = go.AddComponent<OneShotFx>();
            fx._renderer = renderer;
            fx._frames = frames;
            return fx;
        }

        void Update()
        {
            if (_loopUntil > 0f && Time.time >= _loopUntil)
            {
                Destroy(gameObject);
                return;
            }

            _timer += Time.deltaTime;
            float frameDuration = 1f / FramesPerSecond;
            while (_timer >= frameDuration)
            {
                _timer -= frameDuration;
                _frame++;
                if (_frame >= _frames.Length)
                {
                    if (_loopUntil <= 0f)
                    {
                        Destroy(gameObject);
                        return;
                    }
                    if (_loopFrames != null)
                    {
                        _frames = _loopFrames;
                        _loopFrames = null;
                    }
                    _frame = 0;
                }
                _renderer.sprite = _frames[_frame];
            }
        }
    }
}
