# HabKit
Command line tool that provides the user with a set of APIs to manipulate the Habbo Hotel client, or simply retrieving information/data from the game.  

Download, and extract the latest release somewhere accessible on your computer, excluding your recycle bin.
You can find the latest release here: https://github.com/ArachisH/HabKit/releases/latest

Starting at [HabKit v4.4](https://github.com/ArachisH/HabKit/releases/tag/v4.4.0.0)+, the download(s) will begin including binaries targeting both .NET Framework 4.7, and .NET Core 2.0.  
If you're using the .NET Core 2.0 binaries, you must prefix all commands with `dotnet`, ex:  
`dotnet HabKit.dll /fetch`

## Requirements
* [.NET Core 2.0 Runtime](https://www.microsoft.com/net/core)  
OR
* [.NET Framework 4.7](https://www.microsoft.com/en-us/download/details.aspx?id=55170)

## Commands
| Command                                                                         | Description                                                                                                                                                    |
|:-------------------------------------------------------------------------------:|:--------------------------------------------------------------------------------------------------------------------------------------------------------------:|
| /c [none, zlib, lzma]                                                           | Compression to use when assembling client.                                                                                                                     |
| /clean ?[-deob] ?[-rr] ?[-ir]                                                   | Sanitizes the client by deobfuscating methods, and renaming invalid identifiers.                                                                               |
| /dcrypto                                                                        | Disables all aspects of cryptography within the client(RC4/Handshake).                                                                                         |
| /dhost                                                                          | Disable certain methods in the client to allow it to run from any host.                                                                                        |
| /dir [directoryName]                                                            | Specify a custom output directory, rather than defaulting to the base directory of the specified client.                                                       |
| /dlog ?[functionName]                                                           | Enables the client's internal log function, and invokes the external function name **console.log** by default.                                                 |
| /dump                                                                           | Dumps Outgoing/Incoming message data to a text file(Header, Hash, Constructor Signature).                                                                      |
| /fetch ?[revisionName]                                                          | Downloads the latest client, or a specific build based on the provided revision                                                                                |
| /hardep [host:port]                                                             | Hardcodes an address that will be used by all SocketConnection instances.                                                                                      |
| /kshout ?[id]                                                                   | Client will be forced to publicly share the DH(RC4 Stream Cipher) private key to any connected parties.                                                        |
| /match ?[-mc] ?[-ii] ?[-h] [clientPath clientHeadersPath serverHeadersPath]     | Replaces the headers in the given Client/Server header files by comparing the hashes with the provided client, against the current one.                        |
| /rev [revisionValue]                                                            | Sets the client's revision value found in the Outgoing[4000] message class handler.                                                                            |
| /rsa ?[[keySize], [modulus exponent]]                                           | Override the client's internal public RSA keys with a newly generated pair, or an already existing one.                                                        |
| /rawcam                                                                         | Sends a raw blob of binary data to the server that represents the picture the user is trying to preview.                                                       |
| /desc                                                                           | Re-enables furniture, and badge descriptions in the client.                                                                                                    |
| /avtags                                                                         | Re-enables user tags/avatar tags in the client                                                                                                                 |
| /binrep { [id] [replacementPath]] }                                             | Replaces an internal file inside of the swf, based on the given id, and the specified path of the replacement data.                                            |
| /gamecenter                                                                     | Re-enables the game center icon in the client toolbar.                                                                                                         |

## Command Table Legend
**[]** = Needed Parameter(s)  
**?[]** = Optional Parameter(s)  
**[,]** = Multiple Options, Choose One  
**{ [] }** = Enumerable Paramter, multiple instances allowed

## Command Examples
* Fetching Client  
`HabKit.dll /fetch`  
`HabKit.dll /fetch REVISION-XXXXXXXXXXXXXXX`  

* Change RSA Keys  
`HabKit.dll Client.swf /dcrypto`  
`HabKit.dll Client.swf /dcrypto 1024`  
`HabKit.dll Client.swf /dcrypto 3 86851dd364..........1a9dc783b7`  

* Binary Data Replacement  
`HabKit.dll Client.swf /binrep 2727 C:\SomeNewFile.xml`  
`HabKit.dll Client.swf /binrep 2727 C:\SomeNewFile.xml 6382 C:\SomeOtherFile.xml`  

* Hardcode EndPoint  
`HabKit.dll Client.swf /hardep 127.0.0.1:8989`  
`HabKit.dll Client.swf /hardep yourHost.com:8989`  

* Message ID Updating  
`HabKit.dll Client.swf /match Previous.swf ClientHeaders.file ServerHeaders.file`  
`HabKit.dll Client.swf /match Previous.swf ClientHeaders.file ServerHeaders.file -h`  
`HabKit.dll Client.swf /match Previous.swf ClientHeaders.file ServerHeaders.file -mc`  
`HabKit.dll Client.swf /match Previous.swf ClientHeaders.file ServerHeaders.file -ii 1`  

* Client Sanitation  
  * Register Renaming `HabKit.dll Client.swf /clean -rr`  
  * Identifier Renaming `HabKit.dll Client.swf /clean -ir`  
  * Control Flow Deobfuscation `HabKit.dll Client.swf /clean -deob`  
  * Default/All Sanitation Flags `HabKit.dll Client.swf /clean -rr -ir -deob`  

#### Default RSA Keys
These keys will be used when no parameters are given to the **/dcrypto** command.
```
[E]Exponent: 3
[N]Modulus: 86851dd364d5c5cece3c883171cc6ddc5760779b992482bd1e20dd296888df91b33b936a7b93f06d29e8870f703a216257dec7c81de0058fea4cc5116f75e6efc4e9113513e45357dc3fd43d4efab5963ef178b78bd61e81a14c603b24c8bcce0a12230b320045498edc29282ff0603bc7b7dae8fc1b05b52b2f301a9dc783b7
[D]Private Exponent: 59ae13e243392e89ded305764bdd9e92e4eafa67bb6dac7e1415e8c645b0950bccd26246fd0d4af37145af5fa026c0ec3a94853013eaae5ff1888360f4f9449ee023762ec195dff3f30ca0b08b8c947e3859877b5d7dced5c8715c58b53740b84e11fbc71349a27c31745fcefeeea57cff291099205e230e0c7c27e8e1c0512b
```
