using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using HabKit.Commands;
using HabKit.Commands.Foundation;

using Sulakore.Habbo;

namespace HabKit.Utilities
{
    public class KitOptions
    {
        private readonly List<KitCommand> _commands;

        private static readonly Dictionary<string, Type> _commandTypes;

        public HGame Game { get; set; }

        static KitOptions()
        {
            _commandTypes = new Dictionary<string, Type>();

            Type[] assemblyTypes = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type assemblyType in assemblyTypes)
            {
                var kitCommandAtt = assemblyType.GetCustomAttribute<KitCommandAttribute>();
                if (kitCommandAtt == null) continue;

                _commandTypes.Add(kitCommandAtt.Name, assemblyType);
            }
        }
        public KitOptions(string[] args)
        {
            _commands = new List<KitCommand>();

            var arguments = new Queue<string>(args);
            while (arguments.Count > 0)
            {
                Type commandType = _commandTypes[arguments.Dequeue()];
                var command = (KitCommand)Activator.CreateInstance(commandType, this, arguments);

                // We want the fetch commands to be executed first.
                _commands.Insert(command is FetchCommand ? 0 : _commands.Count, command);
            }
        }

        public async Task ExecuteAsync()
        {
            foreach (KitCommand command in _commands)
            {
                await command.ExecuteAsync().ConfigureAwait(false);
            }
        }
    }
}