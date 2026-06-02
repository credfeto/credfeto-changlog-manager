using System;
using Credfeto.ChangeLog.Cmd.Exceptions;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Cmd.Tests.Exceptions;

public sealed class MissingChangelogExceptionTests : TestBase
{
    [Fact]
    public void DefaultConstructorCreatesException()
    {
        MissingChangelogException exception = new();
        Assert.NotNull(exception);
    }

    [Fact]
    public void MessageConstructorCreatesExceptionWithMessage()
    {
        const string message = "Could not find changelog";
        MissingChangelogException exception = new(message);
        Assert.Equal(expected: message, actual: exception.Message);
    }

    [Fact]
    public void MessageAndInnerExceptionConstructorCreatesException()
    {
        const string message = "Could not find changelog";
        InvalidOperationException innerException = new("inner");
        MissingChangelogException exception = new(message: message, innerException: innerException);
        Assert.Equal(expected: message, actual: exception.Message);
        Assert.Same(expected: innerException, actual: exception.InnerException);
    }
}
