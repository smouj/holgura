using Holgura.Core;
using Xunit;

namespace Holgura.Core.Tests;

public sealed class PathSafetyValidatorTests
{
    private static PathSafetyValidator CreateValidator() => new(
    [
        "C:\\",
        "C:\\Windows",
        "C:\\Program Files",
        "C:\\Program Files (x86)",
        "C:\\Users",
        "C:\\Users\\Tester",
        "C:\\Users\\Tester\\Documents",
        "C:\\Users\\Tester\\Desktop",
    ]);

    [Theory]
    [InlineData("C:\\")]
    [InlineData("C:\\Windows")]
    [InlineData("C:\\Program Files")]
    [InlineData("C:\\Program Files (x86)")]
    [InlineData("C:\\Users")]
    [InlineData("C:\\Users\\Tester")]
    [InlineData("C:\\Users\\Tester\\Documents")]
    [InlineData("C:\\Users\\Tester\\Desktop")]
    public void Rejects_protected_roots(string root)
    {
        var validator = CreateValidator();
        Assert.Throws<ProtectedPathException>(() => validator.EnsureSafeRoot(root));
    }

    [Fact]
    public void Rejects_a_root_that_contains_a_protected_child()
    {
        var validator = CreateValidator();
        Assert.Throws<ProtectedPathException>(() => validator.EnsureSafeRoot("C:\\Users\\Tester"));
    }

    [Theory]
    [InlineData("C:\\Users\\Tester\\AppData\\Local\\Temp")]
    [InlineData("C:\\Windows\\Temp")]
    [InlineData("D:\\Cache")]
    public void Accepts_known_disposable_roots(string root)
    {
        var validator = CreateValidator();
        validator.EnsureSafeRoot(root);
    }

    [Fact]
    public void Rejects_a_file_that_escapes_the_root()
    {
        var validator = CreateValidator();
        Assert.Throws<ProtectedPathException>(() =>
            validator.EnsureContained("C:\\Users\\Tester\\AppData\\Local\\Temp", "C:\\Users\\Tester\\Documents\\cv.docx"));
    }

    [Fact]
    public void Rejects_a_sibling_with_a_shared_prefix()
    {
        var validator = CreateValidator();
        Assert.Throws<ProtectedPathException>(() =>
            validator.EnsureContained("C:\\Temp", "C:\\Temporary\\file.tmp"));
    }

    [Fact]
    public void Accepts_a_file_inside_the_root()
    {
        var validator = CreateValidator();
        validator.EnsureContained(
            "C:\\Users\\Tester\\AppData\\Local\\Temp",
            "C:\\Users\\Tester\\AppData\\Local\\Temp\\sub\\file.tmp");
    }
}
