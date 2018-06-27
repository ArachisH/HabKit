using System;

namespace HabKit.Commands.Foundation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class KitArgumentAttribute : Attribute
    {
        public char Alias { get; }
        public string Name { get; }
        public KitPermissions Permissions { get; }

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
        public KitArgumentAttribute(KitPermissions permissions, string name)
        {
            Name = name;
            Permissions = permissions;
        }
        public KitArgumentAttribute(KitPermissions permissions, string name, char alias)
            : this(permissions, name)
        {
            Alias = alias;
        }
    }
}