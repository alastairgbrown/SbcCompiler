using SbcLibrary;

namespace SbcCore
{
    [ImplementClass("System.Reflection.MemberInfo")]
    public class System_Reflection_MemberInfo
    {
        public string Name { get; set; }
    }

    [ImplementClass("System.Type")]
    public class System_Type : System_Reflection_MemberInfo
    {
    }
}
