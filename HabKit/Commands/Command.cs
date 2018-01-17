using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using HabKit.Utilities;

using Sulakore.Habbo;

namespace HabKit.Commands
{
    public abstract class Command
    {
        private readonly Dictionary<string, (PhysicalArgumentAttribute, PropertyInfo)> _physicalArguments;

        protected HGame Game
        {
            get => Options?.Game;
            set
            {
                if (Options != null)
                {
                    Options.Game = value;
                }
            }
        }
        protected HOptions Options { get; }

        private Command()
        {
            _physicalArguments = new Dictionary<string, (PhysicalArgumentAttribute, PropertyInfo)>();
            foreach (PropertyInfo property in GetType().GetProperties())
            {
                var physicalArgumentAtt = property.GetCustomAttribute<PhysicalArgumentAttribute>();
                if (physicalArgumentAtt == null) continue;

                if (physicalArgumentAtt.Alias != char.MinValue)
                {
                    _physicalArguments.Add("-" + physicalArgumentAtt.Alias, (physicalArgumentAtt, property));
                }
                _physicalArguments.Add("--" + physicalArgumentAtt.Name, (physicalArgumentAtt, property));
            }
        }
        public Command(HOptions options, Queue<string> arguments)
            : this()
        {
            Options = options;
            while (arguments.Count > 0)
            {
                string argument = arguments.Peek();
                if (_physicalArguments.TryGetValue(argument, out (PhysicalArgumentAttribute attribute, PropertyInfo property) physical))
                {
                    // Argument is valid, and exists on this command; Remove the argument from the queue.
                    argument = arguments.Dequeue(); 


                }
            }
        }

        public virtual void Run()
        {
            Execute();
        }

        protected abstract void Execute();
    }
    public abstract class AsyncCommand : Command
    {
        public AsyncCommand(HOptions options, Queue<string> arguments)
            : base(options, arguments)
        { }

        protected override void Execute()
        {
            // Since this is a command-line application, we don't need to worry about blocking the thread, for now.
            ExecuteAsync().Wait();
        }
        protected abstract Task ExecuteAsync();
    }
}