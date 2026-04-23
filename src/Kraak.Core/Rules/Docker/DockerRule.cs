using System.Text.RegularExpressions;
using Kraak.Core.Models;

namespace Kraak.Core.Rules.Docker;

public class DockerRule : IRule
{
    public string RuleId => "KRK008";
    public string Title => "Secret Exposto em Docker Compose";

    private static readonly List<(string Name, Regex Pattern)> _patterns =
    [
        ("Senha em variável de ambiente", new Regex(@"(?i)(PASSWORD|PASSWD|SECRET|API_KEY|TOKEN)\s*:\s*.{6,}", RegexOptions.Compiled)),
        ("AWS Access Key",               new Regex(@"AKIA[0-9A-Z]{16}", RegexOptions.Compiled)),
        ("Stripe Secret Key",            new Regex(@"sk_live_[0-9a-zA-Z]{24,}", RegexOptions.Compiled)),
        ("GitHub Token",                 new Regex(@"ghp_[0-9a-zA-Z]{36}", RegexOptions.Compiled)),
        ("OpenAI Key",                   new Regex(@"sk-[a-zA-Z0-9]{32,}", RegexOptions.Compiled)),
    ];

    public IEnumerable<Finding> Analyze(string filePath, string fileContent)
    {
        var name = Path.GetFileName(filePath).ToLower();
        Console.WriteLine($"[DockerRule] filename: '{name}'");
        if (!name.Contains("docker-compose") && !name.Contains("compose"))
            yield break;

        var lines = fileContent.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            foreach (var (patternName, pattern) in _patterns)
            {
                if (pattern.IsMatch(line))
                {
                    yield return new Finding
                    {
                        RuleId = RuleId,
                        Title = Title,
                        Description = $"Possível {patternName} encontrado exposto no docker-compose.",
                        FilePath = filePath,
                        LineContent = $"Linha {i + 1}: {line}",
                        Severity = Severity.Critical
                    };
                    break;
                }
            }
        }
    }
}