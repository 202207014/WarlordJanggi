using UnityEngine;
using WarlordJanggi.Core;

namespace WarlordJanggi.Presentation
{
    /// <summary>
    /// 장기 10x9 교차점 좌표(BoardCoord)와 유니티 3D 월드 좌표(Vector3) 간의 상호 변환기
    /// 보드 중앙(X: 4, Y: 4.5)이 월드 좌표 (0, 0, 0)에 위치하도록 배치
    /// </summary>
    public sealed class BoardCoordinateConverter
    {
        public float CellSpacingX { get; }
        public float CellSpacingZ { get; }
        public Vector3 BoardCenterOffset { get; }

        public BoardCoordinateConverter(float cellSpacing = 1.2f, Vector3 centerOffset = default)
        {
            CellSpacingX = cellSpacing;
            CellSpacingZ = cellSpacing;
            BoardCenterOffset = centerOffset;
        }

        /// <summary>
        /// 장기 교차점 좌표 -> 월드 3D 위치 변환
        /// </summary>
        public Vector3 ToWorldPosition(BoardCoord coord, float yHeight = 0f)
        {
            float x = (coord.X - 4f) * CellSpacingX + BoardCenterOffset.x;
            float z = (coord.Y - 4.5f) * CellSpacingZ + BoardCenterOffset.z;
            float y = BoardCenterOffset.y + yHeight;
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// 월드 3D 위치 -> 가장 가까운 장기 교차점 좌표로 스냅 변환
        /// </summary>
        public bool TryToBoardCoord(Vector3 worldPos, out BoardCoord coord, float maxDistanceTolerance = 0.6f)
        {
            float relativeX = (worldPos.x - BoardCenterOffset.x) / CellSpacingX + 4f;
            float relativeZ = (worldPos.z - BoardCenterOffset.z) / CellSpacingZ + 4.5f;

            int roundX = Mathf.RoundToInt(relativeX);
            int roundY = Mathf.RoundToInt(relativeZ);

            var candidate = new BoardCoord(roundX, roundY);
            if (!candidate.IsValid)
            {
                coord = BoardCoord.Invalid;
                return false;
            }

            Vector3 snappedWorld = ToWorldPosition(candidate, worldPos.y);
            float distSq = (snappedWorld.x - worldPos.x) * (snappedWorld.x - worldPos.x) +
                           (snappedWorld.z - worldPos.z) * (snappedWorld.z - worldPos.z);

            if (distSq <= maxDistanceTolerance * maxDistanceTolerance)
            {
                coord = candidate;
                return true;
            }

            coord = BoardCoord.Invalid;
            return false;
        }
    }
}
