### This is an add-in for [Fody](https://github.com/Fody/Fody/) [![Build status](https://ci.appveyor.com/api/projects/status/mu7g9vwg31oj1pey?svg=true)](https://ci.appveyor.com/project/tom-englert/substitute-fody) [![NuGet Status](http://img.shields.io/nuget/v/Substitute.Fody.svg?style=flat-square)](https://www.nuget.org/packages/Substitute.Fody)

![Icon](package_icon.png)













Substitute types with other types to e.g. intercept generated code


This is an add-in for [Fody](https://github.com/Fody/Fody/); it is available via [NuGet](https://nuget.org/packages/Substitute.Fody/):

    PM> Install-Package Substitute.Fody

---

Generated code usually leaves you out of control, you have to take it as it is, 
especially if the code generator is even built in to the framework. 
One sample is e.g. resource access, where the  resource editor or the WinForms designer
generate some background code that you can't modify, because it will be overwritten every 
time you change something in the designer or editor.

Now if you have e.g. the requirement to enable customer specific resource string overrides 
provided via a plain text file at runtime, and you don't want to reinvent the wheel by re-writing
the complete resource management infrastructure, this is hard to achieve when you can't get 
hold of the generated code.

With this Fody add-in you can e.g. substitute the `System.ComponentModel.ComponentResourceManager` 
and the `System.Resources.ResourceManager` with your own derived implementations, that just check 
for user overrides in the text file before returning control to the original implementations.

---

### Sample

##### Your code:

```csharp
[assembly: Substitute(typeof(System.ComponentModel.ComponentResourceManager), typeof(MyResourceManager))]
[assembly: Substitute(typeof(System.Resources.ResourceManager), typeof(MyResourceManager))]

public class MyResourceManager
{
    private System.ComponentModel.ComponentResourceManager _resources;

    public MyResourceManager(Type component)
    {
        _resources = new System.ComponentModel.ComponentResourceManager(component);
    }

    public void ApplyResources(object value, string objectName)
    {
        _resources.ApplyResources(value, objectName);

        // apply your overrides here...
    }
}

```

##### The designer generated source code:

```csharp
#region Windows Form Designer generated code

private void InitializeComponent()
{
    System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TestForm));
    this.SuspendLayout();
    // 
    // TestForm
    // 
    resources.ApplyResources(this, "$this");
    this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
    this.Name = "TestForm";
    this.ResumeLayout(false);
}

#endregion
```

##### The designer generated code after weaving:

```csharp
#region Windows Form Designer generated code

private void InitializeComponent()
{
    MyResourceManager resources = new MyResourceManager(typeof(TestForm));
    this.SuspendLayout();
    // 
    // TestForm
    // 
    resources.ApplyResources(this, "$this");
    this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
    this.Name = "TestForm";
    this.ResumeLayout(false);
}

#endregion
```

---

### Requirements to your code

If you want to substitute a class A with a class X

- class X must implement all members of class A that are accessed by code in the assembly, with the same name, parameters and return type.
- class X does not need to implement members of class A that are not accessed by code in this assembly.
- you must not use any class derived from A unless you substitute it, too.
- if class A implements any interface, either directly or by inheritance, class X must implement at least all interfaces of A, either directly or by inheritance.

If class A is derived from class B, and any member of B is accessed by code in the assembly, either 

1. derive class X also from B
2. substitute class B also with class X and implement all needed methods in X
3. derive X from Y and substitute B with Y

```
    1.                            2.                        3.
    A : B =substitute=> X : B     A : B =substitute=> X     A : B =substitute=> X : Y
                                  B     =substitute=> X     B     =substitute=> Y
```

If you are not sure about which methods to implement, start with an empty class and fix all errors successively.











