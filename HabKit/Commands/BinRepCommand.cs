using System.IO;
using System.Collections.Generic;

namespace HabKit.Commands
{
    public class BinRepCommand : Command
    {
        public Dictionary<ushort, byte[]> Replacements { get; }

        public BinRepCommand()
        {
            Replacements = new Dictionary<ushort, byte[]>();
        }

        public override void Populate(Queue<string> parameters)
        {
            while (parameters.Count > 0)
            {
                var id = ushort.Parse(parameters.Dequeue());
                byte[] data = File.ReadAllBytes(parameters.Dequeue());

                Replacements.Add(id, data);
            }
        }
    }
}