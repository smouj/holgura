using Holgura.Core;
using Xunit;

namespace Holgura.Core.Tests;

public sealed class ExclusionServiceTests
{
    [Fact]
    public void Excludes_a_directory_and_everything_below_it()
    {
        var service = new ExclusionService([ExclusionRule.Prefix("D:\\Trabajo")]);
        Assert.True(service.IsExcluded("D:\\Trabajo\\informe.docx"));
        Assert.True(service.IsExcluded("D:\\Trabajo"));
        Assert.False(service.IsExcluded("D:\\Otro\\informe.docx"));
    }

    [Fact]
    public void Excludes_by_wildcard_pattern()
    {
        var service = new ExclusionService([ExclusionRule.Pattern("*.blend")]);
        Assert.True(service.IsExcluded("C:\\Temp\\escena.blend"));
        Assert.False(service.IsExcluded("C:\\Temp\\escena.png"));
    }

    [Fact]
    public void Empty_list_excludes_nothing()
    {
        var service = new ExclusionService();
        Assert.False(service.IsExcluded("C:\\Temp\\file.tmp"));
    }
}
