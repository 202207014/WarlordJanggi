using System;
using System.Collections.Generic;

namespace WarlordJanggi.Core
{
    /// <summary>
    /// 장기 보드 상태 모델 (10행 9열 교차점)
    /// 순수 C# 클래스로서 이동 적용, 무르기, 점수 계산, 기물 위치 조회를 담당
    /// </summary>
    public sealed class GameBoard
    {
        public const int Width = BoardCoord.BoardWidth;   // 9
        public const int Height = BoardCoord.BoardHeight; // 10

        private readonly Piece[,] _grid = new Piece[Width, Height];
        private readonly List<Piece> _choPieces = new List<Piece>(16);
        private readonly List<Piece> _hanPieces = new List<Piece>(16);
        private readonly Stack<Move> _moveHistory = new Stack<Move>(128);

        public Piece ChoGeneral { get; private set; }
        public Piece HanGeneral { get; private set; }

        public IReadOnlyList<Piece> ChoPieces => _choPieces;
        public IReadOnlyList<Piece> HanPieces => _hanPieces;
        public IReadOnlyCollection<Move> MoveHistory => _moveHistory;

        public Piece GetPieceAt(BoardCoord coord)
        {
            if (!coord.IsValid) return null;
            return _grid[coord.X, coord.Y];
        }

        public Piece GetPieceAt(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return null;
            return _grid[x, y];
        }

        public bool IsOccupied(BoardCoord coord)
        {
            return GetPieceAt(coord) != null;
        }

        public BoardCoord GetGeneralCoord(CampType camp)
        {
            var general = camp == CampType.Cho ? ChoGeneral : HanGeneral;
            return general != null && !general.IsCaptured ? general.Coord : BoardCoord.Invalid;
        }

        /// <summary>
        /// 기본 장기판 세팅
        /// </summary>
        public void SetupBoard(
            FormationType choFormation = FormationType.HorseElephantHorseElephant,
            FormationType hanFormation = FormationType.HorseElephantHorseElephant)
        {
            Clear();
            int pieceId = 1;

            // --- 초(楚) 진영 배치 (Y: 0 ~ 4) ---
            // 궁, 사
            ChoGeneral = CreateAndPlace(ref pieceId, CampType.Cho, PieceType.General, new BoardCoord(4, 1));
            CreateAndPlace(ref pieceId, CampType.Cho, PieceType.Guard, new BoardCoord(3, 0));
            CreateAndPlace(ref pieceId, CampType.Cho, PieceType.Guard, new BoardCoord(5, 0));

            // 차
            CreateAndPlace(ref pieceId, CampType.Cho, PieceType.Chariot, new BoardCoord(0, 0));
            CreateAndPlace(ref pieceId, CampType.Cho, PieceType.Chariot, new BoardCoord(8, 0));

            // 포
            CreateAndPlace(ref pieceId, CampType.Cho, PieceType.Cannon, new BoardCoord(1, 2));
            CreateAndPlace(ref pieceId, CampType.Cho, PieceType.Cannon, new BoardCoord(7, 2));

            // 졸 (5개)
            for (int x = 0; x <= 8; x += 2)
            {
                CreateAndPlace(ref pieceId, CampType.Cho, PieceType.Soldier, new BoardCoord(x, 3));
            }

            // 초 상/마 포진 배치
            PlaceHorsesAndElephants(ref pieceId, CampType.Cho, choFormation, y: 0);

            // --- 한(漢) 진영 배치 (Y: 5 ~ 9) ---
            // 궁, 사
            HanGeneral = CreateAndPlace(ref pieceId, CampType.Han, PieceType.General, new BoardCoord(4, 8));
            CreateAndPlace(ref pieceId, CampType.Han, PieceType.Guard, new BoardCoord(3, 9));
            CreateAndPlace(ref pieceId, CampType.Han, PieceType.Guard, new BoardCoord(5, 9));

            // 차
            CreateAndPlace(ref pieceId, CampType.Han, PieceType.Chariot, new BoardCoord(0, 9));
            CreateAndPlace(ref pieceId, CampType.Han, PieceType.Chariot, new BoardCoord(8, 9));

            // 포
            CreateAndPlace(ref pieceId, CampType.Han, PieceType.Cannon, new BoardCoord(1, 7));
            CreateAndPlace(ref pieceId, CampType.Han, PieceType.Cannon, new BoardCoord(7, 7));

            // 병 (5개)
            for (int x = 0; x <= 8; x += 2)
            {
                CreateAndPlace(ref pieceId, CampType.Han, PieceType.Soldier, new BoardCoord(x, 6));
            }

            // 한 상/마 포진 배치
            PlaceHorsesAndElephants(ref pieceId, CampType.Han, hanFormation, y: 9);
        }

