using JetFlow.Attributes;
using JetFlow.Helpers;
using JetFlow.Interfaces;

namespace JetFlow.Testing;

[TestClass]
public class Workflow_NameHelperTests
{
    private static void TestGetWorkflowName<TWorkflow>(string cleanedName, string rawName)
    {
        Assert.AreEqual(cleanedName, NameHelper.GetWorkflowName<TWorkflow>().cleanedName);
        Assert.AreEqual(rawName, NameHelper.GetWorkflowName<TWorkflow>().rawName);
    }

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
        TestGetWorkflowName<TestWorkflowWithoutAttribute>("TestWorkflowWithoutAttribute", "TestWorkflowWithoutAttribute");
        TestGetWorkflowName<TestWorkflowWithInputWithoutAttribute>("TestWorkflowWithInputWithoutAttribute", "TestWorkflowWithInputWithoutAttribute");
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
        TestGetWorkflowName<TestWorkflowWithAttribute>("CustomWorkflowName", "CustomWorkflowName");
        TestGetWorkflowName<TestWorkflowWithInputWithAttribute>("CustomWorkflowWithInputName", "CustomWorkflowWithInputName");
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
        TestGetWorkflowName<TestWorkflowImplementingInterface>("CustomWorkflowInterfaceName", "CustomWorkflowInterfaceName");
        TestGetWorkflowName<TestWorkflowImplementingInputInterface>("CustomWorkflowWithInputInterfaceName", "CustomWorkflowWithInputInterfaceName");
    }

    private class TestWorkflowInheritingFromBase : TestWorkflowWithAttribute
    {}
    private class TestWorkflowInheritingFromBaseWithInput : TestWorkflowWithInputWithAttribute
    { }
    [TestMethod]
    public void GetWorkflowName_ShouldReturnBaseClassAttributeValue_WhenInheritingFromBaseClassWithAttribute()
    {
        TestGetWorkflowName<TestWorkflowInheritingFromBase>("CustomWorkflowName", "CustomWorkflowName");
        TestGetWorkflowName<TestWorkflowInheritingFromBaseWithInput>("CustomWorkflowWithInputName", "CustomWorkflowWithInputName");
    }

    private class TestWorkflowInheritingFromBaseWithoutAttribute : TestWorkflowImplementingInterface
    { }
    private class TestWorkflowInheritingFromBaseWithInputWithoutAttribute : TestWorkflowImplementingInputInterface
    { }
    [TestMethod]
    public void GetWorkflowName_ShouldReturnBaseInterfaceAttributeValue_WhenInheritingFromBaseClassImplementingInterfaceWithAttribute()
    {
        TestGetWorkflowName<TestWorkflowInheritingFromBaseWithoutAttribute>("CustomWorkflowInterfaceName", "CustomWorkflowInterfaceName");
        TestGetWorkflowName<TestWorkflowInheritingFromBaseWithInputWithoutAttribute>("CustomWorkflowWithInputInterfaceName", "CustomWorkflowWithInputInterfaceName");
    }

    private class TestSubWorkflowInheritingFromBase : TestWorkflowInheritingFromBase
    { }
    private class TestSubWorkflowInheritingFromBaseWithInput : TestWorkflowInheritingFromBaseWithInput
    { }
    [TestMethod]
    public void GetWorkflowName_ShouldReturnBaseClassAttributeValue_WhenInheritingFromSubClassOfBaseClassWithAttribute()
    {
        TestGetWorkflowName<TestSubWorkflowInheritingFromBase>("CustomWorkflowName", "CustomWorkflowName");
        TestGetWorkflowName<TestSubWorkflowInheritingFromBaseWithInput>("CustomWorkflowWithInputName", "CustomWorkflowWithInputName");
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
        TestGetWorkflowName<TestWorkflowWithInvalidCharactersInName>("CustomWorkflowWith_Invalid_Characters", "CustomWorkflowWith Invalid Characters!@#$%^&*()");
        TestGetWorkflowName<TestWorkflowWithInputInvalidCharactersInName>("CustomWorkflowWithInput_Invalid_Characters", "CustomWorkflowWithInput Invalid Characters!@#$%^&*()");
    }
}
