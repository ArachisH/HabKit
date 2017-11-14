using System.Collections.Generic;

namespace HabKit.Commands
{
    public class DumpCommand : Command
    {
        public bool IsDumpingImages { get; set; }
        public bool IsMergingBinaryData { get; set; }
        public bool IsDumpingBinaryData { get; set; }

        public override void Populate(Queue<string> parameters)
        {
            while (parameters.Count > 0)
            {
                switch (parameters.Dequeue())
                {
                    case "-img": IsDumpingImages = true; break;
                    case "-mb": IsMergingBinaryData = true; break;
                    case "-bin": IsDumpingBinaryData = true; break;
                }
            }
        }
    }
}