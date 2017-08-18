using System.Linq;

using JetBrains.Annotations;

using NUnit.Framework;

using Tests;

[TestFixture]
public class WeaverTests
{
    [TestCase("Test1")]
    [TestCase("Test2")]
    [TestCase("Test3")]
    [TestCase("Test4")]
    [TestCase("Test5")]
    [TestCase("Test6")]
    [TestCase("Test7")]
    public void PeVerify([NotNull] string test)
    {
        var weaverHelper = WeaverHelper.Create($"{test}/AssemblyToProcess");

        if (weaverHelper.Errors.Any())
            return; // weaver has reported errors, output *is* damaged, no need to verify...

        Verifier.Verify(weaverHelper.OriginalAssemblyPath, weaverHelper.NewAssemblyPath);
    }
}