using Substitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyToProcess
{
    #region External Stuff
    public abstract class ExternalClass
    {
        public abstract ExternalDataProvider GetDataProvider();

        public string GetText() => GetDataProvider().GetText();
    }

    public class ExternalDataProvider
    {
        public virtual string GetText() => "Test";
    }
    #endregion

    [Substitute(typeof(ExternalDataProvider), typeof(QuotingDataProvider), DoNotChangeSignature = true)]
    public class SubstitutionSubjectClass : ExternalClass
    {
        public override ExternalDataProvider GetDataProvider() => new ExternalDataProvider();

        [Substitute(typeof(ExternalDataProvider), typeof(QuotingDataProvider), Disable = true)]
        public ExternalDataProvider GetOriginalDataProvider() => new ExternalDataProvider();

        public string GetOriginalText() => GetOriginalDataProvider().GetText();
    }

    public class QuotingDataProvider : ExternalDataProvider
    {
        public override string GetText() => "\"" + base.GetText() + "\"";
    }
}
