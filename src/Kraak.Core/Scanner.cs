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

    public IEnumerable<Finding> Scan(string filePath)
    {
        if (!File.Exists(filePath))
            yield break;

        var content = File.ReadAllText(filePath);

        foreach (var rule in _rules)
        {
            var findings = rule.Analyze(filePath, content);
            foreach (var finding in findings)
                yield return finding;
        }
    }
}