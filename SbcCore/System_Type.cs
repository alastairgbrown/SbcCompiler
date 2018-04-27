using SbcLibrary;

namespace SbcCore
{
    [Implement("System.Reflection.MemberInfo")]
    public class System_Reflection_MemberInfo
    {
        public string Name { get; set; }
    }

    [Implement("System.Type")]
    public class System_Type : System_Reflection_MemberInfo
    {
    }

}
