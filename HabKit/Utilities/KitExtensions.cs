using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using HabKit.Commands.Foundation;

using Sulakore.Habbo;

namespace HabKit.Utilities
{
    public static class KitExtensions
    {
        public static void PopulateMembers(this object instance, Queue<string> arguments)
        {
            PopulateMembers(instance, arguments, out List<(MethodInfo, object[])> methods);
        }
        public static void PopulateMembers(this object instance, Queue<string> arguments, out List<(MethodInfo, object[])> methods)
        {
            var members = new Dictionary<string, MemberInfo>();
            foreach (MemberInfo member in instance.GetType().GetAllMembers())
            {
                var kitArgumentAtt = member.GetCustomAttribute<KitArgumentAttribute>();
                if (kitArgumentAtt == null) continue;

                members.Add("--" + kitArgumentAtt.Name, member);
                if (!string.IsNullOrWhiteSpace(kitArgumentAtt.Alias))
                {
                    members.Add('-' + kitArgumentAtt.Alias, member);
                }
            }

            methods = new List<(MethodInfo, object[])>();
            while (arguments.Count > 0 && arguments.Peek().StartsWith("-"))
            {
                string argument = arguments.Dequeue();
                MemberInfo member = members[argument];
                if (member is PropertyInfo property)
                {
                    object value = GetMemberValue(arguments, property.PropertyType, property.GetValue(instance));
                    property.SetValue(instance, value);
                }
                else if (member is MethodInfo method)
                {
                    object[] values = GetMethodValues(arguments, method);
                    methods.Add((method, values));
                }
            }
        }

        private static object DequeOrDefault(Queue<string> arguments, object value = null)
        {
            if (arguments.Count > 0 && !arguments.Peek().StartsWith("-"))
            {
                return arguments.Dequeue();
            }
            return value;
        }
        private static object[] GetMethodValues(Queue<string> arguments, MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            var values = new object[parameters.Length];
            for (int i = 0; i < values.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                object defaultValue = parameter.DefaultValue;
                if (!parameter.HasDefaultValue)
                {
                    defaultValue = (parameter.ParameterType.IsValueType ?
                        Activator.CreateInstance(parameter.ParameterType) : null);
                }
                if (defaultValue is bool)
                {
                    throw new NotSupportedException("Boolean properties are not supported.");
                }
                values[i] = GetMemberValue(arguments, parameter.ParameterType, defaultValue);
            }
            return values;
        }
        private static object GetMemberValue(Queue<string> arguments, Type memberType, object value = null)
        {
            memberType = (Nullable.GetUnderlyingType(memberType) ?? memberType);
            if (memberType.IsEnum)
            {
                if (arguments.Count == 0) return value;

                string argument = arguments.Dequeue();
                if (memberType.GetCustomAttributes<FlagsAttribute>().Any())
                {
                    int bits = 0;
                    string[] flags = argument.Split(',');
                    foreach (string flag in flags)
                    {
                        bits |= (int)Enum.Parse(memberType, flag, true);
                    }
                    argument = bits.ToString();
                }
                return Enum.Parse(memberType, argument, true);
            }

            switch (Type.GetTypeCode(memberType))
            {
                case TypeCode.Boolean: return !(bool)value;
                case TypeCode.String: return DequeOrDefault(arguments, value);
                case TypeCode.Int32: return Convert.ToInt32(DequeOrDefault(arguments, value));
            }
            return value;
        }

        //private static void PopulateOrphans(Queue<string> arguments)
        //{
        //    IEnumerable<MemberInfo> members = GetType().GetProperties().Cast<MemberInfo>().Concat(GetType().GetMethods());
        //    foreach (MemberInfo member in members)
        //    {
        //        var argumentAtt = member.GetCustomAttribute<KitArgumentAttribute>();
        //        if (argumentAtt == null || argumentAtt.OrphanIndex < 0) continue;

        //        if (member is PropertyInfo property)
        //        {
        //            object value = GetMemberValue(arguments, property.PropertyType, null);
        //            property.SetValue(this, value);
        //        }
        //        else if (member is MethodInfo method)
        //        { }
        //    }
        //}
    }
}