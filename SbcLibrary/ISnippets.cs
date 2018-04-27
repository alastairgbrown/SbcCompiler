using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SbcLibrary
{
    public interface ISnippets
    {
        Snippet GetY { get; }
        Snippet Ldlen { get; }
        Snippet StackPush { get; }
        Snippet StackPop { get; }
        Snippet StackPop2 { get; }
        Snippet StackDup { get; }
        Snippet StackDrop { get; }
        Snippet StackGet { get; }
        Snippet StackSet { get; }
        Snippet MethodPreamble { get; }
        Snippet MethodReturn { get; }
    }
}
