using System;
using System.Reflection;
using System.Collections.Generic;

using HabKit.Commands.Foundation;

namespace HabKit.Utilities
{
    public class KitOptions
    {
        private readonly SortedList<int, KitCommand> _commands;

        private static readonly Dictionary<string, Type> _commandTypes;

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
            _commands = new SortedList<int, KitCommand>();

            var arguments = new Queue<string>(args);
            while (arguments.Count > 0)
            {
                Type commandType = _commandTypes[arguments.Dequeue()];
                var command = (KitCommand)Activator.CreateInstance(commandType, this, arguments);
            }
        }
    }
}