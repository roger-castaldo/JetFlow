namespace JetFlow.Testing;

[TestClass]
public class WorkflowScheduleBuilderTests
{
    private void ValidateError(Func<WorkflowScheduleBuilder> callback, string field, int min, int max)
    {
        var error = Assert.ThrowsExactly<ArgumentException>(callback);
        Assert.AreEqual($"Invalid value specified, values must be between {min} and {max} (Parameter '{field}')", error.Message);
    }

    [TestMethod]
    public void TestDefaultValue()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();

        //Act
        var value = builder.Build();

        //Assert
        Assert.AreEqual("0 * * * * *", value.AsString);
    }

    [TestMethod]
    public void TestSeconds()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();

        //Act
        var every30Seconds = builder.EverySeconds(30).Build();
        var at30Seconds = builder.AtSecond(30).Build();
        
        //Assert
        Assert.AreEqual("*/30 * * * * *", every30Seconds.AsString);
        Assert.AreEqual("30 * * * * *", at30Seconds.AsString);
    }

    [TestMethod]
    public void TestInvalidSeconds()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();
        //Act
        ValidateError(() => builder.AtSecond(-1), "second", 0, 59);
        ValidateError(() => builder.AtSecond(60), "second", 0, 59);
        ValidateError(() => builder.EverySeconds(0), "seconds", 1, 60);
        ValidateError(() => builder.EverySeconds(61), "seconds", 1, 60);
    }

    [TestMethod]
    public void TestMinutes()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();

        //Act
        var every30Minutes = builder.EveryMinutes(30).Build();
        var at30Minutes = builder.AtMinute(30).Build();
        var atMinutes = builder.AtMinutes([1,30,59]).Build();

        //Assert
        Assert.AreEqual("0 */30 * * * *", every30Minutes.AsString);
        Assert.AreEqual("0 30 * * * *", at30Minutes.AsString);
        Assert.AreEqual("0 1,30,59 * * * *", atMinutes.AsString);
    }

    [TestMethod]
    public void TestInvalidMinutes()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();
        //Act
        ValidateError(() => builder.AtMinute(-1), "minute", 0, 59);
        ValidateError(() => builder.AtMinute(60), "minute", 0, 59);
        ValidateError(() => builder.EveryMinutes(0), "minutes", 1, 60);
        ValidateError(() => builder.EveryMinutes(61), "minutes", 1, 60);
        ValidateError(() => builder.AtMinutes([2, 4, -1]), "minutes", 0, 59);
        ValidateError(() => builder.AtMinutes([58, 59, 60]), "minutes", 0, 59);
    }

    [TestMethod]
    public void TestHours()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();

        //Act
        var every12Hours = builder.EveryHours(12).Build();
        var at12Hours = builder.AtHour(12).Build();
        var atHours = builder.AtHours([1, 12, 23]).Build();

        //Assert
        Assert.AreEqual("0 * */12 * * *", every12Hours.AsString);
        Assert.AreEqual("0 * 12 * * *", at12Hours.AsString);
        Assert.AreEqual("0 * 1,12,23 * * *", atHours.AsString);
    }

    [TestMethod]
    public void TestInvalidHours()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();
        //Act
        ValidateError(() => builder.AtHour(-1), "hour", 0, 23);
        ValidateError(() => builder.AtHour(24), "hour", 0, 23);
        ValidateError(() => builder.EveryHours(0), "hours", 1, 24);
        ValidateError(() => builder.EveryHours(25), "hours", 1, 24);
        ValidateError(() => builder.AtHours([2, 4, -1]), "hours", 0, 23);
        ValidateError(() => builder.AtHours([22, 23, 24]), "hours", 0, 23);
    }

    [TestMethod]
    public void TestDays()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();

        //Act
        var every12Days = builder.EveryDays(12).Build();
        var at12Days = builder.AtDay(12).Build();
        var atDays = builder.AtDays([1, 12, 23]).Build();

        //Assert
        Assert.AreEqual("0 * * */12 * *", every12Days.AsString);
        Assert.AreEqual("0 * * 12 * *", at12Days.AsString);
        Assert.AreEqual("0 * * 1,12,23 * *", atDays.AsString);
    }

    [TestMethod]
    public void TestInvalidDays()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();
        //Act
        ValidateError(() => builder.AtDay(-1), "day", 1, 31);
        ValidateError(() => builder.AtDay(32), "day", 1, 31);
        ValidateError(() => builder.EveryDays(0), "days", 1, 31);
        ValidateError(() => builder.EveryDays(32), "days", 1, 31);
        ValidateError(() => builder.AtDays([2, 4, -1]), "days", 1, 31);
        ValidateError(() => builder.AtDays([22, 23, 32]), "days", 1, 31);
    }

    [TestMethod]
    public void TestMonths()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();

        //Act
        var every6Months = builder.EveryMonths(6).Build();
        var at6Months = builder.AtMonth(6).Build();
        var atMonths = builder.AtMonths([1, 6, 12]).Build();

        //Assert
        Assert.AreEqual("0 * * * */6 *", every6Months.AsString);
        Assert.AreEqual("0 * * * 6 *", at6Months.AsString);
        Assert.AreEqual("0 * * * 1,6,12 *", atMonths.AsString);
    }

    [TestMethod]
    public void TestInvalidMonths()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();
        //Act
        ValidateError(() => builder.AtMonth(-1), "month", 1, 12);
        ValidateError(() => builder.AtMonth(13), "month", 1, 12);
        ValidateError(() => builder.EveryMonths(0), "months", 1, 12);
        ValidateError(() => builder.EveryMonths(13), "months", 1, 12);
        ValidateError(() => builder.AtMonths([2, 4, -1]), "months", 1, 12);
        ValidateError(() => builder.AtMonths([11, 12, 13]), "months", 1, 12);
    }

    [TestMethod]
    public void TestDayOfWeek()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();

        //Act
        var onWednesday= builder.AtDayOfWeek(3).Build();
        var onWedThurFri = builder.AtDaysOfWeek([3,4,5]).Build();

        //Assert
        Assert.AreEqual("0 * * * * 3", onWednesday.AsString);
        Assert.AreEqual("0 * * * * 3,4,5", onWedThurFri.AsString);
    }

    [TestMethod]
    public void TestInvalidWeekDays()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();
        //Act
        ValidateError(() => builder.AtDayOfWeek(0), "dayOfWeek", 1, 7);
        ValidateError(() => builder.AtDayOfWeek(8), "dayOfWeek", 1, 7);
        ValidateError(() => builder.AtDaysOfWeek([1,2,0]), "daysOfWeek", 1, 7);
        ValidateError(() => builder.AtDaysOfWeek([6,7,8]), "daysOfWeek", 1, 7);
    }

    [TestMethod]
    public void TestWeekDays()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();

        //Act
        var weekdays = builder.WeekDays().Build();
        var weekdaysTime = builder.WeekDaysAt(6,30).Build();

        //Assert
        Assert.AreEqual("0 * * * * 1,2,3,4,5", weekdays.AsString);
        Assert.AreEqual("0 30 6 * * 1,2,3,4,5", weekdaysTime.AsString);
    }

    [TestMethod]
    public void TestInvalidWeekDaysAt()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();
        //Act
        ValidateError(() => builder.WeekDaysAt(-1, 0), "hour", 0, 23);
        ValidateError(() => builder.WeekDaysAt(24, 0), "hour", 0, 23);
        ValidateError(() => builder.WeekDaysAt(12, -1), "minute", 0, 59);
        ValidateError(() => builder.WeekDaysAt(12, 60), "minute", 0, 59);
    }

    [TestMethod]
    public void TestDailyAt()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();

        //Act
        var daily = builder.DailyAt(6,30).Build();

        //Assert
        Assert.AreEqual("0 30 6 * * *", daily.AsString);
    }

    [TestMethod]
    public void TestInvalidDailyAt()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();
        //Act
        ValidateError(() => builder.DailyAt(-1, 0), "hour", 0, 23);
        ValidateError(() => builder.DailyAt(24, 0), "hour", 0, 23);
        ValidateError(() => builder.DailyAt(12, -1), "minute", 0, 59);
        ValidateError(() => builder.DailyAt(12, 60), "minute", 0, 59);
    }

    [TestMethod]
    public void TestMonthlyOn()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();

        //Act
        var monthly = builder.MonthlyOn(15).Build();
        var monthlyAt = builder.MonthlyOnAt(15,6,30).Build();

        //Assert
        Assert.AreEqual("0 0 0 15 * *", monthly.AsString);
        Assert.AreEqual("0 30 6 15 * *", monthlyAt.AsString);
    }

    [TestMethod]
    public void TestInvalidMonthlyOn()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();
        //Act
        ValidateError(() => builder.MonthlyOn(-1), "day", 1, 31);
        ValidateError(() => builder.MonthlyOn(32), "day", 1, 31);
        ValidateError(() => builder.MonthlyOnAt(15, -1, 0), "hour", 0, 23);
        ValidateError(() => builder.MonthlyOnAt(15, 24, 0), "hour", 0, 23);
        ValidateError(() => builder.MonthlyOnAt(15, 12, -1), "minute", 0, 59);
        ValidateError(() => builder.MonthlyOnAt(15, 12, 60), "minute", 0, 59);
    }

    [TestMethod]
    public void TestFluentBuild()
    {
        //Arrange
        var builder = new WorkflowScheduleBuilder();

        //Act
        var value = builder
            .AtSecond(30)
            .AtMinute(30)
            .AtHour(6)
            .AtDay(15)
            .AtMonth(6)
            .AtDayOfWeek(3)
            .Build();

        //Assert
        Assert.AreEqual("30 30 6 15 6 3", value.AsString);
    }

    [TestMethod]
    public void TestParse()
    {
        //Arrange
        var cronString = "30 30 6 15 6 3";
        //Act
        var builder = WorkflowScheduleBuilder.Parse(cronString);
        var value = builder.Build();
        //Assert
        Assert.AreEqual(cronString, value.AsString);
    }

    [TestMethod]
    public void TestParseInvalid()
    {
        //Arrange
        var invalidCronString = "invalid cron string";
        //Act
        var error = Assert.ThrowsExactly<FormatException>(() => WorkflowScheduleBuilder.Parse(invalidCronString));
        //Assert
        Assert.AreEqual("Invalid cron string specified, please specify a crontab string format including seconds", error.Message);
    }
}
