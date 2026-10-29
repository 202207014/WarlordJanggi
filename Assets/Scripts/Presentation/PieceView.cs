using System;
using System.Collections;
using TMPro;
using UnityEngine;
using WarlordJanggi.Core;

namespace WarlordJanggi.Presentation
{
    /// <summary>
    /// 3D 기물 뷰 컴포넌트
    /// - 3D 외형, 진영별 네온 베이스, 한자 표식
    /// - 부드러운 이동(도약 포물선), 선택 하이라이트 연출
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PieceView : MonoBehaviour
    {
        public Piece Data { get; private set; }
        public BoardCoord CurrentCoord => Data.Coord;

        private Material _baseMaterial;
        private Transform _bodyTransform;
        private TextMeshPro _labelTMP;
        private bool _isMoving;

        private static readonly Color ChoColor = new Color(0.1f, 0.62f, 1f); // 청룡군 네온 청색
        private static readonly Color HanColor = new Color(1f, 0.21f, 0.21f); // 홍마군 진홍색

        public void Initialize(Piece pieceData, Vector3 initialWorldPos)
        {
            Data = pieceData;
            transform.position = initialWorldPos;
            name = $"Piece_{pieceData.Camp}_{pieceData.Type}_{pieceData.Id}";

            BuildVisualRepresentation();
        }

        private void BuildVisualRepresentation()
        {
            // 1. 기물 바닥 네온 링 베이스 (원판)
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "NeonBase";
            baseObj.transform.SetParent(transform, false);
            baseObj.transform.localPosition = new Vector3(0, 0.04f, 0);
            baseObj.transform.localScale = new Vector3(0.55f, 0.04f, 0.55f);

            var baseCol = baseObj.GetComponent<Collider>();
            if (baseCol != null) Destroy(baseCol);

            var baseRend = baseObj.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            _baseMaterial = new Material(shader)
            {
                color = (Data.Camp == CampType.Cho) ? ChoColor : HanColor
            };
            baseRend.material = _baseMaterial;

            // 2. 기물 본체 (기물 종류별 높이와 형태 차별화)
            float bodyHeight = GetHeightForPieceType(Data.Type);
            var bodyObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bodyObj.name = "Body";
            bodyObj.transform.SetParent(transform, false);
            bodyObj.transform.localPosition = new Vector3(0, bodyHeight * 0.5f + 0.08f, 0);
            bodyObj.transform.localScale = new Vector3(0.42f, bodyHeight * 0.5f, 0.42f);

            var bodyCol = bodyObj.GetComponent<Collider>();
            if (bodyCol != null) Destroy(bodyCol);

            var bodyRend = bodyObj.GetComponent<MeshRenderer>();
            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var bodyMat = new Material(litShader)
            {
                color = (Data.Camp == CampType.Cho) ? new Color(0.12f, 0.2f, 0.32f) : new Color(0.32f, 0.12f, 0.12f)
            };
            bodyRend.material = bodyMat;
            _bodyTransform = bodyObj.transform;

            // 3. 상단 한자 라벨 (TextMeshPro)
            var labelObj = new GameObject("HanjaLabel");
            labelObj.transform.SetParent(transform, false);
            labelObj.transform.localPosition = new Vector3(0, bodyHeight + 0.18f, 0);
            labelObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // 위를 향하도록 회전

            _labelTMP = labelObj.AddComponent<TextMeshPro>();
            _labelTMP.text = Data.Type.GetHanja(Data.Camp);
            _labelTMP.fontSize = 4.5f;
            _labelTMP.alignment = TextAlignmentOptions.Center;
            _labelTMP.color = Color.white;

            // 4. 클릭 감지용 박스 컬라이더 설정
            var collider = GetComponent<BoxCollider>();
            if (collider == null) collider = gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, bodyHeight * 0.5f, 0);
            collider.size = new Vector3(0.6f, bodyHeight + 0.2f, 0.6f);
        }

        private static float GetHeightForPieceType(PieceType type)
        {
            return type switch
            {
                PieceType.General => 0.55f, // 궁이 가장 큼
                PieceType.Chariot => 0.45f,
                PieceType.Cannon => 0.42f,
                PieceType.Horse => 0.40f,
                PieceType.Elephant => 0.42f,
                PieceType.Guard => 0.35f,
                PieceType.Soldier => 0.28f, // 졸이 가장 아담함
                _ => 0.35f
            };
        }

        /// <summary>
        /// 선택되었을 때 위로 살짝 부유하는 효과
        /// </summary>
        public void SetSelected(bool isSelected)
        {
            float targetY = isSelected ? 0.25f : 0f;
            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
        }

        /// <summary>
        /// 목표 좌표로 포물선을 그리며 부드럽게 도약 이동
        /// </summary>
        public void AnimateMoveTo(Vector3 targetWorldPos, Action onComplete = null)
        {
            if (_isMoving) return;
            StartCoroutine(CoJumpMove(targetWorldPos, 0.32f, onComplete));
        }

        private IEnumerator CoJumpMove(Vector3 targetPos, float duration, Action onComplete)
        {
            _isMoving = true;
            Vector3 startPos = transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // 부드러운 수평 보간
                Vector3 current = Vector3.Lerp(startPos, targetPos, t);
                // 포물선 점프 곡선 (sin)
                current.y += Mathf.Sin(t * Mathf.PI) * 0.4f;

                transform.position = current;
                yield return null;
            }

            transform.position = targetPos;
            _isMoving = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 피격 포획 소멸 연출
        /// </summary>
        public void AnimateCaptured(Action onComplete = null)
        {
            StartCoroutine(CoDisappear(0.25f, onComplete));
        }

        private IEnumerator CoDisappear(float duration, Action onComplete)
        {
            float elapsed = 0f;
            Vector3 initScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(initScale, Vector3.zero, t);
                yield return null;
            }

            gameObject.SetActive(false);
            onComplete?.Invoke();
        }
    }
}
