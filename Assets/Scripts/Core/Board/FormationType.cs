namespace WarlordJanggi.Core
{
    /// <summary>
    /// 장기 시작 시 상(象)과 마(馬)의 포진(배치) 유형
    /// 보드 아래에서 위를 바라보는 기준 (왼쪽 -> 오른쪽)
    /// </summary>
    public enum FormationType
    {
        /// <summary>
        /// 마 - 상 - 마 - 상 (왼마 차림)
        /// </summary>
        HorseElephantHorseElephant = 0,

        /// <summary>
        /// 상 - 마 - 상 - 마 (오른마 차림)
        /// </summary>
        ElephantHorseElephantHorse = 1,

        /// <summary>
        /// 마 - 상 - 상 - 마 (안상 차림 / 원앙마의 변형 등)
        /// </summary>
        HorseElephantElephantHorse = 2,

        /// <summary>
        /// 상 - 마 - 마 - 상 (양귀마 차림)
        /// </summary>
        ElephantHorseHorseElephant = 3
    }
}
