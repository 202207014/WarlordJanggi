using System.Collections.Generic;

namespace WarlordJanggi.Core
{
    /// <summary>
    /// 기물별 이동 가능 경로 계산 인터페이스
    /// GC 최소화를 위해 버퍼 리스트(outDestinations)에 채우는 패턴 적용
    /// </summary>
    public interface IMoveStrategy
    {
        PieceType TargetType { get; }

        /// <summary>
        /// 해당 기물이 현재 판에서 이동/공격 가능한 모든 유효 목적지 좌표 목록을 반환
        /// (장군을 유발하는 자살수 필터링은 RuleEngine/GameBoard 수준에서 최종 검증)
        /// </summary>
        void CalculateDestinations(Piece piece, GameBoard board, List<BoardCoord> outDestinations);
    }
}
