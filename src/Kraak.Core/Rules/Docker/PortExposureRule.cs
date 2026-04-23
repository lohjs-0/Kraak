using Kraak.Core.Models;

namespace Kraak.Core.Rules.Docker;

public class PortExposureRule : IRule
{
    public string RuleId => "KRK014";
    public string Title => "Porta Sensível Exposta ao Host";

    private static readonly Dictionary<int, string> _sensitivePorts = new()
    {
        [22]    = "SSH",
        [23]    = "Telnet",
        [3306]  = "MySQL",
        [5432]  = "PostgreSQL",
        [6379]  = "Redis",
        [27017] = "MongoDB",
        [9200]  = "Elasticsearch",
        [2375]  = "Docker daemon (sem TLS)",
        [2376]  = "Docker daemon (com TLS)"
    };

    public IEnumerable<Finding> Analyze(string filePath, string fileContent)
    {
        if (!IsDockerCompose(filePath)) yield break;

        var lines = fileContent.Split('\n');
        bool insidePorts = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();

            if (trimmed.Equals("ports:", StringComparison.OrdinalIgnoreCase))
            {
                insidePorts = true;
                continue;
            }

            if (insidePorts)
            {
                if (!trimmed.StartsWith('-') && trimmed.Contains(':') && !trimmed.StartsWith('#'))
                {
                    insidePorts = false;
                    continue;
                }

                if (trimmed.StartsWith('-'))
                {
                    var portMapping = trimmed.TrimStart('-').Trim().Trim('"');
                    var hostPart = portMapping.Split(':')[0].Trim();

                    if (int.TryParse(hostPart, out int port) && _sensitivePorts.TryGetValue(port, out var service))
                        yield return new Finding
                        {
                            RuleId = RuleId,
                            Title = Title,
                            Description = $"Porta {port} ({service}) exposta diretamente ao host.",
                            FilePath = filePath,
                            LineContent = $"Linha {i + 1}: {lines[i].TrimEnd()}",
                            Severity = port == 2375 ? Severity.Critical : Severity.Warning,
                            Suggestion = port == 2375
                                ? "Nunca exponha o Docker daemon sem TLS. Remova essa porta imediatamente."
                                : $"Remova a exposição da porta {port} ({service}) ao host e acesse o serviço via rede interna do Docker."
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