using SbcCore;
using System;
using System.Collections.Generic;

namespace HelloWorld
{
    class Program
    {
        static int TestStatic { get; } = 789;

        static void Main()
        {
            //Console.WriteLine("Hello World");
            //Console.WriteLine($"Hello World {123} {"456"} {TestStatic} {'0'}");

            Tests.RunAllTests();

            //GC.Collect();
        }
    }
}
