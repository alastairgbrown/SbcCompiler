using SbcLibrary;

namespace SbcCore
{
    [ImplementClass("System.Object")]
    public class System_Object
    {
        public virtual string ToString() => GetType().Name;

        [Inline("System.Type System.Object::GetType()")]
        public void GetTypeObject()
        => Global.Emit(Global.Snippets.StackGet, -1, Opcode.LDA, -Global.Compiler.SizeOfClass("System.Type"), Opcode.AKA,
                       Global.Snippets.StackSet);

        [Inline("void System.Object::.ctor()")]
        public void Ctor()
        => Global.Emit(Global.Snippets.StackDrop);
    }
}
