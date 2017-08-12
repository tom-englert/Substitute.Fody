using System.Reflection;

using JetBrains.Annotations;

using NUnit.Framework;

using Tests;

public class FodyTests
{
    [NotNull]
    private readonly Assembly assembly = WeaverHelper.Create().Assembly;

    [Test]
    public void Test()
    {
        var form = assembly.GetInstance("AssemblyToProcess.TestForm");

        Assert.AreEqual("Form:MyResourceManager", form.Text);
        Assert.AreEqual("$this.Text=>MyResourceManager", form.Value);
    }
}
