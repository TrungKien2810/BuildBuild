using BuildBuild.Combat;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Mot cho dung o bai dat sau thanh (GAME_DESIGN.md muc 2). Giong o hero (nguoi dung 2026-09-28): giua wave cham cho
    /// co linh -> bo chon, cham cho trong -> bang chon linh (`SoldierBoard`); cho trong hien khung vet tren dat.
    /// Trong tran: con linh dung idle, tren dau la vong HOI CHIEU. Vong day -> KEO con linh ra san va tha: `SoldierCamp`
    /// sinh nguyen top tai diem tha. Vong chua day thi linh o bai xam lai, keo ra chi thay bong do, tha khong ra gi.
    ///
    /// Bat cham/keo qua Physics2DRaycaster tren camera (can BoxCollider2D) nhu `HeroSlot`. Moi quyet dinh (doi linh, vong
    /// day, diem tha hop le) nam o `SoldierCamp` / `SoldierBoard` — lop nay chi ve va chuyen toa do.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class SoldierPost : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        static readonly Color Unavailable = new Color(0.45f, 0.45f, 0.5f);
        static readonly Color GhostOk = new Color(1f, 1f, 1f, 0.6f);
        static readonly Color GhostBad = new Color(1f, 0.35f, 0.35f, 0.5f);

        [SerializeField] SoldierCamp camp;
        [SerializeField] SoldierBoard board;
        [Tooltip("Thu tu cho dung o bai, tu tren xuong (`SaveData.soldiers`).")]
        [SerializeField] int index;
        [SerializeField] SpriteRenderer body;
        [Tooltip("Khung vet tren dat — chi hien khi cho TRONG va dang chuan bi giua wave.")]
        [SerializeField] SpriteRenderer frame;
        [Tooltip("Vong hoi chieu (Image Filled Radial360) tren canvas world-space tren dau linh.")]
        [SerializeField] Image cooldownFill;

        SpriteRenderer _ghost;
        SpriteAnimator _animator;
        Canvas _ringCanvas;
        SoldierDefinition _shown;
        bool _dragging;

        public int Index => index;

        void Start()
        {
            if (body != null) _animator = body.GetComponent<SpriteAnimator>();
            // Cache: `Image.canvas` tra null khi canvas da bi tat.
            if (cooldownFill != null) _ringCanvas = cooldownFill.GetComponentInParent<Canvas>();
        }

        void Update()
        {
            if (camp == null) return;
            // Doi linh doi giua wave (bang chon) nen doc moi frame; doi loai thi doi bo animation.
            var soldier = camp.Assigned(index);
            if (soldier != _shown)
            {
                _shown = soldier;
                if (soldier != null && _animator != null && soldier.animations != null) _animator.Bind(soldier.animations);
                // Linh o bai nhin ve phia san (ben phai). Art linh ve quay phai thi khong lat.
                if (soldier != null && body != null) body.flipX = !soldier.artFacesRight;
            }

            bool has = soldier != null;
            if (body != null) body.enabled = has;
            if (_ringCanvas != null) _ringCanvas.enabled = has;
            if (frame != null) frame.enabled = !has && !WaveManager.Fighting;
            if (!has) return;

            bool ready = camp.CanSummon(index);
            if (cooldownFill != null) cooldownFill.fillAmount = camp.CooldownFraction(index);
            // Ngoai tran van de mau goc: xam ca bai luc chuan bi thi doc ra nhu linh bi khoa.
            if (body != null) body.color = ready || !WaveManager.Fighting ? Color.white : Unavailable;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (board != null) board.OnPostClicked(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (camp == null || body == null || !WaveManager.Fighting || camp.Assigned(index) == null) return;
            _dragging = true;
            if (_ghost == null)
            {
                var go = new GameObject("SoldierGhost");
                _ghost = go.AddComponent<SpriteRenderer>();
                _ghost.sortingOrder = SortOrder.Air;
            }
            _ghost.flipX = body.flipX;  // bong nhin cung huong con linh o bai (ve phia quai)
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
