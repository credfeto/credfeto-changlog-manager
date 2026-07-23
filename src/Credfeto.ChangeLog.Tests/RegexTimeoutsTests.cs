using System;
using Credfeto.ChangeLog.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class RegexTimeoutsTests : TestBase
{
    [Fact]
    public void RegexTimeoutsShortIsOneSecond()
    {
        TimeSpan timeout = RegexTimeouts.ShortTimeout;
        Assert.Equal(expected: TimeSpan.FromSeconds(1), actual: timeout);
    }
}
