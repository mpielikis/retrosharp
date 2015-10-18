# RetroSharp
convert modern C# code to the generics free code

```cs
class A<T>
{
    public T GenericProperty { get; set; }
    public T GenericMethod(T arg) { return arg; }
}

class Program
{
    static void Main(string[] args) 
    { 
        var a = new A<string>();

        a.GenericProperty = "Hello, World!";

        Console.WriteLine(a.GenericMethod(a.GenericProperty));
    }
}
```
becomes
```cs
class A
{
    public object GenericProperty { get; set; }
    public object GenericMethod(object arg) { return arg; }
}

class Program
{
    static void Main(string[] args) 
    { 
        var a = new A();

        a.GenericProperty = "Hello, World!";

        Console.WriteLine((string)a.GenericMethod((string)a.GenericProperty));
    }
}
```

#Build

####Windows
Requires a minimum of .NET Framework 4.5.2.
```
    git clone https://github.com/mpielikis/retrosharp.git
    cd retrosharp
    build.bat
```

#Samples

To run a sample you should copy the sample and modify it with RetroSharp

```
xcopy samples\Solution1 test\Solution1\ /E
bin\RetroSharp.exe -s test\Solution1\Solution1.sln
```
