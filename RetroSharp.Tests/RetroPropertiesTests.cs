using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using Microsoft.CodeAnalysis.MSBuild;
using System.Threading.Tasks;

namespace RetroSharp.Tests
{
    public class RetroPropertiesTests
    {
        // ADD test with not generic propery

        [Test]
        public void Property_Cast()
        {
            string source =@"
                 using System; 

                 namespace HelloWorld 
                 { 
                     class A<T>
                     {
                         public T A { get; set; }
                         public T B { get; set; }
                     }

                     class Program
                     { 
                         static void Main(string[] args) 
                         { 
                             var a = new A<string>();

                             a.A = ""Hello, World!"";
                             string s = a.A;

                             Console.WriteLine(  a.A);
                             Console.WriteLine(a.B);
                         } 
                     } 
                 }";

            var result =
               @"using System; 

                 namespace HelloWorld 
                 { 
                     class A
                     {
                         public object A { get; set; }
                         public object B { get; set; }
                     }

                     class Program
                     { 
                         static void Main(string[] args) 
                         { 
                             var a = new A();

                             a.A = ""Hello, World!"";
                             string s = (string)a.A;

                             Console.WriteLine(  (string)a.A);
                             Console.WriteLine((string)a.B);
                         } 
                     } 
                 }";

            var compilation = Compilation(source);

            var change = Generator.MakeChanges(compilation).Single();

            Assert.AreEqual(result, change.Item2.ToString());
        }

        [Test]
        public void Property_ResursiveCast()
        {
            string source = @"
                 using System; 

                 namespace HelloWorld 
                 { 
                     class A<T>
                     {
                         public T A { get; set; }
                         public T B { get; set; }
                     }

                     class B<T>
                     {
                         public T A { get; set; }
                         public T B { get; set; }
                     }

                     class Program
                     { 
                         static void Main(string[] args) 
                         { 
                             var a = new A<B<string>>();

                             a.A = new B<string>();
                             a.A.A = ""Hello, World!"";

                             Console.WriteLine(a.A.A);
                             Console.WriteLine(a.B.B);
                         } 
                     } 
                 }";

            var result =
               @"using System; 

                 namespace HelloWorld 
                 { 
                     class A
                     {
                         public object A { get; set; }
                         public object B { get; set; }
                     }

                     class B
                     {
                         public object A { get; set; }
                         public object B { get; set; }
                     }

                     class Program
                     { 
                         static void Main(string[] args) 
                         { 
                             var a = new A();

                             a.A = new B();
                             ((HelloWorld.B)a.A).A = ""Hello, World!"";

                             Console.WriteLine((string)((HelloWorld.B)a.A).A);
                             Console.WriteLine((string)((HelloWorld.B)a.B).B);
                         } 
                     } 
                 }";

            var compilation = Compilation(source);

            var change = Generator.MakeChanges(compilation).Single();

            Assert.AreEqual(result, change.Item2.ToString());
        }

        [Test]
        public void Property_SimpleRecursiveCast()
        {
            string source = @"using System;

                class A<T>
                {
                    public T A { get; set; }

                    static void Main() 
                    {
                        var a = new A<A<string>>();

                        a.A = new A<string>();
                        a.A.A = ""Hello, World!"";

                        Console.WriteLine(a.A.A);
                    } 
                }";

            var result = @"using System;

                class A
                {
                    public object A { get; set; }

                    static void Main() 
                    {
                        var a = new A();

                        a.A = new A();
                        ((A)a.A).A = ""Hello, World!"";

                        Console.WriteLine((string)((A)a.A).A);
                    } 
                }";

            var compilation = Compilation(source);

            var change = Generator.MakeChanges(compilation).Single();

            var changestr = change.Item2.ToString();

            Assert.AreEqual(result, changestr);
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
