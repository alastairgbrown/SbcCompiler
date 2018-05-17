using System.Collections.Generic;
using System.Diagnostics;

namespace SbcCore
{
    public static class Tests
    {
        public static void RunAllTests()
        {
            BasicTests();
            StructTests();
            ListTests();
        }

        public static void BasicTests()
        {
            var c4 = '4';
            Debug.Assert(100.ToString() == "100", "int to string 100");
            Debug.Assert(123.ToString() == "123", "int to string 123");
            Debug.Assert((-123).ToString() == "-123", "int to string -123");
            Debug.Assert(true.ToString() == "True", "true to string");
            Debug.Assert(false.ToString() == "False", "false to string");
            Debug.Assert($"{1}" == "1", "string format 1");
            Debug.Assert($"{1} {"2"}" == "1 2", "string format 2");
            Debug.Assert($"{1} {"2"} 3" == "1 2 3", "string format 3");
            Debug.Assert($"{1} {"2"} {3} {4}" == "1 2 3 4", "string format 4");
            Debug.Assert($"{1} {"2"} {3} {4} {5}" == "1 2 3 4 5", "string format 5");
            Debug.Assert(1 + "2" == "12", "string concat 1");
            Debug.Assert(1 + "2" + 3 == "123", "string concat 2");
            Debug.Assert(1 + "2" + 3 + c4 == "1234", "string concat 3");
        }

        public static void StructTests()
        {
            var ts1 = new TestStruct();
            var ts2 = TestStruct.Instance();
            var ts3 = new TestStruct[] { new TestStruct(), new TestStruct(11), new TestStruct() };

            // A test to check that the stack isn't corrupted
            TestStruct.Instance();

            ts1.A = 4;
            ts1.B = 5;
            ts1.C = 6;
            ts2.A = 7;
            ts2.B = 8;
            ts2.C = 9;
            ts3[2].A = ts3[2].B = ts3[2].C = 12;

            Debug.Assert(TestStruct.Instance().A == 1, "struct assignment 1");
            Debug.Assert(TestStruct.Instance().B == 2, "struct assignment 2");
            Debug.Assert(TestStruct.Instance().C == 3, "struct assignmenbt 3");
            Debug.Assert(ts1.A == 4, "struct assignment 4");
            Debug.Assert(ts1.B == 5, "struct assignment 5");
            Debug.Assert(ts1.C == 6, "struct assignment 6");
            Debug.Assert(ts2.A == 7, "struct assignment 7");
            Debug.Assert(ts2.B == 8, "struct assignment 8");
            Debug.Assert(ts2.C == 9, "struct assignment 9");
            Debug.Assert(ts3.Length == 3, "struct array 1");
            Debug.Assert(ts3[0].B == 0, "struct array 8");
            Debug.Assert(ts2.C == 9, "struct array 9");
        }

        public static void ListTests()
        {
            var ints = new List<int>();
            var strings = new List<string>();

            ints.Add(100);
            ints.Add(200);
            ints.Add(300);
            ints.Insert(1, 150);
            ints.Insert(3, 250);
            ints.Insert(3, 240);
            strings.Add(null);
            strings.Add("200");
            strings.Add("250");
            strings.Add("300");
            strings[0] = "100";
            strings.RemoveAt(2);

            Debug.Assert(ints.Count == 6, "int count");
            Debug.Assert(ints[0] == 100, "int item 0");
            Debug.Assert(ints[1] == 150, "int item 1");
            Debug.Assert(ints[2] == 200, "int item 2");
            Debug.Assert(ints[3] == 240, "int item 3");
            Debug.Assert(ints[4] == 250, "int item 4");
            Debug.Assert(ints[5] == 300, "int item 5");
            Debug.Assert(strings.Count == 3, "string count");
            Debug.Assert(strings[0] == "100", "string item 0");
            Debug.Assert(strings[1] == "200", "string item 1");
            Debug.Assert(strings[2] == "300", "string item 2");

            ints.RemoveRange(1, 4);
            Debug.Assert(ints.Count == 2, "int count - post delete");
            Debug.Assert(ints[0] == 100, "int item 0 - post delete");
            Debug.Assert(ints[1] == 300, "int item 1 - post delete");

            //foreach (string s in strings)
            //{
            //}
        }

        public struct TestStruct
        {
            public int A { get; set; }
            public int B { get; set; }
            public int C { get; set; }

            public TestStruct(int x) => A = B = C = x;

            public static TestStruct Instance() => new TestStruct { A = 1, B = 2, C = 3 };
        }
    }
}