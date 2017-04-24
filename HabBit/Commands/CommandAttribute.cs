using System;

namespace HabBit.Commands
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CommandAttribute : Attribute
    {
        public string Name { get; }
        public GameAccess Access { get; }
        public object Default { get; set; }
        
        public CommandAttribute(string name, GameAccess access)
        {
            Name = name;
            Access = access;
        }
    }
}