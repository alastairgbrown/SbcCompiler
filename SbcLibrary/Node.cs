namespace SbcLibrary
{
    public abstract class Node
    {
        public Compiler Compiler { get; }
        public Compilation Compilation => Compiler.Compilation;
        public Config Config => Compiler.Config;
        public ISnippets Snippets => Compiler.Snippets;
        public abstract string Id { get; }
        public object IncludedFrom { get; set; }
        public abstract void GenerateExecutable();
        public virtual void GenerateConstData() { }
        public virtual void OnInclude() { }
        public override string ToString() => Id;

        public Node(Compiler compiler) => Compiler = compiler;
    }
}
