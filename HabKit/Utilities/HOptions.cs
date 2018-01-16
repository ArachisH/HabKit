using HabKit.Commands;

using Flazzy;

using Sulakore.Habbo;

namespace HabKit.Utilities
{
    public class HOptions
    {
        private HGame _game;
        public HGame Game
        {
            get => _game;
            set => _game = value;
        }

        [PhysicalArgument("output", Alias = 'o')]
        public string OutDirectory { get; }

        [PhysicalArgument("compression", Alias = 'c')]
        public CompressionKind? Compression { get; }

        public HOptions(string[] args)
        { }
    }
}