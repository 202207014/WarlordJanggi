namespace WarlordJanggi.Core
{
    /// <summary>
    /// 장기 진영 구분
    /// 초(楚 / 청룡군 - 선수, 파랑) vs 한(漢 / 홍마군 - 후수, 빨강)
    /// </summary>
    public enum CampType
    {
        Cho = 0, // 초(楚) - 선수 (Blue / 청룡군)
        Han = 1  // 한(漢) - 후수 (Red / 홍마군)
    }

    public static class CampExtensions
    {
        public static CampType Opposite(this CampType camp)
        {
            return camp == CampType.Cho ? CampType.Han : CampType.Cho;
        }
    }
}
