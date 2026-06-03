using System;
using Credfeto.ChangeLog.Constants;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class BuildNumberHelpersTests : TestBase
{
    [Theory]
    [InlineData("abc")]
    [InlineData("x.y.z")]
    public void DetermineVersionForChangeLogReturnsNullForUnparsableVersionWithoutHyphen(string version)
    {
        Version? result = BuildNumberHelpers.DetermineVersionForChangeLog(version);
        Assert.Null(result);
    }
}
