# HabKit
Command line tool that provides the user with a set of APIs to manipulate the Habbo Hotel client, or simply retrieving information/data from the game.

## Requirements
* [.NET Core 2.0 Runtime](https://www.microsoft.com/net/core)

Download, and extract the latest release somewhere accessible on your computer, excluding your recycle bin.
You can find the latest release here: https://github.com/ArachisH/HabBit/releases/latest

## Arguments
| Argument                                                                        | Description                                                                                                                                                    |
|:-------------------------------------------------------------------------------:|:--------------------------------------------------------------------------------------------------------------------------------------------------------------:|
| /c (none : zlib : lzma)                                                         | Compression to use when assembling client.                                                                                                                     |
| /clean                                                                          | Sanitizes the client by deobfuscating methods, and renaming invalid identifiers.                                                                               |
| /dcrypto                                                                        | Disables all aspects of cryptography within the client(RC4/Handshake).                                                                                         |
| /dhost                                                                          | Disable certain methods in the client to allow it to run from any host.                                                                                        |
| /dir (directoryName)                                                            | Specify a custom output directory, rather than defaulting to the base directory of the specified client.                                                       |
| /dlog (?functionName)                                                           | Enables the client's internal log function, and invokes the external function name **console.log** by default.                                                 |
| /dump                                                                           | Dumps Outgoing/Incoming message data to a text file(Header, Hash, Constructor Signature).                                                                      |
| /fetch (?revisionName)                                                          | Downloads the latest client, or a specific build based on the provided revision                                                                                |
| /hardep ("host:port")                                                           | Hardcodes an address that will be used by all SocketConnection instances.                                                                                      |
| /kshout (?id)                                                                   | Client will be forced to publicly share the DH(RC4 Stream Cipher) private key to any connected parties.                                                        |
| /match (?-mc)(?-p regexPattern)(clientPath clientHeadersPath serverHeadersPath) | Replaces the headers in the given Client/Server header files by comparing the hashes with the provided client, against the current one.                        |
| /rev  (revisionValue)                                                           | Sets the client's revision value found in the Outgoing[4000] message class handler.                                                                            |
| /rsa (?keySize : ?(modulus exponent))                                           | Override the client's internal public RSA keys with a newly generated pair, or an already existing one.                                                        |

#### Default RSA Keys
```
[E]Exponent: 3
[N]Modulus: 86851dd364d5c5cece3c883171cc6ddc5760779b992482bd1e20dd296888df91b33b936a7b93f06d29e8870f703a216257dec7c81de0058fea4cc5116f75e6efc4e9113513e45357dc3fd43d4efab5963ef178b78bd61e81a14c603b24c8bcce0a12230b320045498edc29282ff0603bc7b7dae8fc1b05b52b2f301a9dc783b7
[D]Private Exponent: 59ae13e243392e89ded305764bdd9e92e4eafa67bb6dac7e1415e8c645b0950bccd26246fd0d4af37145af5fa026c0ec3a94853013eaae5ff1888360f4f9449ee023762ec195dff3f30ca0b08b8c947e3859877b5d7dced5c8715c58b53740b84e11fbc71349a27c31745fcefeeea57cff291099205e230e0c7c27e8e1c0512b
```
