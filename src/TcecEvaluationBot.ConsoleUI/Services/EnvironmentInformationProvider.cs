namespace TcecEvaluationBot.ConsoleUI.Services
{
    using System;
    using System.Reflection;

    public class EnvironmentInformationProvider
    {
        public string VersionNumber
        {
            get
            {
                var runtimeVersion = typeof(Program).GetTypeInfo().Assembly
                    .GetCustomAttribute<AssemblyFileVersionAttribute>();
                var version = new Version(runtimeVersion.Version);
                return $"{version.Major}.{version.Minor}";
            }
        }
    }
}
