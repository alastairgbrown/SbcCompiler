using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SbcLibrary
{
    public interface ISnippets
    {
        Action Ldlen { get; }
        Action StackPush { get; }
        Action StackPop { get; }
        Action StackPop2 { get; }
        Action StackPop3 { get; }
        Action StackDup { get; }
        Action StackDrop { get; }
        Action StackGet { get; }
        Action StackSet { get; }
        Action MethodPreamble { get; }
        Action MethodReturn { get; }
        Action CopyFromFrameToStack(ArgData arg);
        Action CopyFromStackToFrame(ArgData arg);
        Action CopyFromMemoryToStack(Type type);
        Action CopyFromStackToMemory(Type type);
        Action InsertStack(int index, int count);
        Action Compare(string comp);
    }
}
