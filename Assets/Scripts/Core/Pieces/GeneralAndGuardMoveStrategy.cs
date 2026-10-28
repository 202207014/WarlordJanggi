using System.Collections.Generic;

namespace WarlordJanggi.Core
{
    /// <summary>
    /// 궁(將/楚/漢) 및 사(士) 이동 전략
    /// - 자신의 궁성(3x3) 내부에서만 이동 가능
    /// - 상하좌우 1칸 및 궁성 대각선 선을 따라 1칸 이동
    /// </summary>
    public sealed class GeneralAndGuardMoveStrategy : IMoveStrategy
    {
        public PieceType TargetType { get; }

        public GeneralAndGuardMoveStrategy(PieceType type)
        {
            TargetType = type;
        }

        private static readonly (int dx, int dy)[] OrthogonalSteps = {
            (0, 1), (0, -1), (-1, 0), (1, 0)
        };

        private static readonly (int dx, int dy)[] DiagonalSteps = {
            (-1, -1), (1, -1), (-1, 1), (1, 1)
        };

        public void CalculateDestinations(Piece piece, GameBoard board, List<BoardCoord> outDestinations)
        {
            var from = piece.Coord;

            // 1. 상하좌우 1칸 이동
            for (int i = 0; i < OrthogonalSteps.Length; i++)
            {
                var target = from + OrthogonalSteps[i];
                if (IsInOwnPalace(piece.Camp, target) && CanMoveOrCapture(piece, board, target))
                {
                    outDestinations.Add(target);
                }
            }

            // 2. 궁성 대각선 1칸 이동
            for (int i = 0; i < DiagonalSteps.Length; i++)
            {
                var target = from + DiagonalSteps[i];
                if (IsInOwnPalace(piece.Camp, target) &&
                    PalaceHelper.IsConnectedDiagonally(from, target) &&
                    CanMoveOrCapture(piece, board, target))
                {
                    outDestinations.Add(target);
                }
            }
        }

        private static bool IsInOwnPalace(CampType camp, BoardCoord coord)
        {
            return (camp == CampType.Cho) ? coord.IsInChoPalace : coord.IsInHanPalace;
        }

        private static bool CanMoveOrCapture(Piece piece, GameBoard board, BoardCoord target)
        {
            var targetPiece = board.GetPieceAt(target);
            return targetPiece == null || targetPiece.Camp != piece.Camp;
        }
    }
}
