using JetBrains.Annotations;

using NUnit.Framework;

using Tests;

[TestFixture]
public class WeaverTests
{
    [NotNull]
    private readonly WeaverHelper _weaverHelper = WeaverHelper.Create();

    [Test]
    public void PeVerify()
    {
        Verifier.Verify(_weaverHelper.OriginalAssemblyPath, _weaverHelper.NewAssemblyPath);
    }
}