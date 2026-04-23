using Kraak.Core;
using Kraak.Core.Models;
using Kraak.Core.Rules.AppSettings;
using Kraak.Core.Rules.DotEnv;

var filePath = args.Length > 0 ? args[0] : "appsettings.json";

Console.WriteLine($"""
    
     ██╗  ██╗██████╗  █████╗  █████╗ ██╗  ██╗
     ██║ ██╔╝██╔══██╗██╔══██╗██╔══██╗██║ ██╔╝
     █████╔╝ ██████╔╝███████║███████║█████╔╝ 
     ██╔═██╗ ██╔══██╗██╔══██║██╔══██║██╔═██╗ 
     ██║  ██╗██║  ██║██║  ██║██║  ██║██║  ██╗
     ╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝
     Security Analyzer | v0.1.0
    
    """);

var scanner = new Scanner();
scanner.RegisterRule(new ConnStringRule());
scanner.RegisterRule(new AllowedHostsRule());
scanner.RegisterRule(new SecretsRule());
scanner.RegisterRule(new EnvGitignoreRule());
scanner.RegisterRule(new HttpsRule());
scanner.RegisterRule(new DebugModeRule());
scanner.RegisterRule(new EnvSecretsRule());
scanner.RegisterRule(new EntropyRule());

Console.WriteLine($"🔍 Analisando: {filePath}\n");

var findings = scanner.Scan(filePath).ToList();

if (findings.Count == 0)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✅ Nenhum problema encontrado!");
    Console.ResetColor();
}
else
{
    foreach (var finding in findings)
    {
        var color = finding.Severity switch
        {
            Severity.Critical => ConsoleColor.Red,
            Severity.Warning => ConsoleColor.Yellow,
            Severity.Info => ConsoleColor.Cyan,
            _ => ConsoleColor.White
        };

        Console.ForegroundColor = color;
        Console.WriteLine($"[{finding.Severity.ToString().ToUpper()}] {finding.RuleId} — {finding.Title}");
        Console.ResetColor();
        Console.WriteLine($"  📄 {finding.FilePath}");
        Console.WriteLine($"  💬 {finding.Description}");
        Console.WriteLine($"  🔎 {finding.LineContent}");
        Console.WriteLine();
    }

    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"⚠️  {findings.Count} problema(s) encontrado(s).");
    Console.ResetColor();
}