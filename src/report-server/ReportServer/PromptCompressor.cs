using System.Text.RegularExpressions;

namespace ReportServer;

public static partial class PromptCompressor
{
    public static string Compress(string prompt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var compressed = MarkdownSyntax().Replace(prompt, string.Empty);
        compressed = BoldSyntax().Replace(compressed, "$1");
        compressed = ItalicAsteriskSyntax().Replace(compressed, "$1");
        compressed = ItalicUnderscoreSyntax().Replace(compressed, "$1");
        compressed = InlineCodeSyntax().Replace(compressed, "$1");
        compressed = HorizontalRuleSyntax().Replace(compressed, string.Empty);
        compressed = HorizontalWhitespace().Replace(compressed, " ");
        compressed = string.Join("\n", compressed.Split('\n').Select(line => line.Trim()));
        return ExcessNewlines().Replace(compressed, "\n\n").Trim();
    }

    [GeneratedRegex(@"^#{1,6}\s+", RegexOptions.Multiline)]
    private static partial Regex MarkdownSyntax();

    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex BoldSyntax();

    [GeneratedRegex(@"\*(.+?)\*")]
    private static partial Regex ItalicAsteriskSyntax();

    [GeneratedRegex(@"_(.+?)_")]
    private static partial Regex ItalicUnderscoreSyntax();

    [GeneratedRegex(@"`(.+?)`")]
    private static partial Regex InlineCodeSyntax();

    [GeneratedRegex(@"^(-{3,}|\*{3,})\s*$", RegexOptions.Multiline)]
    private static partial Regex HorizontalRuleSyntax();

    [GeneratedRegex(@"[ \t]+")]
    private static partial Regex HorizontalWhitespace();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExcessNewlines();
}
