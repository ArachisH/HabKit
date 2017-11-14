using System.Drawing;
using System.Collections.Generic;
using SixLabors.ImageSharp;

namespace HabKit.Commands
{
    public class ImgRepCommand : Command
    {
        public Dictionary<ushort, Color[,]> Replacements { get; }

        public ImgRepCommand()
        {
            Replacements = new Dictionary<ushort, Color[,]>();
        }

        public override void Populate(Queue<string> parameters)
        {
            while (parameters.Count > 0)
            {
                var id = ushort.Parse(parameters.Dequeue());
                using (var asset = SixLabors.ImageSharp.Image.Load(parameters.Dequeue()))
                {
                    var table = new Color[asset.Width, asset.Height];
                    for (int y = 0; y < asset.Height; y++)
                    {
                        for (int x = 0; x < asset.Width; x++)
                        {
                            Rgba32 pixel = asset[x, y];
                            table[x, y] = Color.FromArgb(pixel.A, pixel.R, pixel.G, pixel.B);
                        }
                    }
                    Replacements.Add(id, table);
                }
            }
        }
    }
}