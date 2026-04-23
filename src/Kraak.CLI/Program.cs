using Kraak.Core;
using Kraak.Core.Models;
using Kraak.Core.Rules;
using Kraak.Core.Rules.AppSettings;
using Kraak.Core.Rules.DotEnv;
using Kraak.Core.Rules.Docker;

var command = args.Length > 0 ? args[0] : "scan";
var filePath = args.Length > 1 ? args[1] : "appsettings.json";

// Se o primeiro arg não é um comando conhecido, assume que é o arquivo
if (command != "scan" && command != "snapshot")
{
    filePath = command;
    command = "scan";
}

Console.WriteLine($"""
    
     ██╗  ██╗██████╗  █████╗  █████╗ ██╗  ██╗
     ██║ ██╔╝██╔══██╗██╔══██╗██╔══██╗██║ ██╔╝
     █████╔╝ ██████╔╝███████║███████║█████╔╝ 
     ██╔═██╗ ██╔══██╗██╔══██║██╔══██║██╔═██╗ 
     ██║  ██╗██║  ██║██║  ██║██║  ██║██║  ██╗
     ╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝
     Security Analyzer | v0.1.0
    
    """);

if (!File.Exists(filePath))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ Arquivo não encontrado: {filePath}");
    Console.ResetColor();
    return;
}

// Comando: snapshot
if (command == "snapshot")
{
    var content = File.ReadAllText(filePath);
    DriftDetector.SaveSnapshot(filePath, content);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"✅ Snapshot salvo para '{filePath}'.");
    Console.ResetColor();
    return;
}

// Comando: scan (default)
var scanner = new Scanner();

// AppSettings
scanner.RegisterRule(new ConnStringRule());
scanner.RegisterRule(new AllowedHostsRule());
scanner.RegisterRule(new SecretsRule());
scanner.RegisterRule(new HttpsRule());
scanner.RegisterRule(new DebugModeRule());
scanner.RegisterRule(new EnvSecretsRule());
scanner.RegisterRule(new EntropyRule());

// DotEnv
scanner.RegisterRule(new EnvGitignoreRule());

// Docker
scanner.RegisterRule(new DockerRule());
scanner.RegisterRule(new PrivilegedRule());
scanner.RegisterRule(new RunAsRootRule());
scanner.RegisterRule(new CapAddRule());
scanner.RegisterRule(new HostNetworkRule());
scanner.RegisterRule(new PortExposureRule());

// Drift
scanner.RegisterRule(new DriftRule());

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
            Severity.Warning  => ConsoleColor.Yellow,
            Severity.Info     => ConsoleColor.Cyan,
            _                 => ConsoleColor.White
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