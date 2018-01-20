using System;

namespace HabKit.Utilities
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
    public class OptionAttribute : Attribute
    {
        public string Name { get; }
        public string Alias { get; }

        public OptionAttribute(string name)
            : this(name, null)
        { }
        public OptionAttribute(string name, string alias)
        {
            Name = name;
            Alias = alias;
        }
    }
}