using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace WeaverSmokeTestCS
{
    public class MyComponentResourceManager : ComponentResourceManager
    {
        public MyComponentResourceManager(Type t)
        {
        }

        public new void ApplyResources(object value, string objectName)
        {
            base.ApplyResources(value, objectName);
        }

        public override void ApplyResources(object value, string objectName, CultureInfo culture)
        {
            base.ApplyResources(value, objectName, culture);
        }
    }

    public class MyResourceManager : ResourceManager
    {
        public MyResourceManager(string s, Assembly assembly)
        {
            
        }

        public override string GetString(string name, CultureInfo culture)
        {
            return base.GetString(name, culture);
        }
    }
}