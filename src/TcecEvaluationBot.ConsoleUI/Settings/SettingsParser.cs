namespace TcecEvaluationBot.ConsoleUI.Settings
{
    using System.IO;

    using Newtonsoft.Json;

    public class SettingsParser
    {
        public Settings ParseSettings(string appSettingsFileName)
        {
            if (File.Exists(appSettingsFileName))
            {
                return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(appSettingsFileName));
            }

            return new Settings();
        }
    }
}
