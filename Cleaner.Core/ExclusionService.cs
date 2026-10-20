using System.IO.Enumeration;

namespace Cleaner.Core;

public sealed class ExclusionService
{
    private readonly List<ExclusionRule> _rules = [];

    public ExclusionService()
    {
    }

    public ExclusionService(IEnumerable<ExclusionRule> rules)
    {
        _rules.AddRange(rules);
    }

    public IReadOnlyList<ExclusionRule> Rules => _rules;

    public void Add(ExclusionRule rule)
    {
        if (!_rules.Contains(rule))
        {
            _rules.Add(rule);
        }
    }

    public bool Remove(ExclusionRule rule) => _rules.Remove(rule);

    public void Replace(IEnumerable<ExclusionRule> rules)
    {
        _rules.Clear();
        _rules.AddRange(rules.Distinct());
    }

    public bool IsExcluded(string path)
    {
        if (_rules.Count == 0 || string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        string normalized;
        try
        {
            normalized = PathUtil.Normalize(path);
        }
        catch (Exception)
        {
            return false;
        }

        var fileName = Path.GetFileName(normalized);

        foreach (var rule in _rules)
        {
            if (rule.Kind == ExclusionKind.Prefix)
            {
                if (string.Equals(normalized, rule.Value, StringComparison.OrdinalIgnoreCase) ||
                    PathUtil.IsUnder(normalized, rule.Value) ||
                    PathUtil.IsUnder(rule.Value, normalized))
                {
                    return true;
                }
            }
            else if (FileSystemName.MatchesSimpleExpression(rule.Value, fileName, ignoreCase: true))
            {
                return true;
            }
        }

        return false;
    }
}