        private void PlaceHorsesAndElephants(ref int pieceId, CampType camp, FormationType formation, int y)
        {
            // X: 1, 2, 6, 7
            PieceType p1, p2, p6, p7;

            switch (formation)
            {
                case FormationType.HorseElephantHorseElephant: // 마-상-마-상
                    p1 = PieceType.Horse; p2 = PieceType.Elephant;
                    p6 = PieceType.Horse; p7 = PieceType.Elephant;
                    break;
                case FormationType.ElephantHorseElephantHorse: // 상-마-상-마
                    p1 = PieceType.Elephant; p2 = PieceType.Horse;
                    p6 = PieceType.Elephant; p7 = PieceType.Horse;
                    break;
                case FormationType.HorseElephantElephantHorse: // 마-상-상-마 (안상)
                    p1 = PieceType.Horse; p2 = PieceType.Elephant;
                    p6 = PieceType.Elephant; p7 = PieceType.Horse;
                    break;
                case FormationType.ElephantHorseHorseElephant: // 상-마-마-상 (양귀마)
                    p1 = PieceType.Elephant; p2 = PieceType.Horse;
                    p6 = PieceType.Horse; p7 = PieceType.Elephant;
                    break;
                default:
                    p1 = PieceType.Horse; p2 = PieceType.Elephant;
                    p6 = PieceType.Horse; p7 = PieceType.Elephant;
                    break;
            }

            CreateAndPlace(ref pieceId, camp, p1, new BoardCoord(1, y));
            CreateAndPlace(ref pieceId, camp, p2, new BoardCoord(2, y));
            CreateAndPlace(ref pieceId, camp, p6, new BoardCoord(6, y));
            CreateAndPlace(ref pieceId, camp, p7, new BoardCoord(7, y));
        }

        private Piece CreateAndPlace(ref int id, CampType camp, PieceType type, BoardCoord coord)
        {
            var piece = new Piece(id++, camp, type, coord);
            _grid[coord.X, coord.Y] = piece;

            if (camp == CampType.Cho)
                _choPieces.Add(piece);
            else
                _hanPieces.Add(piece);

            return piece;
        }

        /// <summary>
        /// 테스트 및 특수 상황용 기물 수동 배치
        /// </summary>
        public Piece PlaceCustomPiece(CampType camp, PieceType type, BoardCoord coord)
        {
            int id = (_choPieces.Count + _hanPieces.Count) + 1;
            var piece = CreateAndPlace(ref id, camp, type, coord);
            if (type == PieceType.General)
            {
                if (camp == CampType.Cho) ChoGeneral = piece;
                else HanGeneral = piece;
            }
            return piece;
        }

        public void Clear()
        {
            Array.Clear(_grid, 0, _grid.Length);
            _choPieces.Clear();
            _hanPieces.Clear();
            _moveHistory.Clear();
            ChoGeneral = null;
            HanGeneral = null;
        }

        /// <summary>
        /// 수를 적용하고 기록을 스택에 보관
        /// </summary>
        public Move ApplyMove(BoardCoord from, BoardCoord to)
        {
            var piece = GetPieceAt(from);
            if (piece == null)
                throw new InvalidOperationException($"시작 지점 {from}에 기물이 없습니다.");

            var captured = GetPieceAt(to);
            if (captured != null)
            {
                captured.SetCaptured(true);
            }

            _grid[from.X, from.Y] = null;
            _grid[to.X, to.Y] = piece;
            piece.MoveTo(to);

            var move = new Move(piece, from, to, captured);
            _moveHistory.Push(move);
            return move;
        }

        /// <summary>
        /// 한수 쉼 적용
        /// </summary>
        public Move ApplyPass()
        {
            var move = Move.Pass();
            _moveHistory.Push(move);
            return move;
        }

        /// <summary>
        /// 가장 최근의 수를 취소 (무르기 / AI 탐색 백트래킹)
        /// </summary>
        public Move UndoLastMove()
        {
            if (_moveHistory.Count == 0)
                throw new InvalidOperationException("되돌릴 수(Move)가 없습니다.");

            var move = _moveHistory.Pop();
            if (move.IsPass)
                return move;

            _grid[move.To.X, move.To.Y] = move.CapturedPiece;
            _grid[move.From.X, move.From.Y] = move.Piece;
            move.Piece.MoveTo(move.From);

            if (move.CapturedPiece != null)
            {
                move.CapturedPiece.SetCaptured(false);
            }

            return move;
        }

        /// <summary>
        /// 현재 잔여 기물 점수 계산 (한나라는 후수 덤 1.5점 가산)
        /// </summary>
        public float CalculateScore(CampType camp)
        {
            float score = (camp == CampType.Han) ? 1.5f : 0f;
            var list = (camp == CampType.Cho) ? _choPieces : _hanPieces;

            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                if (!p.IsCaptured)
                {
                    score += p.Type.GetStandardScore();
                }
            }

            return score;
        }
    }
}
