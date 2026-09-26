using BuildBuild.Combat;
using BuildBuild.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Cho dung cua MOT loai linh o bai dat sau thanh (GAME_DESIGN.md muc 2): con linh dung idle, tren dau la vong
    /// HOI CHIEU + gia tai nguyen. Vong day + du tai nguyen -> KEO con linh ra san va tha: `SoldierCamp` sinh
    /// nguyen top tai diem tha. Chua trieu hoi duoc thi linh o bai xam lai, keo ra chi thay bong do, tha khong ra gi.
    ///
    /// Bat keo qua Physics2DRaycaster tren camera (can BoxCollider2D) nhu `HeroSlot`. Moi quyet dinh (du tien, diem
    /// tha hop le) nam o `SoldierCamp` — lop nay chi ve va chuyen toa do.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class SoldierPost : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        static readonly Color Unavailable = new Color(0.45f, 0.45f, 0.5f);
        static readonly Color GhostOk = new Color(1f, 1f, 1f, 0.6f);
        static readonly Color GhostBad = new Color(1f, 0.35f, 0.35f, 0.5f);
        static readonly Color CostOk = Color.white;
        static readonly Color CostShort = new Color(1f, 0.45f, 0.4f);

        [SerializeField] SoldierCamp camp;
        [Tooltip("Chi so cua loai linh trong `SoldierCamp.roster`.")]
        [SerializeField] int index;
        [SerializeField] SpriteRenderer body;
        [Tooltip("Vong hoi chieu (Image Filled Radial360) tren canvas world-space tren dau linh.")]
        [SerializeField] Image cooldownFill;
        [SerializeField] Text costText;

        SpriteRenderer _ghost;
        bool _dragging;

        void Start()
        {
            if (costText != null && camp != null && index < camp.RosterCount)
                costText.text = camp.Soldier(index).summonCost.ToString();
        }

        void Update()
        {
            if (camp == null || index >= camp.RosterCount) return;
            bool ready = camp.CanSummon(index);
            if (cooldownFill != null) cooldownFill.fillAmount = camp.CooldownFraction(index);
            if (costText != null) costText.color = camp.CanAfford(index) ? CostOk : CostShort;
            // Ngoai tran van de mau goc: xam ca bai luc chuan bi thi doc ra nhu linh bi khoa.
            if (body != null) body.color = ready || !WaveManager.Fighting ? Color.white : Unavailable;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (camp == null || body == null || !WaveManager.Fighting) return;
            _dragging = true;
            if (_ghost == null)
            {
                var go = new GameObject("SoldierGhost");
                _ghost = go.AddComponent<SpriteRenderer>();
                _ghost.sortingOrder = SortOrder.Air;
                _ghost.flipX = true;    // art quay trai; linh ra san nhin ve phia quai
            }
            _ghost.sprite = body.sprite;
            _ghost.transform.localScale = body.transform.lossyScale;
            _ghost.gameObject.SetActive(true);
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || _ghost == null) return;
            var point = ToWorld(eventData);
            _ghost.transform.position = point;
            _ghost.color = camp.CanSummon(index) && Battlefield.OnField(point) ? GhostOk : GhostBad;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            _dragging = false;
            if (_ghost != null) _ghost.gameObject.SetActive(false);
            camp.TrySummon(index, ToWorld(eventData));
        }

        void OnDisable()
        {
            _dragging = false;
            if (_ghost != null) _ghost.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (_ghost != null) Destroy(_ghost.gameObject);
        }

        static Vector3 ToWorld(PointerEventData eventData)
        {
            var cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : Camera.main;
            var point = cam.ScreenToWorldPoint(eventData.position);
            point.z = 0f;
            return point;
        }
    }
}
