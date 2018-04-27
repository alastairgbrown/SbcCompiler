using SbcLibrary;

namespace SbcCore
{
    [Implement("System.Object")]
    public class System_Object
    {
        [Implement("string System.Object::ToString()")]
        public virtual string _ToString() => GetType().Name;

        [Snippet("System.Type System.Object::GetType()")]
        public static Snippet GetTypeObject => new Snippet((compiler, config)
        => compiler.Emit(compiler.Snippets.StackGet, -1, Opcode.LDA, Opcode.LDA, -compiler.SizeOfClass("System.Type"), Opcode.AKA,
                         compiler.Snippets.StackSet));

        [Snippet("void System.Object::.ctor()")]
        public static Snippet Ctor => new Snippet((compiler, config)
        => compiler.Emit(compiler.Snippets.StackDrop));
    }
}
