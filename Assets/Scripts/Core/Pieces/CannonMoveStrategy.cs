using System.Collections.Generic;

namespace WarlordJanggi.Core
{
    /// <summary>
    /// 포(包) 이동 전략
    /// - 반드시 기물 정확히 1개를 뛰어넘어야 이동/공격 가능
    /// - [특수 룰 1] 디딤돌이 되는 기물은 '포(Cannon)'일 수 없음 (포는 포를 넘을 수 없음)
    /// - [특수 룰 2] 목표 지점의 기물이 '포(Cannon)'인 경우 포획할 수 없음 (포는 포를 잡을 수 없음)
    /// - 궁성 내 대각선 레일에서도 동일 규칙 적용
    /// </summary>
    public sealed class CannonMoveStrategy : IMoveStrategy
    {
        public PieceType TargetType => PieceType.Cannon;

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
                bool foundHurdle = false;

                while (current.IsValid)
                {
                    var targetPiece = board.GetPieceAt(current);

                    if (!foundHurdle)
                    {
                        // 디딤돌(허들)을 찾는 단계
                        if (targetPiece != null)
                        {
                            // 포는 포를 뛰어넘을 수 없음
                            if (targetPiece.Type == PieceType.Cannon)
                                break;

                            foundHurdle = true;
                        }
                    }
                    else
                    {
                        // 디딤돌 1개를 넘은 후 착지 지점 탐색
                        if (targetPiece == null)
                        {
                            outDestinations.Add(current);
                        }
                        else
                        {
                            // 포는 포를 포획할 수 없고, 아군 기물도 잡을 수 없음
                            if (targetPiece.Camp != piece.Camp && targetPiece.Type != PieceType.Cannon)
                            {
                                outDestinations.Add(current);
                            }
                            // 2번째 기물을 만나면 직선 경로 즉시 종료
                            break;
                        }
                    }

                    current += dir;
                }
            }

            // 2. 궁성 대각선 스캔
            AddPalaceDiagonalMoves(piece, board, outDestinations);
        }

        private static void AddPalaceDiagonalMoves(Piece piece, GameBoard board, List<BoardCoord> outDestinations)
        {
            var from = piece.Coord;
            if (!from.IsInPalace) return;

            // 포는 디딤돌 1개가 반드시 필요하므로,
            // 궁성 꼭짓점에서 중심에 있는 기물(포가 아님)을 딛고 반대편 꼭짓점으로만 뛸 수 있음
            if (from.IsPalaceCorner && PalaceHelper.TryGetOppositeCorner(from, out var opposite, out var center))
            {
                var centerPiece = board.GetPieceAt(center);
                // 중심에 디딤돌 기물이 존재하고, 그 기물이 포가 아니어야 함
                if (centerPiece != null && centerPiece.Type != PieceType.Cannon)
                {
                    var targetPiece = board.GetPieceAt(opposite);
                    if (targetPiece == null)
                    {
                        outDestinations.Add(opposite);
                    }
                    else if (targetPiece.Camp != piece.Camp && targetPiece.Type != PieceType.Cannon)
                    {
                        outDestinations.Add(opposite);
                    }
                }
            }
        }
    }
}
