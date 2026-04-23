using Kraak.Core.Models;

namespace Kraak.Core.Rules;

public interface IRule
{
    string RuleId { get; }
    string Title { get; }

    IEnumerable<Finding> Analyze(string filePath, string fileContent);
    IEnumerable<Finding> AnalyzeAll(IReadOnlyList<(string FilePath, string Content)> files) =>
        files.SelectMany(f => Analyze(f.FilePath, f.Content));
}