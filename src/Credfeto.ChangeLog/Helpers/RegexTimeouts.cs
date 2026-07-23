using System;

namespace Credfeto.ChangeLog.Helpers;

public static class RegexTimeouts
{
    public static TimeSpan ShortTimeout { get; } = TimeSpan.FromSeconds(1);
}
