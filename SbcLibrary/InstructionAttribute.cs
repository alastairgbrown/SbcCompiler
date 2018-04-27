namespace SbcLibrary
{
    public class InstructionAttribute : ArgsAttribute
    {
        public InstructionAttribute(params string[] regex) : base(regex)
        {
        }
    }
}