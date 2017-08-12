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
    public class MyResourceManager
    {
        public MyResourceManager(Type component)
        {
        }

        public void ApplyResources(object a, string b)
        {
            if (a is Form form)
            {
                form.Text = @"Form:MyResourceManager";
            }
        }

        public string GetString(string key)
        {
            return $@"{key}=>MyResourceManager";
        }
    }
}