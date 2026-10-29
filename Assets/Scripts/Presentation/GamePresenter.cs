using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using WarlordJanggi.Core;

namespace WarlordJanggi.Presentation
{
    /// <summary>
    /// Phase 2 메인 게임 프레젠터
    /// - 룰 엔진, 3D 기물 뷰, 네온 그리드 렌더러, 카메라, UI를 통합 제어
    /// - 마우스/터치 클릭을 통한 기물 선택 및 합법수 착점 인터랙션 처리
    /// </summary>
    public sealed class GamePresenter : MonoBehaviour
    {
        [Header("보드 규격 설정")]
        [SerializeField] private float cellSpacing = 1.25f;
        [SerializeField] private Vector3 boardCenterOffset = Vector3.zero;

        [Header("컴포넌트 참조 (미할당 시 자동 부착)")]
        [SerializeField] private TacticalGridRenderer gridRenderer;
        [SerializeField] private CameraController cameraController;

        private RuleEngine _engine;
        private BoardCoordinateConverter _converter;
        private readonly Dictionary<int, PieceView> _pieceViews = new Dictionary<int, PieceView>(32);

        private PieceView _selectedPieceView;
        private readonly List<BoardCoord> _currentLegalMoves = new List<BoardCoord>(16);
        private readonly List<string> _battleLogs = new List<string>(16);

        private Camera _camera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapInPlayMode()
        {
            if (FindAnyObjectByType<GamePresenter>() == null)
            {
                var bootstrapper = new GameObject("[WarlordJanggi_GamePresenter]");
                bootstrapper.AddComponent<GamePresenter>();
                Debug.Log("<b>[Warlord Janggi]</b> Phase 2 3D 전장 시스템이 자동으로 초기화되었습니다.");
            }
        }

        private void Start()
        {
            _camera = Camera.main;
            _converter = new BoardCoordinateConverter(cellSpacing, boardCenterOffset);

            // 컴포넌트 자동 준비
            if (gridRenderer == null)
            {
                gridRenderer = gameObject.AddComponent<TacticalGridRenderer>();
            }
            gridRenderer.Initialize(_converter);

            if (cameraController == null && _camera != null)
            {
                cameraController = _camera.gameObject.AddComponent<CameraController>();
            }

            // Phase 3 전투 타격 VFX 매니저 준비
            if (FindAnyObjectByType<CombatFXManager>() == null)
            {
                var fxObj = new GameObject("[WarlordJanggi_CombatFX]");
                fxObj.AddComponent<CombatFXManager>();
            }

            // 룰 엔진 초기화 및 대국 시작
            _engine = new RuleEngine();
            _engine.OnMoveExecuted += HandleMoveExecuted;
            _engine.OnTurnChanged += HandleTurnChanged;
            _engine.OnCheck += HandleCheck;
            _engine.OnGameOver += HandleGameOver;

            _engine.StartGame();

            SpawnAllPieceViews();
            AddBattleLog("대국이 시작되었습니다. [청룡군(초) 선수]");
        }

        private void SpawnAllPieceViews()
        {
            var piecesParent = new GameObject("[Pieces_Root]").transform;
            piecesParent.SetParent(transform);

            SpawnPiecesFromList(_engine.Board.ChoPieces, piecesParent);
            SpawnPiecesFromList(_engine.Board.HanPieces, piecesParent);
        }

        private void SpawnPiecesFromList(IReadOnlyList<Piece> pieces, Transform parent)
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                var piece = pieces[i];
                var go = new GameObject();
                go.transform.SetParent(parent);

