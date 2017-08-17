using System.Reflection;

using JetBrains.Annotations;

using NUnit.Framework;

using Tests;

public class FodyTests
{
    [Test]
    public void Test1()
    {
        var assembly = WeaverHelper.Create("Test1/AssemblyToProcess").Assembly;

        var form = assembly.GetInstance("AssemblyToProcess.TestForm");

        Assert.AreEqual("Form:MyResourceManager", form.Text);
        Assert.AreEqual("MyForm", form.Value);
    }

    [Test]
    public void Test2()
    {
        var assembly = WeaverHelper.Create("Test2/AssemblyToProcess").Assembly;

        var form = assembly.GetInstance("AssemblyToProcess.TestForm");

        Assert.AreEqual("Form:MyResourceManager", form.Text);
        Assert.AreEqual("$this.Text=>MyResourceManager", form.Value);
    }

    [Test]
    public void Test3()
    {
        var weaverHelper = WeaverHelper.Create("Test3/AssemblyToProcess");

        const string expectedError = @"System.ComponentModel.ComponentResourceManager => AssemblyToProcess.MyResourceManager substitution error. System.ComponentModel.ComponentResourceManager derives from System.Resources.ResourceManager, but there is no direct or substituted counterpart for System.Resources.ResourceManager in the targets base classes.
Either derive AssemblyToProcess.MyResourceManager from System.Resources.ResourceManager, or substitute System.Resources.ResourceManager with AssemblyToProcess.MyResourceManager or one of it's base classes.";

        Assert.AreEqual(1, weaverHelper.Errors.Count);
        Assert.AreEqual(expectedError, weaverHelper.Errors[0]);
    }

    [Test]
    public void Test4()
    {
        var assembly = WeaverHelper.Create("Test4/AssemblyToProcess").Assembly;

        var form = assembly.GetInstance("AssemblyToProcess.TestForm");

        Assert.AreEqual("Form:MyResourceManager", form.Text);
        Assert.AreEqual("Constant", form.Value);
    }
}
