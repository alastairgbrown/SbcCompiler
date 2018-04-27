using System.Collections.Generic;

namespace SbcCore
{
    public class Tests
    {
        public List<TestResult> Results { get; } = new List<TestResult>();

        public void RunAllTests()
        {
            Results.Add(new TestResult("int to string 100", 100.ToString(), "100"));
            Results[0] = Results[0];
            new List<int>();
            new List<KeyValuePair<int,int>>();
        }
    }

    public class TestResult
    {
        public TestResult(string name, string expected, string actual)
        {
            Name = name;
            Expected = expected;
            Actual = actual;
        }

        public string Name { get;  }
        public string Expected { get;  }
        public string Actual { get; }
        public bool IsPass => string.Compare(Expected, Actual) == 0;
    }
}
