using JetFlow.Attributes;
using JetFlow.Helpers;
using JetFlow.Interfaces;

namespace JetFlow.Testing;

[TestClass]
public class Activity_NameHelperTests
{
    private static (string cleanedName, string rawName) GetActivityName<TActivity>() 
        => NameHelper.GetActivityName<TActivity>();

    private class TestActivityWithoutAttribute : IActivity
    { 
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    private class TestActivityWithInputWithoutAttribute : IActivity<string>
    {
        Task IActivity<string>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    private class TestActivityWithOutputWithoutAttribute : IActivityWithReturn<string>
    {
        Task<string> IActivityWithReturn<string>.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    private class TestActivityWithInputAndOutputWithoutAttribute : IActivityWithReturn<string, string>
    {
        Task<string> IActivityWithReturn<string, string>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    [TestMethod]
    public void GetActivityName_ShouldReturnClassName_WhenNoAttributeIsPresent()
    {
        Assert.AreEqual("TestActivityWithoutAttribute", GetActivityName<TestActivityWithoutAttribute>().rawName);
        Assert.AreEqual("TestActivityWithInputWithoutAttribute", GetActivityName<TestActivityWithInputWithoutAttribute>().rawName);
        Assert.AreEqual("TestActivityWithOutputWithoutAttribute", GetActivityName<TestActivityWithOutputWithoutAttribute>().rawName);
        Assert.AreEqual("TestActivityWithInputAndOutputWithoutAttribute", GetActivityName<TestActivityWithInputAndOutputWithoutAttribute>().rawName);
    }

    private static void TestGetActivityName<TActivity>(string cleanedName, string rawName)
    {
        Assert.AreEqual(cleanedName, GetActivityName<TActivity>().cleanedName);
        Assert.AreEqual(rawName, GetActivityName<TActivity>().rawName);
    }

    [ActivityName("CustomActivityName")]
    private class TestActivityWithAttribute : IActivity
    {
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    [ActivityName("CustomActivityWithInputName")]
    private class TestActivityWithInputWithAttribute : IActivity<string>
    {
        Task IActivity<string>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    [ActivityName("CustomActivityWithOutputName")]
    private class TestActivityWithOutputWithAttribute : IActivityWithReturn<string>
    {
        Task<string> IActivityWithReturn<string>.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    [ActivityName("CustomActivityWithInputAndOutputName")]
    private class TestActivityWithInputAndOutputWithAttribute : IActivityWithReturn<string, string>
    {
        Task<string> IActivityWithReturn<string, string>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    [TestMethod]
    public void GetActivityName_ShouldReturnAttributeValue_WhenAttributeIsPresent()
    {
        TestGetActivityName<TestActivityWithAttribute>("CustomActivityName", "CustomActivityName");
        TestGetActivityName<TestActivityWithInputWithAttribute>("CustomActivityWithInputName", "CustomActivityWithInputName");
        TestGetActivityName<TestActivityWithOutputWithAttribute>("CustomActivityWithOutputName", "CustomActivityWithOutputName");
        TestGetActivityName<TestActivityWithInputAndOutputWithAttribute>("CustomActivityWithInputAndOutputName", "CustomActivityWithInputAndOutputName");
    }

    [ActivityName("CustomActivityInterfaceName")]
    private interface ITestActivityInterface : IActivity
    { }
    [ActivityName("CustomActivityWithInputInterfaceName")]
    private interface ITestActivityWithInputInterface : IActivity<string>
    { }
    [ActivityName("CustomActivityWithOutputInterfaceName")]
    private interface ITestActivityWithOutputInterface : IActivityWithReturn<string>
    { }
    [ActivityName("CustomActivityWithInputAndOutputInterfaceName")]
    private interface ITestActivityWithInputAndOutputInterface : IActivityWithReturn<string, string>
    { }
    private class TestActivityImplementingInterface : ITestActivityInterface
    {
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    private class TestActivityImplementingInputInterface : ITestActivityWithInputInterface
    {
        Task IActivity<string>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    private class TestActivityImplementingOutputInterface : ITestActivityWithOutputInterface
    {
        Task<string> IActivityWithReturn<string>.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    private class TestActivityImplementingInputAndOutputInterface : ITestActivityWithInputAndOutputInterface
    {
        Task<string> IActivityWithReturn<string, string>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    [TestMethod]
    public void GetActivityName_ShouldReturnInterfaceAttributeValue_WhenImplementedInterfaceHasAttribute()
    {
        TestGetActivityName<TestActivityImplementingInterface>("CustomActivityInterfaceName", "CustomActivityInterfaceName");
        TestGetActivityName<TestActivityImplementingInputInterface>("CustomActivityWithInputInterfaceName", "CustomActivityWithInputInterfaceName");
        TestGetActivityName<TestActivityImplementingOutputInterface>("CustomActivityWithOutputInterfaceName", "CustomActivityWithOutputInterfaceName");
        TestGetActivityName<TestActivityImplementingInputAndOutputInterface>("CustomActivityWithInputAndOutputInterfaceName", "CustomActivityWithInputAndOutputInterfaceName");
    }

    private class TestActivityInheritingFromBase : TestActivityWithAttribute
    { }
    private class TestActivityInheritingFromBaseWithInput : TestActivityWithInputWithAttribute
    { }
    private class TestActivityInheritingFromBaseWithOutput : TestActivityWithOutputWithAttribute
    { }
    private class TestActivityInheritingFromBaseWithInputAndOutput : TestActivityWithInputAndOutputWithAttribute
    { }
    [TestMethod]
    public void GetActivityName_ShouldReturnBaseClassAttributeValue_WhenInheritingFromBaseClassWithAttribute()
    {
        TestGetActivityName<TestActivityInheritingFromBase>("CustomActivityName", "CustomActivityName");
        TestGetActivityName<TestActivityInheritingFromBaseWithInput>("CustomActivityWithInputName", "CustomActivityWithInputName");
        TestGetActivityName<TestActivityInheritingFromBaseWithOutput>("CustomActivityWithOutputName", "CustomActivityWithOutputName");
        TestGetActivityName<TestActivityInheritingFromBaseWithInputAndOutput>("CustomActivityWithInputAndOutputName", "CustomActivityWithInputAndOutputName");
    }

    private class TestActivityInheritingFromBaseWithoutAttribute : TestActivityImplementingInterface
    { }
    private class TestActivityInheritingFromBaseWithInputWithoutAttribute : TestActivityImplementingInputInterface
    { }
    private class TestActivityInheritingFromBaseWithOutputWithoutAttribute : TestActivityImplementingOutputInterface
    { }
    private class TestActivityInheritingFromBaseWithInputAndOutputWithoutAttribute : TestActivityImplementingInputAndOutputInterface
    { }
    [TestMethod]
    public void GetActivityName_ShouldReturnBaseInterfaceAttributeValue_WhenInheritingFromBaseClassImplementingInterfaceWithAttribute()
    {
        TestGetActivityName<TestActivityInheritingFromBaseWithoutAttribute>("CustomActivityInterfaceName", "CustomActivityInterfaceName");
        TestGetActivityName<TestActivityInheritingFromBaseWithInputWithoutAttribute>("CustomActivityWithInputInterfaceName", "CustomActivityWithInputInterfaceName");
        TestGetActivityName<TestActivityInheritingFromBaseWithOutputWithoutAttribute>("CustomActivityWithOutputInterfaceName", "CustomActivityWithOutputInterfaceName");
        TestGetActivityName<TestActivityInheritingFromBaseWithInputAndOutputWithoutAttribute>("CustomActivityWithInputAndOutputInterfaceName", "CustomActivityWithInputAndOutputInterfaceName");
    }

    private class TestSubActivityInheritingFromBase : TestActivityInheritingFromBase
    { }
    private class TestSubActivityInheritingFromBaseWithInput : TestActivityInheritingFromBaseWithInput
    { }
    private class TestSubActivityInheritingFromBaseWithOutput : TestActivityInheritingFromBaseWithOutput
    { }
    private class TestSubActivityInheritingFromBaseWithInputAndOutput : TestActivityInheritingFromBaseWithInputAndOutput
    { }
    [TestMethod]
    public void GetActivityName_ShouldReturnBaseClassAttributeValue_WhenInheritingFromSubClassOfBaseClassWithAttribute()
    {
        TestGetActivityName<TestSubActivityInheritingFromBase>("CustomActivityName", "CustomActivityName");
        TestGetActivityName<TestSubActivityInheritingFromBaseWithInput>("CustomActivityWithInputName", "CustomActivityWithInputName");
        TestGetActivityName<TestSubActivityInheritingFromBaseWithOutput>("CustomActivityWithOutputName", "CustomActivityWithOutputName");
        TestGetActivityName<TestSubActivityInheritingFromBaseWithInputAndOutput>("CustomActivityWithInputAndOutputName", "CustomActivityWithInputAndOutputName");
    }

    [ActivityName("CustomActivityWith Invalid Characters!@#$%^&*()")]
    private class TestActivityWithInvalidCharactersInName : IActivity
    {
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    [ActivityName("CustomActivityWithInput Invalid Characters!@#$%^&*()")]
    private class TestActivityWithInputInvalidCharactersInName : IActivity<string>
    {
        Task IActivity<string>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    [ActivityName("CustomActivityWithOutput Invalid Characters!@#$%^&*()")]
    private class TestActivityWithOutputInvalidCharactersInName : IActivityWithReturn<string>
    {
        Task<string> IActivityWithReturn<string>.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    [ActivityName("CustomActivityWithInputAndOutput Invalid Characters!@#$%^&*()")]
    private class TestActivityWithInputAndOutputInvalidCharactersInName : IActivityWithReturn<string, string>
    {
        Task<string> IActivityWithReturn<string, string>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    [TestMethod]
    public void GetActivityName_ShouldReturnCleanedName_WhenAttributeValueContainsInvalidCharacters()
    {
        TestGetActivityName<TestActivityWithInvalidCharactersInName>("CustomActivityWith_Invalid_Characters", "CustomActivityWith Invalid Characters!@#$%^&*()");
        TestGetActivityName<TestActivityWithInputInvalidCharactersInName>("CustomActivityWithInput_Invalid_Characters", "CustomActivityWithInput Invalid Characters!@#$%^&*()");
        TestGetActivityName<TestActivityWithOutputInvalidCharactersInName>("CustomActivityWithOutput_Invalid_Characters", "CustomActivityWithOutput Invalid Characters!@#$%^&*()");
        TestGetActivityName<TestActivityWithInputAndOutputInvalidCharactersInName>("CustomActivityWithInputAndOutput_Invalid_Characters", "CustomActivityWithInputAndOutput Invalid Characters!@#$%^&*()");
    }
}