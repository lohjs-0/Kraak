using Kraak.Core.Models;

namespace Kraak.Core.Rules.Docker;

public class CapAddRule : IRule
{
    public string RuleId => "KRK012";
    public string Title => "Capabilities Perigosas Adicionadas ao Container";

    private static readonly Dictionary<string, string> _dangerousCaps = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SYS_ADMIN"]  = "permite operações administrativas do kernel e montagem de filesystems",
        ["NET_ADMIN"]  = "permite modificar interfaces de rede, rotas e firewall do host",
        ["SYS_PTRACE"] = "permite inspecionar e modificar processos de outros containers",
        ["ALL"]        = "concede TODAS as capabilities, equivalente a modo privilegiado"
    };

    public IEnumerable<Finding> Analyze(string filePath, string fileContent)
    {
        if (!IsDockerCompose(filePath)) yield break;

        var lines = fileContent.Split('\n');
        bool insideCapAdd = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();

            if (trimmed.Equals("cap_add:", StringComparison.OrdinalIgnoreCase))
            {
                insideCapAdd = true;
                continue;
            }

            if (insideCapAdd)
            {
                if (!trimmed.StartsWith('-') && trimmed.Contains(':'))
                {
                    insideCapAdd = false;
                    continue;
                }

                if (trimmed.StartsWith('-'))
                {
                    var cap = trimmed.TrimStart('-').Trim().Trim('"');
                    if (_dangerousCaps.TryGetValue(cap, out var reason))
                        yield return new Finding
                        {
                            RuleId = RuleId,
                            Title = Title,
                            Description = $"Capability '{cap}' adicionada: {reason}.",
                            FilePath = filePath,
                            LineContent = $"Linha {i + 1}: {lines[i].TrimEnd()}",
                            Severity = cap.Equals("ALL", StringComparison.OrdinalIgnoreCase)
                                ? Severity.Critical
                                : Severity.Warning,
                            Suggestion = cap.Equals("ALL", StringComparison.OrdinalIgnoreCase)
                                ? "Remova 'ALL' e adicione apenas as capabilities específicas que a aplicação requer."
                                : $"Avalie se '{cap}' é realmente necessária e considere alternativas com menor privilégio."
                        };
                }
            }
        }
    }

    private static bool IsDockerCompose(string filePath)
    {
        var name = Path.GetFileName(filePath).ToLower();
        return name.Contains("docker-compose") || name.Contains("compose");
    }
}