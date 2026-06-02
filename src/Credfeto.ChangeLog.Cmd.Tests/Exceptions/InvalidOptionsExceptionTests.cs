using System;
using Credfeto.ChangeLog.Cmd.Exceptions;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Cmd.Tests.Exceptions;

public sealed class InvalidOptionsExceptionTests : TestBase
{
    [Fact]
    public void DefaultConstructorCreatesException()
    {
        InvalidOptionsException exception = new();
        Assert.NotNull(exception);
    }

    [Fact]
    public void MessageConstructorCreatesExceptionWithMessage()
    {
        const string message = "Invalid options provided";
        InvalidOptionsException exception = new(message);
        Assert.Equal(expected: message, actual: exception.Message);
    }

    [Fact]
    public void MessageAndInnerExceptionConstructorCreatesException()
    {
        const string message = "Invalid options provided";
        InvalidOperationException innerException = new("inner");
        InvalidOptionsException exception = new(message: message, innerException: innerException);
        Assert.Equal(expected: message, actual: exception.Message);
        Assert.Same(expected: innerException, actual: exception.InnerException);
    }
}
