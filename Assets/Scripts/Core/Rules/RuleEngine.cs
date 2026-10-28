using System;
using System.Collections.Generic;

namespace WarlordJanggi.Core
{
    /// <summary>
    /// 장기 대국 총괄 룰 엔진
    /// 턴 전환, 수 적용, 장군/외통수/한수쉼 판정 및 이벤트 발행
    /// </summary>
    public sealed class RuleEngine
    {
        public GameBoard Board { get; }
        public MoveGenerator MoveGen { get; }

        public CampType CurrentTurn { get; private set; }
        public GameState State { get; private set; }
        public CampType? Winner { get; private set; }
        public int TurnNumber { get; private set; }

        public event Action<Move> OnMoveExecuted;
        public event Action<CampType> OnTurnChanged;
        public event Action<CampType> OnCheck;
        public event Action<CampType?> OnGameOver;

        public RuleEngine(GameBoard board = null, MoveGenerator moveGen = null)
        {
            Board = board ?? new GameBoard();
            MoveGen = moveGen ?? new MoveGenerator();
            State = GameState.NotStarted;
        }

        /// <summary>
        /// 대국 시작
        /// </summary>
        public void StartGame(
            FormationType choFormation = FormationType.HorseElephantHorseElephant,
            FormationType hanFormation = FormationType.HorseElephantHorseElephant)
        {
            Board.SetupBoard(choFormation, hanFormation);
            CurrentTurn = CampType.Cho; // 초(楚) 선수
            State = GameState.InProgress;
            Winner = null;
            TurnNumber = 1;

            OnTurnChanged?.Invoke(CurrentTurn);
        }

        /// <summary>
        /// 특정 기물의 합법 이동 목록 반환
        /// </summary>
        public void GetLegalMoves(Piece piece, List<BoardCoord> outDestinations)
        {
            outDestinations.Clear();
            if (State != GameState.InProgress && State != GameState.Check) return;
            if (piece.Camp != CurrentTurn) return;

            MoveGen.GetLegalDestinations(piece, Board, outDestinations);
        }

        /// <summary>
        /// 수 착점 실행
        /// </summary>
        public bool TryMakeMove(BoardCoord from, BoardCoord to, out Move executedMove)
        {
            executedMove = default;
            if (State != GameState.InProgress && State != GameState.Check) return false;

            var piece = Board.GetPieceAt(from);
            if (piece == null || piece.Camp != CurrentTurn) return false;

            var legalMoves = new List<BoardCoord>(16);
            MoveGen.GetLegalDestinations(piece, Board, legalMoves);

            if (!legalMoves.Contains(to)) return false;

            // 이동 적용
            executedMove = Board.ApplyMove(from, to);
            OnMoveExecuted?.Invoke(executedMove);

            // 턴 전환 및 승패/장군 판정
            EndTurn();
            return true;
        }

        /// <summary>
        /// 한수 쉼 (장군 상태가 아닐 때만 가능)
        /// </summary>
        public bool TryPassTurn(out Move executedPass)
        {
            executedPass = default;
            if (State == GameState.Check)
            {
                // 장군 상태에서는 한수 쉴 수 없음 (반드시 멍군해야 함)
                return false;
            }

            executedPass = Board.ApplyPass();
            OnMoveExecuted?.Invoke(executedPass);

            EndTurn();
            return true;
        }

        /// <summary>
        /// 무르기 실행
        /// </summary>
        public bool TryUndo()
        {
            if (Board.MoveHistory.Count == 0) return false;

            Board.UndoLastMove();
            CurrentTurn = CurrentTurn.Opposite();
            if (CurrentTurn == CampType.Han) TurnNumber = Math.Max(1, TurnNumber - 1);

            // 상태 재계산
            bool inCheck = MoveGen.IsKingInCheck(CurrentTurn, Board);
            State = inCheck ? GameState.Check : GameState.InProgress;
            Winner = null;

            OnTurnChanged?.Invoke(CurrentTurn);
            return true;
        }

        private void EndTurn()
        {
            var nextTurn = CurrentTurn.Opposite();
            CurrentTurn = nextTurn;
            if (nextTurn == CampType.Cho) TurnNumber++;

            // 상대방 장군 검사
            bool inCheck = MoveGen.IsKingInCheck(nextTurn, Board);

            // 상대방이 둘 수 있는 합법 수가 전혀 없는지 검사
            bool hasNoMoves = MoveGen.HasNoLegalMoves(nextTurn, Board);

            if (inCheck)
            {
                if (hasNoMoves)
                {
                    // 장군인데 피할 길이 없음 -> 외통수 (Checkmate)
                    State = GameState.Checkmate;
                    Winner = nextTurn.Opposite();
                    OnGameOver?.Invoke(Winner);
                    return;
                }

                State = GameState.Check;
                OnCheck?.Invoke(nextTurn);
            }
            else
            {
                if (hasNoMoves)
                {
                    // 장군은 아닌데 둘 수가 없음 -> 한수쉼 유도 또는 무승부(장기에서는 한수쉼 처리)
                    State = GameState.InProgress;
                }
                else
                {
                    State = GameState.InProgress;
                }
            }

            OnTurnChanged?.Invoke(CurrentTurn);
        }
    }
}
