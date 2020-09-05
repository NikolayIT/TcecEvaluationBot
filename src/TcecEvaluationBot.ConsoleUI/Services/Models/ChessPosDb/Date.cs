namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    using System;
    using System.Linq;

    using Newtonsoft.Json.Linq;

    public struct Date
    {
        public Date(ushort year, byte month, byte day)
        {
            this.Year = Optional<ushort>.Create(year);
            this.Month = Optional<byte>.Create(month);
            this.Day = Optional<byte>.Create(day);
        }

        public Optional<ushort> Year { get; set; }

        public Optional<byte> Month { get; set; }

        public Optional<byte> Day { get; set; }

        public static Date Min(Date lhs, Date rhs)
        {
            if (lhs.Year.Or(0) < rhs.Year.Or(0))
            {
                return lhs;
            }

            if (lhs.Year.Or(0) > rhs.Year.Or(0))
            {
                return rhs;
            }

            if (lhs.Month.Or(0) < rhs.Month.Or(0))
            {
                return lhs;
            }

            if (lhs.Month.Or(0) > rhs.Month.Or(0))
            {
                return rhs;
            }

            if (lhs.Day.Or(0) < rhs.Day.Or(0))
            {
                return lhs;
            }

            return rhs;
        }

        public static Date Max(Date lhs, Date rhs)
        {
            if (lhs.Year.Or(0) > rhs.Year.Or(0))
            {
                return lhs;
            }

            if (lhs.Year.Or(0) < rhs.Year.Or(0))
            {
                return rhs;
            }

            if (lhs.Month.Or(0) > rhs.Month.Or(0))
            {
                return lhs;
            }

            if (lhs.Month.Or(0) < rhs.Month.Or(0))
            {
                return rhs;
            }

            if (lhs.Day.Or(0) > rhs.Day.Or(0))
            {
                return lhs;
            }

            return rhs;
        }

        public static Date FromJson(JToken json)
        {
            return Date.FromString(json.Value<string>());
        }

        public static Date FromString(string str, char sep = '.')
        {
            string[] parts = str.Split(sep);
            if (parts.Length != 3)
            {
                throw new ArgumentException();
            }

            return new Date
            {
                Year = ushort.TryParse(parts[0], out ushort year) ? Optional<ushort>.Create(year) : Optional<ushort>.CreateEmpty(),
                Month = byte.TryParse(parts[1], out byte month) ? Optional<byte>.Create(month) : Optional<byte>.CreateEmpty(),
                Day = byte.TryParse(parts[2], out byte day) ? Optional<byte>.Create(day) : Optional<byte>.CreateEmpty(),
            };
        }

        public override string ToString()
        {
            return this.ToString('.');
        }

        public string ToStringYear()
        {
            return this.Year.Select(y => y.ToString("D4")).DefaultIfEmpty("????").First();
        }

        public string ToString(char sep)
        {
            var parts = new string[]
            {
                this.Year.Select(y => y.ToString("D4")).DefaultIfEmpty("????").First(),
                this.Month.Select(y => y.ToString("D2")).DefaultIfEmpty("??").First(),
                this.Day.Select(y => y.ToString("D2")).DefaultIfEmpty("??").First(),
            };
            return string.Join(sep.ToString(), parts);
        }

        public string ToStringOmitUnknown()
        {
            string str = string.Empty;
            if (this.Year.Count() != 1)
            {
                return str;
            }

            str += this.Year.Select(y => y.ToString("D4")).First();

            if (this.Month.Count() != 1)
            {
                return str;
            }

            str += ".";
            str += this.Month.Select(y => y.ToString("D2")).First();

            if (this.Day.Count() != 1)
            {
                return str;
            }

            str += ".";
            str += this.Day.Select(y => y.ToString("D2")).First();

            return str;
        }

        public bool IsBefore(Date other)
        {
            // missing is less than 1
            int y0 = this.Year.Or(0);
            int y1 = other.Year.Or(0);
            if (y0 < y1)
            {
                return true;
            }
            else if (y1 < y0)
            {
                return false;
            }

            int m0 = this.Month.Or(0);
            int m1 = other.Month.Or(0);
            if (m0 < m1)
            {
                return true;
            }
            else if (m1 < m0)
            {
                return false;
            }

            int d0 = this.Day.Or(0);
            int d1 = other.Day.Or(0);
            return d0 < d1;
        }
    }
}
