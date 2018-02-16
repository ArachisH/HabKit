using System;

namespace HabKit.Commands.Foundation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class KitArgumentAttribute : Attribute
    {
        public string Name { get; }
        public string Alias { get; }
        public int OrphanIndex { get; } = -1;

        public KitArgumentAttribute(string name)
            : this(name, null)
        { }
        public KitArgumentAttribute(int orphanIndex)
        {
            if (orphanIndex < 0)
            {
                throw new ArgumentException("The index cannot be less than zero.", nameof(orphanIndex));
            }
            OrphanIndex = orphanIndex;
        }
        public KitArgumentAttribute(string name, string alias)
        {
            Name = name;
            Alias = alias;
        }
    }
}