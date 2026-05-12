using System.Text.RegularExpressions;

namespace JetFlow;

public readonly record struct WorkflowSchedule
{
    public string CronString { get; private init; }

    public WorkflowSchedule()
        : this(null, null, null, null, null, null) { }

    public WorkflowSchedule(
        byte[]? second = null,
        byte[]? minute = null,
        byte[]? hour = null,
        byte[]? dayOfMonth = null,
        byte[]? month = null,
        byte[]? dayOfTheWeek = null)
    {
        if (second !=null && second.Any(s=> s<0 || s>59))
            throw new ArgumentException("Invalid second value specified, values must be between 0 and 59", nameof(second));
        if (minute!=null && minute.Any(m => m<0||m>59))
            throw new ArgumentException("Invalid minute value specified, values must be between 0 and 59", nameof(minute));
        if (hour!=null && hour.Any(h => h<0||h>23))
            throw new ArgumentException("Invalid hour value specified, values must be between 0 and 23", nameof(hour));
        if (dayOfMonth!=null && dayOfMonth.Any(d=>d<1||d>31))
            throw new ArgumentException("Invalid dayOfMonth value specified, values must be between 1 and 31", nameof(dayOfMonth));
        if (month!=null && month.Any(m => m<1||m>12))
            throw new ArgumentException("Invalid month value specified, values must be between 1 and 12", nameof(month));
        if (dayOfTheWeek!=null && dayOfTheWeek.Any(d => d<1||d>12))
            throw new ArgumentException("Invalid dayOfTheWeek value specified, values must be between 1 and 7", nameof(dayOfTheWeek));
        CronString = $"{ToCronValue(second)} {ToCronValue(minute)} {ToCronValue(hour)} {ToCronValue(dayOfMonth)} {ToCronValue(month)} {ToCronValue(dayOfTheWeek)}";
    }

    private static readonly Regex cronRegex = new Regex(@"^\s*(?<second>(?:(?:\*|(?:[0-5]?\d)(?:-(?:[0-5]?\d))?)(?:/\d+)?)(?:\s*,\s*(?:\*|(?:[0-5]?\d)(?:-(?:[0-5]?\d))?)(?:/\d+)?)*))\s+(?<minute>(?:(?:\*|(?:[0-5]?\d)(?:-(?:[0-5]?\d))?)(?:/\d+)?)(?:\s*,\s*(?:\*|(?:[0-5]?\d)(?:-(?:[0-5]?\d))?)(?:/\d+)?)*))\s+(?<hour>(?:(?:\*|(?:[01]?\d|2[0-3])(?:-(?:[01]?\d|2[0-3]))?)(?:/\d+)?)(?:\s*,\s*(?:\*|(?:[01]?\d|2[0-3])(?:-(?:[01]?\d|2[0-3]))?)(?:/\d+)?)*))\s+(?<dayOfMonth>(?:(?:\*|(?:[1-9]|[12]\d|3[01])(?:-(?:[1-9]|[12]\d|3[01]))?)(?:/\d+)?)(?:\s*,\s*(?:\*|(?:[1-9]|[12]\d|3[01])(?:-(?:[1-9]|[12]\d|3[01]))?)(?:/\d+)?)*))\s+(?<month>(?:(?:\*|(?:[1-9]|1[0-2]|(?:jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec))(?:-(?:[1-9]|1[0-2]|(?:jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)))?)(?:/\d+)?)(?:\s*,\s*(?:\*|(?:[1-9]|1[0-2]|(?:jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec))(?:-(?:[1-9]|1[0-2]|(?:jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)))?)(?:/\d+)?)*))\s+(?<dayOfWeek>(?:(?:\*|(?:[0-7]|(?:sun|mon|tue|wed|thu|fri|sat))(?:-(?:[0-7]|(?:sun|mon|tue|wed|thu|fri|sat)))?)(?:/\d+)?)(?:\s*,\s*(?:\*|(?:[0-7]|(?:sun|mon|tue|wed|thu|fri|sat))(?:-(?:[0-7]|(?:sun|mon|tue|wed|thu|fri|sat)))?)(?:/\d+)?)*))\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
    public static WorkflowSchedule Parse(string cronString)
    {
        var match = cronRegex.Match(cronString);
        if (match.Success) {
            return new(
                ParseNumber(match.Groups["second"]),
                ParseNumber(match.Groups["minute"]),
                ParseNumber(match.Groups["hour"]),
                ParseNumber(match.Groups["dayOfMonth"]),
                ParseNumber(match.Groups["month"]),
                ParseNumber(match.Groups["dayOfWeek"])
            );
        }
        throw new ArgumentException("Invalid crontab string format", nameof(cronString));
    }
    private static byte[]? ParseNumber(Group group)
        => (group.Value=="*" ? null : group.Value.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => byte.Parse(s)).ToArray());
    private static string ToCronValue(byte[]? value)
        => (value==null ? "*" : string.Join(',', value.Select(v => v.ToString())));
}
