using System.Collections.Generic;

namespace WarlordJanggi.Core
{
    /// <summary>
    /// 졸(卒) / 병(兵) 이동 전략
    /// - 초(Cho) 졸: 전진(+Y), 좌우(+X, -X) 1칸. 후진 불가
    /// - 한(Han) 병: 전진(-Y), 좌우(+X, -X) 1칸. 후진 불가
    /// - 적 궁성 진입 시: 궁성 대각선을 따라 대각선 "전진" 1칸 가능
    /// </summary>
    public sealed class SoldierMoveStrategy : IMoveStrategy
    {
        public PieceType TargetType => PieceType.Soldier;

        public void CalculateDestinations(Piece piece, GameBoard board, List<BoardCoord> outDestinations)
        {
            var from = piece.Coord;
            int forwardY = (piece.Camp == CampType.Cho) ? 1 : -1;

            // 1. 기본 3방향 (전진 1칸, 좌우 1칸)
            (int dx, int dy)[] standardSteps = {
                (0, forwardY),
                (-1, 0),
                (1, 0)
            };

            for (int i = 0; i < standardSteps.Length; i++)
            {
                var target = from + standardSteps[i];
                if (CanMoveOrCapture(piece, board, target))
                {
                    outDestinations.Add(target);
                }
            }

            // 2. 적 궁성 내 대각선 전진 특수 규칙
            // 초 졸이 한 궁성(Y: 7~9)에 들어갔거나, 한 병이 초 궁성(Y: 0~2)에 들어갔을 때
            bool inEnemyPalace = (piece.Camp == CampType.Cho) ? from.IsInHanPalace : from.IsInChoPalace;
            if (inEnemyPalace)
            {
                // 중심에서 대각선 전진하는 경우
                if (from.IsPalaceCenter)
                {
                    // 초 졸: (4,8) -> (3,9), (5,9)
                    // 한 병: (4,1) -> (3,0), (5,0)
                    var diagLeft = from + (-1, forwardY);
                    var diagRight = from + (1, forwardY);

                    if (CanMoveOrCapture(piece, board, diagLeft)) outDestinations.Add(diagLeft);
                    if (CanMoveOrCapture(piece, board, diagRight)) outDestinations.Add(diagRight);
                }
                // 입구 모서리에서 중심(대각선 전진)으로 들어가는 경우
                else if (from.IsPalaceCorner)
                {
                    // 초 졸이 Y=7 모서리 (3,7) or (5,7)에 있으면 (4,8)로 전진 가능
                    // 한 병이 Y=2 모서리 (3,2) or (5,2)에 있으면 (4,1)로 전진 가능
                    int enemyCenterY = (piece.Camp == CampType.Cho) ? 8 : 1;
                    int enemyEntryY = (piece.Camp == CampType.Cho) ? 7 : 2;

                    if (from.Y == enemyEntryY)
                    {
                        var center = new BoardCoord(4, enemyCenterY);
                        if (CanMoveOrCapture(piece, board, center))
                        {
                            outDestinations.Add(center);
                        }
                    }
                }
            }
        }

        private static bool CanMoveOrCapture(Piece piece, GameBoard board, BoardCoord target)
        {
            if (!target.IsValid) return false;
            var targetPiece = board.GetPieceAt(target);
            return targetPiece == null || targetPiece.Camp != piece.Camp;
        }
    }
}
