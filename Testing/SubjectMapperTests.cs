using JetFlow.Testing.Helpers;

namespace JetFlow.Testing;

[TestClass]
public class SubjectMapperTests
{
    [TestMethod]
    [DataRow(null, "", "")]
    [DataRow("Fred Flintstone", "FREDFLINTSTONE_","fredflintstone.")]
    [DataRow("short", "SHORT_","short.")]
    [DataRow("SHORT", "SHORT_", "short.")]
    [DataRow("customer 123456789", "CUSTOMER123456789_","customer123456789.")]
    [DataRow("almost too long a instance namespace1", "ALMOSTTOOLONGAINSTANCENAMESPACE1_","almosttoolongainstancenamespace1.")]
    public void TestCorrectedNamespaces(string? instanceNamespace,string streamStart, string subjectNamespace)
    {
        //Arrange
        var subjectMapper = new SubjectMapper(instanceNamespace);
        var workflowName = TestsHelper.GenerateRandomString(32);
        var instance = TestsHelper.GenerateRandomString(32);
        var stepName = TestsHelper.GenerateRandomString(32);
        var activityName = TestsHelper.GenerateRandomString(32);

        //Verify
        Assert.AreEqual($"JETFLOW_{streamStart}WORKFLOW_EVENTS", subjectMapper.WorkflowEventsStreamsName);
        Assert.AreEqual($"jetflow.{subjectNamespace}wkf.>", subjectMapper.WorkflowEventsFilter);
        Assert.AreEqual($"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.config", subjectMapper.WorkflowConfigure(workflowName, instance));
        Assert.AreEqual($"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.start", subjectMapper.WorkflowStart(workflowName, instance));
        Assert.AreEqual($"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.end", subjectMapper.WorkflowEnd(workflowName, instance));
        Assert.AreEqual($"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.archived", subjectMapper.WorkflowArchived(workflowName, instance));
        Assert.AreEqual($"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.purge", subjectMapper.WorkflowPurge(workflowName, instance));
        Assert.AreEqual($"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.delaystart", subjectMapper.WorkflowDelayStart(workflowName, instance));
        Assert.AreEqual($"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.delayend", subjectMapper.WorkflowDelayEnd(workflowName, instance));
        Assert.AreEqual($"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.timer", subjectMapper.WorkflowTimer(workflowName, instance));
        Assert.AreEqual($"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.{stepName}.stepstart", subjectMapper.WorkflowStepStart(workflowName, instance, stepName));
        Assert.AreEqual($"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.{stepName}.stepend", subjectMapper.WorkflowStepEnd(workflowName, instance, stepName));
        Assert.AreEqual($"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.{stepName}.steperror", subjectMapper.WorkflowStepError(workflowName, instance, stepName));
        Assert.AreEqual($"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.{stepName}.steptimeout", subjectMapper.WorkflowStepTimeout(workflowName, instance, stepName));
        Assert.AreEqual($"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.{stepName}.stepretry", subjectMapper.WorkflowStepRetry(workflowName, instance, stepName));
        Assert.AreEqual($"jetflow.{subjectNamespace}wkf.{workflowName}.{instance}.>", subjectMapper.WorkflowPurgeFilter(workflowName, instance));
        
        Assert.AreEqual($"JETFLOW_{streamStart}ACTIVITY_QUEUE", subjectMapper.ActivityQueueStream);
        Assert.AreEqual($"jetflow.{subjectNamespace}act.>", subjectMapper.ActivityEventsFilter);
        Assert.AreEqual($"jetflow.{subjectNamespace}act.{activityName}.{workflowName}.{instance}.start", subjectMapper.ActivityStart(activityName, workflowName, instance));
        Assert.AreEqual($"jetflow.{subjectNamespace}act.{activityName}.{workflowName}.{instance}.timer", subjectMapper.ActivityTimer(activityName, workflowName, instance));
        Assert.AreEqual($"jetflow.{subjectNamespace}act.{activityName}.{workflowName}.{instance}.timeout", subjectMapper.ActivityTimeout(activityName, workflowName, instance));
        Assert.AreEqual($"jetflow.{subjectNamespace}act.*.{workflowName}.{instance}.>", subjectMapper.WorkflowActivityPurgeFilter(workflowName, instance));

        
        Assert.AreEqual($"JETFLOW_{streamStart}ACTIVITY_LOCKS", subjectMapper.ActivityLocksKeystore);
        Assert.AreEqual($"JETFLOW_{streamStart}WORKFLOW_CONFIGS", subjectMapper.WorkflowConfigKeystore);
        Assert.AreEqual($"JETFLOW_{streamStart}WORKFLOW_ARCHIVES", subjectMapper.WorkflowArchiveKeystore);

        Assert.AreEqual($"JETFLOW_{streamStart}SCHEDULED_WORKFLOWS", subjectMapper.ScheduledWorkflowStreamsName);
        Assert.AreEqual($"jetflow.{subjectNamespace}swf.>", subjectMapper.ScheduledWorkflowsFilter);
        Assert.AreEqual($"jetflow.{subjectNamespace}swf.{workflowName}.{instance}.config", subjectMapper.ScheduledWorkflowConfigure(workflowName, instance));
        Assert.AreEqual($"jetflow.{subjectNamespace}swf.{workflowName}.{instance}.start", subjectMapper.ScheduledWorkflowStart(workflowName, instance));
        Assert.AreEqual($"jetflow.{subjectNamespace}swf.{workflowName}.{instance}.timer", subjectMapper.ScheduledWorkflowTimer(workflowName, instance));
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
