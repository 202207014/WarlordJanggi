using System.Collections.Generic;
using NUnit.Framework;
using WarlordJanggi.Core;

namespace WarlordJanggi.Tests
{
    [TestFixture]
    public class RuleEngineTests
    {
        private GameBoard _board;
        private MoveGenerator _moveGen;

        [SetUp]
        public void SetUp()
        {
            _board = new GameBoard();
            _moveGen = new MoveGenerator();
        }

        [Test]
        public void SetupBoard_ShouldPlaceCorrectNumberOfPiecesAndScore()
        {
            _board.SetupBoard(FormationType.HorseElephantHorseElephant, FormationType.HorseElephantHorseElephant);

            Assert.AreEqual(16, _board.ChoPieces.Count);
            Assert.AreEqual(16, _board.HanPieces.Count);

            // 초기 점수 검증 (초: 72, 한: 72 + 덤 1.5 = 73.5)
            Assert.AreEqual(72f, _board.CalculateScore(CampType.Cho));
            Assert.AreEqual(73.5f, _board.CalculateScore(CampType.Han));
        }

        [Test]
        public void Horse_BlockedByObstacle_ShouldNotMove()
        {
            _board.SetupBoard();
            var horse = _board.GetPieceAt(new BoardCoord(1, 0)); // 초 마
            Assert.IsNotNull(horse);
            Assert.AreEqual(PieceType.Horse, horse.Type);

            var moves = new List<BoardCoord>();
            _moveGen.GetLegalDestinations(horse, _board, moves);

            // 초기 상태에서는 졸(1, 3)과 사(3, 0) 등에 의해 멱과 진로가 제한되지만,
            // (1,0)에서 위로 가려면 (1,1)이 비어있어야 하고, 목적지는 (0,2) or (2,2)
            // (1,1)은 비어있으므로 (0,2), (2,2)로 이동 가능해야 함
            Assert.IsTrue(moves.Contains(new BoardCoord(0, 2)));
            Assert.IsTrue(moves.Contains(new BoardCoord(2, 2)));

            // (1,1)에 장애물을 놓아 멱을 막아봄
            _board.ApplyMove(new BoardCoord(1, 2), new BoardCoord(1, 1)); // 포를 (1,1)로 옮겨서 멱을 막음
            moves.Clear();
            _moveGen.GetLegalDestinations(horse, _board, moves);

            // 멱이 막혔으므로 (0,2), (2,2)는 불가능해야 함
            Assert.IsFalse(moves.Contains(new BoardCoord(0, 2)));
            Assert.IsFalse(moves.Contains(new BoardCoord(2, 2)));
        }

        [Test]
        public void Cannon_CannotJumpOverOrCaptureCannon()
        {
            _board.SetupBoard();
            var cannon = _board.GetPieceAt(new BoardCoord(1, 2)); // 초 포
            Assert.IsNotNull(cannon);
            Assert.AreEqual(PieceType.Cannon, cannon.Type);

            var moves = new List<BoardCoord>();

            // 1. 초기 상태: X=1 열에는 디딤돌이 없고 (1,7)에 한나라 포만 마주보므로 이동할 수 없어야 함
            _moveGen.GetLegalDestinations(cannon, _board, moves);
            Assert.IsFalse(moves.Contains(new BoardCoord(1, 4))); // 디딤돌 없음
            Assert.IsFalse(moves.Contains(new BoardCoord(1, 7))); // 포끼리는 잡을 수 없음
            Assert.IsFalse(moves.Contains(new BoardCoord(1, 8))); // 포를 넘을 수 없음

            // 2. (1, 4)에 디딤돌 기물(졸)을 하나 배치했을 때
            _board.PlaceCustomPiece(CampType.Cho, PieceType.Soldier, new BoardCoord(1, 4));
            moves.Clear();
            _moveGen.GetLegalDestinations(cannon, _board, moves);

            // 졸(1, 4)을 뛰어넘어 (1, 5), (1, 6)으로는 이동 가능해야 함
            Assert.IsTrue(moves.Contains(new BoardCoord(1, 5)));
            Assert.IsTrue(moves.Contains(new BoardCoord(1, 6)));

            // 하지만 여전히 한나라 포(1, 7)를 잡거나 뛰어넘어 (1, 8)로 갈 수는 없음
            Assert.IsFalse(moves.Contains(new BoardCoord(1, 7)));
            Assert.IsFalse(moves.Contains(new BoardCoord(1, 8)));
        }

        [Test]
        public void Chariot_PalaceDiagonal_CanMoveAcrossCenter()
        {
            _board.Clear();
            // 초 궁성 꼭짓점 (3, 0)에 차 배치, 중심 (4, 1)이 비어있음
            var chariot = _board.PlaceCustomPiece(CampType.Cho, PieceType.Chariot, new BoardCoord(3, 0));
            _board.PlaceCustomPiece(CampType.Cho, PieceType.General, new BoardCoord(0, 5)); // 맞궁/장군 방지용 멀리 둔 궁

            var moves = new List<BoardCoord>();
            _moveGen.GetLegalDestinations(chariot, _board, moves);

            // 중심 (4, 1)과 반대편 모서리 (5, 2) 모두 대각선으로 갈 수 있어야 함
            Assert.IsTrue(moves.Contains(new BoardCoord(4, 1)));
            Assert.IsTrue(moves.Contains(new BoardCoord(5, 2)));

            // 만약 중심 (4, 1)에 아군 기물이 있으면 (5, 2)는 못 가야 함
            _board.PlaceCustomPiece(CampType.Cho, PieceType.Guard, new BoardCoord(4, 1));
            moves.Clear();
            _moveGen.GetLegalDestinations(chariot, _board, moves);

            Assert.IsFalse(moves.Contains(new BoardCoord(4, 1))); // 아군이므로 불가
            Assert.IsFalse(moves.Contains(new BoardCoord(5, 2))); // 아군에 막혀서 건너가지 못함
        }

        [Test]
        public void RuleEngine_MakeMoveAndUndo_ShouldRestoreState()
        {
            var engine = new RuleEngine();
            engine.StartGame();

            Assert.AreEqual(CampType.Cho, engine.CurrentTurn);
            var soldierCoord = new BoardCoord(4, 3); // 중앙 초 졸

            // 중앙 졸 한 칸 전진 (4, 3) -> (4, 4)
            bool success = engine.TryMakeMove(soldierCoord, new BoardCoord(4, 4), out var move);
            Assert.IsTrue(success);
            Assert.AreEqual(CampType.Han, engine.CurrentTurn);
            Assert.AreEqual(new BoardCoord(4, 4), move.Piece.Coord);

            // 무르기 실행
            bool undoSuccess = engine.TryUndo();
            Assert.IsTrue(undoSuccess);
            Assert.AreEqual(CampType.Cho, engine.CurrentTurn);
            Assert.AreEqual(soldierCoord, move.Piece.Coord);
        }
    }
}
