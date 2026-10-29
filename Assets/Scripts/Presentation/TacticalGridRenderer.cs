using System.Collections.Generic;
using UnityEngine;
using WarlordJanggi.Core;

namespace WarlordJanggi.Presentation
{
    /// <summary>
    /// 2번째 콘셉트 아트에 맞춘 전술 네온 그리드 시각화 렌더러
    /// - 10x9 기본 장기판 선 및 궁성 'X' 대각선 렌더링
    /// - 선택된 기물의 이동 가능 노드(파란 네온 원 + 연결 라인) 표시
    /// - 포획 가능 적 기물 위치(빨간 네온 타깃 링) 표시
    /// </summary>
    public sealed class TacticalGridRenderer : MonoBehaviour
    {
        [Header("컬러 팔레트 (콘셉트 아트 기준)")]
        [SerializeField] private Color boardLineColor = new Color(0.35f, 0.28f, 0.2f, 0.8f);
        [SerializeField] private Color moveNodeColor = new Color(0.1f, 0.62f, 1f, 0.9f);       // #1A9FFF 청룡군 네온
        [SerializeField] private Color captureRingColor = new Color(1f, 0.2f, 0.2f, 0.95f);    // #FF3535 홍마군 네온
        [SerializeField] private Color guideLineColor = new Color(0.1f, 0.62f, 1f, 0.65f);

        private BoardCoordinateConverter _converter;
        private readonly List<GameObject> _activeMoveNodes = new List<GameObject>(32);
        private readonly List<GameObject> _activeCaptureRings = new List<GameObject>(16);
        private readonly List<LineRenderer> _activeGuideLines = new List<LineRenderer>(16);

        private GameObject _nodePoolParent;
        private Material _lineMaterial;
        private Material _glowNodeMaterial;
        private Material _captureRingMaterial;

        public void Initialize(BoardCoordinateConverter converter)
        {
            _converter = converter;
            _nodePoolParent = new GameObject("[TacticalGrid_Visuals]");
            _nodePoolParent.transform.SetParent(transform);

            InitMaterials();
            BuildStaticBoardLines();
        }

        private void InitMaterials()
        {
            // URP Unlit 또는 기본 스프라이트 셰이더 기반 발광 머티리얼 구성
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            _lineMaterial = new Material(shader) { color = boardLineColor };

            _glowNodeMaterial = new Material(shader) { color = moveNodeColor };
            _captureRingMaterial = new Material(shader) { color = captureRingColor };
        }

        /// <summary>
        /// 기본 10x9 장기판 격자 선 및 궁성 'X' 대각선 절차적 생성
        /// </summary>
        private void BuildStaticBoardLines()
        {
            var linesParent = new GameObject("StaticBoardLines");
            linesParent.transform.SetParent(_nodePoolParent.transform);

            // 0. 마우스 클릭용 투명 바닥 콜라이더 (작전 테이블 평면)
            var boardPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boardPlate.name = "BoardClickPlane";
            boardPlate.transform.SetParent(linesParent.transform);
            boardPlate.transform.position = _converter.BoardCenterOffset + new Vector3(0f, -0.05f, 0f);
            boardPlate.transform.localScale = new Vector3(
                BoardCoord.BoardWidth * _converter.CellSpacingX + 1.5f,
                0.1f,
                BoardCoord.BoardHeight * _converter.CellSpacingZ + 1.5f);

            var plateMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = new Color(0.12f, 0.09f, 0.07f) // 어두운 목재 작전판 컬러 (콘셉트 아트)
            };
            boardPlate.GetComponent<MeshRenderer>().material = plateMat;

            // 1. 가로선 10개 (Y: 0 ~ 9)
            for (int y = 0; y < BoardCoord.BoardHeight; y++)
            {
                var start = _converter.ToWorldPosition(new BoardCoord(0, y), 0.005f);
                var end = _converter.ToWorldPosition(new BoardCoord(BoardCoord.BoardWidth - 1, y), 0.005f);
                CreateLine(linesParent.transform, "HorizLine_" + y, start, end, boardLineColor, 0.02f);
            }

            // 2. 세로선 9개 (X: 0 ~ 8)
            for (int x = 0; x < BoardCoord.BoardWidth; x++)
            {
                var start = _converter.ToWorldPosition(new BoardCoord(x, 0), 0.005f);
                var end = _converter.ToWorldPosition(new BoardCoord(x, BoardCoord.BoardHeight - 1), 0.005f);
                CreateLine(linesParent.transform, "VertLine_" + x, start, end, boardLineColor, 0.02f);
            }

