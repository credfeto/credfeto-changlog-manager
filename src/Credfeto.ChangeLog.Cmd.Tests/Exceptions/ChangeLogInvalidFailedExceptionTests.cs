using System;
using Credfeto.ChangeLog.Cmd.Exceptions;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Cmd.Tests.Exceptions;

public sealed class ChangeLogInvalidFailedExceptionTests : TestBase
{
    [Fact]
    public void DefaultConstructorCreatesException()
    {
        ChangeLogInvalidFailedException exception = new();
        Assert.NotNull(exception);
    }

    [Fact]
    public void MessageConstructorCreatesExceptionWithMessage()
    {
        const string message = "Changelog has lint errors";
        ChangeLogInvalidFailedException exception = new(message);
        Assert.Equal(expected: message, actual: exception.Message);
    }

    [Fact]
    public void MessageAndInnerExceptionConstructorCreatesException()
    {
        const string message = "Changelog has lint errors";
        InvalidOperationException innerException = new("inner");
        ChangeLogInvalidFailedException exception = new(message: message, innerException: innerException);
        Assert.Equal(expected: message, actual: exception.Message);
        Assert.Same(expected: innerException, actual: exception.InnerException);
    }
}
