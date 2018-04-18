namespace TcecEvaluationBot.ConsoleUI.Services
{
    using System;
    using System.IO;
    using System.Text;

    public class FileLogger : ILogger
    {
        private readonly StreamWriter streamWriter;

        public FileLogger(string filePath)
        {
            this.streamWriter = new StreamWriter(filePath, true, Encoding.Unicode) { AutoFlush = true };
        }

        public void Log(string message)
        {
            this.streamWriter.WriteLine($"[{DateTime.UtcNow:o}] {message}");
        }
    }
}
