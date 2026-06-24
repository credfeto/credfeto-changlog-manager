using System;
using System.Collections.Generic;
using ZLinq;

namespace Credfeto.ChangeLog.Extensions;

public static class TextBlockToLines
{
    public static IReadOnlyList<string> SplitToLines(this string value)
    {
        return
        [
            .. value
                .Split("\r\n")
                .SelectMany(x => x.Split("\n\r").SelectMany(y => y.Split("\n").SelectMany(z => z.Split("\r")))),
        ];
    }

    public static string LinesToText(this IEnumerable<string> lines)
    {
        return string.Join(separator: Environment.NewLine, values: lines).Trim();
    }
}
