using System.Collections.Generic;
using ZLinq;

namespace Credfeto.ChangeLog.Helpers;

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
}
