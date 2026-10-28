using System.Collections.Generic;

namespace WarlordJanggi.Core
{
    /// <summary>
    /// 차(車) 이동 전략
    /// - 상하좌우 4방향 무제한 직선 이동
    /// - 궁성 내 대각선 레일을 따라 대각선 직선 이동 가능
    /// </summary>
    public sealed class ChariotMoveStrategy : IMoveStrategy
    {
        public PieceType TargetType => PieceType.Chariot;

        private static readonly (int dx, int dy)[] OrthogonalDirections = {
            (0, 1), (0, -1), (-1, 0), (1, 0)
        };

        public void CalculateDestinations(Piece piece, GameBoard board, List<BoardCoord> outDestinations)
        {
            var from = piece.Coord;

            // 1. 상하좌우 직선 스캔
            for (int i = 0; i < OrthogonalDirections.Length; i++)
            {
                var dir = OrthogonalDirections[i];
                var current = from + dir;

                while (current.IsValid)
                {
                    var targetPiece = board.GetPieceAt(current);
                    if (targetPiece == null)
                    {
                        outDestinations.Add(current);
                    }
                    else
                    {
                        if (targetPiece.Camp != piece.Camp)
                        {
                            outDestinations.Add(current); // 적 기물 포획 가능
                        }
                        break; // 기물을 만나면 직선 경로 차단
                    }
                    current += dir;
                }
            }

            // 2. 궁성 대각선 레일 이동
            AddPalaceDiagonalMoves(piece, board, outDestinations);
        }

        private static void AddPalaceDiagonalMoves(Piece piece, GameBoard board, List<BoardCoord> outDestinations)
        {
            var from = piece.Coord;
            if (!from.IsInPalace) return;

            // 2-1. 궁성 중심에 있는 경우: 4개 꼭짓점으로 1칸 대각선 이동
            if (from.IsPalaceCenter)
            {
                int centerY = from.Y;
                BoardCoord[] corners = {
                    new BoardCoord(3, centerY - 1),
                    new BoardCoord(5, centerY - 1),
                    new BoardCoord(3, centerY + 1),
                    new BoardCoord(5, centerY + 1)
                };

                for (int i = 0; i < corners.Length; i++)
                {
                    var corner = corners[i];
                    var target = board.GetPieceAt(corner);
                    if (target == null || target.Camp != piece.Camp)
                    {
                        outDestinations.Add(corner);
                    }
                }
                return;
            }

            // 2-2. 궁성 꼭짓점에 있는 경우: 중심을 통과하여 반대편 꼭짓점까지 관통 이동
            if (from.IsPalaceCorner && PalaceHelper.TryGetOppositeCorner(from, out var opposite, out var center))
            {
                var centerPiece = board.GetPieceAt(center);
                if (centerPiece == null)
                {
                    outDestinations.Add(center); // 중심이 비어있으므로 이동 가능

                    // 중심이 비었으므로 반대편 꼭짓점까지 전진 가능
                    var oppositePiece = board.GetPieceAt(opposite);
                    if (oppositePiece == null || oppositePiece.Camp != piece.Camp)
                    {
                        outDestinations.Add(opposite);
                    }
                }
                else if (centerPiece.Camp != piece.Camp)
                {
                    outDestinations.Add(center); // 적 기물이면 포획 가능하나 반대편으로는 통과 불가
                }
            }
        }
    }
}
