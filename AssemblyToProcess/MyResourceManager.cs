using System;
using System.Windows.Forms;

using AssemblyToProcess;

using Substitute;

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedParameter.Global

[assembly: Substitute(typeof(System.ComponentModel.ComponentResourceManager), typeof(MyResourceManager))]
[assembly: Substitute(typeof(System.Resources.ResourceManager), typeof(MyResourceManager))]

namespace AssemblyToProcess
{
    public class MyResourceManager : System.Resources.ResourceManager
    {
        private System.ComponentModel.ComponentResourceManager _resources;

        public MyResourceManager(Type component)
            : base(component)
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