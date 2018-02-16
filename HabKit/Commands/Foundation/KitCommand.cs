using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using HabKit.Utilities;
using Sulakore.Habbo;

namespace HabKit.Commands.Foundation
{
    public abstract class KitCommand
    {
        public KitOptions Options { get; }

        private const BindingFlags BINDINGS = (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

        public KitCommand(KitOptions options, Queue<string> arguments)
        {
            Options = options;

            IDictionary<string, MethodInfo> methods = GetOptionMembers(GetType().GetMethods());
            IDictionary<string, PropertyInfo> properties = GetOptionMembers(GetType().GetProperties());
            while (arguments.Count > 0)
            {
                string argument = arguments.Dequeue();
                if (properties.TryGetValue(argument, out PropertyInfo property))
                {
                    object value = GetMemberValue(arguments, property.PropertyType, property.GetValue(this));
                    property.SetValue(this, value);
                }
                else if (methods.TryGetValue(argument, out MethodInfo method))
                {
                    object[] values = GetMethodValues(arguments, method);
                   // _optionMethods.Add(method.Name == nameof(FetchAsync) ? 0 : 1, (method, values));
                   // TODO: Put this in a dictionary or something, ye
                }
            }
        }

        private IDictionary<string, T> GetOptionMembers<T>(T[] members) where T : MemberInfo
        {
            var optionMembers = new Dictionary<string, T>();
            foreach (MemberInfo member in members)
            {
                var optionAtt = member.GetCustomAttribute<KitArgumentAttribute>();
                if (optionAtt == null) continue;

                optionMembers.Add("--" + optionAtt.Name, (T)member);
                if (!string.IsNullOrWhiteSpace(optionAtt.Alias))
                {
                    optionMembers.Add("-" + optionAtt.Alias, (T)member);
                }
            }
            return optionMembers;
        }
        private object[] GetMethodValues(Queue<string> arguments, MethodInfo method)
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
                values[i] = GetMemberValue(arguments, parameter.ParameterType, defaultValue);
            }

            return values;
        }
        private object GetMemberValue(Queue<string> arguments, Type memberType, object value = null)
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

            TypeCode code = Type.GetTypeCode(memberType);
            if (code == TypeCode.Boolean)
            {
                return !((bool)value);
            }
            else if (arguments.Count == 0 || arguments.Peek().StartsWith("-")) return value;

            switch (code)
            {
                case TypeCode.String: return arguments.Dequeue();
                case TypeCode.Int32: return int.Parse(arguments.Dequeue());
            }
            return value;
        }
    }
}