using SbcLibrary;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SbcCore
{
    [ImplementClass(typeof(MemberInfo))]
    public class System_Reflection_MemberInfo
    {
        public string _name;
        public string Name { get => _name; }
    }

    [ImplementClass(typeof(Type))]
    public class System_Type : System_Reflection_MemberInfo
    {
        public System_Type _base;
        public System_Type _elementType;
        public int[] _interfaces;
        public int _interfaceIndex;
        public EnumValues _enumValues;

        public System_Type GetElementType() => _elementType;
        public bool IsArray => _base == InternalCast(typeof(Array));
        public bool IsValueType => _base == InternalCast(typeof(ValueType));
        public bool IsInterface => _interfaceIndex >= 0;
        public bool IsEnum => _enumValues != null;

        [Inline]
        private static System_Type InternalCast(Type type) => Global.Emit<System_Type>();

        [Inline]
        public static System_Type GetTypeFromHandle(RuntimeTypeHandle handle) => Global.Emit<System_Type>();

        public static bool op_Equality(Type a, Type b) => ReferenceEquals(a, b);

        public bool IsInstanceOfType(object obj) => IsInst(obj) != null;

        public virtual string[] GetEnumNames() => _enumValues._names;
        public virtual Array GetEnumValues() => _enumValues._values;

        public object IsInst(object obj)
        {
            var type = InternalCast(obj.GetType());

            if (_interfaceIndex >= 0)
                return type._interfaces[_interfaceIndex] > 0 ? obj : null;

            while (type != null && type != this)
                type = type._base;

            return type == this ? obj : null;
        }
    }

}
