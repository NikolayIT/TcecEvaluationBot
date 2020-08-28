namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json.Linq;

    public class SegregatedEntries : IEnumerable<KeyValuePair<Origin, Entry>>
    {
        public SegregatedEntries()
        {
            this.Entries = new Dictionary<Origin, Entry>();
        }

        private Dictionary<Origin, Entry> Entries { get; set; }

        public static SegregatedEntries FromJson(JObject json)
        {
            var e = new SegregatedEntries();

            foreach (KeyValuePair<string, JToken> byLevel in json)
            {
                GameLevel level = GameLevelHelper.FromString(byLevel.Key).First();
                foreach (KeyValuePair<string, JToken> byResult in byLevel.Value.Value<JObject>())
                {
                    GameResult result = GameResultHelper.FromStringWordFormat(byResult.Key).First();

                    e.Add(level, result, Entry.FromJson(byResult.Value.Value<JObject>()));
                }
            }

            return e;
        }

        public void Add(GameLevel level, GameResult result, Entry entry)
        {
            this.Entries.Add(new Origin(level, result), entry);
        }

        public Entry Get(GameLevel level, GameResult result)
        {
            if (this.Entries.TryGetValue(new Origin(level, result), out Entry e))
            {
                return e;
            }

            return null;
        }

        IEnumerator<KeyValuePair<Origin, Entry>> IEnumerable<KeyValuePair<Origin, Entry>>.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<Origin, Entry>>)this.Entries).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<Origin, Entry>>)this.Entries).GetEnumerator();
        }
    }

    internal struct Origin
    {
        public Origin(GameLevel level, GameResult result)
        {
            this.Level = level;
            this.Result = result;
        }

        public GameLevel Level { get; set; }

        public GameResult Result { get; set; }
    }
}
