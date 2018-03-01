using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using HabKit.Utilities;

namespace HabKit.Commands.Foundation
{
    public abstract class KitCommand
    {
        public KitOptions Options { get; }

        public KitCommand(KitOptions options, Queue<string> arguments)
        {
            Options = options;
            PopulateOrphans(arguments);

            IDictionary<string, MethodInfo> methods = GetArgumentMembers(GetType().GetMethods());
            IDictionary<string, PropertyInfo> properties = GetArgumentMembers(GetType().GetProperties());
            while (arguments.Count > 0 && arguments.Peek().StartsWith("-"))
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

        public async Task ExecuteAsync()
        {
            await Task.Delay(100);
        }

        private IDictionary<string, T> GetArgumentMembers<T>(T[] members) where T : MemberInfo
        {
            var optionMembers = new Dictionary<string, T>();
            foreach (MemberInfo member in members)
            {
                var argumentAtt = member.GetCustomAttribute<KitArgumentAttribute>();
                if (argumentAtt == null || argumentAtt.OrphanIndex >= 0) continue;

                optionMembers.Add("--" + argumentAtt.Name, (T)member);
                if (!string.IsNullOrWhiteSpace(argumentAtt.Alias))
                {
                    optionMembers.Add("-" + argumentAtt.Alias, (T)member);
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

            switch (Type.GetTypeCode(memberType))
            {
                case TypeCode.Boolean: return !(bool)value;
                case TypeCode.String: return DequeOrDefault(arguments, value);
                case TypeCode.Int32: return Convert.ToInt32(DequeOrDefault(arguments, value));
            }
            return value;
        }

        private void PopulateOrphans(Queue<string> arguments)
        {
            IEnumerable<MemberInfo> members = GetType().GetProperties().Cast<MemberInfo>().Concat(GetType().GetMethods());
            foreach (MemberInfo member in members)
            {
                var argumentAtt = member.GetCustomAttribute<KitArgumentAttribute>();
                if (argumentAtt == null || argumentAtt.OrphanIndex < 0) continue;

                if (member is PropertyInfo property)
                {
                    object value = GetMemberValue(arguments, property.PropertyType, null);
                    property.SetValue(this, value);
                }
                else if (member is MethodInfo method)
                { }
            }
        }
        private object DequeOrDefault(Queue<string> arguments, object value = null)
        {
            if (arguments.Count > 0)
            {
                string argument = arguments.Peek();
                if (!argument.StartsWith("-"))
                {
                    return arguments.Dequeue();
                }
            }
            return value;
        }
    }
}