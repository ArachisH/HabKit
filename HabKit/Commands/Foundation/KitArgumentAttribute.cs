using System;

namespace HabKit.Commands.Foundation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class KitArgumentAttribute : Attribute
    {
        public char Alias { get; }
        public string Name { get; }
        public KitAction Action { get; }

        public int OrphanIndex { get; } = -1;
        public int MethodOrder { get; set; } = -1;

        public KitArgumentAttribute(int orphanIndex)
        {
            if (orphanIndex < 0)
            {
                throw new ArgumentException("The index cannot be less than zero.", nameof(orphanIndex));
            }
            OrphanIndex = orphanIndex;
        }
        public KitArgumentAttribute(KitAction action, string name)
        {
            Name = name;
            Action = action;
        }
        public KitArgumentAttribute(KitAction action, string name, char alias)
            : this(action, name)
        {
            Alias = alias;
        }
    }
}