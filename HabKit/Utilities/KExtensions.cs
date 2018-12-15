using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using HabKit.Commands.Foundation;

using Sulakore.Habbo;
using Sulakore.Habbo.Web;

namespace HabKit.Utilities
{
    public static class KExtensions
    {
        public static bool WriteResult(this bool value)
        {
            KLogger.WriteLine(value ? "Success!" : "Failed!", value ? ConsoleColor.Green : ConsoleColor.Red);
            return value;
        }

        public static void PopulateMembers(this object instance, Queue<string> arguments)
        {
            PopulateMembers(instance, arguments, out List<(MethodInfo, object[])> methods);
        }
        public static void PopulateMembers(this object instance, Queue<string> arguments, out List<(MethodInfo, object[])> methods)
        {
            var members = new Dictionary<string, MemberInfo>();
            var orphanProperties = new SortedList<int, PropertyInfo>();
            foreach (MemberInfo member in instance.GetType().GetAllMembers())
            {
                var kitArgumentAtt = member.GetCustomAttribute<KitArgumentAttribute>();
                if (kitArgumentAtt == null) continue;

                if (kitArgumentAtt.OrphanIndex < 0)
                {
                    members.Add("--" + kitArgumentAtt.Name, member);
                    if (!char.IsWhiteSpace(kitArgumentAtt.Alias) && kitArgumentAtt.Alias != '\0')
                    {
                        members.Add("-" + kitArgumentAtt.Alias, member);
                    }
                }
                else orphanProperties.Add(kitArgumentAtt.OrphanIndex, (PropertyInfo)member);
            }

            foreach (PropertyInfo orphan in orphanProperties.Values)
            {
                if (orphan.PropertyType == typeof(HGame))
                {
                    var fileName = (string)DequeOrDefault(arguments);
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        orphan.SetValue(instance, new HGame(Path.GetFullPath(fileName)));
                    }
                }
                else
                {
                    object value = GetMemberValue(arguments, orphan.PropertyType, orphan.GetValue(instance));
                    orphan.SetValue(instance, value);
                }
            }

            var invalidArgs = new List<string>();
            methods = new List<(MethodInfo, object[])>();
            while (arguments.Count > 0 && !invalidArgs.Contains(arguments.Peek()) && arguments.Peek().StartsWith("-"))
            {
                string argument = arguments.Dequeue();
                if (!members.TryGetValue(argument, out MemberInfo member))
                {
                    arguments.Enqueue(argument);
                    invalidArgs.Add(argument);
                    continue;
                }

                if (member is PropertyInfo property)
                {
                    object value = GetMemberValue(arguments, property.PropertyType, property.GetValue(instance));
                    property.SetValue(instance, value);
                }
                else if (member is MethodInfo method)
                {
                    object[] values = GetMethodValues(arguments, method);

                    var kitArgumentAtt = member.GetCustomAttribute<KitArgumentAttribute>();
                    int index = kitArgumentAtt.MethodOrder < 0 ? methods.Count : kitArgumentAtt.MethodOrder;

                    methods.Insert(index, (method, values));
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
                    defaultValue = parameter.ParameterType.IsValueType ?
                        Activator.CreateInstance(parameter.ParameterType) : null;
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
            memberType = Nullable.GetUnderlyingType(memberType) ?? memberType;
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
    }
}