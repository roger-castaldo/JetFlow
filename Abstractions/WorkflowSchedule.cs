using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace JetFlow
{
    public record struct WorkflowSchedule(
        byte[]? Minute = null,
        byte[]? Hour = null,
        byte[]? DayOfMonth = null,
        byte[]? Month = null,
        byte[]? DayOfTheWeek = null
    )
    {
        private static readonly Regex cronRegex = new Regex(@"^\s*(?<minute>(?:(?:\*|(?:[0-5]?\d)(?:-(?:[0-5]?\d))?)(?:/\d+)?)(?:\s*,\s*(?:\*|(?:[0-5]?\d)(?:-(?:[0-5]?\d))?)(?:/\d+)?)*))\s+(?<hour>(?:(?:\*|(?:[01]?\d|2[0-3])(?:-(?:[01]?\d|2[0-3]))?)(?:/\d+)?)(?:\s*,\s*(?:\*|(?:[01]?\d|2[0-3])(?:-(?:[01]?\d|2[0-3]))?)(?:/\d+)?)*))\s+(?<dayOfMonth>(?:(?:\*|(?:[1-9]|[12]\d|3[01])(?:-(?:[1-9]|[12]\d|3[01]))?)(?:/\d+)?)(?:\s*,\s*(?:\*|(?:[1-9]|[12]\d|3[01])(?:-(?:[1-9]|[12]\d|3[01]))?)(?:/\d+)?)*))\s+(?<month>(?:(?:\*|(?:[1-9]|1[0-2]|(?:jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec))(?:-(?:[1-9]|1[0-2]|(?:jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)))?)(?:/\d+)?)(?:\s*,\s*(?:\*|(?:[1-9]|1[0-2]|(?:jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec))(?:-(?:[1-9]|1[0-2]|(?:jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)))?)(?:/\d+)?)*))\s+(?<dayOfWeek>(?:(?:\*|(?:[0-7]|(?:sun|mon|tue|wed|thu|fri|sat))(?:-(?:[0-7]|(?:sun|mon|tue|wed|thu|fri|sat)))?)(?:/\d+)?)(?:\s*,\s*(?:\*|(?:[0-7]|(?:sun|mon|tue|wed|thu|fri|sat))(?:-(?:[0-7]|(?:sun|mon|tue|wed|thu|fri|sat)))?)(?:/\d+)?)*))\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static WorkflowSchedule Parse(string cronString)
        {
            var match = cronRegex.Match(cronString);
            if (match.Success) {
                return new(
                    ParseNumber(match.Groups["minute"]),
                    ParseNumber(match.Groups["hour"]),
                    ParseNumber(match.Groups["dayOfMonth"]),
                    ParseNumber(match.Groups["month"]),
                    ParseNumber(match.Groups["dayOfWeek"])
                );
            }
            throw new ArgumentException("Invalid crontab string format", nameof(cronString));
        }

        public string ToCronString()
            => $"{ToCronValue(Minute)} {ToCronValue(Hour)} {ToCronValue(DayOfMonth)} {ToCronValue(Month)} {ToCronValue(DayOfTheWeek)}";

        private static byte[]? ParseNumber(Group group)
            => (group.Value=="*" ? null : group.Value.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => byte.Parse(s)).ToArray());
        private static string ToCronValue(byte[]? value)
            => (value==null ? "*" : string.Join(',', value.Select(v => v.ToString())));
    }
}
