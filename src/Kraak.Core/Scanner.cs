using Kraak.Core.Models;
using Kraak.Core.Rules;

namespace Kraak.Core;

public class Scanner
{
    private readonly List<IRule> _rules;

    public Scanner()
    {
        _rules = new List<IRule>();
    }

    public void RegisterRule(IRule rule)
    {
        _rules.Add(rule);
    }

    public int GetRuleCount() => _rules.Count;

    public IEnumerable<Finding> Scan(string filePath)
    {
        if (!File.Exists(filePath)) yield break;
        var content = File.ReadAllText(filePath);
        foreach (var rule in _rules)
            foreach (var finding in rule.Analyze(filePath, content))
                yield return finding;
    }

    public IEnumerable<Finding> ScanAll(IReadOnlyList<(string FilePath, string Content)> files)
    {
        foreach (var rule in _rules)
            foreach (var finding in rule.AnalyzeAll(files))
                yield return finding;
    }
}