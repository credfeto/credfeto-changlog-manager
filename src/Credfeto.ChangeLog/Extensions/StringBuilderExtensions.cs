using System.Collections.Immutable;
using System.Text;

namespace Credfeto.ChangeLog.Extensions;

internal static class StringBuilderExtensions
{
    public static StringBuilder AppendSectionHeadings(this StringBuilder sb, in ImmutableArray<string> sections)
    {
        foreach (string section in sections)
        {
            sb = sb.Append(section.AsChangeTypeHeading()).Append('\n');
        }

        return sb;
    }
}
