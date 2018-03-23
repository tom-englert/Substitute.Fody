Imports System
Imports System.ComponentModel
Imports System.Reflection
Imports System.Resources
Imports System.Runtime.InteropServices
Imports Substitute
Imports WeaverSmokeTestCS

' General Information about an assembly is controlled through the following
' set of attributes. Change these attribute values to modify the information
' associated with an assembly.

' Review the values of the assembly attributes

<Assembly: AssemblyTitle("WeaverSmokeTestVB")>
<Assembly: AssemblyDescription("")>
<Assembly: AssemblyCompany("")>
<Assembly: AssemblyProduct("WeaverSmokeTestVB")>
<Assembly: AssemblyCopyright("Copyright ©  2018")>
<Assembly: AssemblyTrademark("")>

<Assembly: ComVisible(False)>

'The following GUID is for the ID of the typelib if this project is exposed to COM
<Assembly: Guid("dec3ed1a-b244-4625-9587-b52a12e167bd")>

' Version information for an assembly consists of the following four values:
'
'      Major Version
'      Minor Version
'      Build Number
'      Revision
'
' You can specify all the values or you can default the Build and Revision Numbers
' by using the '*' as shown below:
' <Assembly: AssemblyVersion("1.0.*")>

<Assembly: AssemblyVersion("1.0.0.0")>
<Assembly: AssemblyFileVersion("1.0.0.0")>

<assembly: Substitute(GetType(ComponentResourceManager), GetType(MyComponentResourceManager))>
<assembly: Substitute(GetType(ResourceManager), GetType(MyResourceManager))>