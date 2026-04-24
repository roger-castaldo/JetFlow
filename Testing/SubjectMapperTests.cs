namespace JetFlow.Testing;

[TestClass]
public class SubjectMapperTests
{
    [TestMethod]
    [DataRow(null, "JETFLOW_WORKFLOW_EVENTS")]
    [DataRow("Fred Flintstone", "FREDFLINTSTONE_JETFLOW_WORKFLOW_EVENTS")]
    [DataRow("short", "SHORT_JETFLOW_WORKFLOW_EVENTS")]
    [DataRow("SHORT", "SHORT_JETFLOW_WORKFLOW_EVENTS")]
    [DataRow("customer 123456789", "CUSTOMER123456789_JETFLOW_WORKFLOW_EVENTS")]
    [DataRow("almost too long a instance namespace1", "ALMOSTTOOLONGAINSTANCENAMESPACE1_JETFLOW_WORKFLOW_EVENTS")]
    public void TestCorrectedNamespaces(string? instanceNamespace,string expectedWorfklowEventsStream)
    {
        //Arrange
        var subjectMapper = new SubjectMapper(instanceNamespace);

        //Act
        var workflowEventsStream = subjectMapper.WorkflowEventsStreamsName;

        //Verify
        Assert.AreEqual(expectedWorfklowEventsStream, workflowEventsStream);
    }

    [TestMethod]
    [DataRow("definitely a very long and not valid length namespace")]
    [DataRow("customer 12345678912345678912345679123456789123456789")]
    [DataRow("customer 00000000-0000-0000-0000-000000000000.abcdefgh>123456")]
    public void TestErrorNamespaces(string instanceNamespace)
    {
        //Act
        var exception = Assert.Throws<ArgumentException>(() => new SubjectMapper(instanceNamespace));

        //Verify
        Assert.IsNotNull(exception);
        Assert.StartsWith("Namespace must be less than or equal to 32 characters after removing non-alphanumeric characters.", exception.Message);
        Assert.AreEqual("instanceNamespace", exception.ParamName);
    }
}
