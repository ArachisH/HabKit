using System;

namespace HabKit.Commands
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class PhysicalArgumentAttribute : Attribute
    {
        public string Name { get; }
        public int RequiredValues { get; }

        public char Alias { get; set; }

        public PhysicalArgumentAttribute(string name, int requiredValues = 0)
        {
            Name = name;
            RequiredValues = requiredValues;
        }
    }
}