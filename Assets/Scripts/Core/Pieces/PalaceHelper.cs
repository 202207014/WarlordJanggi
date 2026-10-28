namespace WarlordJanggi.Core
{
    /// <summary>
    /// 궁성(Palace) 내 대각선 이동 및 연결선 판정 헬퍼
    /// </summary>
    public static class PalaceHelper
    {
        /// <summary>
        /// 두 좌표가 궁성 내 대각선 선으로 직접 1칸 연결되어 있는지 확인
        /// (사, 궁, 상대 궁성의 졸/병 1칸 이동에 사용)
        /// </summary>
        public static bool IsConnectedDiagonally(BoardCoord from, BoardCoord to)
        {
            // 같은 궁성 내에 있어야 함
            if (from.IsInChoPalace && to.IsInChoPalace)
            {
                return CheckPalaceDiagonalStep(from, to, centerY: 1);
            }
            if (from.IsInHanPalace && to.IsInHanPalace)
            {
                return CheckPalaceDiagonalStep(from, to, centerY: 8);
            }
            return false;
        }

        private static bool CheckPalaceDiagonalStep(BoardCoord a, BoardCoord b, int centerY)
        {
            int dx = System.Math.Abs(a.X - b.X);
            int dy = System.Math.Abs(a.Y - b.Y);
            if (dx != 1 || dy != 1) return false;

            // 대각선 1칸 이동은 반드시 중심점(4, centerY)을 거쳐야만 유효함
            return (a.X == 4 && a.Y == centerY) || (b.X == 4 && b.Y == centerY);
        }

        /// <summary>
        /// 궁성 모서리 간의 대각선 맞은편 모서리 좌표 반환 (반드시 중심점을 통과함)
        /// </summary>
        public static bool TryGetOppositeCorner(BoardCoord corner, out BoardCoord opposite, out BoardCoord center)
        {
            opposite = BoardCoord.Invalid;
            center = BoardCoord.Invalid;

            if (corner.IsInChoPalace && corner.IsPalaceCorner)
            {
                center = new BoardCoord(4, 1);
                opposite = new BoardCoord(corner.X == 3 ? 5 : 3, corner.Y == 0 ? 2 : 0);
                return true;
            }
            if (corner.IsInHanPalace && corner.IsPalaceCorner)
            {
                center = new BoardCoord(4, 8);
                opposite = new BoardCoord(corner.X == 3 ? 5 : 3, corner.Y == 7 ? 9 : 7);
                return true;
            }
            return false;
        }
    }
}
