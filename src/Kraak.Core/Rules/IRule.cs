using Kraak.Core.Models;

namespace Kraak.Core.Rules;

public interface IRule
{
    string RuleId { get; }
    string Title { get; }

    IEnumerable<Finding> Analyze(string filePath, string fileContent);
}