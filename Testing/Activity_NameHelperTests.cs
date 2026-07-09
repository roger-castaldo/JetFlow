using JetFlow.Attributes;
using JetFlow.Helpers;
using JetFlow.Interfaces;

namespace JetFlow.Testing;

[TestClass]
public class Activity_NameHelperTests
{
    private static string GetActivityName<TActivity>() 
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
        Assert.AreEqual("TestActivityWithoutAttribute", GetActivityName<TestActivityWithoutAttribute>());
        Assert.AreEqual("TestActivityWithInputWithoutAttribute", GetActivityName<TestActivityWithInputWithoutAttribute>());
        Assert.AreEqual("TestActivityWithOutputWithoutAttribute", GetActivityName<TestActivityWithOutputWithoutAttribute>());
        Assert.AreEqual("TestActivityWithInputAndOutputWithoutAttribute", GetActivityName<TestActivityWithInputAndOutputWithoutAttribute>());
    }

    private static void TestGetActivityName<TActivity>(string name)
        => Assert.AreEqual(name, GetActivityName<TActivity>());

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
        TestGetActivityName<TestActivityWithAttribute>("CustomActivityName");
        TestGetActivityName<TestActivityWithInputWithAttribute>("CustomActivityWithInputName");
        TestGetActivityName<TestActivityWithOutputWithAttribute>("CustomActivityWithOutputName");
        TestGetActivityName<TestActivityWithInputAndOutputWithAttribute>("CustomActivityWithInputAndOutputName");
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
        TestGetActivityName<TestActivityImplementingInterface>("CustomActivityInterfaceName");
        TestGetActivityName<TestActivityImplementingInputInterface>("CustomActivityWithInputInterfaceName");
        TestGetActivityName<TestActivityImplementingOutputInterface>("CustomActivityWithOutputInterfaceName");
        TestGetActivityName<TestActivityImplementingInputAndOutputInterface>("CustomActivityWithInputAndOutputInterfaceName");
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
        TestGetActivityName<TestActivityInheritingFromBase>("CustomActivityName");
        TestGetActivityName<TestActivityInheritingFromBaseWithInput>("CustomActivityWithInputName");
        TestGetActivityName<TestActivityInheritingFromBaseWithOutput>("CustomActivityWithOutputName");
        TestGetActivityName<TestActivityInheritingFromBaseWithInputAndOutput>("CustomActivityWithInputAndOutputName");
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
        TestGetActivityName<TestActivityInheritingFromBaseWithoutAttribute>("CustomActivityInterfaceName");
        TestGetActivityName<TestActivityInheritingFromBaseWithInputWithoutAttribute>("CustomActivityWithInputInterfaceName");
        TestGetActivityName<TestActivityInheritingFromBaseWithOutputWithoutAttribute>("CustomActivityWithOutputInterfaceName");
        TestGetActivityName<TestActivityInheritingFromBaseWithInputAndOutputWithoutAttribute>("CustomActivityWithInputAndOutputInterfaceName");
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
        TestGetActivityName<TestSubActivityInheritingFromBase>("CustomActivityName");
        TestGetActivityName<TestSubActivityInheritingFromBaseWithInput>("CustomActivityWithInputName");
        TestGetActivityName<TestSubActivityInheritingFromBaseWithOutput>("CustomActivityWithOutputName");
        TestGetActivityName<TestSubActivityInheritingFromBaseWithInputAndOutput>("CustomActivityWithInputAndOutputName");
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
        TestGetActivityName<TestActivityWithInvalidCharactersInName>("CustomActivityWith_Invalid_Characters");
        TestGetActivityName<TestActivityWithInputInvalidCharactersInName>("CustomActivityWithInput_Invalid_Characters");
        TestGetActivityName<TestActivityWithOutputInvalidCharactersInName>("CustomActivityWithOutput_Invalid_Characters");
        TestGetActivityName<TestActivityWithInputAndOutputInvalidCharactersInName>("CustomActivityWithInputAndOutput_Invalid_Characters");
    }
}