using System;

namespace HabBit.Commands
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CommandAttribute : Attribute
    {
        public string Name { get; }
        public CommandActions Actions { get; }

        public int MinParams { get; set; }
        public object Default { get; set; }

        public CommandAttribute(string name, CommandActions actions)
        {
            Name = name;
            Actions = actions;
        }
    }
}