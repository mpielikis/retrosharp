using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace RetroSharp.Tests
{
    public class RetroMethodsTests
    {
        [Test]
        public void Method_Cast()
        {
            string source =
              @"using System; 

                class A<T>
                {
                    public T Foo(T arg) { return arg; }
                }

                class Program
                { 
                    static void Main() 
                    { 
                        var a = new A<string>();

                        string s = a.Foo(""Hello, World!"");
                    } 
                }";

            var result =
              @"using System; 

                class A
                {
                    public object Foo(object arg) { return arg; }
                }

                class Program
                { 
                    static void Main() 
                    { 
                        var a = new A();

                        string s = (string)a.Foo(""Hello, World!"");
                    } 
                }";

            var compilation = Compilation(source);

            var change = Generator.MakeChanges(compilation).First();

            Assert.AreEqual(result, change.Item2.ToString());
        }

        [Test]
        public void MethodResucrsive_Cast()
        {
            string source =
              @"using System; 

                class A<T>
                {
                    public T Foo(T arg) { return arg; }
                }

                class Program
                { 
                    static void Main() 
                    { 
                        var a = new A<A<string>>();

                        string s = a.Foo(new A<string>()).Foo(""Hello"");
                    } 
                }";

            var result =
              @"using System; 

                class A
                {
                    public object Foo(object arg) { return arg; }
                }

                class Program
                { 
                    static void Main() 
                    { 
                        var a = new A();

                        string s = (string)((A)a.Foo(new A())).Foo(""Hello"");
                    } 
                }";

            var compilation = Compilation(source);

            var change = Generator.MakeChanges(compilation).First();

            Assert.AreEqual(result, change.Item2.ToString());
        }

        [Test]
        public void MethodResucrsiveWithTVar_Cast()
        {
            string source =
              @"using System; 

                class A<T, U>
                {
                    public T Foo(T arg)
                    {
                        T o = arg;
                        return o;
                    }
                    public U Bar(U arg)
                    {
                        U o = arg;
                        return o;
                    }
                }

                class Program
                {
                    static void Main() 
                    {
                        var a = new A<A<string, string>, int>();

                        string s = a.Foo(new A<string, string>()).Foo(""Hello"");
                        string s2 = a.Foo(new A<string, string>()).Bar(""World"");
                        int i = a.Bar(2);
                    }
                }";

            var result =
              @"using System; 

                class A
                {
                    public object Foo(object arg)
                    {
                        object o = arg;
                        return o;
                    }
                    public object Bar(object arg)
                    {
                        object o = arg;
                        return o;
                    }
                }

                class Program
                {
                    static void Main() 
                    {
                        var a = new A();

                        string s = (string)((A)a.Foo(new A())).Foo(""Hello"");
                        string s2 = (string)((A)a.Foo(new A())).Bar(""World"");
                        int i = (int)a.Bar(2);
                    }
                }";

            var compilation = Compilation(source);

            var change = Generator.MakeChanges(compilation).First();

            Assert.AreEqual(result, change.Item2.ToString());
        }

        private static CSharpCompilation Compilation(string source)
        {
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            var tree = CSharpSyntaxTree.ParseText(source);

            var compilation = CSharpCompilation.Create("HelloWorld")
                .AddReferences(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")))
                .AddSyntaxTrees(tree);

            return compilation;
        }
    }
}
