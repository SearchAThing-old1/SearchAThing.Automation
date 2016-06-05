# SearchAThing.Automation

Automation helpers ( get completions using Roslyn )

## Features

- extract a list of information about suggestion coming from Roslyn CompletionService.GetService(...)

### Missing features

- signature documentation ( seems roslyn team is working to expose SignatureHelp API [#3540](https://github.com/dotnet/roslyn/issues/3540) )

## Examples

### Example1

Follow example shows how to complete hint for the System.windows.MessageBox method:

```csharp
static void test1()
{
    var str =
@"
using System;
using System.Windows;
public class MyClass
{
public void CompleteMe()
{
/**/ MessageBox.
}
}";

    var off = str.IndexOf("/**/ MessageBox.") + "/**/ MessageBox.".Length;

    var a = typeof(System.Windows.MessageBox);

    var assemblies = new[]
    {
        a.Assembly
    };

    var task = str.AutoComplete(off, assemblies);

    foreach (var x in task.Result)
    {
        Console.WriteLine(x);
    }
}
```

results in follow console output:
```
[m]     Boolean Equals(Object obj)
[m]     Boolean Equals(Object objA, Object objB)
[m]     Boolean ReferenceEquals(Object objA, Object objB)
[m]     MessageBoxResult Show(String messageBoxText, String caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions
 options)
[m]     MessageBoxResult Show(String messageBoxText, String caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
[m]     MessageBoxResult Show(String messageBoxText, String caption, MessageBoxButton button, MessageBoxImage icon)
[m]     MessageBoxResult Show(String messageBoxText, String caption, MessageBoxButton button)
[m]     MessageBoxResult Show(String messageBoxText, String caption)
[m]     MessageBoxResult Show(String messageBoxText)
[m]     MessageBoxResult Show(Window owner, String messageBoxText, String caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, Mes
sageBoxOptions options)
[m]     MessageBoxResult Show(Window owner, String messageBoxText, String caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
[m]     MessageBoxResult Show(Window owner, String messageBoxText, String caption, MessageBoxButton button, MessageBoxImage icon)
[m]     MessageBoxResult Show(Window owner, String messageBoxText, String caption, MessageBoxButton button)
[m]     MessageBoxResult Show(Window owner, String messageBoxText, String caption)
[m]     MessageBoxResult Show(Window owner, String messageBoxText)
```

### Example2

Follow example shows how to complete hint for the executing assembly.

```csharp
namespace SearchAThing.Automation.Example
{
    public static class MyUtils
    {

        public static int Sum(int x, int y)
        {
            return x + y;
        }

        public static int Sum(int x, int y, int z)
        {
            return x + y + z;
        }

    }
}

static void test2()
{
    var str =
@"
using SearchAThing.Automation.Example;
public class MyClass
{       
public void CompleteMe()
{
/**/ MyUtils.
}
}";

    var off = str.IndexOf("/**/ MyUtils.") + "/**/ MyUtils.".Length;

    var a = typeof(System.Windows.MessageBox);

    var assemblies = new[]
    {
        Assembly.GetExecutingAssembly()
    };
            
    var task = str.AutoComplete(off, assemblies);

    foreach (var x in task.Result)
    {
        Console.WriteLine(x);
    }
}
```

results in follow console output:
```
[m]     Boolean Equals(Object obj)
[m]     Boolean Equals(Object objA, Object objB)
[m]     Boolean ReferenceEquals(Object objA, Object objB)
[m]     Int32 Sum(Int32 x, Int32 y)
[m]     Int32 Sum(Int32 x, Int32 y, Int32 z)
```
