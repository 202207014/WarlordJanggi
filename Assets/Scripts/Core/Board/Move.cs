namespace WarlordJanggi.Core
{
    /// <summary>
    /// 장기의 한 수(Move)를 나타내는 구조체
    /// </summary>
    public readonly struct Move
    {
        public readonly Piece Piece;
        public readonly BoardCoord From;
        public readonly BoardCoord To;
        public readonly Piece CapturedPiece;
        public readonly bool IsPass;

        public bool IsCapture => CapturedPiece != null;

        public Move(Piece piece, BoardCoord from, BoardCoord to, Piece capturedPiece = null)
        {
            Piece = piece;
            From = from;
            To = to;
            CapturedPiece = capturedPiece;
            IsPass = false;
        }

        private Move(bool isPass)
        {
            Piece = null;
            From = BoardCoord.Invalid;
            To = BoardCoord.Invalid;
            CapturedPiece = null;
            IsPass = isPass;
        }

        public static Move Pass() => new Move(true);

        public override string ToString()
        {
            if (IsPass) return "[한수 쉼 (Pass)]";
            string action = IsCapture ? $"포획({CapturedPiece.Type})" : "이동";
            return $"{Piece.Type} {From} -> {To} [{action}]";
        }
    }
}
