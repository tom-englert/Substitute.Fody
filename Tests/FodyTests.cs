using System.Collections.Generic;
using System.Linq;
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
        Assert.AreEqual("Constant", form.Value);

        var target = assembly.GetInstance("AssemblyToProcess.UsageOfInterface");
        var expected = new[] { "A", "B", "C" };

        Assert.IsTrue(expected.SequenceEqual((IEnumerable<string>)target.Explicit));
        Assert.IsTrue(expected.SequenceEqual((IEnumerable<string>)target.Derived));
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
        var assembly = WeaverHelper.Create("Test3/AssemblyToProcess").Assembly;

        var form = assembly.GetInstance("AssemblyToProcess.TestForm");

        Assert.AreEqual("Form:MyResourceManager", form.Text);
        Assert.AreEqual("MyForm", form.Value);
    }

    const string expectedError4 = @"System.ComponentModel.ComponentResourceManager => AssemblyToProcess.MyResourceManager substitution error. System.ComponentModel.ComponentResourceManager derives from System.Resources.ResourceManager, but there is no direct or substituted counterpart for System.Resources.ResourceManager in the targets base classes.
Either derive AssemblyToProcess.MyResourceManager from System.Resources.ResourceManager, or substitute System.Resources.ResourceManager with AssemblyToProcess.MyResourceManager or one of it's base classes.";

    const string expectedError5 = @"AssemblyToProcess.WithDerivedInterfaces => AssemblyToProcess.SubstituteWithLessDerivedInterfaces substitution error. Target must implement the same interfaces as source.";

    const string expectedError6 = @"Duplicate substitution mapping for type AssemblyToProcess.WithDerivedInterfaces.";

    const string expectedError7 = @"AssemblyToProcess.SubstituteWithExplicitInterfaces is both source and target of a substitution.";

    [TestCase("Test4", expectedError4)]
    [TestCase("Test5", expectedError5)]
    [TestCase("Test6", expectedError6)]
    [TestCase("Test7", expectedError7)]
    public void Test_Errors(string test, string expectedError)
    {
        var weaverHelper = WeaverHelper.Create($"{test}/AssemblyToProcess");

        Assert.AreEqual(1, weaverHelper.Errors.Count);
        Assert.AreEqual(expectedError, weaverHelper.Errors[0]);
    }
}
