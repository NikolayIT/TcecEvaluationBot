using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    public struct Date
    {
        public Optional<ushort> Year { get; set; }
        public Optional<byte> Month { get; set; }
        public Optional<byte> Day { get; set; }

        public static Date Min(Date lhs, Date rhs)
        {
            if (lhs.Year.Or(0) < rhs.Year.Or(0))
                return lhs;
            if (lhs.Year.Or(0) > rhs.Year.Or(0))
                return rhs;

            if (lhs.Month.Or(0) < rhs.Month.Or(0))
                return lhs;
            if (lhs.Month.Or(0) > rhs.Month.Or(0))
                return rhs;

            if (lhs.Day.Or(0) < rhs.Day.Or(0))
                return lhs;

            return rhs;
        }

        public static Date Max(Date lhs, Date rhs)
        {
            if (lhs.Year.Or(0) > rhs.Year.Or(0))
                return lhs;
            if (lhs.Year.Or(0) < rhs.Year.Or(0))
                return rhs;

            if (lhs.Month.Or(0) > rhs.Month.Or(0))
                return lhs;
            if (lhs.Month.Or(0) < rhs.Month.Or(0))
                return rhs;

            if (lhs.Day.Or(0) > rhs.Day.Or(0))
                return lhs;

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
                Year = UInt16.TryParse(parts[0], out ushort year) ? Optional<ushort>.Create(year) : Optional<ushort>.CreateEmpty(),
                Month = Byte.TryParse(parts[1], out byte month) ? Optional<byte>.Create(month) : Optional<byte>.CreateEmpty(),
                Day = Byte.TryParse(parts[2], out byte day) ? Optional<byte>.Create(day) : Optional<byte>.CreateEmpty()
            };
        }

        public Date(UInt16 year, Byte month, Byte day)
        {
            Year = Optional<ushort>.Create(year);
            Month = Optional<byte>.Create(month);
            Day = Optional<byte>.Create(day);
        }

        public override string ToString()
        {
            return ToString('.');
        }

        public string ToString(char sep)
        {
            return string.Join(sep.ToString(), new string[]{
                Year.Select(y => y.ToString("D4")).DefaultIfEmpty("????").First(),
                Month.Select(y => y.ToString("D2")).DefaultIfEmpty("??").First(),
                Day.Select(y => y.ToString("D2")).DefaultIfEmpty("??").First()
            });
        }

        public string ToStringOmitUnknown()
        {
            string str = "";
            if (Year.Count() != 1)
            {
                return str;
            }
            str += Year.Select(y => y.ToString("D4")).First();

            if (Month.Count() != 1)
            {
                return str;
            }
            str += ".";
            str += Month.Select(y => y.ToString("D2")).First();

            if (Day.Count() != 1)
            {
                return str;
            }
            str += ".";
            str += Day.Select(y => y.ToString("D2")).First();

            return str;
        }

        public bool IsBefore(Date other)
        {
            // missing is less than 1

            int y0 = Year.Or(0);
            int y1 = other.Year.Or(0);
            if (y0 < y1)
            {
                return true;
            }
            else if (y1 < y0)
            {
                return false;
            }

            int m0 = Month.Or(0);
            int m1 = other.Month.Or(0);
            if (m0 < m1)
            {
                return true;
            }
            else if (m1 < m0)
            {
                return false;
            }

            int d0 = Day.Or(0);
            int d1 = other.Day.Or(0);
            return d0 < d1;
        }
    }
}
