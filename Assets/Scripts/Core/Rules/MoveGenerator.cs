using System;
using System.Collections.Generic;

namespace WarlordJanggi.Core
{
    /// <summary>
    /// 기물의 유효 이동 경로 생성 및 장군/맞궁 판정기
    /// </summary>
    public sealed class MoveGenerator
    {
        private readonly Dictionary<PieceType, IMoveStrategy> _strategies = new Dictionary<PieceType, IMoveStrategy>(7);
        private readonly List<BoardCoord> _scratchCoords = new List<BoardCoord>(32);

        public MoveGenerator()
        {
            _strategies[PieceType.Soldier] = new SoldierMoveStrategy();
            _strategies[PieceType.Horse] = new HorseMoveStrategy();
            _strategies[PieceType.Elephant] = new ElephantMoveStrategy();
            _strategies[PieceType.Chariot] = new ChariotMoveStrategy();
            _strategies[PieceType.Cannon] = new CannonMoveStrategy();
            _strategies[PieceType.General] = new GeneralAndGuardMoveStrategy(PieceType.General);
            _strategies[PieceType.Guard] = new GeneralAndGuardMoveStrategy(PieceType.Guard);
        }

        /// <summary>
        /// 장군/맞궁 등의 제약을 고려하지 않은 기물 본연의 의사 유효 이동(Pseudo-Legal) 경로 계산
        /// </summary>
        public void GetPseudoLegalDestinations(Piece piece, GameBoard board, List<BoardCoord> outDestinations)
        {
            if (piece == null || piece.IsCaptured) return;
            if (_strategies.TryGetValue(piece.Type, out var strategy))
            {
                strategy.CalculateDestinations(piece, board, outDestinations);
            }
        }

        /// <summary>
        /// 이동 후 자신의 궁이 장군에 걸리거나 맞궁이 되는 불법수를 모두 제외한 최종 합법 수(Legal Moves) 계산
        /// </summary>
        public void GetLegalDestinations(Piece piece, GameBoard board, List<BoardCoord> outDestinations)
        {
            outDestinations.Clear();
            if (piece == null || piece.IsCaptured) return;

            _scratchCoords.Clear();
            GetPseudoLegalDestinations(piece, board, _scratchCoords);

            var from = piece.Coord;
            var camp = piece.Camp;

            for (int i = 0; i < _scratchCoords.Count; i++)
            {
                var to = _scratchCoords[i];

                // 가상으로 수를 둔 후 검증 (Apply -> Test -> Undo)
                var move = board.ApplyMove(from, to);

                bool isSuicideCheck = IsKingInCheck(camp, board);
                bool isDirectFacing = IsDirectKingFacing(board);

                board.UndoLastMove();

                // 자신의 왕이 장군 상태가 되거나 두 왕이 마주보는 맞궁 상태가 되면 둘 수 없음
                if (!isSuicideCheck && !isDirectFacing)
                {
                    outDestinations.Add(to);
                }
            }
        }

        /// <summary>
        /// 특정 진영의 궁이 장군(Check) 상태인지 판정
        /// </summary>
        public bool IsKingInCheck(CampType camp, GameBoard board)
        {
            var kingCoord = board.GetGeneralCoord(camp);
            if (!kingCoord.IsValid) return false;

            var opponentCamp = camp.Opposite();
            var opponentPieces = (opponentCamp == CampType.Cho) ? board.ChoPieces : board.HanPieces;

            for (int i = 0; i < opponentPieces.Count; i++)
            {
                var enemy = opponentPieces[i];
                if (enemy.IsCaptured) continue;

                var candidateMoves = new List<BoardCoord>(16);
                GetPseudoLegalDestinations(enemy, board, candidateMoves);

                for (int j = 0; j < candidateMoves.Count; j++)
                {
                    if (candidateMoves[j] == kingCoord)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 맞궁(두 궁이 같은 열에서 사이에 기물이 하나도 없이 마주보는 상태) 판정
        /// </summary>
        public bool IsDirectKingFacing(GameBoard board)
        {
            var choCoord = board.GetGeneralCoord(CampType.Cho);
            var hanCoord = board.GetGeneralCoord(CampType.Han);

            if (!choCoord.IsValid || !hanCoord.IsValid) return false;

            // X 좌표(열)가 같아야 마주볼 가능성이 있음
            if (choCoord.X != hanCoord.X) return false;

            int x = choCoord.X;
            int minY = Math.Min(choCoord.Y, hanCoord.Y);
            int maxY = Math.Max(choCoord.Y, hanCoord.Y);

            // 두 궁 사이에 기물이 하나라도 있으면 맞궁이 아님
            for (int y = minY + 1; y < maxY; y++)
            {
                if (board.IsOccupied(new BoardCoord(x, y)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 해당 진영이 둘 수 있는 합법 수가 전혀 없는지(외통수 판정용) 검사
        /// </summary>
        public bool HasNoLegalMoves(CampType camp, GameBoard board)
        {
            var pieces = (camp == CampType.Cho) ? board.ChoPieces : board.HanPieces;
            var legalMoves = new List<BoardCoord>(16);

            for (int i = 0; i < pieces.Count; i++)
            {
                var piece = pieces[i];
                if (piece.IsCaptured) continue;

                GetLegalDestinations(piece, board, legalMoves);
                if (legalMoves.Count > 0)
                {
                    return false; // 둘 수 있는 수가 최소 1개 있음
                }
            }

            return true;
        }
    }
}
