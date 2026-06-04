using JetFlow.Interfaces;
using System.Text.RegularExpressions;

namespace JetFlow;

/// <summary>
/// Used to build a workflow schedule, which is used to specify when a workflow should be executed. The schedule is based on the cron format, which is a string format that specifies the schedule using a combination of seconds, minutes, hours, day of month, month, and day of week. This builder provides a fluent API for building a workflow schedule, and also provides a method for parsing a cron string into a workflow schedule builder.
/// </summary>
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

    /// <summary>
    /// Parses a cron string into a workflow schedule builder. The cron string must be in the format of "second minute hour dayOfMonth month dayOfWeek", where each field can be a specific value, a range, a list, or a wildcard. For example, "0 0 * * * *" would represent a schedule that runs every hour at the top of the hour, while "*/5 * * * * *" would represent a schedule that runs every 5 seconds. If the cron string is invalid, a FormatException will be thrown.
    /// </summary>
    /// <param name="cronString">The cron string to parse.</param>
    /// <returns>A WorkflowScheduleBuilder initialized with the values from the cron string.</returns>
    /// <exception cref="FormatException">Thrown if the cron string is invalid.</exception>
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

    /// <summary>
    /// Builds the workflow schedule based on the values specified in the builder. The resulting IWorkflowSchedule will have a string representation in the cron format, which can be used to specify when a workflow should be executed. For example, if the builder is configured to run every hour at the top of the hour, the resulting IWorkflowSchedule will have an AsString value of "0 0 * * * *". If the builder is configured to run every 5 seconds, the resulting IWorkflowSchedule will have an AsString value of "*/5 * * * * *".
    /// </summary>
    /// <returns>A workflow schedule that can be used to specify when a workflow should be executed.</returns>
    public IWorkflowSchedule Build()
        => new WorkflowSchedule(seconds, minutes, hours, dayOfMonth, months, dayOfWeek);

    /// <summary>
    /// Schedules the workflow to run every specified number of seconds. For example, calling EverySeconds(5) would schedule the workflow to run every 5 seconds. The seconds value must be between 1 and 60, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling EverySeconds(5).EveryMinutes(1) would schedule the workflow to run every 5 seconds and every minute.
    /// </summary>
    /// <param name="seconds">The number of seconds between each execution of the workflow.</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
    public WorkflowScheduleBuilder EverySeconds(int seconds)
    {
        ValidateCronValue(seconds, 1, 60, nameof(seconds));
        this.seconds = $"*/{seconds}";
        return this;
    }
    /// <summary>
    /// Schedules the workflow to run at a specific second. For example, calling AtSecond(30) would schedule the workflow to run at the 30th second of every minute. The second value must be between 0 and 59, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtSecond(30).EveryMinutes(1) would schedule the workflow to run at the 30th second of every minute.
    /// </summary>
    /// <param name="second">The specific second at which the workflow should run.</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
    public WorkflowScheduleBuilder AtSecond(int second)
    {
        ValidateCronValue(second, 0, 59, nameof(second));
        this.seconds = $"{second}";
        return this;
    }
    /// <summary>
    /// Schedules the workflow to run every specified number of minutes. For example, calling EveryMinutes(5) would schedule the workflow to run every 5 minutes. The minutes value must be between 1 and 60, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling EveryMinutes(5).EveryHours(1) would schedule the workflow to run every 5 minutes and every hour.
    /// </summary>
    /// <param name="minutes">The number of minutes between each execution of the workflow.</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
    public WorkflowScheduleBuilder EveryMinutes(int minutes)
    {
        ValidateCronValue(minutes, 1, 60, nameof(minutes));
        this.minutes = $"*/{minutes}";
        return this;
    }
    /// <summary>
    /// Schedules the workflow to run at a specific minute. For example, calling AtMinute(30) would schedule the workflow to run at the 30th minute of every hour. The minute value must be between 0 and 59, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtMinute(30).EveryHours(1) would schedule the workflow to run at the 30th minute of every hour.
    /// </summary>
    /// <param name="minute">The specific minute at which the workflow should run.</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
    public WorkflowScheduleBuilder AtMinute(int minute)
    {
        ValidateCronValue(minute, 0, 59, nameof(minute));
        this.minutes = $"{minute}";
        return this;
    }
    /// <summary>
    /// Schedules the workflow to run at specific minutes. For example, calling AtMinutes(new[] { 15, 45 }) would schedule the workflow to run at the 15th and 45th minute of every hour. The minute values must be between 0 and 59, and if any invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtMinutes(new[] { 15, 45 }).EveryHours(1) would schedule the workflow to run at the 15th and 45th minute of every hour.
    /// </summary>
    /// <param name="minutes">An array of specific minutes at which the workflow should run.</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
    public WorkflowScheduleBuilder AtMinutes(int[] minutes)
    {
        ValidateCronValue(minutes, 0, 59, nameof(minutes));
        this.minutes = $"{string.Join(',',minutes.Select(m=>m.ToString()))}";
        return this;
    }
    /// <summary>
    /// Schedules the workflow to run every specified number of hours. For example, calling EveryHours(2) would schedule the workflow to run every 2 hours. The hours value must be between 1 and 24, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling EveryHours(2).EveryDays(1) would schedule the workflow to run every 2 hours and every day.
    /// </summary>
    /// <param name="hours">The number of hours between each execution of the workflow.</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
    public WorkflowScheduleBuilder EveryHours(int hours)
    {
        ValidateCronValue(hours, 1, 24, nameof(hours));
        this.hours = $"*/{hours}";
        return this;
    }
    /// <summary>
    /// Schedules the workflow to run at a specific hour. For example, calling AtHour(14) would schedule the workflow to run at 2 PM every day. The hour value must be between 0 and 23, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtHour(14).EveryDays(1) would schedule the workflow to run at 2 PM every day.
    /// </summary>
    /// <param name="hour">The specific hour at which the workflow should run.</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
    public WorkflowScheduleBuilder AtHour(int hour)
    {
        ValidateCronValue(hour, 0, 23, nameof(hour));
        this.hours = $"{hour}";
        return this;
    }
    /// <summary>
    /// Schedules the workflow to run at specific hours. For example, calling AtHours(new[] { 9, 17 }) would schedule the workflow to run at 9 AM and 5 PM every day. The hour values must be between 0 and 23, and if any invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtHours(new[] { 9, 17 }).EveryDays(1) would schedule the workflow to run at 9 AM and 5 PM every day.
    /// </summary>
    /// <param name="hours">An array of specific hours at which the workflow should run.</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
    public WorkflowScheduleBuilder AtHours(int[] hours)
    {
        ValidateCronValue(hours, 0, 23, nameof(hours));
        this.hours = $"{string.Join(',',hours.Select(h=>h.ToString()))}";
        return this;
    }
    /// <summary>
    /// Schedules the workflow to run every specified number of days. For example, calling EveryDays(2) would schedule the workflow to run every 2 days. The days value must be between 1 and 31, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling EveryDays(2).EveryMonths(1) would schedule the workflow to run every 2 days and every month.
    /// </summary>
    /// <param name="days">The number of days between each execution of the workflow.</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
    public WorkflowScheduleBuilder EveryDays(int days)
    {
        ValidateCronValue(days, 1, 31, nameof(days));
        this.dayOfMonth = $"*/{days}";
        return this;
    }
    /// <summary>
    /// Schedules the workflow to run at a specific day of the month. For example, calling AtDay(15) would schedule the workflow to run on the 15th day of every month. The day value must be between 1 and 31, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtDay(15).EveryMonths(1) would schedule the workflow to run on the 15th day of every month.
    /// </summary>
    /// <param name="day">The specific day of the month on which the workflow should run.</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
    public WorkflowScheduleBuilder AtDay(int day)
    {
        ValidateCronValue(day, 1, 31, nameof(day));
        this.dayOfMonth = $"{day}";
        return this;
    }
    /// <summary>
    /// Schedules the workflow to run at specific days of the month. For example, calling AtDays(new[] { 1, 15 }) would schedule the workflow to run on the 1st and 15th day of every month. The day values must be between 1 and 31, and if any invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtDays(new[] { 1, 15 }).EveryMonths(1) would schedule the workflow to run on the 1st and 15th day of every month.
    /// </summary>
    /// <param name="days">An array of specific days of the month on which the workflow should run.</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
    public WorkflowScheduleBuilder AtDays(int[] days)
    {
        ValidateCronValue(days, 1, 31, nameof(days));
        this.dayOfMonth = $"{string.Join(',', days.Select(d => d.ToString()))}";
        return this;
    }
    /// <summary>
    /// Schedules the workflow to run every specified number of months. For example, calling EveryMonths(3) would schedule the workflow to run every 3 months. The months value must be between 1 and 12, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling EveryMonths(3).EveryDays(1) would schedule the workflow to run every 3 months and every day.
    /// </summary>
    /// <param name="months">The number of months between each workflow execution.</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
    public WorkflowScheduleBuilder EveryMonths(int months)
    {
        ValidateCronValue(months, 1, 12, nameof(months));
        this.months = $"*/{months}";
        return this;
    }
    /// <summary>
    /// Schedules the workflow to run at a specific month. For example, calling AtMonth(5) would schedule the workflow to run in May every year. The month value must be between 1 and 12, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtMonth(5).EveryDays(1) would schedule the workflow to run in May every year and every day.
    /// </summary>
    /// <param name="month">The specific month in which the workflow should run.</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
    public WorkflowScheduleBuilder AtMonth(int month)
    {
        ValidateCronValue(month, 1, 12, nameof(month));
        this.months = $"{month}";
        return this;
    }
    /// <summary>
    /// Schedules the workflow to run at specific months. For example, calling AtMonths(new[] { 3, 9 }) would schedule the workflow to run in March and September every year. The month values must be between 1 and 12, and if any invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtMonths(new[] { 3, 9 }).EveryDays(1) would schedule the workflow to run in March and September every year and every day.
    /// </summary>
    /// <param name="months">An array of specific months in which the workflow should run.</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
    public WorkflowScheduleBuilder AtMonths(int[] months)
    {
        ValidateCronValue(months, 1, 12, nameof(months));
        this.months = $"{string.Join(',', months.Select(m => m.ToString()))}";
        return this;
    }
    /// <summary>
    /// Schedules the workflow to run at a specific day of the week. For example, calling AtDayOfWeek(1) would schedule the workflow to run on Mondays every week. The dayOfWeek value must be between 1 and 7, where 1 represents Monday and 7 represents Sunday. If an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtDayOfWeek(1).EveryHours(1) would schedule the workflow to run on Mondays every hour.
    /// </summary>
    /// <param name="dayOfWeek">The specific day of the week on which the workflow should run.</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
    public WorkflowScheduleBuilder AtDayOfWeek(int dayOfWeek)
    {
        ValidateCronValue(dayOfWeek, 1, 7, nameof(dayOfWeek));
        this.dayOfWeek = $"{dayOfWeek}";
        return this;
    }
    /// <summary>
    /// Schedules the workflow to run at specific days of the week. For example, calling AtDaysOfWeek(new[] { 1, 5 }) would schedule the workflow to run on Mondays and Fridays every week. The dayOfWeek values must be between 1 and 7, where 1 represents Monday and 7 represents Sunday. If any invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling AtDaysOfWeek(new[] { 1, 5 }).EveryHours(1) would schedule the workflow to run on Mondays and Fridays every hour.
    /// </summary>
    /// <param name="daysOfWeek">An array of specific days of the week on which the workflow should run.</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
    public WorkflowScheduleBuilder AtDaysOfWeek(int[] daysOfWeek)
    {
        ValidateCronValue(daysOfWeek, 1, 7, nameof(daysOfWeek));
        this.dayOfWeek = $"{string.Join(',', daysOfWeek.Select(m => m.ToString()))}";
        return this;
    }
    /// <summary>
    /// Schedules the workflow to run on weekdays (Monday through Friday). This is a convenience method that is equivalent to calling AtDaysOfWeek(new[] { 1, 2, 3, 4, 5 }). This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling WeekDays().EveryHours(1) would schedule the workflow to run on weekdays every hour.
    /// </summary>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
    public WorkflowScheduleBuilder WeekDays()
        => AtDaysOfWeek([1, 2, 3, 4, 5]);
    /// <summary>
    /// Schedules the workflow to run on weekdays (Monday through Friday) at a specific hour and minute. This is a convenience method that is equivalent to calling AtDaysOfWeek(new[] { 1, 2, 3, 4, 5 }).AtHour(hour).AtMinute(minute). The hour value must be between 0 and 23, and the minute value must be between 0 and 59. If any invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling WeekDaysAt(9, 30).EveryMonths(1) would schedule the workflow to run on weekdays at 9:30 AM every month.
    /// </summary>
    /// <param name="hour">The hour at which the workflow should run (0-23).</param>
    /// <param name="minute">The minute at which the workflow should run (0-59).</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
    public WorkflowScheduleBuilder WeekDaysAt(int hour, int minute)
    {
        dayOfMonth = "*";
        months = "*";
        return AtSecond(0)
        .AtHour(hour)
        .AtMinute(minute)
        .WeekDays();
    }
    /// <summary>
    /// Schedules the workflow to run daily at a specific hour and minute. For example, calling DailyAt(14, 30) would schedule the workflow to run every day at 2:30 PM. The hour value must be between 0 and 23, and the minute value must be between 0 and 59. If any invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling DailyAt(14, 30).EveryMonths(1) would schedule the workflow to run every day at 2:30 PM every month.
    /// </summary>
    /// <param name="hour">The hour at which the workflow should run (0-23).</param>
    /// <param name="minute">The minute at which the workflow should run (0-59).</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
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
    /// <summary>
    /// Schedules the workflow to run monthly on a specific day of the month. For example, calling MonthlyOn(15) would schedule the workflow to run on the 15th day of every month. The dayOfMonth value must be between 1 and 31, and if an invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling MonthlyOn(15).AtHour(14).AtMinute(30) would schedule the workflow to run on the 15th day of every month at 2:30 PM.
    /// </summary>
    /// <param name="dayOfMonth">The day of the month on which the workflow should run (1-31).</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
    public WorkflowScheduleBuilder MonthlyOn(int dayOfMonth)
    {
        months = "*";
        dayOfWeek = "*";
        return AtSecond(0)
        .AtMinute(0)
        .AtHour(0)
        .AtDay(dayOfMonth);
    }
    /// <summary>
    /// Schedules the workflow to run monthly on a specific day of the month at a specific hour and minute. For example, calling MonthlyOnAt(15, 14, 30) would schedule the workflow to run on the 15th day of every month at 2:30 PM. The dayOfMonth value must be between 1 and 31, the hour value must be between 0 and 23, and the minute value must be between 0 and 59. If any invalid value is specified, an ArgumentException will be thrown. This method can be used in combination with other methods in the builder to create more complex schedules. For example, calling MonthlyOnAt(15, 14, 30).EveryMonths(2) would schedule the workflow to run on the 15th day of every other month at 2:30 PM.
    /// </summary>
    /// <param name="dayOfMonth">The day of the month on which the workflow should run (1-31).</param>
    /// <param name="hour">The hour at which the workflow should run (0-23).</param>
    /// <param name="minute">The minute at which the workflow should run (0-59).</param>
    /// <returns>The current instance of the WorkflowScheduleBuilder, allowing for method chaining.</returns>
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

