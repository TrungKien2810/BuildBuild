using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BuildBuild.Core
{
    /// <summary>
    /// Khung tran Chinh Phat (nguoi dung chot 2026-10-01): camera lui ra 1,5x tren nen rieng 2880x1620 "Chien Dia Bien
    /// Gioi" thay cho nen tran wave. The gioi KHONG doi ti le (1 px nen = 0,01 unit nhu tran wave) — nen dat sao cho
    /// thanh minh dung yen cho cu, camera + dai dat doi theo. SceneBuilder dien moi toa do. Bong may va dan qua do lop
    /// nay chay; co / khoi la `BackdropLoop` con cua nen.
    /// Zoom (nguoi dung 2026-10-02): cuon chuot / chum hai ngon phong to quanh diem tro toi `MaxZoom`; chuot phai / hai
    /// ngon keo de lia. Khung nhin luon nam trong khung toan canh — khong bao gio lo ra ngoai nen.
    /// </summary>
    public sealed class ConquestArena : MonoBehaviour
    {
        static ConquestArena _current;

        [SerializeField] GameObject[] waveBackdrop;
        [SerializeField] GameObject conquestBackdrop;
        [SerializeField] Vector3 cameraCentre;
        [SerializeField] float cameraSize;
        [Header("Dai di duoc tren nen Chinh Phat (world y)")]
        [SerializeField] float laneTop;
        [SerializeField] float laneBottom;

        [Header("Bong may: troi sang phai, ra khoi mep phai thi quay lai mep trai")]
        [SerializeField] Transform[] clouds;
        [SerializeField] float[] cloudSpeeds;

        [Header("Dan qua bay ngang mep tren, phai sang trai")]
        [SerializeField] Sprite[] crowFrames;
        [SerializeField] float crowFps = 10f;
        [SerializeField] int crowOrder;
        [SerializeField] Vector2 crowBandY;       // world y thap / cao cua dan
        [SerializeField] Vector2 crowGap = new Vector2(20f, 40f);
        [SerializeField] float left;
        [SerializeField] float right;

        Camera _camera;
        Vector3 _homePosition;
        float _homeSize;
        readonly List<Transform> _crows = new List<Transform>();
        readonly List<float> _crowSpeeds = new List<float>();
        float _nextFlock;

        const float Pixel = 0.02f;   // nen ve tren luoi 2 px
        const float MaxZoom = 2.5f;     // phong to toi da so voi khung toan canh
        const float WheelStep = 1.15f;  // moi nac cuon chuot

        float _pinchDistance;
        Vector2 _pinchCentre;
        Vector2 _dragFrom;

        void Awake() => _current = this;

        void OnDestroy()
        {
            if (_current == this) _current = null;
        }

        /// <summary>Bat / tat khung Chinh Phat. Goi TRUOC khi dung thanh NPC — truc doi xung lay theo camera.</summary>
        public static void Show(bool on)
        {
            if (_current != null) _current.Apply(on);
        }

        void Apply(bool on)
        {
            // Roi scene giua tran: WaveManager.OnDisable goi End khi nen / camera co the da bi huy.
            if (conquestBackdrop == null || conquestBackdrop.activeSelf == on) return;
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null) return;
                _homePosition = _camera.transform.position;
                _homeSize = _camera.orthographicSize;
            }

            foreach (var go in waveBackdrop) if (go != null) go.SetActive(!on);
            conquestBackdrop.SetActive(on);
            _camera.transform.position = on ? new Vector3(cameraCentre.x, cameraCentre.y, _homePosition.z) : _homePosition;
            _camera.orthographicSize = on ? cameraSize : _homeSize;
            Battlefield.OverrideLane(on ? new Vector2(laneBottom, laneTop) : (Vector2?)null);

            foreach (var crow in _crows) Destroy(crow.gameObject);
            _crows.Clear();
            _crowSpeeds.Clear();
            _nextFlock = Time.time + Random.Range(3f, 8f);
        }

        void Update()
        {
            if (!conquestBackdrop.activeSelf) return;
            UpdateZoom();

            for (int i = 0; i < clouds.Length; i++)
            {
                var p = clouds[i].position;
                float halfWidth = clouds[i].GetComponent<SpriteRenderer>().bounds.extents.x;
                p.x += cloudSpeeds[i] * Time.deltaTime;
                if (p.x - halfWidth > right + 0.5f) p.x = left - halfWidth - 0.5f;
                clouds[i].position = p;
            }

            for (int i = _crows.Count - 1; i >= 0; i--)
            {
                var p = _crows[i].position;
                p.x -= _crowSpeeds[i] * Time.deltaTime;
                if (p.x < left - 0.6f)
                {
                    Destroy(_crows[i].gameObject);
                    _crows.RemoveAt(i);
                    _crowSpeeds.RemoveAt(i);
                    continue;
                }
                _crows[i].position = p;
                // Nhap nho len xuong + ghim luoi 2 px: con chau dung con, khong nhoe.
                var child = _crows[i].GetChild(0);
                float bob = Mathf.Sin(Time.time * 2f + i) * 0.06f;
                child.position = new Vector3(Snap(p.x), Snap(p.y + bob), p.z);
            }

            if (_crows.Count == 0 && Time.time >= _nextFlock) SpawnFlock();
        }

        void SpawnFlock()
        {
            int count = Random.Range(3, 6);
            float y = Random.Range(crowBandY.x, crowBandY.y);
            for (int i = 0; i < count; i++)
            {
                var root = new GameObject("Crow").transform;
                root.SetParent(conquestBackdrop.transform, false);
                root.position = new Vector3(right + 0.4f + i * 0.5f + Random.value * 0.3f, y + Random.Range(-0.2f, 0.2f), 0f);
                var body = new GameObject("Body", typeof(SpriteRenderer));
                body.transform.SetParent(root, false);
                var sr = body.GetComponent<SpriteRenderer>();
                sr.sprite = crowFrames[0];
                sr.sortingOrder = crowOrder;
                body.AddComponent<BackdropLoop>().Setup(crowFrames, crowFps);
                _crows.Add(root);
                _crowSpeeds.Add(Random.Range(1.7f, 2.1f));
            }
            _nextFlock = Time.time + Random.Range(crowGap.x, crowGap.y);
        }

        void UpdateZoom()
        {
            if (_camera == null) return;
            var touches = Touchscreen.current;
            if (touches != null && touches.touches[0].press.isPressed && touches.touches[1].press.isPressed)
            {
                Vector2 a = touches.touches[0].position.ReadValue(), b = touches.touches[1].position.ReadValue();
                float distance = Vector2.Distance(a, b);
                var centre = (a + b) * 0.5f;
                if (_pinchDistance > 0f)
                {
                    Pan(_pinchCentre, centre);
                    ZoomAt(centre, distance / _pinchDistance);
                }
                _pinchDistance = distance;
                _pinchCentre = centre;
                return;
            }
            _pinchDistance = 0f;

            var mouse = Mouse.current;
            if (mouse == null) return;
            var pointer = mouse.position.ReadValue();
            float scroll = mouse.scroll.ReadValue().y;
            // Cuon tren bang UI (ban do, popup) la cuon UI, khong zoom san.
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (scroll != 0f && !overUi) ZoomAt(pointer, scroll > 0f ? WheelStep : 1f / WheelStep);
            // Chuot phai keo = lia; chuot trai danh cho keo tha hero / linh.
            if (mouse.rightButton.wasPressedThisFrame) _dragFrom = pointer;
            else if (mouse.rightButton.isPressed)
            {
                Pan(_dragFrom, pointer);
                _dragFrom = pointer;
            }
        }

        /// <summary>Doi co nhin, giu nguyen diem the gioi duoi <paramref name="screen"/>.</summary>
        void ZoomAt(Vector2 screen, float factor)
        {
            var before = _camera.ScreenToWorldPoint(screen);
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize / factor, cameraSize / MaxZoom, cameraSize);
            var after = _camera.ScreenToWorldPoint(screen);
            _camera.transform.position += new Vector3(before.x - after.x, before.y - after.y, 0f);
            ClampView();
        }

        void Pan(Vector2 fromScreen, Vector2 toScreen)
        {
            var delta = _camera.ScreenToWorldPoint(fromScreen) - _camera.ScreenToWorldPoint(toScreen);
            _camera.transform.position += new Vector3(delta.x, delta.y, 0f);
            ClampView();
        }

        /// <summary>Khung nhin nam tron trong khung toan canh (tam `cameraCentre`, co `cameraSize`).</summary>
        void ClampView()
        {
            float spareY = cameraSize - _camera.orthographicSize;
            float spareX = spareY * _camera.aspect;
            var p = _camera.transform.position;
            p.x = Mathf.Clamp(p.x, cameraCentre.x - spareX, cameraCentre.x + spareX);
            p.y = Mathf.Clamp(p.y, cameraCentre.y - spareY, cameraCentre.y + spareY);
            _camera.transform.position = p;
        }

        static float Snap(float v) => Mathf.Round(v / Pixel) * Pixel;
    }
}
