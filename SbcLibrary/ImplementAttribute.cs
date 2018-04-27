using System;

namespace SbcLibrary
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
    public class ImplementAttribute : Attribute
    {
        public ImplementAttribute(string name)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class NoclassAttribute : Attribute
    {
    }
}