                var view = go.AddComponent<PieceView>();
                view.Initialize(piece, _converter.ToWorldPosition(piece.Coord));
                _pieceViews[piece.Id] = view;
            }
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                OnPrimaryClick(mouse.position.ReadValue());
            }

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                // Z키: 무르기 (Undo)
                if (keyboard.zKey.wasPressedThisFrame)
                {
                    UndoMove();
                }

                // P키: 한수 쉼 (Pass)
                if (keyboard.pKey.wasPressedThisFrame)
                {
                    PassTurn();
                }
            }
        }

        private void OnPrimaryClick(Vector2 screenPosition)
        {
            if (_camera == null) return;

            Ray ray = _camera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out var hit, 100f))
            {
                // 1. 기물을 직접 클릭한 경우
                var clickedView = hit.collider.GetComponentInParent<PieceView>();
                if (clickedView != null)
                {
                    HandlePieceClicked(clickedView);
                    return;
                }

                // 2. 바닥(교차점)을 클릭한 경우
                if (_selectedPieceView != null &&
                    _converter.TryToBoardCoord(hit.point, out var targetCoord))
                {
                    TryExecuteMoveTo(targetCoord);
                    return;
                }
            }

            // 허공 클릭 시 선택 해제
            DeselectPiece();
        }

        private void HandlePieceClicked(PieceView clickedView)
        {
            // 자신의 턴 기물을 선택한 경우
            if (clickedView.Data.Camp == _engine.CurrentTurn)
            {
                SelectPiece(clickedView);
                return;
            }

            // 상대 기물을 클릭했는데, 이미 아군 기물이 선택되어 있고 그 상대가 유효한 포획 대상인 경우 -> 공격
            if (_selectedPieceView != null && _currentLegalMoves.Contains(clickedView.CurrentCoord))
            {
                TryExecuteMoveTo(clickedView.CurrentCoord);
            }
        }

        private void SelectPiece(PieceView view)
        {
            DeselectPiece();

            _selectedPieceView = view;
            _selectedPieceView.SetSelected(true);

            _engine.GetLegalMoves(view.Data, _currentLegalMoves);
            gridRenderer.ShowTacticalGuides(view.CurrentCoord, _currentLegalMoves, _engine.Board);
        }

        private void DeselectPiece()
        {
            if (_selectedPieceView != null)
            {
                _selectedPieceView.SetSelected(false);
                _selectedPieceView = null;
            }

            _currentLegalMoves.Clear();
            gridRenderer.ClearTacticalGuides();
        }

        private void TryExecuteMoveTo(BoardCoord targetCoord)
        {
            if (_selectedPieceView == null) return;

            var fromCoord = _selectedPieceView.CurrentCoord;
            if (_currentLegalMoves.Contains(targetCoord))
            {
                _engine.TryMakeMove(fromCoord, targetCoord, out _);
                DeselectPiece();
            }
            else
            {
                DeselectPiece();
            }
        }

        private void HandleMoveExecuted(Move move)
        {
            if (move.IsPass)
            {
                AddBattleLog($"[{GetCampName(_engine.CurrentTurn.Opposite())}] 한수 쉬었습니다.");
                return;
            }

            if (_pieceViews.TryGetValue(move.Piece.Id, out var pieceView))
            {
                Vector3 targetWorld = _converter.ToWorldPosition(move.To);
                pieceView.AnimateMoveTo(targetWorld);
            }

            if (move.IsCapture && _pieceViews.TryGetValue(move.CapturedPiece.Id, out var capturedView))
            {
                // Phase 3: 낙뢰 직격 VFX 및 카메라 쉐이크 재생
                if (CombatFXManager.Instance != null)
                {
                    Vector3 targetWorld = _converter.ToWorldPosition(move.To);
                    CombatFXManager.Instance.PlayCaptureStrike(targetWorld, move.Piece.Camp, move.Piece.Type);
                }

                capturedView.AnimateCaptured();
                AddBattleLog($"⚡ {GetCampName(move.Piece.Camp)}의 {move.Piece.Type.GetKoreanName(move.Piece.Camp)}이(가) " +
                             $"{GetCampName(move.CapturedPiece.Camp)}의 {move.CapturedPiece.Type.GetKoreanName(move.CapturedPiece.Camp)}을(를) 격파했습니다!");
            }
            else
            {
                AddBattleLog($"{GetCampName(move.Piece.Camp)}의 {move.Piece.Type.GetKoreanName(move.Piece.Camp)}: {move.From} → {move.To}");
            }
        }

        private void HandleTurnChanged(CampType newTurn)
        {
            // 턴 전환 후 필요한 상태 갱신
        }

        private void HandleCheck(CampType checkedCamp)
        {
            AddBattleLog($"⚔️ [{GetCampName(checkedCamp)}] 장군!");
        }

        private void HandleGameOver(CampType? winner)
        {
            if (winner.HasValue)
            {
                AddBattleLog($"👑 [{GetCampName(winner.Value)}] 승리! (외통수)");
            }
            else
            {
                AddBattleLog("비겼습니다. (무승부)");
            }
        }

        public void UndoMove()
        {
            if (_engine.TryUndo())
            {
                DeselectPiece();
                // 뷰 위치 동기화
                foreach (var kvp in _pieceViews)
                {
                    var view = kvp.Value;
                    if (!view.Data.IsCaptured)
                    {
                        view.gameObject.SetActive(true);
                        view.transform.position = _converter.ToWorldPosition(view.Data.Coord);
                        view.transform.localScale = Vector3.one;
                    }
                }
                AddBattleLog("수를 되돌렸습니다.");
            }
        }

        public void PassTurn()
        {
            DeselectPiece();
            _engine.TryPassTurn(out _);
        }

        private void AddBattleLog(string message)
        {
            _battleLogs.Add(message);
            if (_battleLogs.Count > 8) _battleLogs.RemoveAt(0);
        }

        private static string GetCampName(CampType camp)
        {
            return camp == CampType.Cho ? "청룡군(초)" : "홍마군(한)";
        }

        #region 콘셉트 아트 스타일의 간이 HUD (OnGUI)
        private void OnGUI()
        {
            if (_engine == null) return;

            GUI.skin.label.fontSize = 14;

            // 1. 상단 진영 체력/점수 게이지 (콘셉트 2번 상단 HUD 재현)
            float choScore = _engine.Board.CalculateScore(CampType.Cho);
            float hanScore = _engine.Board.CalculateScore(CampType.Han);

            GUILayout.BeginArea(new Rect(Screen.width * 0.5f - 240, 15, 480, 50), GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.2f, 0.7f, 1f);
            GUILayout.Label($"청룡군(초)  점수: {choScore:F1} / 72.0", GUILayout.Width(220));

            GUI.color = (_engine.CurrentTurn == CampType.Cho) ? Color.cyan : Color.red;
            GUILayout.Label($"[{(_engine.CurrentTurn == CampType.Cho ? "청룡 턴" : "홍마 턴")}]", GUILayout.Width(80));

            GUI.color = new Color(1f, 0.35f, 0.35f);
            GUILayout.Label($"홍마군(한)  점수: {hanScore:F1} / 73.5", GUILayout.Width(220));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // 2. 좌하단 전투 로그창 (콘셉트 2번 좌하단 로그 재현)
            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(20, Screen.height - 180, 360, 160), GUI.skin.box);
            GUILayout.Label("<b>[전투 기록 (Battle Log)]</b>");
            for (int i = 0; i < _battleLogs.Count; i++)
            {
                GUILayout.Label(_battleLogs[i]);
            }
            GUILayout.EndArea();

            // 3. 우하단 액션 버튼 (무르기, 시점 전환, 한수 쉼)
            GUILayout.BeginArea(new Rect(Screen.width - 240, Screen.height - 130, 220, 110), GUI.skin.box);
            GUILayout.BeginVertical();
            if (GUILayout.Button("시점 전환 (C / Space)"))
            {
                if (cameraController != null) cameraController.ToggleViewMode();
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("무르기 (Z)"))
            {
                UndoMove();
            }
            if (GUILayout.Button("한수 쉼 (P)"))
            {
                PassTurn();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        #endregion
    }
}
