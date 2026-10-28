using System.Collections.Generic;

namespace WarlordJanggi.Core
{
    /// <summary>
    /// 마(馬) 이동 전략
    /// - 직선 1칸 이동 후 대각선 1칸 (총 8방향)
    /// - 멱(길막힘): 직선 1칸 위치에 기물이 있으면 해당 방향 2개 이동 불가
    /// </summary>
    public sealed class HorseMoveStrategy : IMoveStrategy
    {
        public PieceType TargetType => PieceType.Horse;

        private struct HorseStep
        {
            public (int dx, int dy) Step1; // 멱 위치 (직선 1칸)
            public (int dx, int dy) Step2; // 최종 목적지 (대각선 1칸 추가)

            public HorseStep(int s1x, int s1y, int s2x, int s2y)
            {
                Step1 = (s1x, s1y);
                Step2 = (s2x, s2y);
            }
        }

        private static readonly HorseStep[] Steps = {
            // 상(Y+1) 방향
            new HorseStep(0, 1, -1, 2),
            new HorseStep(0, 1, 1, 2),
            // 하(Y-1) 방향
            new HorseStep(0, -1, -1, -2),
            new HorseStep(0, -1, 1, -2),
            // 좌(X-1) 방향
            new HorseStep(-1, 0, -2, -1),
            new HorseStep(-1, 0, -2, 1),
            // 우(X+1) 방향
            new HorseStep(1, 0, 2, -1),
            new HorseStep(1, 0, 2, 1),
        };

        public void CalculateDestinations(Piece piece, GameBoard board, List<BoardCoord> outDestinations)
        {
            var from = piece.Coord;

            for (int i = 0; i < Steps.Length; i++)
            {
                var step = Steps[i];
                var blockCoord = from + step.Step1;

                // 멱 검사: 직선 1칸 위치가 유효하고 비어있어야 함
                if (!blockCoord.IsValid || board.IsOccupied(blockCoord))
                    continue;

                var dest = from + step.Step2;
                if (!dest.IsValid)
                    continue;

                var targetPiece = board.GetPieceAt(dest);
                if (targetPiece == null || targetPiece.Camp != piece.Camp)
                {
                    outDestinations.Add(dest);
                }
            }
        }
    }
}
