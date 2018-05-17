using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SbcLibrary
{
    public abstract class ArgsAttribute : Attribute
    {
        public bool ExecuteOnParse { get; set; }
        public bool ExecuteOnCompileConst { get; set; }
        public bool HasChildren { get; set; }

        public bool HasType { get; set; }
        public bool HasClassName { get; set; }
        public bool HasName { get; set; }
        public bool HasArgTypes { get; set; }
        public bool HasArgNames { get; set; }
        public bool HasExtensions { get; set; }
        public MethodInfo Method { get; set; }
        public Regex Regex { get; }

        public ArgsAttribute SetMethod(MethodInfo methodInfo)
        {
            Method = methodInfo;
            return this;
        }

        protected ArgsAttribute(params string[] regex)
        {
            Regex = regex.Any() ? new Regex(string.Join(null, regex)) : new Regex("(?<Value>.*)$");
        }

    }
}
