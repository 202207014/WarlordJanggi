using System.Collections.Generic;

namespace WarlordJanggi.Core
{
    /// <summary>
    /// 상(象) 이동 전략
    /// - 직선 1칸 이동 후 대각선 2칸 (총 8방향)
    /// - 멱: 직선 1칸 위치(Block1)와 대각선 1칸 위치(Block2) 모두 비어있어야 함
    /// </summary>
    public sealed class ElephantMoveStrategy : IMoveStrategy
    {
        public PieceType TargetType => PieceType.Elephant;

        private struct ElephantStep
        {
            public (int dx, int dy) Block1; // 직선 1칸
            public (int dx, int dy) Block2; // 직선 1칸 + 대각선 1칸
            public (int dx, int dy) Dest;   // 최종 목적지 (직선 1칸 + 대각선 2칸)

            public ElephantStep(int b1x, int b1y, int b2x, int b2y, int destX, int destY)
            {
                Block1 = (b1x, b1y);
                Block2 = (b2x, b2y);
                Dest = (destX, destY);
            }
        }

        private static readonly ElephantStep[] Steps = {
            // 상(Y+1) 방향
            new ElephantStep(0, 1, -1, 2, -2, 3),
            new ElephantStep(0, 1,  1, 2,  2, 3),
            // 하(Y-1) 방향
            new ElephantStep(0, -1, -1, -2, -2, -3),
            new ElephantStep(0, -1,  1, -2,  2, -3),
            // 좌(X-1) 방향
            new ElephantStep(-1, 0, -2, -1, -3, -2),
            new ElephantStep(-1, 0, -2,  1, -3,  2),
            // 우(X+1) 방향
            new ElephantStep(1, 0, 2, -1, 3, -2),
            new ElephantStep(1, 0, 2,  1, 3,  2),
        };

        public void CalculateDestinations(Piece piece, GameBoard board, List<BoardCoord> outDestinations)
        {
            var from = piece.Coord;

            for (int i = 0; i < Steps.Length; i++)
            {
                var step = Steps[i];
                var block1 = from + step.Block1;
                var block2 = from + step.Block2;

                // 멱 2곳 검사: 둘 중 하나라도 차있으면 이동 불가
                if (!block1.IsValid || board.IsOccupied(block1)) continue;
                if (!block2.IsValid || board.IsOccupied(block2)) continue;

                var dest = from + step.Dest;
                if (!dest.IsValid) continue;

                var targetPiece = board.GetPieceAt(dest);
                if (targetPiece == null || targetPiece.Camp != piece.Camp)
                {
                    outDestinations.Add(dest);
                }
            }
        }
    }
}
