using JetFlow.Attributes;
using JetFlow.Helpers;
using JetFlow.Interfaces;

namespace JetFlow.Testing;

[TestClass]
public class Workflow_NameHelperTests
{
    private static void TestGetWorkflowName<TWorkflow>(string name)
        => Assert.AreEqual(name, NameHelper.GetWorkflowName<TWorkflow>());

    private class TestWorkflowWithoutAttribute : IWorkflow
    {
        ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            throw new NotImplementedException();
        }
    }
    private class TestWorkflowWithInputWithoutAttribute : IWorkflow<string>
    {
        ValueTask IWorkflow<string>.ExecuteAsync(IWorkflowContext context, string? input)
        {
            throw new NotImplementedException();
        }
    }
    [TestMethod]
    public void GetWorkflowName_ShouldReturnClassName_WhenNoAttributeIsPresent()
    {
        TestGetWorkflowName<TestWorkflowWithoutAttribute>("TestWorkflowWithoutAttribute");
        TestGetWorkflowName<TestWorkflowWithInputWithoutAttribute>("TestWorkflowWithInputWithoutAttribute");
    }

    [WorkflowName("CustomWorkflowName")]
    private class TestWorkflowWithAttribute : IWorkflow
    {
        ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            throw new NotImplementedException();
        }
    }
    [WorkflowName("CustomWorkflowWithInputName")]
    private class TestWorkflowWithInputWithAttribute : IWorkflow<string>
    {
        ValueTask IWorkflow<string>.ExecuteAsync(IWorkflowContext context, string? input)
        {
            throw new NotImplementedException();
        }
    }
    [TestMethod]
    public void GetWorkflowName_ShouldReturnAttributeValue_WhenAttributeIsPresent()
    {
        TestGetWorkflowName<TestWorkflowWithAttribute>("CustomWorkflowName");
        TestGetWorkflowName<TestWorkflowWithInputWithAttribute>("CustomWorkflowWithInputName");
    }

    [WorkflowName("CustomWorkflowInterfaceName")]
    private interface ITestWorkflowInterface : IWorkflow
    {}
    [WorkflowName("CustomWorkflowWithInputInterfaceName")]
    private interface ITestWorkflowWithInputInterface : IWorkflow<string>
    { }
    private class TestWorkflowImplementingInterface : ITestWorkflowInterface
    {
        ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            throw new NotImplementedException();
        }
    }
    private class TestWorkflowImplementingInputInterface : ITestWorkflowWithInputInterface
    {
        ValueTask IWorkflow<string>.ExecuteAsync(IWorkflowContext context, string? input)
        {
            throw new NotImplementedException();
        }
    }
    [TestMethod]
    public void GetWorkflowName_ShouldReturnInterfaceAttributeValue_WhenImplementedInterfaceHasAttribute()
    {
        TestGetWorkflowName<TestWorkflowImplementingInterface>("CustomWorkflowInterfaceName");
        TestGetWorkflowName<TestWorkflowImplementingInputInterface>("CustomWorkflowWithInputInterfaceName");
    }

    private class TestWorkflowInheritingFromBase : TestWorkflowWithAttribute
    {}
    private class TestWorkflowInheritingFromBaseWithInput : TestWorkflowWithInputWithAttribute
    { }
    [TestMethod]
    public void GetWorkflowName_ShouldReturnBaseClassAttributeValue_WhenInheritingFromBaseClassWithAttribute()
    {
        TestGetWorkflowName<TestWorkflowInheritingFromBase>("CustomWorkflowName");
        TestGetWorkflowName<TestWorkflowInheritingFromBaseWithInput>("CustomWorkflowWithInputName");
    }

    private class TestWorkflowInheritingFromBaseWithoutAttribute : TestWorkflowImplementingInterface
    { }
    private class TestWorkflowInheritingFromBaseWithInputWithoutAttribute : TestWorkflowImplementingInputInterface
    { }
    [TestMethod]
    public void GetWorkflowName_ShouldReturnBaseInterfaceAttributeValue_WhenInheritingFromBaseClassImplementingInterfaceWithAttribute()
    {
        TestGetWorkflowName<TestWorkflowInheritingFromBaseWithoutAttribute>("CustomWorkflowInterfaceName");
        TestGetWorkflowName<TestWorkflowInheritingFromBaseWithInputWithoutAttribute>("CustomWorkflowWithInputInterfaceName");
    }

    private class TestSubWorkflowInheritingFromBase : TestWorkflowInheritingFromBase
    { }
    private class TestSubWorkflowInheritingFromBaseWithInput : TestWorkflowInheritingFromBaseWithInput
    { }
    [TestMethod]
    public void GetWorkflowName_ShouldReturnBaseClassAttributeValue_WhenInheritingFromSubClassOfBaseClassWithAttribute()
    {
        TestGetWorkflowName<TestSubWorkflowInheritingFromBase>("CustomWorkflowName");
        TestGetWorkflowName<TestSubWorkflowInheritingFromBaseWithInput>("CustomWorkflowWithInputName");
    }

    [WorkflowName("CustomWorkflowWith Invalid Characters!@#$%^&*()")]
    private class TestWorkflowWithInvalidCharactersInName : IWorkflow
    {
        ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            throw new NotImplementedException();
        }
    }
    [WorkflowName("CustomWorkflowWithInput Invalid Characters!@#$%^&*()")]
    private class TestWorkflowWithInputInvalidCharactersInName : IWorkflow<string>
    {
        ValueTask IWorkflow<string>.ExecuteAsync(IWorkflowContext context, string? input)
        {
            throw new NotImplementedException();
        }
    }
    [TestMethod]
    public void GetWorkflowName_ShouldReturnCleanedName_WhenAttributeValueContainsInvalidCharacters()
    {
        TestGetWorkflowName<TestWorkflowWithInvalidCharactersInName>("CustomWorkflowWith_Invalid_Characters");
        TestGetWorkflowName<TestWorkflowWithInputInvalidCharactersInName>("CustomWorkflowWithInput_Invalid_Characters");
    }
}
