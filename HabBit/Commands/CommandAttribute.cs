using System;

namespace HabBit.Commands
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CommandAttribute : Attribute
    {
        public string Name { get; }
        public CommandActions Actions { get; }
        public object Default { get; set; }
        
        public CommandAttribute(string name, CommandActions actions)
        {
            Name = name;
            Actions = actions;
        }
    }
}