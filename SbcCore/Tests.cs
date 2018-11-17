using SbcLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SbcCore
{
    public static class Tests
    {
        public static void RunAllTests()
        {
            BasicTests();
            StringTests();
            StructTests();
            TypeTests();
            ListTests();
            DictionaryTests();
            IteratorTests();
            FloatTests();
            ThrowTests();
            DelegateTests();
            EnumTests();
        }

        public static void BasicTests()
        {
            Debug.Assert(100.ToString() == "100", "int to string 100");
            Debug.Assert(123.ToString() == "123", "int to string 123");
            Debug.Assert(123.ToString() != "124", "int to string 123");
            Debug.Assert((-123).ToString() == "-123", "int to string -123");
            Debug.Assert(true.ToString() == "True", "true to string");
            Debug.Assert(false.ToString() == "False", "false to string");
            Console.WriteLine(nameof(BasicTests) + " passed");
        }

        public static void StringTests()
        {
            var c4 = '4';
            Debug.Assert($"{1}" == "1", "string format 1");
            Debug.Assert($"{1} {"2"}" == "1 2", "string format 2");
            Debug.Assert($"{1} {"2"} 3" == "1 2 3", "string format 3");
            Debug.Assert($"{1} {"2"} {3} {4}" == "1 2 3 4", "string format 4");
            Debug.Assert($"{1} {"2"} {3} {4} {5}" == "1 2 3 4 5", "string format 5");
            Debug.Assert(1 + "2" == "12", "string concat 1");
            Debug.Assert(1 + "2" + 3 == "123", "string concat 2");
            Debug.Assert(1 + "2" + 3 + c4 == "1234", "string concat 3");
            Debug.Assert("1e7".IndexOf("1") == 0, "string index of 1");
            Debug.Assert("1e7".IndexOf("e") == 1, "string index of 2");
            Debug.Assert("1e7".IndexOf("7") == 2, "string index of 3");
            Debug.Assert("1e7".IndexOf("f") == -1, "string contains 2");
            Debug.Assert("1e".Contains("e"), "string contains 1");
            Debug.Assert("1e7".Contains("e"), "string contains 2");
            Debug.Assert("1e7".StartsWith("1"), "string starts with 1");
            Debug.Assert(!"1e7".StartsWith("7"), "string starts with 2");
            Debug.Assert("1e7".EndsWith("7"), "string ends with 1");
            Debug.Assert(!"1e7".EndsWith("1"), "string ends with 2");
            Console.WriteLine(nameof(StringTests) + " passed");
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
            Debug.Assert(ts3[0].A == 0, "struct array 2");
            Debug.Assert(ts3[1].B == 11, "struct array 3");
            Debug.Assert(ts3[2].C == 12, "struct array 4");
            Console.WriteLine(nameof(StructTests) + " passed");
        }

        public static void TypeTests()
        {
            Debug.Assert(0.GetType() == typeof(int), "int type test");
            Debug.Assert(true.GetType() == typeof(bool), "bool type test");
            Debug.Assert("".GetType() == typeof(string), "string type test");
            Debug.Assert('a'.GetType() == typeof(char), "char type test");
            Debug.Assert((object)"" is string, "is string test");
            Debug.Assert((object)'a' is char, "is char test");
            Debug.Assert(new List<int>() is IEnumerable, "is IEnumerable test 1");
            Debug.Assert(new List<int>() is IEnumerable<int>, "is IEnumerable test 2");
            Debug.Assert(!(new List<int>() is IEnumerator), "is IEnumerator test");
            Debug.Assert(new List<int>() is object, "is object test");
            Console.WriteLine(nameof(TypeTests) + " passed");
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
            Console.WriteLine(nameof(ListTests) + " passed");
        }

        public static void DictionaryTests()
        {
            Dictionary<int, int> dict = new Dictionary<int, int> { { 1, 10 }, { 2, 20 }, { 3, 30 }, { 4, 40 }, { 9, 90 } };

            Debug.Assert(dict[1] == 10, "dictionary test 1");
            Debug.Assert(dict[9] == 90, "dictionary test 2");
            Debug.Assert(dict.Count == 5, "dictionary test 3");
            Debug.Assert(dict.ContainsKey(2), "dictionary test 4");
            Debug.Assert(!dict.ContainsKey(6), "dictionary test 5");
            Console.WriteLine(nameof(DictionaryTests) + " passed");
        }

        public static IEnumerable<string> Yielder()
        {
            for (int i = 'X'; i <= 'Z'; i++)
                yield return ((char)i).ToString();
        }

        [Vars("list", "dict", "text")]
        public static void IteratorTests()
        {
            var list = new List<string> { "A", "B", "C" };
            var dict = new Dictionary<string, string> { { "D", "1" }, { "E", "2" }, { "F", "3" } };
            var text = new StringBuilder();

            foreach (string s in list)
            {
                text.Append(s);
            }

            foreach (var kvp in dict)
            {
                text.Append(kvp.Key).Append(kvp.Value);
            }

            foreach (var y in Yielder())
            {
                text.Append(y);
            }

            Debug.Assert(text.ToString() == "ABCD1E2F3XYZ", "iterator test");
            Console.WriteLine(nameof(IteratorTests) + " passed");
        }

        public static void FloatTests()
        {
            float f1 = 1, f3 = 3, f10 = 10, f20 = 20, f0p2 = 0.2f, f100 = 100;
            float f1_000_000p1 = 1_000_000.1f, f10_000_000 = 10_000_000, f0p000_000_1 = 0.000_000_1f, f1p2345 = 1.2345f;
            float epsilon = 0.0000001f;
            int i3 = 3, i11 = 11;

            Debug.Assert(f1 + f3 == 4f, "Float test 1");
            Debug.Assert(f1 + i3 == 4f, "Float test 2");
            Debug.Assert(Math.Abs(f10 * f0p2 - 2f) < epsilon, "Float test 3");
            Debug.Assert(Math.Abs(i11 * f0p2 - 2.2f) < epsilon, "Float test 4");
            Debug.Assert((int)(f0p2 * f20) == 4, "Float test 4");
            Debug.Assert((f100 >= 101) == false, "Float comp test 1");
            Debug.Assert((f100 > 101) == false, "Float comp test 2");
            Debug.Assert((f100 <= 101) == true, "Float comp test 3");
            Debug.Assert((f100 < 101) == true, "Float comp test 4");
            Debug.Assert((f100 == 101) == false, "Float comp test 5");
            Debug.Assert((f100 != 101) == true, "Float comp test 6");
            Debug.Assert((f100 >= 100) == true, "Float comp test 7");
            Debug.Assert((f100 > 100) == false, "Float comp test 8");
            Debug.Assert((f100 <= 100) == true, "Float comp test 9");
            Debug.Assert((f100 < 100) == false, "Float comp test 10");
            Debug.Assert((f100 == 100) == true, "Float comp test 11");
            Debug.Assert((f100 != 100) == false, "Float comp test 12");
            Debug.Assert(new StringBuilder().AddMantissa(1).ToString() == "1", "Float to string test 1");
            Debug.Assert(new StringBuilder().AddMantissa(f1p2345).ToString().StartsWith("1.234"), "Float to string test 1");
            Debug.Assert(f1.ToString() == "1", "Float to string test 1");
            Debug.Assert(f100.ToString() == "100", "Float to string test 2");
            Debug.Assert(f1_000_000p1.ToString() == "1000000", "Float to string test 3");
            Debug.Assert(f10_000_000.ToString().Contains("e"), "Float to string test 4");
            Debug.Assert(f0p000_000_1.ToString().Contains("e-"), "Float to string test 5");
            Debug.Assert(f1p2345.ToString().StartsWith("1.234"), "Float to string test 6");
            Debug.Assert((-f1p2345).ToString().StartsWith("-1.234"), "Float to string test 7");
            Console.WriteLine(nameof(FloatTests) + " passed");
        }

        [Vars("message")]
        public static void ThrowTests()
        {
            var message = new StringBuilder();

            try
            {
                try
                {
                    throw new Exception("Text Exception 1");
                }
                catch
                {
                    message.AppendLine("Catch");
                    throw;
                }
                finally
                {
                    message.AppendLine("Finally1");
                }
            }
            catch (NotImplementedException ex)
            {
                message.AppendLine(nameof(NotImplementedException)).AppendLine(ex.Message);
            }
            catch (Exception ex)
            {
                message.AppendLine(nameof(Exception)).AppendLine(ex.Message);
            }
            finally
            {
                message.AppendLine("Finally2");
            }

            try
            {
                ThrowTestFunction(message);
            }
            catch (Exception ex)
            {
                message.Append(ex.StackTrace);
            }

            Debug.Assert(message.ToString() ==
                "Catch\r\n" +
                "Finally1\r\n" +
                "Exception\r\n" +
                "Text Exception 1\r\n" +
                "Finally2\r\n" +
                "Finally3\r\n" +
                "System.Void SbcCore.Tests::ThrowTestFunction(System.Text.StringBuilder)\r\n" +
                "System.Void SbcCore.Tests::ThrowTests()\r\n" +
                "System.Void SbcCore.Tests::RunAllTests()\r\n" +
                "System.Void HelloWorld.Program::Main()\r\n",
                "Exception Test");
            Console.WriteLine(nameof(ThrowTests) + " passed");
        }

        public static void ThrowTestFunction(StringBuilder message)
        {
            try
            {
                throw new Exception("Text Exception 2");
            }
            finally
            {
                message.AppendLine("Finally3");
            }
        }

        public static void DelegateTests()
        {
            var message = new StringBuilder();
            string lambda2 = "Lambda2", lambda3 = "Lambda3", lambda4 = "Lambda4";
            Action<StringBuilder> function1 = DelegateTestFunction;
            Action<StringBuilder> function2 = x => x.AppendLine("Lambda1");
            Action<StringBuilder> function3 = x => x.AppendLine(lambda2);
            Action<StringBuilder, string> function4 = (x, y) => x.AppendLine(y);
            Func<string> function5 = () => lambda4;

            function1(message);
            function2(message);
            function3(message);
            function4(message, lambda3);
            message.AppendLine(function5());
            Debug.Assert(message.ToString() ==
                "DelegateTestFunction\r\n" +
                "Lambda1\r\n" +
                "Lambda2\r\n" +
                "Lambda3\r\n" +
                "Lambda4\r\n",
                "Delegate Test");
            Console.WriteLine(nameof(DelegateTests) + " passed");
        }

        public static void DelegateTestFunction(StringBuilder message)
        {
            message.AppendLine(nameof(DelegateTestFunction));
        }

        public static void EnumTests()
        {
            int i = 0;
            Debug.Assert((int)TestEnum1.EnumValue0 == i++);
            Debug.Assert((int)TestEnum1.EnumValue1 == i++);
            Debug.Assert((int)TestEnum1.EnumValue2 == i++);
            Debug.Assert((int)TestEnum1.EnumValue3 == i++);
            Debug.Assert($"{TestEnum1.EnumValue3}" == "EnumValue3");
            Debug.Assert($"{(TestEnum1)99}" == "99");
            Debug.Assert($"{TestEnum2.EnumValue1 | TestEnum2.EnumValue2}" == "EnumValue2, EnumValue1");
            Debug.Assert($"{TestEnum2.EnumValue0}" == "EnumValue0");
            Debug.Assert($"{(TestEnum2)99}" == "99");
            Console.WriteLine(nameof(EnumTests) + " passed");
        }

        public enum TestEnum1
        {
            EnumValue0,
            EnumValue1,
            EnumValue2,
            EnumValue3,
        }

        [Flags]
        public enum TestEnum2
        {
            EnumValue0 = 0,
            EnumValue1 = 1,
            EnumValue2 = 2,
            EnumValue4 = 4,
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