            // 3. 초(Cho) 궁성 대각선 'X' (X: 3~5, Y: 0~2)
            CreateLine(linesParent.transform, "Cho_Diag1",
                _converter.ToWorldPosition(new BoardCoord(3, 0), 0.006f),
                _converter.ToWorldPosition(new BoardCoord(5, 2), 0.006f), boardLineColor, 0.02f);
            CreateLine(linesParent.transform, "Cho_Diag2",
                _converter.ToWorldPosition(new BoardCoord(5, 0), 0.006f),
                _converter.ToWorldPosition(new BoardCoord(3, 2), 0.006f), boardLineColor, 0.02f);

            // 4. 한(Han) 궁성 대각선 'X' (X: 3~5, Y: 7~9)
            CreateLine(linesParent.transform, "Han_Diag1",
                _converter.ToWorldPosition(new BoardCoord(3, 7), 0.006f),
                _converter.ToWorldPosition(new BoardCoord(5, 9), 0.006f), boardLineColor, 0.02f);
            CreateLine(linesParent.transform, "Han_Diag2",
                _converter.ToWorldPosition(new BoardCoord(5, 7), 0.006f),
                _converter.ToWorldPosition(new BoardCoord(3, 9), 0.006f), boardLineColor, 0.02f);
        }

        private LineRenderer CreateLine(Transform parent, string name, Vector3 start, Vector3 end, Color color, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            var lr = go.AddComponent<LineRenderer>();
            lr.material = _lineMaterial;
            lr.startColor = color;
            lr.endColor = color;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            lr.useWorldSpace = true;
            return lr;
        }

        /// <summary>
        /// 선택된 기물의 이동/공격 경로 가이드 표시 (콘셉트 2번 스타일)
        /// </summary>
        public void ShowTacticalGuides(BoardCoord selectedCoord, List<BoardCoord> legalMoves, GameBoard board)
        {
            ClearTacticalGuides();

            var startPos = _converter.ToWorldPosition(selectedCoord, 0.02f);

            for (int i = 0; i < legalMoves.Count; i++)
            {
                var targetCoord = legalMoves[i];
                var targetPos = _converter.ToWorldPosition(targetCoord, 0.02f);
                var targetPiece = board.GetPieceAt(targetCoord);

                if (targetPiece != null)
                {
                    // 포획 가능 적: 빨간색 네온 타깃 링 표시
                    var ring = CreateCaptureRing(targetPos);
                    _activeCaptureRings.Add(ring);
                }
                else
                {
                    // 빈 칸 이동: 파란색 네온 원 노드 표시
                    var node = CreateMoveNode(targetPos);
                    _activeMoveNodes.Add(node);
                }

                // 기물 위치로부터 목적지 노드로 이어지는 네온 가이드 라인
                var guideLine = CreateLine(_nodePoolParent.transform, "GuideLine", startPos, targetPos, guideLineColor, 0.015f);
                _activeGuideLines.Add(guideLine);
            }
        }

        /// <summary>
        /// 모든 가이드 표시 제거
        /// </summary>
        public void ClearTacticalGuides()
        {
            for (int i = 0; i < _activeMoveNodes.Count; i++) Destroy(_activeMoveNodes[i]);
            _activeMoveNodes.Clear();

            for (int i = 0; i < _activeCaptureRings.Count; i++) Destroy(_activeCaptureRings[i]);
            _activeCaptureRings.Clear();

            for (int i = 0; i < _activeGuideLines.Count; i++) Destroy(_activeGuideLines[i].gameObject);
            _activeGuideLines.Clear();
        }

        private GameObject CreateMoveNode(Vector3 position)
        {
            var node = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            node.name = "MoveNode";
            node.transform.SetParent(_nodePoolParent.transform);
            node.transform.position = position;
            node.transform.localScale = new Vector3(0.35f, 0.005f, 0.35f);

            var col = node.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            var renderer = node.GetComponent<MeshRenderer>();
            renderer.material = _glowNodeMaterial;
            return node;
        }

        private GameObject CreateCaptureRing(Vector3 position)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ring.name = "CaptureRing";
            ring.transform.SetParent(_nodePoolParent.transform);
            ring.transform.position = position;
            ring.transform.localScale = new Vector3(0.55f, 0.01f, 0.55f);

            var col = ring.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            var renderer = ring.GetComponent<MeshRenderer>();
            renderer.material = _captureRingMaterial;
            return ring;
        }
    }
}
