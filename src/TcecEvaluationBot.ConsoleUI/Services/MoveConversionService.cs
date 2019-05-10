namespace TcecEvaluationBot.ConsoleUI.Services
{
    using ChessDotNet;
    using ChessDotNet.Pieces;

    public class MoveConversionService
    {
        public string AlgebraicToSan(string fen, params string[] moves)
        {
            var game = new ChessGame(fen);
            var playerToMove = fen.Contains(" b ") ? Player.Black : Player.White;
            for (var index = 0; index < moves.Length - 1; index++)
            {
                var move = moves[index];
                if (move == null || move.Length < 4)
                {
                    return null;
                }

                game.MakeMove(
                    new Move(move[0].ToString() + move[1], move[2].ToString() + move[3], playerToMove, 'Q'),
                    false);
                playerToMove = playerToMove == Player.White ? Player.Black : Player.White;
            }

            return this.AlgebraicToSan(game.GetFen(), moves[moves.Length - 1]);
        }

        public string AlgebraicToSan(string fen, string algebraicMove)
        {
            if (algebraicMove == null || algebraicMove.Length < 4)
            {
                return null;
            }

            var game = new ChessGame(fen);

            var playerToMove = fen.Contains(" b ") ? Player.Black : Player.White;
            var playerToDefend = fen.Contains(" b ") ? Player.White : Player.Black;
            var piece = game.GetPieceAt(new Position(algebraicMove[0].ToString() + algebraicMove[1]));
            var moveType = game.MakeMove(
                new Move(
                    algebraicMove[0].ToString() + algebraicMove[1],
                    algebraicMove[2].ToString() + algebraicMove[3],
                    playerToMove,
                    'Q'),
                false);
            var pieceChar = piece is Pawn ? '\0' : char.ToUpper(piece.GetFenCharacter());
            var pieceCharTakes = piece is Pawn ? algebraicMove[0] : char.ToUpper(piece.GetFenCharacter());
            var additionalInfoChar = '\0';

            //// TODO: Rooks and Knights from square when ambiguous
            if (piece is Rook)
            {
                if (algebraicMove[0] == algebraicMove[2])
                {
                    additionalInfoChar = algebraicMove[1];
                }

                if (algebraicMove[1] == algebraicMove[3])
                {
                    additionalInfoChar = algebraicMove[0];
                }
            }

            string san;
            switch (moveType)
            {
                case MoveType.Invalid:
                    san = "xxx";
                    break;
                case MoveType.Move | MoveType.Castling:
                    san = algebraicMove[2] == 'c' ? "O-O-O" : "O-O";
                    break;
                case MoveType.Move:
                    san = $"{pieceChar}{additionalInfoChar}{algebraicMove[2]}{algebraicMove[3]}";
                    break;
                case MoveType.Capture:
                    san = $"{pieceCharTakes}{additionalInfoChar}x{algebraicMove[2]}{algebraicMove[3]}";
                    break;
                case MoveType.Promotion:
                    san = $"{algebraicMove[2]}{algebraicMove[3]}=" + (algebraicMove.Length > 4 ? char.ToUpper(algebraicMove[4]) : '?');
                    break;
                default:
                    san = $"{pieceCharTakes}{additionalInfoChar}x{algebraicMove[2]}{algebraicMove[3]}";
                    break;
            }

            if (game.IsCheckmated(playerToDefend))
            {
                san += "#";
            }
            else if (game.IsInCheck(playerToDefend))
            {
                san += "+";
            }

            return san;
        }
    }
}
