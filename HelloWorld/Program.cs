using System;

namespace HelloWorld
{
    class Program
    {
        static int TestStatic { get; } = 789;

        static void Main()
        {
            //Global.Memory[0] = 0;
            //Console.WriteLine("Hello World");
            Console.WriteLine($"Hello World {123} {"456"} {TestStatic}");
        }
    }
}
