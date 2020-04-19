using System;
using System.Reflection;

namespace Tests
{
    public static class ExtensionMethods
    {
        public static dynamic GetInstance(this Assembly assembly, string className, params object[] args)
        {
            var type = assembly.GetType(className, true);

            // ReSharper disable AssignNullToNotNullAttribute
            return Activator.CreateInstance(type, args);
        }
    }
}
