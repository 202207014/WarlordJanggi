namespace WarlordJanggi.Core
{
    /// <summary>
    /// 장기 기물 데이터 모델
    /// </summary>
    public sealed class Piece
    {
        public int Id { get; }
        public CampType Camp { get; }
        public PieceType Type { get; }
        public BoardCoord Coord { get; private set; }
        public bool IsCaptured { get; private set; }

        public Piece(int id, CampType camp, PieceType type, BoardCoord coord)
        {
            Id = id;
            Camp = camp;
            Type = type;
            Coord = coord;
            IsCaptured = false;
        }

        public void MoveTo(BoardCoord newCoord)
        {
            Coord = newCoord;
        }

        public void SetCaptured(bool captured)
        {
            IsCaptured = captured;
        }

        public override string ToString()
        {
            return $"[{Camp.GetHanja(Type)} {Type.GetKoreanName(Camp)} @ {Coord}]";
        }
    }

    internal static class PieceCampExtensions
    {
        public static string GetHanja(this CampType camp, PieceType type)
        {
            return type.GetHanja(camp);
        }
    }
}
