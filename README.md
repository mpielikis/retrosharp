# RetroSharp
convert modern C# code to the generics free code

```cs
static void Main(string[] args) 
{ 
    var a = new A<string>();

    a.GenericProperty = "Hello, World!";

    Console.WriteLine(a.GenericProperty);
}
```
becomes
```cs
static void Main(string[] args) 
{ 
    var a = new A();

    a.GenericProperty = "Hello, World!";

    Console.WriteLine((string)a.GenericProperty);
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
