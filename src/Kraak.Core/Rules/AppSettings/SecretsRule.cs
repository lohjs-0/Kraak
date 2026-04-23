using System.Text.RegularExpressions;
using Kraak.Core.Models;

namespace Kraak.Core.Rules.AppSettings;

public class SecretsRule : IRule
{
    public string RuleId => "KRK003";
    public string Title => "Chave de API Hardcoded";

    private static readonly List<(string Name, Regex Pattern)> _patterns =
    [
        ("AWS Access Key",         new Regex(@"AKIA[0-9A-Z]{16}", RegexOptions.Compiled)),
        ("Stripe Secret Key",      new Regex(@"sk_live_[0-9a-zA-Z]{24,}", RegexOptions.Compiled)),
        ("Stripe Publishable Key", new Regex(@"pk_live_[0-9a-zA-Z]{24,}", RegexOptions.Compiled)),
        ("GitHub Token",           new Regex(@"ghp_[0-9a-zA-Z]{36}", RegexOptions.Compiled)),
        ("GitHub OAuth",           new Regex(@"gho_[0-9a-zA-Z]{36}", RegexOptions.Compiled)),
        ("Google API Key",         new Regex(@"AIza[0-9A-Za-z\-_]{35}", RegexOptions.Compiled)),
        ("Slack Token",            new Regex(@"xox[baprs]-[0-9a-zA-Z\-]{10,}", RegexOptions.Compiled)),
        ("OpenAI Key",             new Regex(@"sk-[a-zA-Z0-9]{32,}", RegexOptions.Compiled)),
        ("Azure Key",              new Regex(@"(?i)azure.{0,20}key.{0,20}[a-zA-Z0-9+/]{32,}", RegexOptions.Compiled)),
        ("Twilio Key",             new Regex(@"SK[0-9a-fA-F]{32}", RegexOptions.Compiled)),
        ("SendGrid Key",           new Regex(@"SG\.[a-zA-Z0-9\-_]{22}\.[a-zA-Z0-9\-_]{43}", RegexOptions.Compiled)),
    ];

    public IEnumerable<Finding> Analyze(string filePath, string fileContent)
    {
        if (!filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            yield break;

        var lines = fileContent.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            foreach (var (name, pattern) in _patterns)
            {
                var match = pattern.Match(line);
                if (match.Success)
                {
                    yield return new Finding
                    {
                        RuleId = RuleId,
                        Title = Title,
                        Description = $"Possível {name} encontrado hardcoded no arquivo.",
                        FilePath = filePath,
                        LineContent = $"Linha {i + 1}: {line.Trim()}",
                        Severity = Severity.Critical,
                        Suggestion = "Mova a chave para uma variável de ambiente ou use o Secret Manager. Nunca commite credenciais no código."
                    };
                }
            }
        }
    }
}