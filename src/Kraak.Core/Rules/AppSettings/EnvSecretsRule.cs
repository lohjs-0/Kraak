using System.Text.RegularExpressions;
using Kraak.Core.Models;

namespace Kraak.Core.Rules.DotEnv;

public class EnvSecretsRule : IRule
{
    public string RuleId => "KRK007";
    public string Title => "Secret Exposto em .env";

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
        ("Generic Password",       new Regex(@"(?i)^(PASSWORD|PASSWD|SECRET|API_KEY|API_SECRET|TOKEN)\s*=\s*.{6,}", RegexOptions.Compiled)),
    ];

    public IEnumerable<Finding> Analyze(string filePath, string fileContent)
    {
        if (!Path.GetFileName(filePath).StartsWith(".env", StringComparison.OrdinalIgnoreCase))
            yield break;

        var lines = fileContent.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            foreach (var (name, pattern) in _patterns)
            {
                if (pattern.IsMatch(line))
                {
                    yield return new Finding
                    {
                        RuleId = RuleId,
                        Title = Title,
                        Description = $"Possível {name} encontrado exposto no arquivo .env.",
                        FilePath = filePath,
                        LineContent = $"Linha {i + 1}: {line}",
                        Severity = Severity.Critical,
                        Suggestion = "Use variáveis de ambiente do servidor ou um cofre de segredos como AWS Secrets Manager, Azure Key Vault ou HashiCorp Vault."
                    };
                    break;
                }
            }
        }
    }
}