using System.Collections.Generic;

using HabKit.Utilities;
using HabKit.Commands.Foundation;

using Sulakore.Habbo;

namespace HabKit.Commands
{
    [KitCommand("patch")]
    public class PatchCommand : KitCommand
    {
        [KitArgument(0)]
        public HGame Game { get; }

        public PatchCommand(KitOptions options, Queue<string> arguments)
            : base(options, arguments)
        { }

        [KitArgument("disable-crypto", "dc")]
        public void DisableCrypto()
        { }

        [KitArgument("disable-host-checks", "dhc")]
        public void DisableHostChecks()
        { }

        [KitArgument("enable-game-center", "egc")]
        public void EnableGameCenterIcon()
        { }

        [KitArgument("inject-key-shouter", "iks")]
        public void InjectKeyShouter(int messageId = 4001)
        { }

        [KitArgument("inject-raw-camera", "irc")]
        public void InjectRawCamera()
        { }

        [KitArgument("inject-rsa", "ir")]
        public void InjectRSAKeys(params string[] values)
        { }

        [KitArgument("replace-binaries", "rb")]
        public void ReplaceBinaries()
        { }

        [KitArgument("replace-images", "ri")]
        public void ReplaceImages()
        { }
    }
}