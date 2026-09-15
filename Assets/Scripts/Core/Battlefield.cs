using System.Collections.Generic;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Core
{
    /// <summary>
    /// Mat dat cua tran dau (kieu Grow Castle): mot DAI co chieu sau, khong phai mot duong thang.
    /// Quai spawn o y bat ky trong dai. Toa do world — SceneBuilder dien tu so do bg-ground.png.
    /// </summary>
    public class Battlefield : MonoBehaviour
    {
        /// <summary>
        /// Chieu sau dai dat ve BEP DOI tren man (cung ti le `FrostZone.GroundSquash`): 1 unit y tren man = 2 unit
        /// tren mat dat. Tam CAN CHIEN do bang so nay — do thang tren man thi hai con dung lech hang sau van
        /// "cham" nhau va danh vao khong khi.
        /// </summary>
        public const float DepthSquash = 0.5f;

        /// <summary>
        /// He so phong to sprite hero/quai/dan/FX theo than (art 1 px = 0,01 unit tren canvas 1920x1080: unit
        /// cao 30-55 px la qua nho). So NGUYEN de pixel deu. Vung ky nang ve dung ban kinh gameplay
        /// (vung bang, vong an ky) KHONG nhan he so nay.
        /// </summary>
        public const float UnitScale = 2f;

        /// <summary>Ban kinh tren mat dat hai unit cung phe khong dung chong len nhau (nua be ngang than ~0,15 o 1x).</summary>
        public const float PersonalSpace = 0.3f * UnitScale;

        /// <summary>
        /// Hitbox cua unit do tu frame idle dau, da nhan `UnitScale`. Do bang DINH mesh Tight (bam alpha), khong
        /// bang `sprite.bounds` — bounds tinh theo ca canvas 128x128, moi unit ra cung ban kinh 0,64:
        /// <paramref name="radius"/> = nua be ngang than (vong tron tren mat dat), <paramref name="height"/> = dinh
        /// dau tinh tu chan (pivot). Khong co art thi lay so cua nhan vat ~0,3 x 0,5 unit o 1x.
        /// </summary>
        public static void MeasureBody(SpriteAnimationSet set, out float radius, out float height)
        {
            radius = 0.15f * UnitScale;
            height = 0.5f * UnitScale;
            if (set == null || !set.idle.IsValid || set.idle.frames[0] == null) return;
            var vertices = set.idle.frames[0].vertices;
            if (vertices.Length == 0) return;
            float minX = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var v in vertices)
            {
                minX = Mathf.Min(minX, v.x);
                maxX = Mathf.Max(maxX, v.x);
                maxY = Mathf.Max(maxY, v.y);
            }
            radius = (maxX - minX) * 0.5f * UnitScale;
            height = maxY * UnitScale;
        }

        [Header("Dai di duoc (world y)")]
        [SerializeField] float laneTop;
        [SerializeField] float laneBottom;

        [Header("Bien ngang (world x)")]
        [Tooltip("Vach tuong phai phao dai — khong unit nao di qua sang trai.")]
        [SerializeField] float leftBound;
        [Tooltip("Mep phai man — chi chan phe ta; quai spawn ngoai mep nay roi di vao.")]
        [SerializeField] float rightBound;

        static Battlefield _current;

        void Awake() => _current = this;

        void OnDestroy()
        {
            if (_current == this) _current = null;
        }

        public float RandomLaneY() => Random.Range(laneBottom, laneTop);

        /// <summary>
        /// Keo vi tri ve trong san: than (<paramref name="radius"/>) khong lan qua vach thanh, chan khong ra khoi
        /// dai y. <paramref name="clampRight"/> = false cho quai (spawn ngoai mep phai). Chua co Battlefield thi giu nguyen.
        /// </summary>
        public static Vector3 Clamp(Vector3 position, float radius, bool clampRight)
        {
            if (_current == null) return position;
            position.x = Mathf.Max(position.x, _current.leftBound + radius);
            if (clampRight) position.x = Mathf.Min(position.x, _current.rightBound - radius);
            position.y = Mathf.Clamp(position.y, _current.laneBottom, _current.laneTop);
            return position;
        }

        public static float GroundDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dy = (a.y - b.y) / DepthSquash;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Do lan chong cua <paramref name="self"/> voi moi unit khac trong danh sach (do tren mat dat), tra ve
        /// huong day ra theo toa do man. Nguoi goi nhan voi toc tach x deltaTime. Trung khit mot diem thi tach
        /// theo x bang instance id de hai con khong cung day ve mot phia.
        /// </summary>
        public static Vector3 Separation<T>(T self, List<T> others) where T : Component
        {
            var push = Vector3.zero;
            var position = self.transform.position;
            foreach (var other in others)
            {
                if (other == null || other == self) continue;
                float dx = position.x - other.transform.position.x;
                float dy = (position.y - other.transform.position.y) / DepthSquash;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                if (distance >= PersonalSpace) continue;

                var away = distance > 0.0001f
                    ? new Vector2(dx, dy) / distance
                    : new Vector2(self.GetInstanceID() > other.GetInstanceID() ? 1f : -1f, 0f);
                float overlap = PersonalSpace - distance;
                push.x += away.x * overlap;
                push.y += away.y * overlap * DepthSquash;   // ve lai toa do man
            }
            return push;
        }
    }
}
