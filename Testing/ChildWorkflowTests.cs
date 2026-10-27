using JetFlow.Attributes;
using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;
using JetFlow.States;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Net;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace JetFlow.Testing;

[TestClass]
public class ChildWorkflowTests
{
    private const string TestNamespace = "ChildWorkflowTests";

    private static NatsTestHarness? natsTestHarness;

    [ClassInitialize]
    public static async Task Init(TestContext testContext)
    {
        natsTestHarness = new NatsTestHarness();
        await natsTestHarness.StartAsync();
    }

    [ClassCleanup]
    public static async Task Cleanup()
        => await (natsTestHarness?.DisposeAsync()??ValueTask.CompletedTask);
    
    [WorkflowName("Successful Child Workflow Without Input")]
    private sealed class SuccessfulChildWorkflowWithoutInput : IWorkflow
    {
        ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            return ValueTask.CompletedTask;
        }
    }
    [WorkflowName("Successful Child Worfklow With Input")]
    private sealed class SuccessfulChildWorkflowWitInput : IWorkflow<string>
    {
        public static string? SuppliedInput { get; set; } = null;
        ValueTask IWorkflow<string>.ExecuteAsync(IWorkflowContext context, string input)
        {
            SuppliedInput = input;
            return ValueTask.CompletedTask;
        }
    }
    [WorkflowName("Failing Child Workflow Without Input")]
    private sealed class FailingChildWorkflowWithoutInput : IWorkflow
    {
        ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            throw new NotImplementedException();
        }
    }
    [WorkflowName("Failing Child Workflow With Input")]
    private sealed class FailingChildWorkflowWithInput : IWorkflow<string>
    {
        public static string? SuppliedInput { get; set; } = null;
        ValueTask IWorkflow<string>.ExecuteAsync(IWorkflowContext context, string input)
        {
            SuppliedInput = input;
            throw new NotImplementedException();
        }
    }
    [WorkflowName("Parent Workflow")]
    private sealed class ParentWorkflow : IWorkflow<string>
    {
        async ValueTask IWorkflow<string>.ExecuteAsync(IWorkflowContext context, string input)
        {
            await context.ExecuteChildWorkflowAsync<SuccessfulChildWorkflowWithoutInput>();
            await context.ExecuteChildWorkflowAsync<FailingChildWorkflowWithoutInput>();
            await context.ExecuteChildWorkflowAsync<SuccessfulChildWorkflowWitInput, string>(new(input));
            await context.ExecuteChildWorkflowAsync<FailingChildWorkflowWithInput, string>(new(input));
        }
    }

    [TestMethod]
    [DataRow(null, DisplayName = "Default Namespace")]
    [DataRow(TestNamespace, DisplayName = "Supplied Namespace")]
    public async Task ExecuteChildWorkflow(string? namespaceValue)
    {
        Assert.IsNotNull(natsTestHarness);
        SuccessfulChildWorkflowWitInput.SuppliedInput=null;
        FailingChildWorkflowWithInput.SuppliedInput=null;
        var argument = "I am the very model of a modern major general";
        var runId = Guid.Empty;
        var subjectMapper = new SubjectMapper(namespaceValue);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        var objContext = jsContext.CreateObjectStoreContext();
        var connectionOptions = new ConnectionOptions(natsConnection)
        {
            Namespace=namespaceValue
        };
        var messageSerializer = new MessageSerializer(connectionOptions.CompressionType, connectionOptions.JsonTypeInfoResolver);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<ParentWorkflow,string>();
        await connection.RegisterWorkflowAsync<SuccessfulChildWorkflowWithoutInput>();
        await connection.RegisterWorkflowAsync<FailingChildWorkflowWithoutInput>();
        await connection.RegisterWorkflowAsync<SuccessfulChildWorkflowWitInput, string>();
        await connection.RegisterWorkflowAsync<FailingChildWorkflowWithInput, string>();

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForArchive<ParentWorkflow>(
            natsConnection,
            subjectMapper,
            async () =>
            {
                runId = await connection.StartWorkflowAsync<ParentWorkflow,string>(executionRequest: new(argument));
                return runId;
            }
        );

        // Assert
        Assert.IsNotNull(result);

        //Verify
        Assert.AreEqual(argument, SuccessfulChildWorkflowWitInput.SuppliedInput);
        Assert.AreEqual(argument, FailingChildWorkflowWithInput.SuppliedInput);
        var archiveStore = await objContext.GetObjectStoreAsync(subjectMapper.WorkflowArchiveObjectstore);
        var archiveData = await archiveStore.GetBytesAsync($"{NameHelper.GetWorkflowName<ParentWorkflow>().cleanedName}/{runId}");
        var archive = JsonSerializer.Deserialize<ArchivedWorkflow>(archiveData, Constants.JsonOptions);
        Assert.AreEqual(runId, archive.ID);
        Assert.IsNull(archive.SchedulerId);
        Assert.IsTrue(archive.IsSuccessful);
        Assert.HasCount(4, archive.Steps);
        Assert.AreEqual(WorkflowStepTypes.ChildWorkflow, archive.Steps[0].Type);
        Assert.AreEqual(NameHelper.GetChildWorkflowName<SuccessfulChildWorkflowWithoutInput>().rawName, archive.Steps[0].Name);
        Assert.AreEqual(NameHelper.GetWorkflowName<SuccessfulChildWorkflowWithoutInput>().rawName, archive.Steps[0].ChildWorkflowName);
        Assert.IsNull(archive.Steps[0].Input);
        Assert.IsNotNull(archive.Steps[0].Result);
        Assert.AreEqual(WorkflowResult.Success, JsonSerializer.Deserialize<WorkflowResult>((JsonElement)archive.Steps[0].Result!));
        Assert.IsFalse((await archiveStore.GetInfoAsync($"{NameHelper.GetWorkflowName<SuccessfulChildWorkflowWithoutInput>().cleanedName}/{archive.Steps[0].ChildWorkflowID}")).Deleted);
        Assert.AreEqual(WorkflowStepTypes.ChildWorkflow, archive.Steps[1].Type);
        Assert.AreEqual(NameHelper.GetChildWorkflowName<FailingChildWorkflowWithoutInput>().rawName, archive.Steps[1].Name);
        Assert.AreEqual(NameHelper.GetWorkflowName<FailingChildWorkflowWithoutInput>().rawName, archive.Steps[1].ChildWorkflowName);
        Assert.IsNull(archive.Steps[1].Input);
        Assert.IsNotNull(archive.Steps[1].Result);
        Assert.AreEqual(WorkflowResult.Failure, JsonSerializer.Deserialize<WorkflowResult>((JsonElement)archive.Steps[1].Result!));
        Assert.IsFalse((await archiveStore.GetInfoAsync($"{NameHelper.GetWorkflowName<FailingChildWorkflowWithoutInput>().cleanedName}/{archive.Steps[1].ChildWorkflowID}")).Deleted);
        Assert.AreEqual(WorkflowStepTypes.ChildWorkflow, archive.Steps[2].Type);
        Assert.AreEqual(NameHelper.GetChildWorkflowName<SuccessfulChildWorkflowWitInput>().rawName, archive.Steps[2].Name);
        Assert.AreEqual(NameHelper.GetWorkflowName<SuccessfulChildWorkflowWitInput>().rawName, archive.Steps[2].ChildWorkflowName);
        Assert.IsNotNull(archive.Steps[2].Input);
        Assert.AreEqual(argument, JsonSerializer.Deserialize<string>((JsonElement)archive.Steps[2].Input!));
        Assert.IsNotNull(archive.Steps[2].Result);
        Assert.AreEqual(WorkflowResult.Success, JsonSerializer.Deserialize<WorkflowResult>((JsonElement)archive.Steps[2].Result!));
        Assert.IsFalse((await archiveStore.GetInfoAsync($"{NameHelper.GetWorkflowName<SuccessfulChildWorkflowWitInput>().cleanedName}/{archive.Steps[2].ChildWorkflowID}")).Deleted);
        Assert.AreEqual(WorkflowStepTypes.ChildWorkflow, archive.Steps[3].Type);
        Assert.AreEqual(NameHelper.GetChildWorkflowName<FailingChildWorkflowWithInput>().rawName, archive.Steps[3].Name);
        Assert.AreEqual(NameHelper.GetWorkflowName<FailingChildWorkflowWithInput>().rawName, archive.Steps[3].ChildWorkflowName);
        Assert.IsNotNull(archive.Steps[3].Input);
        Assert.AreEqual(argument, JsonSerializer.Deserialize<string>((JsonElement)archive.Steps[3].Input!));
        Assert.IsNotNull(archive.Steps[3].Result);
        Assert.AreEqual(WorkflowResult.Failure, JsonSerializer.Deserialize<WorkflowResult>((JsonElement)archive.Steps[3].Result!));
        Assert.IsFalse((await archiveStore.GetInfoAsync($"{NameHelper.GetWorkflowName<FailingChildWorkflowWithInput>().cleanedName}/{archive.Steps[3].ChildWorkflowID}")).Deleted);
    }

    [WorkflowName("Parallel Child Workflow")]
    private sealed class ParallelChildWorkflow : IWorkflow<int>
    {
        ValueTask IWorkflow<int>.ExecuteAsync(IWorkflowContext context, int input)
        {
            if (input<2)
                return ValueTask.CompletedTask;
            throw new NotImplementedException();
        }
    }
    [WorkflowName("Parallel Parent Workflow")]
    private sealed class ParallelParentWorkflow : IWorkflow
    {
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            _ = await context.ExecuteChildWorkflowsAsync<ParallelChildWorkflow, int>(new([
                    0,1,2
                ]));
            await context.ExecuteChildWorkflowAsync<SuccessfulChildWorkflowWithoutInput>();
        }
    }

    [TestMethod]
    [DataRow(null, DisplayName = "Default Namespace")]
    [DataRow(TestNamespace, DisplayName = "Supplied Namespace")]
    public async Task ExecuteChildWorkflows(string? namespaceValue)
    {
        Assert.IsNotNull(natsTestHarness);
        var runId = Guid.Empty;
        var subjectMapper = new SubjectMapper(namespaceValue);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var jsContext = new NatsJSContext(natsConnection);
        var objContext = jsContext.CreateObjectStoreContext();
        var connectionOptions = new ConnectionOptions(natsConnection)
        {
            Namespace=namespaceValue
        };
        var messageSerializer = new MessageSerializer(connectionOptions.CompressionType, connectionOptions.JsonTypeInfoResolver);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<ParallelParentWorkflow>();
        await connection.RegisterWorkflowAsync<SuccessfulChildWorkflowWithoutInput>();
        await connection.RegisterWorkflowAsync<ParallelChildWorkflow, int>();

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForArchive<ParallelParentWorkflow>(
            natsConnection,
            subjectMapper,
            async () =>
            {
                runId = await connection.StartWorkflowAsync<ParallelParentWorkflow>();
                return runId;
            }
        );

        // Assert
        Assert.IsNotNull(result);
        await Task.Delay(TimeSpan.FromSeconds(5)); //ensure all archiving is complete

        //Verify
        var archiveStore = await objContext.GetObjectStoreAsync(subjectMapper.WorkflowArchiveObjectstore);
        var archiveData = await archiveStore.GetBytesAsync($"{NameHelper.GetWorkflowName<ParallelParentWorkflow>().cleanedName}/{runId}");
        var archive = JsonSerializer.Deserialize<ArchivedWorkflow>(archiveData, Constants.JsonOptions);
        Assert.AreEqual(runId, archive.ID);
        Assert.IsNull(archive.SchedulerId);
        Assert.IsTrue(archive.IsSuccessful);
        Assert.HasCount(4, archive.Steps);
        Assert.AreEqual(WorkflowStepTypes.ChildWorkflow, archive.Steps[0].Type);
        Assert.AreEqual(NameHelper.GetChildWorkflowName<ParallelChildWorkflow>().rawName, archive.Steps[0].Name);
        Assert.AreEqual(NameHelper.GetWorkflowName<ParallelChildWorkflow>().rawName, archive.Steps[0].ChildWorkflowName);
        Assert.IsNotNull(archive.Steps[0].Input);
        Assert.IsNotNull(archive.Steps[0].Result);
        Assert.AreEqual(WorkflowResult.Success, JsonSerializer.Deserialize<WorkflowResult>((JsonElement)archive.Steps[0].Result!));
        Assert.IsFalse((await archiveStore.GetInfoAsync($"{NameHelper.GetWorkflowName<ParallelChildWorkflow>().cleanedName}/{archive.Steps[0].ChildWorkflowID}")).Deleted);
        Assert.AreEqual(WorkflowStepTypes.ChildWorkflow, archive.Steps[1].Type);
        Assert.AreEqual(NameHelper.GetChildWorkflowName<ParallelChildWorkflow>().rawName, archive.Steps[1].Name);
        Assert.AreEqual(NameHelper.GetWorkflowName<ParallelChildWorkflow>().rawName, archive.Steps[0].ChildWorkflowName);
        Assert.IsNotNull(archive.Steps[1].Input);
        Assert.IsNotNull(archive.Steps[1].Result);
        Assert.AreEqual(WorkflowResult.Success, JsonSerializer.Deserialize<WorkflowResult>((JsonElement)archive.Steps[1].Result!));
        Assert.IsFalse((await archiveStore.GetInfoAsync($"{NameHelper.GetWorkflowName<ParallelChildWorkflow>().cleanedName}/{archive.Steps[1].ChildWorkflowID}")).Deleted);
        Assert.AreEqual(WorkflowStepTypes.ChildWorkflow, archive.Steps[2].Type);
        Assert.AreEqual(NameHelper.GetChildWorkflowName<ParallelChildWorkflow>().rawName, archive.Steps[2].Name);
        Assert.AreEqual(NameHelper.GetWorkflowName<ParallelChildWorkflow>().rawName, archive.Steps[2].ChildWorkflowName);
        Assert.IsNotNull(archive.Steps[2].Input);
        Assert.IsNotNull(archive.Steps[2].Result);
        Assert.AreEqual(WorkflowResult.Failure, JsonSerializer.Deserialize<WorkflowResult>((JsonElement)archive.Steps[2].Result!));
        Assert.IsFalse((await archiveStore.GetInfoAsync($"{NameHelper.GetWorkflowName<ParallelChildWorkflow>().cleanedName}/{archive.Steps[2].ChildWorkflowID}")).Deleted);
        Assert.AreEqual(WorkflowStepTypes.ChildWorkflow, archive.Steps[3].Type);
        Assert.AreEqual(NameHelper.GetChildWorkflowName<SuccessfulChildWorkflowWithoutInput>().rawName, archive.Steps[3].Name);
        Assert.AreEqual(NameHelper.GetWorkflowName<SuccessfulChildWorkflowWithoutInput>().rawName, archive.Steps[3].ChildWorkflowName);
        Assert.IsNull(archive.Steps[3].Input);
        Assert.IsNotNull(archive.Steps[3].Result);
        Assert.AreEqual(WorkflowResult.Success, JsonSerializer.Deserialize<WorkflowResult>((JsonElement)archive.Steps[3].Result!));
        Assert.IsFalse((await archiveStore.GetInfoAsync($"{NameHelper.GetWorkflowName<SuccessfulChildWorkflowWithoutInput>().cleanedName}/{archive.Steps[3].ChildWorkflowID}")).Deleted);
    }
}
