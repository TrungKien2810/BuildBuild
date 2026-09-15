using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Vung bang cua Frostweaver: mot dia dat xuong san, moi con quai DUNG TRONG dia bi dong bang
    /// (khong di, khong danh) cho toi khi dia tan. Vung KHONG gay sat thuong — no la don khong che;
    /// phan sat thuong dien rong da thuoc ve dan thuong (`Projectile`).
    ///
    /// Vung giu quyen dem gio: moi frame no bao cho quai "con bi dong bang them <thoi gian con lai>
    /// giay". Nho vay quai tan bang DUNG luc dia tan, va hai vung chong nhau tu cong don qua
    /// `Mathf.Max` ben `Enemy.ApplyFreeze` — khong can dem so nguon dong bang.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class FrostZone : MonoBehaviour
    {
        const float FramesPerSecond = 9f;   // cung nhip 110ms/frame voi SpriteAnimationSet va Projectile

        /// <summary>
        /// Dia nam TREN MAT DAT nen no la hinh BAU DUC, khong phai hinh tron: dai dat chi sau 1,6
        /// unit (canvas y 870-1030, xem AD-9) trong khi dia rong 3,2 unit — dia tron that se tran
        /// gap doi ra ngoai dai, len ca nen troi. 0,5 cho chieu cao 1,6 unit = dung be day dai dat.
        /// Phep do dong bang dung DUNG ti le nay: nguoi choi phai doc duoc con nao trong vung.
        /// </summary>
        const float GroundSquash = 0.5f;

        static Sprite _placeholder;

        SpriteRenderer _renderer;
        Sprite[] _frames;
        float _radius;
        float _remaining;
        float _timer;
        int _frame;

        public static void Spawn(Vector3 position, float radius, float duration, Sprite[] frames)
        {
            var go = new GameObject("FrostZone");
            go.transform.position = position;

            // Duoi quai (order 10) va duoi dan (order 20): dia nam tren MAT DAT, quai dung de len.
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 5;

            go.AddComponent<FrostZone>().Begin(radius, duration, frames);
        }

        void Begin(float radius, float duration, Sprite[] frames)
        {
            _renderer = GetComponent<SpriteRenderer>();
            _radius = radius;
            _remaining = duration;
            _frames = frames != null && frames.Length > 0 ? frames : null;

            if (_frames != null)
            {
                _renderer.sprite = _frames[0];
                return;
            }

            // Chua co art vung bang trong `Assets/Art/Generated/Frostweaver-v2/` — ve tam mot dia tron
            // de tinh nang NHIN THAY duoc ngay thay vi dong bang trong vo hinh. Placeholder duoc dung
            // o 1 unit / be rong nen scale thang theo ban kinh; khi co frame that do vao
            // `HeroDefinition.skillFrames` thi nhanh nay khong chay nua (va art that giu nguyen scale).
            _renderer.sprite = Placeholder();
            transform.localScale = new Vector3(radius * 2f, radius * 2f * GroundSquash, 1f);
        }

        void Update()
        {
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            foreach (var enemy in Enemy.Alive)
            {
                if (enemy == null || enemy.IsDying) continue;
                if (!Contains(enemy.transform.position)) continue;
                enemy.ApplyFreeze(_remaining);
            }

            Animate();
        }

        /// <summary>Trong bau duc: chuan hoa y theo `GroundSquash` roi so nhu hinh tron.</summary>
        bool Contains(Vector3 position)
        {
            float dx = (position.x - transform.position.x) / _radius;
            float dy = (position.y - transform.position.y) / (_radius * GroundSquash);
            return dx * dx + dy * dy <= 1f;
        }

        void Animate()
        {
            if (_frames == null || _frames.Length < 2) return;

            _timer += Time.deltaTime;
            float frameDuration = 1f / FramesPerSecond;
            while (_timer >= frameDuration)
            {
                _timer -= frameDuration;
                _frame = (_frame + 1) % _frames.Length;
                _renderer.sprite = _frames[_frame];
            }
        }

        static Sprite Placeholder()
        {
            if (_placeholder != null) return _placeholder;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var centre = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre) / (size * 0.5f);
                float alpha = d > 1f ? 0f : d > 0.86f ? 0.8f : 0.3f;   // vien dam, long trong
                texture.SetPixel(x, y, new Color(0.55f, 0.85f, 1f, alpha));
            }
            texture.Apply();

            // pixelsPerUnit = size => sprite rong dung 1 unit, pivot giua. Bep thanh bau duc bang
            // localScale chu khong ve san o day: doi `skillRadius` thi hinh doi theo, khong lech.
            _placeholder = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _placeholder;
        }
    }
}
