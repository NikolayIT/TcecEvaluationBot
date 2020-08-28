using System.Collections.Generic;
using System.Linq;

namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    public class Optional<T> : IEnumerable<T>
    {
        private readonly T[] data;

        private Optional(T[] data)
        {
            this.data = data;
        }

        public static Optional<T> Create(T value)
        {
            return new Optional<T>(new T[] { value });
        }

        public static Optional<T> CreateEmpty()
        {
            return new Optional<T>(new T[0]);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)this.data).GetEnumerator();
        }

        System.Collections.IEnumerator
            System.Collections.IEnumerable.GetEnumerator()
        {
            return this.data.GetEnumerator();
        }

        public T Or(T def)
        {
            if (data.Count() == 0)
            {
                return def;
            }
            else
            {
                return data[0];
            }
        }
    }
}
