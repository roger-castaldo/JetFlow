using System.Text.RegularExpressions;

namespace JetFlow;

internal static partial class CronRegexPatterns
{
    private const string SecondMinute =
        @"(?:\*|(?:[0-5]?\d)(?:-[0-5]?\d)?)(?:/\d+)?(?:,(?:\*|(?:[0-5]?\d)(?:-[0-5]?\d)?)(?:/\d+)?)*";
    private const string Hour =
        @"(?:\*|(?:[01]?\d|2[0-3])(?:-(?:[01]?\d|2[0-3]))?)(?:/\d+)?(?:,(?:\*|(?:[01]?\d|2[0-3])(?:-(?:[01]?\d|2[0-3]))?)(?:/\d+)?)*";
    private const string DayOfMonth =
        @"(?:\*|(?:[1-9]|[12]\d|3[01])(?:-(?:[1-9]|[12]\d|3[01]))?)(?:/\d+)?(?:,(?:\*|(?:[1-9]|[12]\d|3[01])(?:-(?:[1-9]|[12]\d|3[01]))?)(?:/\d+)?)*";
    private const string Month =
        @"(?:\*|(?:[1-9]|1[0-2])(?:-(?:[1-9]|1[0-2]))?)(?:/\d+)?(?:,(?:\*|(?:[1-9]|1[0-2])(?:-(?:[1-9]|1[0-2]))?)(?:/\d+)?)*";
    private const string DayOfWeek =
        @"(?:\*|[1-7](?:-[1-7])?)(?:/\d+)?(?:,(?:\*|[1-7](?:-[1-7])?)(?:/\d+)?)*";

    [GeneratedRegex($@"^
        (?<seconds>{SecondMinute})\s+
        (?<minutes>{SecondMinute})\s+
        (?<hours>{Hour})\s+
        (?<dayOfMonth>{DayOfMonth})\s+
        (?<month>{Month})\s+
        (?<dayOfWeek>{DayOfWeek})
        $",
        RegexOptions.Compiled |
        RegexOptions.IgnorePatternWhitespace, 60_000)]
    public static partial Regex CronRegex { get; }
}
