using System;

namespace HabKit.Commands
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PhysicalCommandAttribute : Attribute
    {
        public string Name { get; }
        
        public PhysicalCommandAttribute(string name)
        {
            Name = name;
        }
    }
}