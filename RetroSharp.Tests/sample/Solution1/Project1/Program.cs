using System;

namespace Solution1
{
    class Program
    {
        static void Main(string[] args) 
        { 
            var a = new A<string>();

            a.GenericProperty = "Hello, World!";

            Console.WriteLine(a.GenericProperty);
        }
    }
}