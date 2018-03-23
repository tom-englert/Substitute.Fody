using System.ComponentModel;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using Substitute;
using WeaverSmokeTestCS;

[assembly: AssemblyTitle("WeaverSmokeTestCS")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("WeaverSmokeTestCS")]
[assembly: AssemblyCopyright("Copyright © 2018")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

[assembly: Substitute(typeof(ComponentResourceManager), typeof(MyComponentResourceManager))]
[assembly: Substitute(typeof(ResourceManager), typeof(MyResourceManager))]