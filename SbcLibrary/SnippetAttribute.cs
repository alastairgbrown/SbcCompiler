using System;

namespace SbcLibrary
{
    public class SnippetAttribute : Attribute
    {
        public string Signature { get; }

        public SnippetAttribute(string signature = null) => Signature = signature;
    }
}