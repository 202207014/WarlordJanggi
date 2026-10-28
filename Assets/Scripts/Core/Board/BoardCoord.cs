using System;

namespace WarlordJanggi.Core
{
    /// <summary>
    /// 장기 보드 교차점 좌표 구조체
    /// 가로 X: 0 ~ 8 (총 9열)
    /// 세로 Y: 0 ~ 9 (총 10행, 초 진영 0~4, 한 진영 5~9)
    /// </summary>
    public readonly struct BoardCoord : IEquatable<BoardCoord>
    {
        public const int BoardWidth = 9;
        public const int BoardHeight = 10;

        public readonly int X;
        public readonly int Y;

        public BoardCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool IsValid => X >= 0 && X < BoardWidth && Y >= 0 && Y < BoardHeight;

        /// <summary>
        /// 초(楚) 궁성 내부인지 여부 (X: 3~5, Y: 0~2)
        /// </summary>
        public bool IsInChoPalace => X >= 3 && X <= 5 && Y >= 0 && Y <= 2;

        /// <summary>
        /// 한(漢) 궁성 내부인지 여부 (X: 3~5, Y: 7~9)
        /// </summary>
        public bool IsInHanPalace => X >= 3 && X <= 5 && Y >= 7 && Y <= 9;

        /// <summary>
        /// 어떤 궁성이든 내부인지 여부
        /// </summary>
        public bool IsInPalace => IsInChoPalace || IsInHanPalace;

        /// <summary>
        /// 궁성 중심점 여부 (초: (4,1), 한: (4,8))
        /// </summary>
        public bool IsPalaceCenter => (X == 4 && Y == 1) || (X == 4 && Y == 8);

        /// <summary>
        /// 궁성 모서리 4개 꼭짓점 여부
        /// </summary>
        public bool IsPalaceCorner =>
            (IsInChoPalace && (X == 3 || X == 5) && (Y == 0 || Y == 2)) ||
            (IsInHanPalace && (X == 3 || X == 5) && (Y == 7 || Y == 9));

        public static BoardCoord Invalid => new BoardCoord(-1, -1);

        public static BoardCoord operator +(BoardCoord a, (int dx, int dy) offset)
        {
            return new BoardCoord(a.X + offset.dx, a.Y + offset.dy);
        }

        public static BoardCoord operator -(BoardCoord a, (int dx, int dy) offset)
        {
            return new BoardCoord(a.X - offset.dx, a.Y - offset.dy);
        }

        public static bool operator ==(BoardCoord a, BoardCoord b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(BoardCoord a, BoardCoord b) => !(a == b);

        public bool Equals(BoardCoord other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is BoardCoord other && Equals(other);

        public override int GetHashCode() => (X * 397) ^ Y;

        public override string ToString() => $"({X}, {Y})";
    }
}
