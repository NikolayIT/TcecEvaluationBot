namespace TcecEvaluationBot.ConsoleUI.Services
{
    using Newtonsoft.Json.Linq;

    public class DatabaseMergableFile
    {
        public DatabaseMergableFile(JObject json)
        {
            this.Name = json["name"].Value<string>();
            this.Size = json["size"].Value<ulong>();
        }

        public string Name { get; private set; }

        public ulong Size { get; private set; }
    }
}
