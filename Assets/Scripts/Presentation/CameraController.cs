using UnityEngine;
using UnityEngine.InputSystem;

namespace WarlordJanggi.Presentation
{
    /// <summary>
    /// 하이브리드 카메라 시스템 컨트롤러
    /// - 3D 쿼터뷰 (45° 전장 시네마틱 뷰) ↔ 2D 탑다운 뷰 (90° 판세 분석용 뷰) 전환
    /// - 마우스 휠 줌 인/아웃 및 부드러운 전환 보간 (New Input System 호환)
    /// </summary>
    public sealed class CameraController : MonoBehaviour
    {
        private static CameraController _instance;
        public static CameraController Instance => _instance;

        [Header("카메라 시점 프리셋")]
        [SerializeField] private Vector3 quarterViewOffset = new Vector3(0f, 9.5f, -9.5f);
        [SerializeField] private Vector3 quarterViewEuler = new Vector3(45f, 0f, 0f);

        [SerializeField] private Vector3 topDownOffset = new Vector3(0f, 13.5f, 0f);
        [SerializeField] private Vector3 topDownEuler = new Vector3(90f, 0f, 0f);

        [Header("줌 & 조작 설정")]
        [SerializeField] private float zoomSpeed = 2.5f;
        [SerializeField] private float minZoomDist = 5f;
        [SerializeField] private float maxZoomDist = 18f;
        [SerializeField] private float transitionDuration = 0.4f;

        private Camera _mainCamera;
        private bool _isTopDown;
        private float _transitionProgress = 1f;

        private Vector3 _startPos;
        private Quaternion _startRot;
        private Vector3 _targetPos;
        private Quaternion _targetRot;

        // 카메라 쉐이크 변수
        private float _shakeTimer;
        private float _shakeIntensity;
        private Vector3 _shakeOffset;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            _mainCamera = GetComponent<Camera>();
            if (_mainCamera == null) _mainCamera = Camera.main;

            ApplyPresetImmediate(false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                // 단축키 C 또는 스페이스바로 시점 전환
                if (keyboard.cKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                {
                    ToggleViewMode();
                }
            }

            // 마우스 휠 줌
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    ApplyZoom(Mathf.Sign(scroll) * 0.5f);
                }
            }

            // 시점 전환 보간
            if (_transitionProgress < 1f)
            {
                _transitionProgress += Time.deltaTime / transitionDuration;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_transitionProgress));

                transform.position = Vector3.Lerp(_startPos, _targetPos, t);
                transform.rotation = Quaternion.Slerp(_startRot, _targetRot, t);
            }

            // 카메라 쉐이크 처리
            if (_shakeTimer > 0f)
            {
                _shakeTimer -= Time.deltaTime;
                float factor = _shakeTimer / 0.35f;
                _shakeOffset = Random.insideUnitSphere * (_shakeIntensity * factor);
                transform.position += _shakeOffset;
            }
        }

        /// <summary>
        /// 타격 순간 카메라 쉐이크 트리거
        /// </summary>
        public void TriggerShake(float intensity = 0.3f, float duration = 0.3f)
        {
            _shakeIntensity = intensity;
            _shakeTimer = duration;
        }

        public void ToggleViewMode()
        {
            SetViewMode(!_isTopDown);
        }

        public void SetViewMode(bool topDown)
        {
            _isTopDown = topDown;
            _startPos = transform.position;
            _startRot = transform.rotation;

            _targetPos = _isTopDown ? topDownOffset : quarterViewOffset;
            _targetRot = Quaternion.Euler(_isTopDown ? topDownEuler : quarterViewEuler);
            _transitionProgress = 0f;
        }

        private void ApplyPresetImmediate(bool topDown)
        {
            _isTopDown = topDown;
            transform.position = _isTopDown ? topDownOffset : quarterViewOffset;
            transform.rotation = Quaternion.Euler(_isTopDown ? topDownEuler : quarterViewEuler);
            _transitionProgress = 1f;
        }

        private void ApplyZoom(float delta)
        {
            Vector3 forward = transform.forward;
            Vector3 newPos = transform.position + forward * (delta * zoomSpeed);

            float dist = newPos.magnitude;
            if (dist >= minZoomDist && dist <= maxZoomDist)
            {
                transform.position = newPos;
                if (_isTopDown) topDownOffset = newPos;
                else quarterViewOffset = newPos;
            }
        }
    }
}
