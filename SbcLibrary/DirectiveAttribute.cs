namespace SbcLibrary
{
    public class DirectiveAttribute : ArgsAttribute
    {
        public DirectiveAttribute(params string[] regex) : base(regex)
        {
        }
    }
}