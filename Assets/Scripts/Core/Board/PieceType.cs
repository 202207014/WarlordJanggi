namespace WarlordJanggi.Core
{
    /// <summary>
    /// 장기 기물 종류
    /// </summary>
    public enum PieceType
    {
        General = 0,  // 궁 / 장(將)
        Guard = 1,    // 사(士)
        Horse = 2,    // 마(馬)
        Elephant = 3, // 상(象)
        Chariot = 4,  // 차(車)
        Cannon = 5,   // 포(包)
        Soldier = 6   // 졸(卒) / 병(兵)
    }

    public static class PieceTypeExtensions
    {
        /// <summary>
        /// 한국 장기 표준 기물 점수 (총 72점 기준)
        /// 차(13), 포(7), 마(5), 상(3), 사(3), 졸/병(2)
        /// </summary>
        public static float GetStandardScore(this PieceType type)
        {
            return type switch
            {
                PieceType.Chariot => 13f,
                PieceType.Cannon => 7f,
                PieceType.Horse => 5f,
                PieceType.Elephant => 3f,
                PieceType.Guard => 3f,
                PieceType.Soldier => 2f,
                _ => 0f // General은 점수 없음
            };
        }

        /// <summary>
        /// 한자 표기 반환 (초 / 한 구분)
        /// </summary>
        public static string GetHanja(this PieceType type, CampType camp)
        {
            return (type, camp) switch
            {
                (PieceType.General, CampType.Cho) => "楚",
                (PieceType.General, CampType.Han) => "漢",
                (PieceType.Guard, _) => "士",
                (PieceType.Horse, _) => "馬",
                (PieceType.Elephant, _) => "象",
                (PieceType.Chariot, _) => "車",
                (PieceType.Cannon, _) => "包",
                (PieceType.Soldier, CampType.Cho) => "卒",
                (PieceType.Soldier, CampType.Han) => "兵",
                _ => string.Empty
            };
        }

        /// <summary>
        /// 한글 명칭 반환
        /// </summary>
        public static string GetKoreanName(this PieceType type, CampType camp)
        {
            return (type, camp) switch
            {
                (PieceType.General, CampType.Cho) => "궁(초)",
                (PieceType.General, CampType.Han) => "궁(한)",
                (PieceType.Guard, _) => "사",
                (PieceType.Horse, _) => "마",
                (PieceType.Elephant, _) => "상",
                (PieceType.Chariot, _) => "차",
                (PieceType.Cannon, _) => "포",
                (PieceType.Soldier, CampType.Cho) => "졸",
                (PieceType.Soldier, CampType.Han) => "병",
                _ => string.Empty
            };
        }
    }
}
