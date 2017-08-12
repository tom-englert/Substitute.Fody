using System.ComponentModel;
using System.Windows.Forms;

using JetBrains.Annotations;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace AssemblyToProcess
{
    public partial class TestForm : Form
    {
        public string Value { get; }

        [NotNull]
        public ComponentResourceManager Resources { get; } = new ComponentResourceManager(typeof(TestForm));

        public TestForm()
        {
            InitializeComponent();

            Value = new MyClass(this).Value;
        }

        private class MyClass
        {
            public string Value { get; }

            public MyClass([NotNull] TestForm owner)
            {
                Value = owner.Resources.GetString("$this.Text");
            }
        }
    }
}
