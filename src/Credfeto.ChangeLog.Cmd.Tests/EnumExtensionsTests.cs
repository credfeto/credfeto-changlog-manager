using System.Diagnostics;
using CommandLine;
using FunFair.Test.Common;
using LibGit2Sharp;
using Xunit;

namespace Credfeto.ChangeLog.Cmd.Tests;

public sealed class EnumExtensionsTests : TestBase
{
    [Theory]
    [InlineData(ErrorType.BadFormatTokenError)]
    [InlineData(ErrorType.MissingValueOptionError)]
    [InlineData(ErrorType.UnknownOptionError)]
    [InlineData(ErrorType.MissingRequiredOptionError)]
    [InlineData(ErrorType.MutuallyExclusiveSetError)]
    [InlineData(ErrorType.BadFormatConversionError)]
    [InlineData(ErrorType.SequenceOutOfRangeError)]
    [InlineData(ErrorType.RepeatedOptionError)]
    [InlineData(ErrorType.NoVerbSelectedError)]
    [InlineData(ErrorType.BadVerbSelectedError)]
    [InlineData(ErrorType.HelpRequestedError)]
    [InlineData(ErrorType.HelpVerbRequestedError)]
    [InlineData(ErrorType.VersionRequestedError)]
    [InlineData(ErrorType.SetValueExceptionError)]
    [InlineData(ErrorType.InvalidAttributeConfigurationError)]
    [InlineData(ErrorType.MissingGroupOptionError)]
    [InlineData(ErrorType.GroupOptionAmbiguityError)]
    [InlineData(ErrorType.MultipleDefaultVerbsError)]
    public void ErrorTypeGetNameReturnsNonEmptyString(ErrorType value)
    {
        string name = value.GetName();
        Assert.NotEmpty(name);
    }

    [Theory]
    [InlineData(ErrorType.BadFormatTokenError)]
    [InlineData(ErrorType.MissingValueOptionError)]
    [InlineData(ErrorType.UnknownOptionError)]
    [InlineData(ErrorType.MissingRequiredOptionError)]
    [InlineData(ErrorType.MutuallyExclusiveSetError)]
    [InlineData(ErrorType.BadFormatConversionError)]
    [InlineData(ErrorType.SequenceOutOfRangeError)]
    [InlineData(ErrorType.RepeatedOptionError)]
    [InlineData(ErrorType.NoVerbSelectedError)]
    [InlineData(ErrorType.BadVerbSelectedError)]
    [InlineData(ErrorType.HelpRequestedError)]
    [InlineData(ErrorType.HelpVerbRequestedError)]
    [InlineData(ErrorType.VersionRequestedError)]
    [InlineData(ErrorType.SetValueExceptionError)]
    [InlineData(ErrorType.InvalidAttributeConfigurationError)]
    [InlineData(ErrorType.MissingGroupOptionError)]
    [InlineData(ErrorType.GroupOptionAmbiguityError)]
    [InlineData(ErrorType.MultipleDefaultVerbsError)]
    public void ErrorTypeGetDescriptionReturnsNonEmptyString(ErrorType value)
    {
        string description = value.GetDescription();
        Assert.NotEmpty(description);
    }

    [Theory]
    [InlineData(ErrorType.BadFormatTokenError)]
    [InlineData(ErrorType.MissingValueOptionError)]
    [InlineData(ErrorType.UnknownOptionError)]
    [InlineData(ErrorType.MissingRequiredOptionError)]
    [InlineData(ErrorType.MutuallyExclusiveSetError)]
    [InlineData(ErrorType.BadFormatConversionError)]
    [InlineData(ErrorType.SequenceOutOfRangeError)]
    [InlineData(ErrorType.RepeatedOptionError)]
    [InlineData(ErrorType.NoVerbSelectedError)]
    [InlineData(ErrorType.BadVerbSelectedError)]
    [InlineData(ErrorType.HelpRequestedError)]
    [InlineData(ErrorType.HelpVerbRequestedError)]
    [InlineData(ErrorType.VersionRequestedError)]
    [InlineData(ErrorType.SetValueExceptionError)]
    [InlineData(ErrorType.InvalidAttributeConfigurationError)]
    [InlineData(ErrorType.MissingGroupOptionError)]
    [InlineData(ErrorType.GroupOptionAmbiguityError)]
    [InlineData(ErrorType.MultipleDefaultVerbsError)]
    public void ErrorTypeIsDefinedReturnsTrueForKnownValues(ErrorType value)
    {
        bool defined = value.IsDefined();
        Assert.True(defined, userMessage: "Expected IsDefined to return true for a known ErrorType value");
    }

    [Fact]
    public void ErrorTypeIsDefinedReturnsFalseForUnknownValue()
    {
        bool defined = ((ErrorType)int.MaxValue).IsDefined();
        Assert.False(defined, userMessage: "Expected IsDefined to return false for an unknown ErrorType value");
    }

    [Fact]
    public void ErrorTypeGetNameThrowsForUnknownValue()
    {
        Assert.Throws<UnreachableException>(() => ((ErrorType)int.MaxValue).GetName());
    }

    [Theory]
    [InlineData(LogLevel.None)]
    [InlineData(LogLevel.Fatal)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Info)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Trace)]
    public void LogLevelGetNameReturnsNonEmptyString(LogLevel value)
    {
        string name = value.GetName();
        Assert.NotEmpty(name);
    }

    [Theory]
    [InlineData(LogLevel.None)]
    [InlineData(LogLevel.Fatal)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Info)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Trace)]
    public void LogLevelGetDescriptionReturnsNonEmptyString(LogLevel value)
    {
        string description = value.GetDescription();
        Assert.NotEmpty(description);
    }

    [Theory]
    [InlineData(LogLevel.None)]
    [InlineData(LogLevel.Fatal)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Info)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Trace)]
    public void LogLevelIsDefinedReturnsTrueForKnownValues(LogLevel value)
    {
        bool defined = value.IsDefined();
        Assert.True(defined, userMessage: "Expected IsDefined to return true for a known LogLevel value");
    }

    [Fact]
    public void LogLevelIsDefinedReturnsFalseForUnknownValue()
    {
        bool defined = ((LogLevel)int.MaxValue).IsDefined();
        Assert.False(defined, userMessage: "Expected IsDefined to return false for an unknown LogLevel value");
    }

    [Fact]
    public void LogLevelGetNameThrowsForUnknownValue()
    {
        Assert.Throws<UnreachableException>(() => ((LogLevel)int.MaxValue).GetName());
    }
}
