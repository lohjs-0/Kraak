using Kraak.Core.Models;

namespace Kraak.Core.Rules;

public class DriftRule : IRule
{
    public string RuleId => "KRK015";
    public string Title => "Drift de Configuração Detectado";

    public IEnumerable<Finding> Analyze(string filePath, string fileContent)
    {
        foreach (var finding in DriftDetector.DetectDrift(filePath, fileContent))
            yield return finding;
    }
}