using JetFlow.Interfaces;
using System.Text.RegularExpressions;

namespace JetFlow;

public sealed class WorkflowScheduleBuilder
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
    private static readonly Regex cronRegex = new(
        $@"^
        (?<seconds>{SecondMinute})\s+
        (?<minutes>{SecondMinute})\s+
        (?<hours>{Hour})\s+
        (?<dayOfMonth>{DayOfMonth})\s+
        (?<month>{Month})\s+
        (?<dayOfWeek>{DayOfWeek})
        $",
        RegexOptions.Compiled |
        RegexOptions.IgnorePatternWhitespace, 
    TimeSpan.FromMilliseconds(500));

    private static void ValidateCronValue(int value, int min, int max, string name)
        => ValidateCronValue([value], min, max, name);
    private static void ValidateCronValue(int[] value, int min, int max, string name)
    {
        if (value.Any(v => v<min || v>max))
            throw new ArgumentException($"Invalid value specified, values must be between {min} and {max}", name);
    }
    private sealed class WorkflowSchedule(string second, string minute, string hour, string dayOfMonth, string month, string dayOfWeek)
           : IWorkflowSchedule
    {
        string IWorkflowSchedule.AsString
            => $"{second} {minute} {hour} {dayOfMonth} {month} {dayOfWeek}";
    }

    public static WorkflowScheduleBuilder Parse(string cronString)
    {
        var match = cronRegex.Match(cronString);
        if (!match.Success)
            throw new FormatException("Invalid cron string specified, please specify a crontab string format including seconds");
        var builder = new WorkflowScheduleBuilder();
        builder.seconds = match.Groups["seconds"].Value;
        builder.minutes = match.Groups["minutes"].Value;
        builder.hours = match.Groups["hours"].Value;
        builder.dayOfMonth = match.Groups["dayOfMonth"].Value;
        builder.months = match.Groups["month"].Value;
        builder.dayOfWeek = match.Groups["dayOfWeek"].Value;
        return builder;
    }

    private string seconds = "0";
    private string minutes = "*";
    private string hours = "*";
    private string dayOfMonth = "*";
    private string months = "*";
    private string dayOfWeek = "*";

    public IWorkflowSchedule Build()
        => new WorkflowSchedule(seconds, minutes, hours, dayOfMonth, months, dayOfWeek);

    public WorkflowScheduleBuilder EverySeconds(int seconds)
    {
        ValidateCronValue(seconds, 1, 60, nameof(seconds));
        this.seconds = $"*/{seconds}";
        return this;
    }
    public WorkflowScheduleBuilder AtSecond(int second)
    {
        ValidateCronValue(second, 0, 59, nameof(second));
        this.seconds = $"{second}";
        return this;
    }
    public WorkflowScheduleBuilder EveryMinutes(int minutes)
    {
        ValidateCronValue(minutes, 1, 60, nameof(minutes));
        this.minutes = $"*/{minutes}";
        return this;
    }
    public WorkflowScheduleBuilder AtMinute(int minute)
    {
        ValidateCronValue(minute, 0, 59, nameof(minute));
        this.minutes = $"{minute}";
        return this;
    }
    public WorkflowScheduleBuilder AtMinutes(int[] minutes)
    {
        ValidateCronValue(minutes, 0, 59, nameof(minutes));
        this.minutes = $"{string.Join(',',minutes.Select(m=>m.ToString()))}";
        return this;
    }
    public WorkflowScheduleBuilder EveryHours(int hours)
    {
        ValidateCronValue(hours, 1, 24, nameof(hours));
        this.hours = $"*/{hours}";
        return this;
    }
    public WorkflowScheduleBuilder AtHour(int hour)
    {
        ValidateCronValue(hour, 0, 23, nameof(hour));
        this.hours = $"{hour}";
        return this;
    }
    public WorkflowScheduleBuilder AtHours(int[] hours)
    {
        ValidateCronValue(hours, 0, 23, nameof(hours));
        this.hours = $"{string.Join(',',hours.Select(h=>h.ToString()))}";
        return this;
    }
    public WorkflowScheduleBuilder EveryDays(int days)
    {
        ValidateCronValue(days, 1, 31, nameof(days));
        this.dayOfMonth = $"*/{days}";
        return this;
    }
    public WorkflowScheduleBuilder AtDay(int day)
    {
        ValidateCronValue(day, 1, 31, nameof(day));
        this.dayOfMonth = $"{day}";
        return this;
    }
    public WorkflowScheduleBuilder AtDays(int[] days)
    {
        ValidateCronValue(days, 1, 31, nameof(days));
        this.dayOfMonth = $"{string.Join(',', days.Select(d => d.ToString()))}";
        return this;
    }
    public WorkflowScheduleBuilder EveryMonths(int months)
    {
        ValidateCronValue(months, 1, 12, nameof(months));
        this.months = $"*/{months}";
        return this;
    }
    public WorkflowScheduleBuilder AtMonth(int month)
    {
        ValidateCronValue(month, 1, 12, nameof(month));
        this.months = $"{month}";
        return this;
    }
    public WorkflowScheduleBuilder AtMonths(int[] months)
    {
        ValidateCronValue(months, 1, 12, nameof(months));
        this.months = $"{string.Join(',', months.Select(m => m.ToString()))}";
        return this;
    }
    public WorkflowScheduleBuilder AtDayOfWeek(int dayOfWeek)
    {
        ValidateCronValue(dayOfWeek, 1, 7, nameof(dayOfWeek));
        this.dayOfWeek = $"{dayOfWeek}";
        return this;
    }
    public WorkflowScheduleBuilder AtDaysOfWeek(int[] daysOfWeek)
    {
        ValidateCronValue(daysOfWeek, 1, 7, nameof(daysOfWeek));
        this.dayOfWeek = $"{string.Join(',', daysOfWeek.Select(m => m.ToString()))}";
        return this;
    }
    public WorkflowScheduleBuilder WeekDays()
        => AtDaysOfWeek([1, 2, 3, 4, 5]);
    public WorkflowScheduleBuilder WeekDaysAt(int hour, int minute)
    {
        dayOfMonth = "*";
        months = "*";
        return AtSecond(0)
        .AtHour(hour)
        .AtMinute(minute)
        .WeekDays();
    }
    public WorkflowScheduleBuilder DailyAt(int hour, int minute)
    {
        seconds = "0";
        minutes = $"{minute}";
        hours = $"{hour}";
        dayOfMonth = "*";
        months= "*";
        dayOfMonth = "*";
        return AtSecond(0)
            .AtMinute(minute)
            .AtHour(hour);
    }
    public WorkflowScheduleBuilder MonthlyOn(int dayOfMonth)
    {
        months = "*";
        dayOfWeek = "*";
        return AtSecond(0)
        .AtMinute(0)
        .AtHour(0)
        .AtDay(dayOfMonth);
    }
    public WorkflowScheduleBuilder MonthlyOnAt(int dayOfMonth, int hour, int minute)
    {
        months = "*";
        dayOfWeek = "*";
        return AtSecond(0)
        .AtMinute(minute)
        .AtHour(hour)
        .AtDay(dayOfMonth);
    }
}

