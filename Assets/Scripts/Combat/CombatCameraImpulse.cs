using UnityEngine;
using UnityEngine.Rendering;

namespace BuildBuild.Combat
{
    /// <summary>Render-only pulse for heavy impacts; camera transform and overlay HUD stay fixed.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CombatCameraImpulse : MonoBehaviour
    {
        const float Duration = 0.18f;
        const float Cooldown = 0.35f;
        Camera _camera;
        Vector2 _direction, _offset;
        float _amplitude;
        float _age = Duration;
        float _sinceStart = Cooldown;
        bool _applied;
        // Camera.main bi tra cuu moi lan Kick (va mot lan AddComponent o cu rung dau); cache lai, tu tra
        // cuu lai khi camera bi huy/doi scene.
        static Camera _mainCamera;

        static Camera MainCamera()
        {
            if (_mainCamera == null || !_mainCamera.isActiveAndEnabled) _mainCamera = Camera.main;
            return _mainCamera;
        }

        public static void Kick(Vector2 direction, float amplitude)
        {
            if (!CombatFeedback.PresentationOn) return;
            var camera = MainCamera();
            if (camera == null) return;
            if (!camera.TryGetComponent(out CombatCameraImpulse impulse))
                impulse = camera.gameObject.AddComponent<CombatCameraImpulse>();
            // Nearby impacts strengthen the pulse, never add duration or displacement.
            if (impulse._age < Duration)
            {
                if (amplitude > impulse._amplitude)
                {
                    impulse._amplitude = amplitude;
                    impulse._direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
                }
                return;
            }
            if (impulse._sinceStart < Cooldown) return;
            impulse._direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
            impulse._amplitude = amplitude;
            // LateUpdate runs after the hit; do not charge the preceding frame's time to a new pulse.
            impulse._age = -Time.unscaledDeltaTime;
            impulse._sinceStart = -Time.unscaledDeltaTime;
        }

        void OnEnable()
        {
            _camera = GetComponent<Camera>();
            RenderPipelineManager.beginCameraRendering += BeginRender;
            RenderPipelineManager.endCameraRendering += EndRender;
        }

        void LateUpdate()
        {
            if (Time.timeScale <= 0f) return;
            _sinceStart += Time.unscaledDeltaTime;
            _age += Time.unscaledDeltaTime;
            if (!CombatFeedback.PresentationOn || _age >= Duration) { _offset = Vector2.zero; return; }
            float fade = 1f - _age / Duration;
            var perpendicular = new Vector2(-_direction.y, _direction.x);
            _offset = (_direction * Mathf.Sin(_age * 95f) + perpendicular * (0.4f * Mathf.Sin(_age * 143f)))
                * (_amplitude * fade * fade);
        }

        void BeginRender(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _camera || !CombatFeedback.PresentationOn || _offset == Vector2.zero) return;
            // Rebuild from the current zoom/aspect, so Conquest pan/zoom never inherits a stale matrix.
            _camera.ResetProjectionMatrix();
            var projection = _camera.projectionMatrix;
            projection.m03 += _offset.x / (_camera.orthographicSize * _camera.aspect);
            projection.m13 += _offset.y / _camera.orthographicSize;
            _camera.projectionMatrix = projection;
            _applied = true;
        }

        void EndRender(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _camera) RestoreProjection();
        }

        void RestoreProjection()
        {
            if (!_applied) return;
            if (_camera != null) _camera.ResetProjectionMatrix();
            _applied = false;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginRender;
            RenderPipelineManager.endCameraRendering -= EndRender;
            RestoreProjection();
            _offset = Vector2.zero;
            _age = Duration;
        }
    }
}
