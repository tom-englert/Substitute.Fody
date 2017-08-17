﻿// ReSharper disable All

using System;
using System.Windows.Forms;

using AssemblyToProcess;

using JetBrains.Annotations;

using Substitute;


[assembly: Substitute(typeof(System.ComponentModel.ComponentResourceManager), typeof(MyResourceManager))]
#if SUBSTITUTE_BASE
[assembly: Substitute(typeof(System.Resources.ResourceManager), typeof(MyResourceManager))]
#endif

namespace AssemblyToProcess
{
    public partial class TestForm : Form
    {
        public string Value { get; }

#if ACCESS_BASE
        [NotNull]
        public System.ComponentModel.ComponentResourceManager Resources { get; } = new System.ComponentModel.ComponentResourceManager(typeof(TestForm));
#endif

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
#if ACCESS_BASE
                Value = owner.Resources.GetString("$this.Text");
#else
                Value = "Constant";
#endif
            }
        }
    }

    public class MyResourceManager
#if DERIVE_BASE
     : System.Resources.ResourceManager
#endif
    {
        private System.ComponentModel.ComponentResourceManager _resources;

        public MyResourceManager(Type component)
#if DERIVE_BASE
            : base(component)
#endif
        {
            _resources = new System.ComponentModel.ComponentResourceManager(component);
        }

        public void ApplyResources(object value, string objectName)
        {
            _resources.ApplyResources(value, objectName);

            if (value is Form form)
            {
                form.Text = @"Form:MyResourceManager";
            }
        }

        public new string GetString(string key)
        {
            return $@"{key}=>MyResourceManager";
        }
    }
}
