using System;
using System.ComponentModel;
using System.Globalization;

namespace WeaverSmokeTestCS
{
    internal class MyComponentResourceManager : ComponentResourceManager
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
}