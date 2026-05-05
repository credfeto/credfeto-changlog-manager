using Credfeto.ChangeLog.Services;
using FunFair.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogSetupTests : TestBase
{
    [Fact]
    public void AddChangeLogRegistersIChangeLogStorage()
    {
        ServiceCollection services = new();
        services.AddChangeLog();

        using ServiceProvider provider = services.BuildServiceProvider();

        IChangeLogStorage storage = provider.GetRequiredService<IChangeLogStorage>();
        Assert.NotNull(storage);
    }

    [Fact]
    public void AddChangeLogRegistersIChangeLogReader()
    {
        ServiceCollection services = new();
        services.AddChangeLog();

        using ServiceProvider provider = services.BuildServiceProvider();

        IChangeLogReader reader = provider.GetRequiredService<IChangeLogReader>();
        Assert.NotNull(reader);
    }

    [Fact]
    public void AddChangeLogRegistersIChangeLogLinter()
    {
        ServiceCollection services = new();
        services.AddChangeLog();

        using ServiceProvider provider = services.BuildServiceProvider();

        IChangeLogLinter linter = provider.GetRequiredService<IChangeLogLinter>();
        Assert.NotNull(linter);
    }

    [Fact]
    public void AddChangeLogRegistersIChangeLogFixer()
    {
        ServiceCollection services = new();
        services.AddChangeLog();

        using ServiceProvider provider = services.BuildServiceProvider();

        IChangeLogFixer fixer = provider.GetRequiredService<IChangeLogFixer>();
        Assert.NotNull(fixer);
    }

    [Fact]
    public void AddChangeLogRegistersIChangeLogUpdater()
    {
        ServiceCollection services = new();
        services.AddChangeLog();

        using ServiceProvider provider = services.BuildServiceProvider();

        IChangeLogUpdater updater = provider.GetRequiredService<IChangeLogUpdater>();
        Assert.NotNull(updater);
    }

    [Fact]
    public void AddChangeLogRegistersIChangeLogChecker()
    {
        ServiceCollection services = new();
        services.AddChangeLog();

        using ServiceProvider provider = services.BuildServiceProvider();

        IChangeLogChecker checker = provider.GetRequiredService<IChangeLogChecker>();
        Assert.NotNull(checker);
    }

    [Fact]
    public void AddChangeLogRegistersIChangeLogDetector()
    {
        ServiceCollection services = new();
        services.AddChangeLog();

        using ServiceProvider provider = services.BuildServiceProvider();

        IChangeLogDetector detector = provider.GetRequiredService<IChangeLogDetector>();
        Assert.NotNull(detector);
    }

    [Fact]
    public void AddChangeLogRegistersIChangeLogParser()
    {
        ServiceCollection services = new();
        services.AddChangeLog();

        using ServiceProvider provider = services.BuildServiceProvider();

        IChangeLogParser parser = provider.GetRequiredService<IChangeLogParser>();
        Assert.NotNull(parser);
    }

    [Fact]
    public void AddChangeLogRegistersIChangeLogSerialiser()
    {
        ServiceCollection services = new();
        services.AddChangeLog();

        using ServiceProvider provider = services.BuildServiceProvider();

        IChangeLogSerialiser serialiser = provider.GetRequiredService<IChangeLogSerialiser>();
        Assert.NotNull(serialiser);
    }

    [Fact]
    public void AddChangeLogRegistersChangeLogLanguage()
    {
        ServiceCollection services = new();
        services.AddChangeLog();

        using ServiceProvider provider = services.BuildServiceProvider();

        ChangeLogLanguage language = provider.GetRequiredService<ChangeLogLanguage>();
        Assert.NotNull(language);
    }
}
