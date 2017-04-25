using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using HabBit.Commands;

using Flazzy;

namespace HabBit.Utilities
{
    public class HBOptions
    {
        private static readonly Dictionary<string, PropertyInfo> _commands;
        private static readonly Dictionary<PropertyInfo, CommandAttribute> _attributes;

        public FileInfo GameInfo { get; set; }
        public CommandActions Actions { get; set; }

        [Command("/c", CommandActions.Modify)]
        public CompressionKind? Compression { get; set; }

        [Command("/clean", CommandActions.Modify)]
        public bool IsSanitizing { get; set; }

        [Command("/dcrypto", CommandActions.Modify)]
        public bool IsDisablingHandshake { get; set; }

        [Command("/dhost", CommandActions.Modify)]
        public bool IsDisablingHostChecks { get; set; }

        [Command("/dir", CommandActions.None)]
        public string OutputDirectory { get; set; }

        [Command("/dlog", CommandActions.Modify, Default = "console.log")]
        public string DebugLogger { get; set; }

        [Command("/dump", CommandActions.Extract)]
        public bool IsDumpingMessageData { get; set; }

        [Command("/fetch", CommandActions.Fetch, Default = "?")]
        public string FetchRevision { get; set; }

        [Command("/hardep", CommandActions.Modify)]
        public HardEPCommand HardEPInfo { get; set; }

        [Command("/kshout", CommandActions.Modify, Default = 4001)]
        public int? KeyShouterId { get; set; }

        [Command("/match", CommandActions.Extract)]
        public MatchCommand MatchInfo { get; set; }

        [Command("/rev", CommandActions.Modify)]
        public string Revision { get; set; }

        [Command("/rsa", CommandActions.Modify)]
        public RSACommand RSAInfo { get; set; }

        static HBOptions()
        {
            _commands = new Dictionary<string, PropertyInfo>();
            _attributes = new Dictionary<PropertyInfo, CommandAttribute>();

            PropertyInfo[] properties = typeof(HBOptions).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                var commandAtts = (CommandAttribute[])property.GetCustomAttributes(typeof(CommandAttribute), false);
                if (commandAtts.Length == 0) continue;

                CommandAttribute commandAtt = commandAtts[0];
                _commands.Add(commandAtt.Name, property);
                _attributes.Add(property, commandAtt);
            }
        }

        public static HBOptions Parse(string[] args)
        {
            var options = new HBOptions();
            var arguments = new Queue<string>(args);

            options.GameInfo = new FileInfo(arguments.Peek());
            if (!options.GameInfo.Exists)
            {
                options.GameInfo = null;
            }
            else arguments.Dequeue();

            while (arguments.Count > 0)
            {
                string command = arguments.Dequeue();
                var parameters = new Queue<string>();
                while (arguments.Count > 0 && !arguments.Peek().StartsWith("/"))
                {
                    parameters.Enqueue(arguments.Dequeue());
                }
                PropertyInfo property = null;
                if (_commands.TryGetValue(command, out property))
                {
                    CommandAttribute commandAtt = _attributes[property];
                    options.Actions |= commandAtt.Actions;

                    object value = null;
                    if (parameters.Count > 0 || commandAtt.Default == null)
                    {
                        value = GenerateValue(property, parameters);
                    }
                    else value = commandAtt.Default;
                    property.SetValue(options, value, null);
                }
            }
            return options;
        }

        private static object GenerateValue(PropertyInfo property, Queue<string> parameters)
        {
            Type propType = property.PropertyType;
            propType = (Nullable.GetUnderlyingType(propType) ?? propType);

            if (propType.IsEnum)
            {
                return Enum.Parse(propType, parameters.Dequeue(), true);
            }
            else if (propType.IsSubclassOf(typeof(Command)))
            {
                var commandObj = (Command)Activator.CreateInstance(propType);
                if (parameters.Count > 0)
                {
                    commandObj.Populate(parameters);
                }
                return commandObj;
            }
            else if (propType == typeof(bool))
            {
                return true;
            }
            else if (propType == typeof(string))
            {
                return parameters.Dequeue();
            }
            else if (propType == typeof(int))
            {
                return int.Parse(parameters.Dequeue());
            }
            return null;
        }
    }
}