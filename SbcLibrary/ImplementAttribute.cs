using System;

namespace SbcLibrary
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    public class ImplementClassAttribute : Attribute
    {
        public string Name { get; }

        public ImplementClassAttribute(string name) => Name = name;
    }
}