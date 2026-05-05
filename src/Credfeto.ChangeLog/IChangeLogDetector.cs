using System.Diagnostics.CodeAnalysis;

namespace Credfeto.ChangeLog;

public interface IChangeLogDetector
{
    bool TryFindChangeLog([NotNullWhen(true)] out string? changeLogFileName);
}
