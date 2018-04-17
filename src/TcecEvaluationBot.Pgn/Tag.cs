namespace TcecEvaluationBot.Pgn
{
    using System;

    public class Tag
    {
        public Tag(string tagLine)
        {
            tagLine = tagLine.Trim();
            if (!tagLine.StartsWith("[") || !tagLine.EndsWith("]") || !tagLine.Contains("\""))
            {
                throw new ArgumentException("Invalid tag line!", nameof(tagLine));
            }

            tagLine = tagLine.Trim('[', ']', '"');
            var tagParts = tagLine.Split('"', 2);
            this.Name = tagParts[0].Trim();
            this.Value = tagParts[1].Replace("\\\"", "\"");
        }

        public string Name { get; set; }

        public string Value { get; set; }

        public override string ToString()
        {
            return $"[{this.Name} \"{this.Value.Replace("\"", "\\\"")}\"]";
        }
    }
}
