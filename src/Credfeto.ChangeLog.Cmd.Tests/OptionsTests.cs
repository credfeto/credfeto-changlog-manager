using System.Collections.Generic;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Cmd.Tests;

public sealed class OptionsTests : TestBase
{
    [Fact]
    public void ChangeLogPropertyIsSetCorrectly()
    {
        Options options = new() { ChangeLog = "CHANGELOG.md" };
        Assert.Equal(expected: "CHANGELOG.md", actual: options.ChangeLog);
    }

    [Fact]
    public void VersionPropertyIsSetCorrectly()
    {
        Options options = new() { Version = "1.0.0" };
        Assert.Equal(expected: "1.0.0", actual: options.Version);
    }

    [Fact]
    public void ExtractPropertyIsSetCorrectly()
    {
        Options options = new() { Extract = "output.md" };
        Assert.Equal(expected: "output.md", actual: options.Extract);
    }

    [Fact]
    public void AddPropertyIsSetCorrectly()
    {
        Options options = new() { Add = "Added" };
        Assert.Equal(expected: "Added", actual: options.Add);
    }

    [Fact]
    public void RemovePropertyIsSetCorrectly()
    {
        Options options = new() { Remove = "Fixed" };
        Assert.Equal(expected: "Fixed", actual: options.Remove);
    }

    [Fact]
    public void MessagePropertyIsSetCorrectly()
    {
        Options options = new() { Message = "Test entry" };
        Assert.Equal(expected: "Test entry", actual: options.Message);
    }

    [Fact]
    public void CheckInsertPropertyIsSetCorrectly()
    {
        Options options = new() { CheckInsert = "origin/main" };
        Assert.Equal(expected: "origin/main", actual: options.CheckInsert);
    }

    [Fact]
    public void CreateReleasePropertyIsSetCorrectly()
    {
        Options options = new() { CreateRelease = "2.0.0" };
        Assert.Equal(expected: "2.0.0", actual: options.CreateRelease);
    }

    [Fact]
    public void PendingPropertyIsSetCorrectly()
    {
        Options options = new() { Pending = true };
        Assert.True(options.Pending, userMessage: "Expected Pending to be true");
    }

    [Fact]
    public void DisplayUnreleasedPropertyIsSetCorrectly()
    {
        Options options = new() { DisplayUnreleased = true };
        Assert.True(options.DisplayUnreleased, userMessage: "Expected DisplayUnreleased to be true");
    }

    [Fact]
    public void LintPropertyIsSetCorrectly()
    {
        Options options = new() { Lint = true };
        Assert.True(options.Lint, userMessage: "Expected Lint to be true");
    }

    [Fact]
    public void FixPropertyIsSetCorrectly()
    {
        Options options = new() { Fix = true };
        Assert.True(options.Fix, userMessage: "Expected Fix to be true");
    }

    [Fact]
    public void AdditionalSectionsPropertyIsSetCorrectly()
    {
        IEnumerable<string> sections = ["Custom", "Experimental"];
        Options options = new() { AdditionalSections = sections };
        Assert.Equal(expected: sections, actual: options.AdditionalSections);
    }
}
