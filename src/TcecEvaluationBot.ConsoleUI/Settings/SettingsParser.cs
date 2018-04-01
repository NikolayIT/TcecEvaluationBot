namespace TcecEvaluationBot.ConsoleUI.Settings
{
    using System.IO;

    using Newtonsoft.Json;

    public class SettingsParser
    {
        public Settings ParseSettings(string appSettingsFileName)
        {
            return File.Exists(appSettingsFileName)
                       ? JsonConvert.DeserializeObject<Settings>(File.ReadAllText(appSettingsFileName))
                       : new Settings();
        }
    }
}
