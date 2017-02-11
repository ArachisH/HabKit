# HabBit
Command line tool for allowing the user to remove certain restrictions from the Harble client that do not originally allow it to be used locally.

## Requirements
* .NET Framework 4.0+

## Getting Started
This section is for people who've never used a command line tool before, proceed to the next section for the list of [arguments](#arguments).

Download, and extract the latest release somewhere accessible on your computer, excluding your recycle bin.
You can find the latest release here: https://github.com/ArachisH/HabBit/releases/latest

I'll assume you extracted it onto your desktop under a folder named **HabBit**, so with that, let's start up the command prompt.
(Windows Key + R) This key combination will bring up some box, type **cmd** into the input box and hit the **OK**.

Some black box should have appeared, the next step involves changing the current active directory that the console is pointing to. By default, it should be pointing to **C:\Users\{Username}**. We need to change the directory to points towards the folder to where HabBit has been extracted to, to do this use the **cd** command followed by the path. If you didn't extract it to a folder named HabBit onto your desktop, then your path should look different based on where it is.
```
cd C:\Users\{Username}\Desktop\HabBit
```
We can now run some basic functions modifications on the client like so:
```
HabBit.exe {Client Path.swf} /clean /dhost /rsa
```
## Arguments
| Argument                                                | Description                                                                                                                             | Parameter State |
|:-------------------------------------------------------:|:---------------------------------------------------------------------------------------------------------------------------------------:|:---------------:|
| /c (none, zlib, lzma)                                   | Compression to use when assembling client.                                                                                              | **Required**    |
| /clean                                                  | Sanitizes the client by deobfuscating methods, and renaming invalid identifiers.                                                        | **Optional**    |
| /dcrypto                                                | Disables all aspects of cryptography within the client(RC4/Handshake).                                                                  | **Optional**    |
| /dhost                                                  | Disable certain methods in the client to allow it to run from any host.                                                                 | **Optional**    |
| /dlog (functionName)                                    | Enables the client's internal log function, and invokes the external function name **console.log** by default.                          | **Optional**    |
| /dump                                                   | Dumps Outgoing/Incoming message data to a text file(Header, SHA1, Constructor Signature).                                               | **Optional**    |
| /fetch (revisionName)                                   | Downloads the latest client, or a specific build based on the provided revision                                                         | **Optional**    |
| /kshout                                                 | Client will be forced to publicly share the DH(RC4 Stream Cipher) private key to any connected parties.                                 | **Optional**    |
| /match (clientName clientHeadersName serverHeadersName) | Replaces the headers in the given Client/Server header files by comparing the hashes with the provided client, against the current one. | **Required**    |
| /mlog (functionName)                                    | Call an external function every time a message is being sent/received with the array of values as a parameter.                          | **Optional**    |
| /rev  (revisionName)                                    | Sets the client's revision value found in the Outgoing[4000] message class handler.                                                     | **Required**    |
| /rsa (keySize, modulus exponent)                        | Override the client's internal public RSA keys with a newly generated pair, or an already existing one.                                 | **Optional**    |

#### Default RSA Keys
```
[E]Exponent: 3
[N]Modulus: 86851dd364d5c5cece3c883171cc6ddc5760779b992482bd1e20dd296888df91b33b936a7b93f06d29e8870f703a216257dec7c81de0058fea4cc5116f75e6efc4e9113513e45357dc3fd43d4efab5963ef178b78bd61e81a14c603b24c8bcce0a12230b320045498edc29282ff0603bc7b7dae8fc1b05b52b2f301a9dc783b7
[D]Private Exponent: 59ae13e243392e89ded305764bdd9e92e4eafa67bb6dac7e1415e8c645b0950bccd26246fd0d4af37145af5fa026c0ec3a94853013eaae5ff1888360f4f9449ee023762ec195dff3f30ca0b08b8c947e3859877b5d7dced5c8715c58b53740b84e11fbc71349a27c31745fcefeeea57cff291099205e230e0c7c27e8e1c0512b
```
