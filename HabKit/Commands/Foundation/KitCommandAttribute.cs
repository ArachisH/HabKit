using System;

namespace HabKit.Commands.Foundation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class KitCommandAttribute : Attribute
    {
        public string Name { get; }

        public KitCommandAttribute(string name)
        {
            Name = name;
        }
    }
}