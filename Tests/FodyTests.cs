using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using Tests;

using Xunit;

public class FodyTests
{
    [Fact]
    public void Test1()
    {
        var assembly = WeaverHelper.Create("Test1/AssemblyToProcess").Assembly;

        var form = assembly.GetInstance("AssemblyToProcess.TestForm");

        Assert.Equal("Form:MyResourceManager", form.Text);
        Assert.Equal("Constant", form.Value);
    }

    [Fact]
    public void Test1a()
    {
        var assembly = WeaverHelper.Create("Test1/AssemblyToProcess").Assembly;

        var target = assembly.GetInstance("AssemblyToProcess.UsageOfInterface");
        var expected = new[] { "A", "B", "C" };

        Assert.True(expected.SequenceEqual((IEnumerable<string>)target.Explicit));
        Assert.True(expected.SequenceEqual((IEnumerable<string>)target.Derived));
    }

    [Fact]
    public void Test2()
    {
        var assembly = WeaverHelper.Create("Test2/AssemblyToProcess").Assembly;

        var form = assembly.GetInstance("AssemblyToProcess.TestForm");

        Assert.Equal("Form:MyResourceManager", form.Text);
        Assert.Equal("$this.Text=>MyResourceManager", form.Value);
    }

    [Fact]
    public void Test3()
    {
        var assembly = WeaverHelper.Create("Test3/AssemblyToProcess").Assembly;

        var form = assembly.GetInstance("AssemblyToProcess.TestForm");

        Assert.Equal("Form:MyResourceManager", form.Text);
        Assert.Equal("MyForm", form.Value);
    }

    const string expectedError4 = @"System.ComponentModel.ComponentResourceManager => AssemblyToProcess.MyResourceManager substitution error. System.ComponentModel.ComponentResourceManager derives from System.Resources.ResourceManager, but there is no direct or substituted counterpart for System.Resources.ResourceManager in the targets base classes.
Either derive AssemblyToProcess.MyResourceManager from System.Resources.ResourceManager, or substitute System.Resources.ResourceManager with AssemblyToProcess.MyResourceManager or one of it's base classes.";

    const string expectedError5 = @"AssemblyToProcess.WithDerivedInterfaces => AssemblyToProcess.SubstituteWithLessDerivedInterfaces substitution error. Target must implement the same interfaces as source.";

    const string expectedError6 = @"Duplicate substitution mapping for type AssemblyToProcess.WithDerivedInterfaces.";

    const string expectedError7 = @"AssemblyToProcess.SubstituteWithExplicitInterfaces is both source and target of a substitution.";

    [Theory]
    [InlineData("Test4", expectedError4)]
    [InlineData("Test5", expectedError5)]
    [InlineData("Test6", expectedError6)]
    [InlineData("Test7", expectedError7)]
    public void Test_Errors([NotNull] string test, [NotNull] string expectedError)
    {
        var weaverHelper = WeaverHelper.Create($"{test}/AssemblyToProcess");

        Assert.Single(weaverHelper.Errors);
        Assert.Equal(expectedError, weaverHelper.Errors.First());
    }
}
