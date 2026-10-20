namespace Holgura.Core;

public enum ExclusionKind
{
    /// <summary>A directory or file; everything below it is excluded.</summary>
    Prefix = 0,

    /// <summary>A wildcard pattern such as *.blend, matched against the file name.</summary>
    Pattern = 1,
}

public sealed record ExclusionRule(string Value, ExclusionKind Kind)
{
    public static ExclusionRule Prefix(string path) => new(PathUtil.Normalize(path), ExclusionKind.Prefix);

    public static ExclusionRule Pattern(string pattern) => new(pattern, ExclusionKind.Pattern);
}
