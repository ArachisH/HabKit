using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using HabKit.Commands;

using Flazzy;

using Sulakore.Habbo;

namespace HabKit.Utilities
{
    public class HOptions
    {
        private static readonly Dictionary<string, Type> _physicalCommandTypes;

        public HGame Game { get; set; }

        [PhysicalArgument("output", Alias = 'o')]
        public string OutDirectory { get; }

        [PhysicalArgument("compression", Alias = 'c')]
        public CompressionKind? Compression { get; }

        static HOptions()
        {
            _physicalCommandTypes = new Dictionary<string, Type>();

            Type[] physicalCommandTypes = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type physicalCommandType in physicalCommandTypes)
            {
                if (physicalCommandType.Namespace != "HabKit.Commands.Physical") continue;

                var physicalCommandAtt = physicalCommandType.GetCustomAttribute<PhysicalCommandAttribute>();
                if (physicalCommandAtt == null) continue;

                _physicalCommandTypes.Add(physicalCommandAtt.Name, physicalCommandType);
            }
        }
        public HOptions(string[] args)
        {
            var arguments = new Queue<string>(args);
            if (File.Exists(arguments.Peek()) && arguments.Peek().ToLower().EndsWith(".swf"))
            {
                Game = new HGame(arguments.Dequeue());
            }

            while (arguments.Count > 0)
            {
                string commandName = arguments.Dequeue();
                if (!_physicalCommandTypes.TryGetValue(commandName, out Type physicalCommandType))
                {
                    throw new ArgumentException("Unknown physical command", commandName);
                }

                var command = (Command)Activator.CreateInstance(physicalCommandType, this, arguments);
              //  command.
            }
        }

        public void RunCommands()
        {

        }
    }
}