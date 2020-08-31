namespace TcecEvaluationBot.ConsoleUI.Services.Models
{
    using ChessDotNet;
    using System;
    using System.Globalization;

    public class ChessDbcnScore : IComparable
    {
        private static readonly int KnownResultThreashold = 18000;
        private static readonly int CursedDtz0 = 20000;
        private static readonly int Dtz0 = 30000;

        public ChessDbcnScore(int v, Player sideToMove)
        {
            this.Value = GetValueForPlayer(v, sideToMove);
            this.Perf = 0;
        }

        public ChessDbcnScore(string str, Player sideToMove)
        {
            this.Value = GetValueForPlayer(ValueFromString(str), sideToMove);
            this.Perf = str == null ? double.NaN : WinPctFromEval(this.Value);
        }

        public ChessDbcnScore(int v, double pct, Player sideToMove)
        {
            this.Value = GetValueForPlayer(v, sideToMove);
            this.Perf = pct;
        }

        public ChessDbcnScore(string value, string winpct, Player sideToMove)
        {
            this.Value = GetValueForPlayer(ValueFromString(value), sideToMove);
            this.Perf = WinPctFromString(winpct);
        }

        public int Value { get; }

        public double Perf { get; }

        public override string ToString()
        {
            int abs = Math.Abs(this.Value);
            string sign = this.Value < 0 ? "-" : string.Empty;
            if (abs > CursedDtz0)
            {
                return "DTZ " + sign + (Dtz0 - abs).ToString();
            }
            else if (abs > KnownResultThreashold)
            {
                // cursed win/loss
                return "DTZ " + sign + (CursedDtz0 - abs).ToString();
            }

            return (this.Value / 100.0).ToString("#0.00");
        }

        public int CompareTo(object b)
        {
            if (b == null)
            {
                return 1;
            }

            if (!(b is ChessDbcnScore bb))
            {
                throw new ArgumentException("rhs is not a Score");
            }

            return this.Value.CompareTo(bb.Value);
        }

        private static int ValueFromString(string str)
        {
            if (str == null)
            {
                return 0;
            }

            try
            {
                var parts = str.Split(' ');
                if (parts.Length == 2)
                {
                    return int.Parse(parts[1].Trim("()".ToCharArray()));
                }
                else
                {
                    return int.Parse(parts[0]);
                }
            }
            catch
            {
                return 0;
            }
        }

        private static double WinPctFromString(string str)
        {
            if (str == null)
            {
                return double.NaN;
            }

            try
            {
                return double.Parse(str, CultureInfo.InvariantCulture) / 100.0;
            }
            catch
            {
            }

            return double.NaN;
        }

        private static double WinPctFromEval(int eval)
        {
            if (Math.Abs(eval) >= KnownResultThreashold)
            {
                return eval < 0 ? 0.0 : 1.0;
            }

            return 1.0 / (1.0 + Math.Exp(-eval / 90.0));
        }

        private static int GetValueForPlayer(int value, Player sideToMove)
        {
            return sideToMove == Player.White ? value : -value;
        }
    }
}
