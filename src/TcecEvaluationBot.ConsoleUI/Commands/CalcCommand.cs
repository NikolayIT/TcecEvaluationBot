namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.Json;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    using RestrictedProcess.Process;

    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib.Client;

    public class CalcCommand : BaseCommand
    {
        public CalcCommand(TwitchClient twitchClient, Options options, Settings settings)
            : base(twitchClient, options, settings)
        {
        }

        public override string Execute(string message)
        {
            var parts = message.Split(' ', 2);
            if (parts.Length != 2)
            {
                return "Usage: !calc [expression]";
            }

            try
            {
                var expression = parts[1].TrimEnd();
                var code = $@"using System;
using System.Linq;
using static System.Math;
namespace ExpressionEvaluation
{{
    public static class Program
    {{
        public static void Main()
        {{
            Console.WriteLine({expression});
        }}

        public static double Factorial(double n)
        {{
            double result = 1;
            for (int i = 2; i <= n; i++)
            {{
                result *= i;
            }}

            return result;
        }}
    }}
}}";
                var dotNetCoreDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
                var compilation = CSharpCompilation.Create("ExpressionEvaluation")
                    .WithOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication))
                    .AddReferences(
                        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                        MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                        MetadataReference.CreateFromFile(Path.Combine(dotNetCoreDir, "System.Runtime.dll")))
                    .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(code));
                Directory.CreateDirectory(@"C:\Temp");
                File.WriteAllText(@"C:\Temp\ExpressionEvaluation.runtimeconfig.json", this.GenerateRuntimeConfig());
                var compilationResult = compilation.Emit(@"C:\Temp\ExpressionEvaluation.dll");
                if (!compilationResult.Success)
                {
                    return "Code error: " + string.Join(
                               "; ",
                               compilationResult.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error)
                                   .Select(x => x.GetMessage()));
                }

                var process = new RestrictedProcess("dotnet.exe", @"C:\Temp", new List<string> { "ExpressionEvaluation.dll" });
                process.Start(1500, 8 * 1024 * 1024);
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(error))
                {
                    return $"Execution error: {error}";
                }

                return $"Result: {output}";
            }
            catch (Exception e)
            {
                return $"Error: {e.Message}";
            }
        }

        private string GenerateRuntimeConfig()
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                writer.WriteStartObject("runtimeOptions");
                writer.WriteStartObject("framework");
                writer.WriteString("name", "Microsoft.NETCore.App");
                writer.WriteString("version", RuntimeInformation.FrameworkDescription.Replace(".NET Core ", string.Empty));
                writer.WriteEndObject();
                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
