using System;
using Credfeto.ChangeLog.Exceptions;
using Credfeto.ChangeLog.Helpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ThrowsTests : TestBase
{
    [Fact]
    public void EmptyChangeLogNoUnreleasedSectionThrowsEmptyChangeLogException()
    {
        Assert.Throws<EmptyChangeLogException>(Throws.EmptyChangeLogNoUnreleasedSection);
    }

    [Fact]
    public void CouldNotFindUnreleasedSectionDictionaryThrowsEmptyChangeLogException()
    {
        Assert.Throws<EmptyChangeLogException>(Throws.CouldNotFindUnreleasedSectionDictionary);
    }

    [Fact]
    public void CouldNotFindUnreleasedSectionIntThrowsInvalidChangeLogException()
    {
        Assert.Throws<InvalidChangeLogException>(() => Throws.CouldNotFindUnreleasedSectionInt());
    }

    [Fact]
    public void CouldNotFindUnreleasedSectionStringThrowsInvalidChangeLogException()
    {
        Assert.Throws<InvalidChangeLogException>(Throws.CouldNotFindUnreleasedSectionString);
    }

    [Fact]
    public void NoChangesForTheReleaseThrowsEmptyChangeLogException()
    {
        Assert.Throws<EmptyChangeLogException>(Throws.NoChangesForTheRelease);
    }

    [Fact]
    public void CouldNotFindTypeHeadingThrowsInvalidChangeLogException()
    {
        Assert.Throws<InvalidChangeLogException>(() => Throws.CouldNotFindTypeHeading("Added"));
    }

    [Fact]
    public void ReleaseTooOldThrowsReleaseTooOldException()
    {
        Assert.Throws<ReleaseTooOldException>(() => Throws.ReleaseTooOld("1.0.0", "2.0.0"));
    }

    [Fact]
    public void ReleaseAlreadyExistsThrowsReleaseAlreadyExistsException()
    {
        Assert.Throws<ReleaseAlreadyExistsException>(() => Throws.ReleaseAlreadyExists("1.0.0"));
    }

    [Fact]
    public void CouldNotFindBranchThrowsBranchMissingException()
    {
        Assert.Throws<BranchMissingException>(() => Throws.CouldNotFindBranch("origin/main"));
    }

    [Fact]
    public void CouldNotProcessDiffLineThrowsDiffException()
    {
        Assert.Throws<DiffException>(() => Throws.CouldNotProcessDiffLine("unexpected line"));
    }

    [Fact]
    public void CouldNotFindChangeLogThrowsInvalidChangeLogException()
    {
        Assert.Throws<InvalidChangeLogException>(() => Throws.CouldNotFindChangeLog("CHANGELOG.md"));
    }
}
