using System;
using Credfeto.ChangeLog.Constants;

namespace Credfeto.ChangeLog.Helpers;

internal static class Unreleased
{
    public static bool IsUnreleasedHeader(string line)
    {
        return StringComparer.Ordinal.Equals(x: line, y: FileConstants.UnreleasedHeader);
    }
}
