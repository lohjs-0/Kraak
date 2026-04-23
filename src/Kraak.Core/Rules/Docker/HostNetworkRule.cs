using Kraak.Core.Models;

namespace Kraak.Core.Rules.Docker;

public class HostNetworkRule : IRule
{
    public string RuleId => "KRK013";
    public string Title => "Container Usando Rede do Host";

    public IEnumerable<Finding> Analyze(string filePath, string fileContent)
    {
        if (!IsDockerCompose(filePath)) yield break;

        var lines = fileContent.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (!trimmed.StartsWith("network_mode:", StringComparison.OrdinalIgnoreCase)) continue;

            var value = trimmed["network_mode:".Length..].Trim().Trim('"');
            if (value.Equals("host", StringComparison.OrdinalIgnoreCase))
                yield return new Finding
                {
                    RuleId = RuleId,
                    Title = Title,
                    Description = "'network_mode: host' compartilha a stack de rede do host, eliminando o isolamento de rede do container.",
                    FilePath = filePath,
                    LineContent = $"Linha {i + 1}: {lines[i].TrimEnd()}",
                    Severity = Severity.Critical,
                    Suggestion = "Remova 'network_mode: host' e use redes bridge nomeadas. Exponha apenas as portas necessárias via 'ports'."
                };
        }
    }

    private static bool IsDockerCompose(string filePath)
    {
        var name = Path.GetFileName(filePath).ToLower();
        return name.Contains("docker-compose") || name.Contains("compose");
    }
}