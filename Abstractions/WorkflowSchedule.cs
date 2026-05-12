using System.Text.RegularExpressions;

namespace JetFlow;

public readonly record struct WorkflowSchedule
{
    public string CronString { get; private init; }

    public WorkflowSchedule()
        : this(second:null, minute:null, hour:null, dayOfMonth:null, month:null, dayOfTheWeek:null) { }

    public WorkflowSchedule(
        byte[]? second = null,
        byte[]? minute = null,
        byte[]? hour = null,
        byte[]? dayOfMonth = null,
        byte[]? month = null,
        byte[]? dayOfTheWeek = null)
    {
        ValidateCronValue(second, 0, 59, nameof(second));
        ValidateCronValue(minute, 0, 59, nameof(minute));
        ValidateCronValue(hour, 0, 23, nameof(hour));
        ValidateCronValue(dayOfMonth, 1, 31, nameof(dayOfMonth));
        ValidateCronValue(month, 1, 12, nameof(month));
        ValidateCronValue(dayOfTheWeek, 1, 7, nameof(dayOfTheWeek));
        CronString = $"{ToCronValue(second)} {ToCronValue(minute)} {ToCronValue(hour)} {ToCronValue(dayOfMonth)} {ToCronValue(month)} {ToCronValue(dayOfTheWeek)}";
    }

    private void ValidateCronValue(byte[]? value, int min, int max, string name)
    {
        if (value !=null && value.Any(s => s<0 || s>59))
            throw new ArgumentException($"Invalid {name} value specified, values must be between {min} and {max}", name);
    }

    private static readonly Regex cronRegex = new Regex(@"^\s*(?<second>(?:(?:\*|(?:[0-5]?\d)(?:-(?:[0-5]?\d))?)(?:/\d+)?)(?:\s*,\s*(?:\*|(?:[0-5]?\d)(?:-(?:[0-5]?\d))?)(?:/\d+)?)*))\s+(?<minute>(?:(?:\*|(?:[0-5]?\d)(?:-(?:[0-5]?\d))?)(?:/\d+)?)(?:\s*,\s*(?:\*|(?:[0-5]?\d)(?:-(?:[0-5]?\d))?)(?:/\d+)?)*))\s+(?<hour>(?:(?:\*|(?:[01]?\d|2[0-3])(?:-(?:[01]?\d|2[0-3]))?)(?:/\d+)?)(?:\s*,\s*(?:\*|(?:[01]?\d|2[0-3])(?:-(?:[01]?\d|2[0-3]))?)(?:/\d+)?)*))\s+(?<dayOfMonth>(?:(?:\*|(?:[1-9]|[12]\d|3[01])(?:-(?:[1-9]|[12]\d|3[01]))?)(?:/\d+)?)(?:\s*,\s*(?:\*|(?:[1-9]|[12]\d|3[01])(?:-(?:[1-9]|[12]\d|3[01]))?)(?:/\d+)?)*))\s+(?<month>(?:(?:\*|(?:[1-9]|1[0-2]|(?:jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec))(?:-(?:[1-9]|1[0-2]|(?:jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)))?)(?:/\d+)?)(?:\s*,\s*(?:\*|(?:[1-9]|1[0-2]|(?:jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec))(?:-(?:[1-9]|1[0-2]|(?:jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)))?)(?:/\d+)?)*))\s+(?<dayOfWeek>(?:(?:\*|(?:[0-7]|(?:sun|mon|tue|wed|thu|fri|sat))(?:-(?:[0-7]|(?:sun|mon|tue|wed|thu|fri|sat)))?)(?:/\d+)?)(?:\s*,\s*(?:\*|(?:[0-7]|(?:sun|mon|tue|wed|thu|fri|sat))(?:-(?:[0-7]|(?:sun|mon|tue|wed|thu|fri|sat)))?)(?:/\d+)?)*))\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(500));
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
