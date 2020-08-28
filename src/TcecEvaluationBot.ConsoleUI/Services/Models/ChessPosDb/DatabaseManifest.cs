namespace TcecEvaluationBot.ConsoleUI.Services
{
    using Newtonsoft.Json.Linq;

    public class DatabaseManifest
    {
        public DatabaseManifest(JObject json)
        {
            this.Schema = json["schema"].Value<string>();
            this.Version = json["version"].Value<string>();
        }

        public string Schema { get; private set; }

        public string Version { get; private set; }
    }
}
