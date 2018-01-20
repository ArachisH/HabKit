using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using Flazzy;

using Sulakore.Habbo;
using System;
using System.Linq;

namespace HabKit.Utilities
{
    public class KitOptions
    {
        private readonly IDictionary<string, MethodInfo> _optionMethods;
        private readonly IDictionary<string, PropertyInfo> _optionProperties;

        public HGame Game { get; set; }

        public KitOptions(string[] args)
        {
            _optionMethods = GetOptionMembers(GetType().GetMethods());
            _optionProperties = GetOptionMembers(GetType().GetProperties());

            var arguments = new Queue<string>(args);
            if (File.Exists(arguments.Peek()) && !arguments.Peek().StartsWith("-"))
            {
                Game = new HGame(arguments.Dequeue());
            }

            while (arguments.Count > 0)
            {
                string argument = arguments.Dequeue();
                if (_optionProperties.TryGetValue(argument, out PropertyInfo property))
                {
                    object value = GetMemberValue(arguments, property.PropertyType, property.GetValue(this));
                    property.SetValue(this, value);
                }
                else if (_optionMethods.TryGetValue(argument, out MethodInfo method))
                {
                    object[] values = GetMethodValues(arguments, method);
                    method.Invoke(this, values);
                }
            }
        }

        #region HabKit Options
        #region Option Properties
        [Option("output", "o")]
        public string OutDirectory { get; set; }

        [Option("revision", "r")]
        public string RevisionOverride { get; set; }

        [Option("compression", "c")]
        public CompressionKind? Compression { get; set; }

        [Option("match-index", "mi")]
        public int MatchIndex { get; set; }

        [Option("match-to-hashes", "mth")]
        public bool IsMatchingToHashes { get; set; }

        [Option("match-pattern", "mp")]
        public string MatchPattern { get; set; } = @"(?<![a-zA-Z_-])-?[^\s\D][0-9]{0,3}(?=\D*$)";

        [Option("match-no-comments", "mnc")]
        public bool IsWritingMatchComments { get; set; } = true;
        #endregion
        #region Option Methods
        [Option("fetch", "f")]
        public async Task FetchAsync(string revision = null)
        {
            await Task.Delay(100);
        }

        [Option("disable-crypto", "dc")]
        public void DisableCrypto()
        { }

        [Option("disable-host-checks", "dhc")]
        public void DisableHostChecks()
        { }

        [Option("dump", "d")]
        public void Dump()
        { }

        [Option("rsa-replace", "rsa")]
        public void ReplaceRSAKeys(params string[] values)
        { }

        [Option("inject-key-shouter", "ks")]
        public void InjectKeyShouter(int messageId = 4001)
        { }

        [Option("inject-raw-camera", "irc")]
        public void InjectRawCamera()
        { }

        [Option("enable-descriptions", "ed")]
        public void EnableDescriptions()
        { }

        [Option("enable-avatar-tags", "eat")]
        public void EnableAvatarTags()
        { }

        [Option("binary-replace", "br")]
        public void BinaryReplace()
        { }

        [Option("image-replace", "ir")]
        public void ImageReplace()
        { }

        [Option("enable-gamecenter", "eg")]
        public void EnableGameCenter()
        { }

        [Option("clean")]
        public void Clean(Sanitizers sanitation = Sanitizers.Deobfuscate | Sanitizers.RegisterRename | Sanitizers.IdentifierRename)
        { }

        [Option("match", "m")]
        public void Match(string gamePath, string clientHeadersPath, string serverHeadersPath)
        { }
        #endregion
        #endregion

        private IDictionary<string, T> GetOptionMembers<T>(T[] members) where T : MemberInfo
        {
            var optionMembers = new Dictionary<string, T>();
            foreach (MemberInfo member in members)
            {
                var optionAtt = member.GetCustomAttribute<OptionAttribute>();
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