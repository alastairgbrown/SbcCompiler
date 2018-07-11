using System;

namespace SbcLibrary
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    public class ImplementClassAttribute : Attribute
    {
        public Type Type { get; }

        public ImplementClassAttribute(Type type) => Type = type;
    }
}