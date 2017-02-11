using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;

using Flazzy;
using Flazzy.IO;
using Flazzy.ABC;
using Flazzy.Tags;
using Flazzy.Records;
using Flazzy.ABC.AVM2;
using Flazzy.ABC.AVM2.Instructions;

namespace HabBit.Habbo
{
    public enum Sanitization
    {
        None = 0,
        Deobfuscate = 1,
        RegisterRename = 2,
        IdentifierRename = 4,

        All = (Deobfuscate | RegisterRename | IdentifierRename)
    }

    public class HGame : ShockwaveFlash
    {
        private static readonly string[] _reservedNames = {
            "case", "class", "default", "do", "else",
            "extends", "false", "for", "get", "if",
            "implements", "import", "in", "package", "true",
            "try", "use", "var", "while", "with",
            "each", "null", "dynamic", "catch", "final",
            "break", "set", "static" };

        private readonly Dictionary<DoABCTag, ABCFile> _abcFileTags;
        private readonly Dictionary<ASClass, MessageItem> _messages;

        public List<ABCFile> ABCFiles { get; }
        public SortedDictionary<ushort, MessageItem> InMessages { get; }
        public SortedDictionary<ushort, MessageItem> OutMessages { get; }
        public SortedDictionary<string, List<MessageItem>> Messages { get; }

        private int _revisionIndex;
        public string Revision
        {
            get
            {
                if (ABCFiles.Count >= 3)
                {
                    return ABCFiles[2].Pool.Strings[_revisionIndex];
                }
                else return "PRODUCTION-000000000000-000000000";
            }
            set
            {
                if (ABCFiles.Count >= 3)
                {
                    ABCFiles[2].Pool.Strings[_revisionIndex] = value;
                }
            }
        }

        public HGame(string path)
            : this(File.OpenRead(path))
        { }
        public HGame(byte[] data)
            : this(new MemoryStream(data))

        { }
        public HGame(Stream input)
            : this(input, false)
        { }
        public HGame(Stream input, bool leaveOpen)
            : this(new FlashReader(input, leaveOpen))
        { }
        protected HGame(FlashReader input)
            : base(input)
        {
            _abcFileTags = new Dictionary<DoABCTag, ABCFile>();
            _messages = new Dictionary<ASClass, MessageItem>();

            ABCFiles = new List<ABCFile>();
            InMessages = new SortedDictionary<ushort, MessageItem>();
            OutMessages = new SortedDictionary<ushort, MessageItem>();
            Messages = new SortedDictionary<string, List<MessageItem>>();
        }

        public void Sanitize(Sanitization sanitization)
        {
            if (sanitization.HasFlag(Sanitization.Deobfuscate))
            {
                Deobfuscate();
            }
            if (sanitization.HasFlag(Sanitization.RegisterRename))
            {
                RenameRegisters();
            }
            if (sanitization.HasFlag(Sanitization.IdentifierRename))
            {
                RenameIdentifiers();
            }
        }
        #region Sanitization Methods
        protected void Deobfuscate()
        {
            foreach (ABCFile abc in ABCFiles)
            {
                foreach (ASMethodBody body in abc.MethodBodies)
                {
                    if (body.Exceptions.Count > 0) continue;
                    if (body.Code[0] == 0x27 && body.Code[1] == 0x26) // PushFalse, PushTrue
                    {
                        ASCode code = body.ParseCode();
                        code.Deobfuscate();

                        body.Code = code.ToArray();
                    }
                }
            }
        }
        protected void RenameRegisters()
        {
            foreach (ABCFile abc in ABCFiles)
            {
                var nameIndices = new Dictionary<string, int>();
                foreach (ASMethodBody body in abc.MethodBodies)
                {
                    if (body.Exceptions.Count > 0) continue;
                    if (body.Code.Length <= 50 && !body.Code.Contains((byte)0xEF)) continue;

                    ASCode code = body.ParseCode();
                    if (!code.Contains(OPCode.Debug)) continue;

                    bool wasModified = false;
                    List<ASParameter> parameters = body.Method.Parameters;
                    foreach (DebugIns debug in code.GetOPGroup(OPCode.Debug))
                    {
                        if (debug.Name != "k") continue;

                        string name = string.Empty;
                        ASParameter parameter = null;
                        int register = debug.RegisterIndex;
                        if (register < parameters.Count)
                        {
                            parameter = parameters[register];
                            if (!string.IsNullOrWhiteSpace(parameter.Name))
                            {
                                nameIndices.Add(parameter.Name, parameter.NameIndex);
                                name = parameter.Name;
                            }
                            else name = $"param{register + 1}";
                        }
                        else name = $"local{(register - parameters.Count) + 1}";

                        int nameIndex = 0;
                        if (!nameIndices.TryGetValue(name, out nameIndex))
                        {
                            nameIndex = abc.Pool.AddConstant(name, false);
                            nameIndices.Add(name, nameIndex);
                        }
                        if (parameter != null)
                        {
                            parameter.NameIndex = nameIndex;
                        }

                        debug.NameIndex = nameIndex;
                        wasModified = true;
                    }
                    if (wasModified)
                    {
                        body.Code = code.ToArray();
                    }
                }
            }
        }
        protected void RenameIdentifiers()
        {
            // [InvalidNamespaceName] - [ValidNamespaceName]
            var validNamespaces = new Dictionary<string, string>();
            // [ValidNamespaceName.InvalidClassName] - [ValidClassName]
            var validClasses = new SortedDictionary<string, string>();

            int classCount = 0;
            int interfaceCount = 0;
            int namespaceCount = 0;
            foreach (ABCFile abc in ABCFiles)
            {
                var nameIndices = new Dictionary<string, int>();
                foreach (KeyValuePair<string, string> fixedNames in validNamespaces.Concat(validClasses))
                {
                    int index = abc.Pool.AddConstant(fixedNames.Value, false);
                    nameIndices.Add(fixedNames.Key, index);
                }

                #region Namespace Renaming
                foreach (ASNamespace @namespace in abc.Pool.Namespaces)
                {
                    if (@namespace == null) continue;

                    namespaceCount++;
                    if (IsValidIdentifier(@namespace.Name)) continue;

                    int validNameIndex = -1;
                    if (!nameIndices.TryGetValue(@namespace.Name, out validNameIndex))
                    {
                        string validName = ("Namespace_" + namespaceCount.ToString("0000"));
                        validNameIndex = abc.Pool.AddConstant(validName, false);

                        nameIndices.Add(@namespace.Name, validNameIndex);
                        if (!validNamespaces.ContainsKey(@namespace.Name))
                        {
                            validNamespaces.Add(@namespace.Name, validName);
                        }
                    }
                    else namespaceCount--;
                    @namespace.NameIndex = validNameIndex;
                }
                #endregion
                #region Class Renaming
                foreach (ASClass @class in abc.Classes)
                {
                    var validName = string.Empty;
                    ASInstance instance = @class.Instance;
                    if (instance.IsInterface)
                    {
                        validName = ("IInterface_" + (++interfaceCount).ToString("0000"));
                    }
                    else
                    {
                        validName = ("Class_" + (++classCount).ToString("0000"));
                    }

                    ASMultiname qname = instance.QName;
                    if (IsValidIdentifier(qname.Name)) continue;

                    int validNameIndex = -1;
                    string key = ($"{qname.Namespace.Name}.{qname.Name}");
                    if (!nameIndices.TryGetValue(key, out validNameIndex))
                    {
                        validNameIndex = abc.Pool.AddConstant(validName, false);

                        nameIndices.Add(key, validNameIndex);
                        if (!validClasses.ContainsKey(key))
                        {
                            validClasses.Add(key, validName);
                        }
                    }
                    else if (instance.IsInterface)
                    {
                        interfaceCount--;
                    }
                    else
                    {
                        classCount--;
                    }
                    qname.NameIndex = validNameIndex;
                }
                #endregion
                #region Multiname Renaming
                foreach (ASMultiname multiname in abc.Pool.Multinames)
                {
                    if (string.IsNullOrWhiteSpace(multiname?.Name)) continue;
                    if (IsValidIdentifier(multiname.Name)) continue;

                    var validClassKeys = new List<string>();
                    switch (multiname.Kind)
                    {
                        default: continue;
                        case MultinameKind.QName:
                        {
                            validClassKeys.Add($"{multiname.Namespace.Name}.{multiname.Name}");
                            break;
                        }
                        case MultinameKind.Multiname:
                        {
                            foreach (ASNamespace @namespace in multiname.NamespaceSet.GetNamespaces())
                            {
                                validClassKeys.Add($"{@namespace.Name}.{multiname.Name}");
                            }
                            break;
                        }
                    }
                    foreach (string key in validClassKeys)
                    {
                        int validNameIndex = -1;
                        if (nameIndices.TryGetValue(key, out validNameIndex))
                        {
                            multiname.NameIndex = validNameIndex;
                        }
                    }
                }
                #endregion

                abc.RebuildCache();
            }
            #region Symbol Renaming
            foreach (SymbolClassTag symbolTag in Tags
                .Where(t => t.Kind == TagKind.SymbolClass)
                .Cast<SymbolClassTag>())
            {
                for (int i = 0; i < symbolTag.Names.Count; i++)
                {
                    string fullName = symbolTag.Names[i];

                    string className = fullName;
                    var namespaceName = string.Empty;
                    string[] names = fullName.Split('.');
                    if (names.Length == 2)
                    {
                        className = names[1];
                        namespaceName = names[0];

                        if (IsValidIdentifier(namespaceName) &&
                            IsValidIdentifier(className))
                        {
                            continue;
                        }
                    }

                    var fixedFullName = string.Empty;
                    var fixedNamespaceName = string.Empty;
                    if (validNamespaces.TryGetValue(namespaceName, out fixedNamespaceName))
                    {
                        fixedFullName += (fixedNamespaceName + ".");
                    }
                    else if (!string.IsNullOrWhiteSpace(namespaceName))
                    {
                        fixedFullName += (namespaceName + ".");
                    }

                    var fixedClassName = string.Empty;
                    if (validClasses.TryGetValue($"{fixedNamespaceName}.{className}", out fixedClassName))
                    {
                        fixedFullName += fixedClassName;
                    }
                    else fixedFullName += className;
                    symbolTag.Names[i] = fixedFullName;
                }
            }
            #endregion
        }
        #endregion

        public void GenerateMessageHashes()
        {
            FindMessageReferences(ABCFiles[2]);
            foreach (MessageItem message in _messages.Values)
            {
                message.GenerateSHA1();
                List<MessageItem> group = null;
                if (!Messages.TryGetValue(message.SHA1, out group))
                {
                    group = new List<MessageItem>();
                    Messages.Add(message.SHA1, group);
                }
                group.Add(message);
            }
        }
        #region Reference Scanning Methods
        private void FindMessageReferences(ABCFile abc)
        {
            foreach (ASClass @class in abc.Classes)
            {
                ASInstance instance = @class.Instance;
                if (_messages.ContainsKey(@class)) continue;
                if (instance.Flags.HasFlag(ClassFlags.Interface)) continue;

                IEnumerable<ASMethod> methods = (new[] { @class.Constructor, instance.Constructor })
                    .Concat(instance.GetMethods())
                    .Concat(@class.GetMethods());

                int classReferenceRank = 0;
                foreach (ASMethod method in methods)
                {
                    bool isStatic = (method.Trait?.IsStatic ?? @class.Constructor == method);
                    if (!FindMessageReferences(@class, method, isStatic, classReferenceRank)) continue;
                    classReferenceRank++;
                }
            }
        }
        private bool FindMessageReferences(ASClass @class, ASMethod method, bool isStatic, int classReferenceRank)
        {
            int methodReferenceRank = 0;
            bool foundReferences = false;
            ABCFile abc = @class.GetABC();
            ASCode code = method.Body.ParseCode();
            var multinameStack = new Stack<ASMultiname>();
            for (int i = 0; i < code.Count; i++)
            {
                int argCount = 0;
                string refernecedQName = null;
                ASInstruction instruction = code[i];
                switch (instruction.OP)
                {
                    default: continue;
                    case OPCode.NewFunction:
                    {
                        var newFunctionIns = (NewFunctionIns)instruction;
                        foundReferences = FindMessageReferences(@class, newFunctionIns.Method, isStatic, classReferenceRank);
                        continue;
                    }
                    case OPCode.GetProperty:
                    {
                        var getPropertyIns = (GetPropertyIns)instruction;
                        multinameStack.Push(getPropertyIns.PropertyName);
                        break;
                    }
                    case OPCode.GetLex:
                    {
                        var getLexIns = (GetLexIns)instruction;
                        if (code[i + 1].OP == OPCode.AsTypeLate)
                        {
                            refernecedQName = getLexIns.TypeName.Name;
                        }
                        break;
                    }
                    case OPCode.Coerce:
                    {
                        var coerceIns = (CoerceIns)instruction;
                        refernecedQName = coerceIns.TypeName.Name;
                        break;
                    }
                    case OPCode.ConstructProp:
                    {
                        var constructPropIns = (ConstructPropIns)instruction;

                        argCount = constructPropIns.ArgCount;
                        refernecedQName = constructPropIns.PropertyName.Name;
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(refernecedQName)) continue;
                ASClass messageClass = abc.GetClasses(refernecedQName).FirstOrDefault();
                if (messageClass == null) continue;

                MessageItem message = null;
                if (!_messages.TryGetValue(messageClass, out message)) continue;

                var reference = new MessageReference(@class);
                reference.MethodReferenceRank = methodReferenceRank++;
                reference.ClassReferenceRank = classReferenceRank;
                reference.IsAnonymous = (method.Trait == null);
                reference.FromMethod = method;
                reference.ReferencedAt = i;

                // Add the callback method to references.
                if (!message.IsOutgoing && argCount > 0)
                {
                    ASMultiname callbackName = multinameStack.Pop();

                    ASMethod callbackMethod = (isStatic ?
                        @class.GetMethod(callbackName.Name) :
                        @class.Instance.GetMethod(callbackName.Name));

                    reference.InCallback = callbackMethod;
                }

                foundReferences = true;
                message.References.Add(reference);
            }
            return foundReferences;
        }
        #endregion

        public bool InjectKeyShouter()
        {
            ABCFile abc = ABCFiles[2];
            int sendMessageQNameIndex = 0;

            if (!DisableEncryption()) return false;
            ASClass socketConnClass = abc.GetClasses("SocketConnection").FirstOrDefault();
            if (socketConnClass == null) return false;
            ASInstance socketConnInstance = socketConnClass.Instance;

            ASMethod sendMethod = socketConnInstance.GetMethod(1, "send", "Boolean");
            if (sendMethod == null) return false;

            ASCode sendCode = sendMethod.Body.ParseCode();
            SimplifySendCode(abc, sendCode);

            #region Adding Method: sendMessage(header:int, ... values) : Boolean
            // Create the method to house to body / instructions.
            var sendMessageMethod = new ASMethod(abc);
            sendMessageMethod.Flags |= MethodFlags.NeedRest;
            sendMessageMethod.ReturnTypeIndex = sendMethod.ReturnTypeIndex;
            int sendMessageMethodIndex = abc.AddMethod(sendMessageMethod);

            // The parameters for the instructions to expect / use.
            var headerParam = new ASParameter(abc, sendMessageMethod);
            headerParam.NameIndex = abc.Pool.AddConstant("header");
            headerParam.TypeIndex = abc.Pool.GetMultinameIndices("int").First();
            sendMessageMethod.Parameters.Add(headerParam);

            // The method body that houses the instructions.
            var sendMessageBody = new ASMethodBody(abc);
            sendMessageBody.MethodIndex = sendMessageMethodIndex;
            sendMessageBody.Code = sendCode.ToArray();
            sendMessageBody.InitialScopeDepth = 5;
            sendMessageBody.MaxScopeDepth = 6;
            sendMessageBody.LocalCount = 10;
            sendMessageBody.MaxStack = 5;
            abc.AddMethodBody(sendMessageBody);

            socketConnInstance.AddMethod(sendMessageMethod, "sendMessage");
            sendMessageQNameIndex = sendMessageMethod.Trait.QNameIndex;
            #endregion

            ASClass habboCommDemoClass = ABCFiles[2].GetClasses("HabboCommunicationDemo").FirstOrDefault();
            if (habboCommDemoClass == null) return false;
            ASInstance habboCommDemoInstance = habboCommDemoClass.Instance;

            ASMethod pubKeyVerifyMethod = habboCommDemoInstance.GetMethods(1, "void")
                .Where(m => m.Body.MaxStack == 4 &&
                            m.Body.LocalCount == 10 &&
                            m.Body.MaxScopeDepth == 6 &&
                            m.Body.InitialScopeDepth == 5)
                .FirstOrDefault();
            if (pubKeyVerifyMethod == null) return false;

            int coereceCount = 0;
            ASCode pubKeyVerCode = pubKeyVerifyMethod.Body.ParseCode();
            foreach (ASInstruction instruct in pubKeyVerCode)
            {
                if (instruct.OP == OPCode.Coerce &&
                    (++coereceCount == 2))
                {
                    var coerceIns = (CoerceIns)instruct;
                    coerceIns.TypeNameIndex = socketConnInstance.QNameIndex;
                    break;
                }
            }
            pubKeyVerCode.InsertRange(pubKeyVerCode.Count - 5, new ASInstruction[]
            {
                new GetLocal2Ins(),
                new PushIntIns(abc, 4001),
                new GetLocalIns(6),
                new PushStringIns(abc, "123testing"),
                new CallPropVoidIns(abc) { PropertyNameIndex = sendMessageQNameIndex, ArgCount = 3 }
            });

            pubKeyVerifyMethod.Body.Code = pubKeyVerCode.ToArray();
            return true;
        }
        public bool DisableHandshake()
        {
            if (!DisableEncryption()) return false;

            ASClass habboCommDemoClass = ABCFiles[2].GetClasses("HabboCommunicationDemo").FirstOrDefault();
            if (habboCommDemoClass == null) return false;
            ASInstance habboCommDemoInstance = habboCommDemoClass.Instance;

            int firstCoerceIndex = 0;
            ASCode initCryptoCode = null;
            int asInterfaceQNameIndex = 0;
            ASMethod initCryptoMethod = null;
            foreach (ASMethod method in habboCommDemoInstance.GetMethods(1, "void"))
            {
                ASParameter parameter = method.Parameters[0];
                if (initCryptoCode == null &&
                    parameter.IsOptional &&
                    parameter.Type.Name == "Event")
                {
                    initCryptoMethod = method;
                    initCryptoCode = method.Body.ParseCode();
                    firstCoerceIndex = initCryptoCode.IndexOf(OPCode.Coerce);
                    asInterfaceQNameIndex = ((CoerceIns)initCryptoCode[firstCoerceIndex]).TypeNameIndex;
                }
                else if (parameter.TypeIndex == asInterfaceQNameIndex)
                {
                    int beforeExitIndex = (initCryptoCode.Count - 6);
                    initCryptoCode.RemoveRange(beforeExitIndex, 5);
                    initCryptoCode.InsertRange(beforeExitIndex, new ASInstruction[]
                    {
                        new GetLocal0Ins(),
                        new GetLocal2Ins(),
                        new CallPropVoidIns(ABCFiles[2])
                        {
                            ArgCount = 1,
                            PropertyNameIndex = method.Trait.QNameIndex
                        }
                    });
                    initCryptoMethod.Body.Code = initCryptoCode.ToArray();
                    return true;
                }
            }
            return false;
        }
        public bool DisableHostChecks()
        {
            ASMethod localHostCheckMethod = ABCFiles[0].Classes[0]
                .GetMethods(1, "Boolean").FirstOrDefault();

            if (localHostCheckMethod == null) return false;
            var hostCheckMethods = new List<ASMethod>();
            hostCheckMethods.Add(localHostCheckMethod);

            ASClass habboClass = ABCFiles[1].GetClasses("Habbo").FirstOrDefault();
            if (habboClass == null) return false;
            ASInstance habboInstance = habboClass.Instance;

            ASMethod remoteHostCheckMethod = habboInstance.GetMethods(2, "Boolean")
                .Where(m => m.Parameters[0].Type.Name == "String" &&
                            m.Parameters[1].Type.Name == "Object")
                .FirstOrDefault();

            if (remoteHostCheckMethod == null) return false;
            hostCheckMethods.Add(remoteHostCheckMethod);

            foreach (ASMethod method in hostCheckMethods)
            {
                ASCode code = method.Body.ParseCode();
                if (!code.StartsWith(OPCode.PushTrue, OPCode.ReturnValue))
                {
                    code.InsertRange(0, new ASInstruction[]
                    {
                        new PushTrueIns(),
                        new ReturnValueIns()
                    });
                    method.Body.Code = code.ToArray();
                }
            }
            return DisableHostChanges();
        }
        public bool EnableDebugLogger(string functionName = null)
        {
            ABCFile abc = ABCFiles[1];

            ASMethod logMethod = null;
            foreach (ASClass @class in abc.Classes)
            {
                if (@class.Traits.Count != 2) continue;
                logMethod = @class.GetMethod(0, "log", "void");
                if (logMethod != null) break;
            }
            if (logMethod == null) return false;

            int externalInterfaceQNameIndex = abc.Pool.GetMultinameIndices("ExternalInterface").FirstOrDefault();
            if (externalInterfaceQNameIndex == 0) return false;

            int availableQNameIndex = abc.Pool.GetMultinameIndices("available").FirstOrDefault();
            if (availableQNameIndex == 0) return false;

            int callQNameIndex = abc.Pool.GetMultinameIndices("call").FirstOrDefault();
            if (callQNameIndex == 0) return false;

            ASCode code = logMethod.Body.ParseCode();
            int startIndex = (code.IndexOf(OPCode.PushScope) + 1);

            var ifNotAvailable = new IfFalseIns() { Offset = 1 };
            ASInstruction jumpExit = code[code.IndexOf(OPCode.ReturnVoid)];

            functionName = (functionName ?? "console.log");
            int functionNameIndex = abc.Pool.AddConstant(functionName, false);

            code.InsertRange(startIndex, new ASInstruction[]
            {
                new GetLexIns(abc) { TypeNameIndex = externalInterfaceQNameIndex },
                new GetPropertyIns(abc) { PropertyNameIndex = availableQNameIndex },
                ifNotAvailable,
                new GetLexIns(abc) { TypeNameIndex = externalInterfaceQNameIndex },
                new PushStringIns(abc, functionNameIndex),
                new GetLocal1Ins(),
                new CallPropVoidIns(abc) { ArgCount = 2, PropertyNameIndex = callQNameIndex }
            });

            code.JumpExits[ifNotAvailable] = jumpExit;
            logMethod.Body.Code = code.ToArray();

            logMethod.Body.MaxStack += 3;
            return true;
        }
        public bool InjectMessageLogger(string functionName = null)
        {
            ASClass coreClass = ABCFiles[1].GetClasses("Core").FirstOrDefault();
            if (coreClass == null) return false;

            ASMethod debugMethod = coreClass.GetMethod(1, "debug", "void");
            if (debugMethod == null) return false;

            debugMethod.Flags |= MethodFlags.NeedRest;
            debugMethod.Parameters.Clear();

            ASCode debugCode = debugMethod.Body.ParseCode();
            for (int i = 0; i < debugCode.Count; i++)
            {
                ASInstruction instruction = debugCode[i];
                if (instruction.OP != OPCode.IfFalse) continue;

                ASInstruction[] block = debugCode.GetJumpBlock((Jumper)instruction);
                debugCode.RemoveRange((i - 1), (block.Length + 1));
                break;
            }

            // 'FlashExternalInterface.logDebug' is the default internal function name.
            if (!string.IsNullOrWhiteSpace(functionName))
            {
                int pushStringIndex = debugCode.IndexOf(OPCode.PushString);
                var pushStringIns = (PushStringIns)debugCode[pushStringIndex];
                pushStringIns.Value = functionName;
            }
            debugMethod.Body.Code = debugCode.ToArray();

            ABCFile abc = ABCFiles[2];
            int coreQNameIndex = abc.Pool.GetMultinameIndices("Core").FirstOrDefault();
            ASMultiname coreQName = abc.Pool.Multinames[coreQNameIndex];

            int debugQNameIndex = abc.Pool.GetMultinameIndices("debug").FirstOrDefault();
            ASMultiname debugQName = abc.Pool.Multinames[debugQNameIndex];

            foreach (MessageItem message in OutMessages.Values)
            {
                ASInstance instance = message.Class.Instance;
                ASMethod arrayMethod = instance.GetMethods(0, "Array").FirstOrDefault();
                if (arrayMethod == null)
                {
                    if (instance.Super.Name != "Object")
                    {
                        instance = abc.GetFirstInstance(instance.Super.Name);
                        arrayMethod = instance.GetMethods(0, "Array").FirstOrDefault();
                    }
                    if (arrayMethod == null) return false;
                }

                if (arrayMethod.Body.Exceptions.Count > 0) continue;
                ASCode code = arrayMethod.Body.ParseCode();

                int returnValIndex = code.IndexOf(OPCode.ReturnValue);
                code.InsertRange(returnValIndex, new ASInstruction[]
                {
                    new DupIns(),
                    new SetLocal1Ins()
                });

                returnValIndex += 2;
                code.InsertRange(returnValIndex, new ASInstruction[]
                {
                    new GetLexIns(abc) { TypeNameIndex = coreQNameIndex },
                    new GetLocal1Ins(),
                    new CallPropVoidIns(abc) { ArgCount = 1, PropertyNameIndex = debugQNameIndex }
                });

                arrayMethod.Body.MaxStack += 3;
                arrayMethod.Body.LocalCount += 1;
                arrayMethod.Body.Code = code.ToArray();
            }
            return true;
        }
        public bool ReplaceRSAKeys(string exponent, string modulus)
        {
            ABCFile abc = ABCFiles[2];
            ASClass keyObfuscatorClass = abc.GetClasses("KeyObfuscator").FirstOrDefault();
            if (keyObfuscatorClass == null) return false;

            int modifyCount = 0;
            foreach (ASMethod method in keyObfuscatorClass.GetMethods(0, "String"))
            {
                int keyIndex = 0;
                switch (method.Trait.Id)
                {
                    // Get Modulus Method
                    case 6:
                    {
                        modifyCount++;
                        keyIndex = abc.Pool.AddConstant(modulus);
                        break;
                    }
                    // Get Exponent Method
                    case 7:
                    {
                        modifyCount++;
                        keyIndex = abc.Pool.AddConstant(exponent);
                        break;
                    }

                    // This is not a method we want to modify, continue enumerating.
                    default: continue;
                }

                ASCode code = method.Body.ParseCode();
                if (code.StartsWith(OPCode.PushString, OPCode.ReturnValue))
                {
                    code.RemoveRange(0, 2);
                }
                code.InsertRange(0, new ASInstruction[]
                {
                        new PushStringIns(abc, keyIndex),
                        new ReturnValueIns()
                });
                method.Body.Code = code.ToArray();
            }
            return (modifyCount == 2);
        }

        private void LoadMessages()
        {
            ABCFile abc = ABCFiles[2];
            ASClass habboMessagesClass = abc.GetClasses("HabboMessages").First();
            ASCode code = habboMessagesClass.Constructor.Body.ParseCode();

            int inMapTypeIndex = habboMessagesClass.Traits[0].QNameIndex;
            int outMapTypeIndex = habboMessagesClass.Traits[1].QNameIndex;

            List<ASInstruction> instructions = code
                .Where(i => i.OP == OPCode.GetLex ||
                            i.OP == OPCode.PushShort ||
                            i.OP == OPCode.PushByte).ToList();

            for (int i = 0; i < instructions.Count; i += 3)
            {
                var getLexInst = (instructions[i + 0] as GetLexIns);
                bool isOutgoing = (getLexInst.TypeNameIndex == outMapTypeIndex);

                var primitive = (instructions[i + 1] as Primitive);
                ushort header = Convert.ToUInt16(primitive.Value);

                getLexInst = (instructions[i + 2] as GetLexIns);
                ASClass messageClass = abc.GetClasses(getLexInst.TypeName.Name).First();

                var message = new MessageItem(messageClass, header, isOutgoing);
                (isOutgoing ? OutMessages : InMessages).Add(header, message);
                _messages.Add(messageClass, message);

                if (header == 4000 && isOutgoing)
                {
                    ASInstance messageInstance = messageClass.Instance;
                    ASMethod toArrayMethod = messageInstance.GetMethods(0, "Array").First();

                    ASCode toArrayCode = toArrayMethod.Body.ParseCode();
                    int index = toArrayCode.IndexOf(OPCode.PushString);

                    if (index != -1)
                    {
                        var pushStringIns = (PushStringIns)toArrayCode[index];
                        _revisionIndex = pushStringIns.ValueIndex;
                    }
                }
            }
        }
        private void SimplifySendCode(ABCFile abc, ASCode sendCode)
        {
            bool isTrimming = true;
            for (int i = 0; i < sendCode.Count; i++)
            {
                ASInstruction instruction = sendCode[i];
                if (!isTrimming && Local.IsValid(instruction.OP))
                {
                    var local = (Local)instruction;
                    int newRegister = (local.Register - 1);
                    if (newRegister < 1) continue;

                    ASInstruction replacement = null;
                    if (Local.IsGetLocal(local.OP))
                    {
                        replacement = new GetLocalIns(newRegister);
                    }
                    else if (Local.IsSetLocal(local.OP))
                    {
                        replacement = new SetLocalIns(newRegister);
                    }
                    sendCode[i] = replacement;
                }
                else
                {
                    if (instruction.OP != OPCode.DebugLine) continue;
                    var debugIns = (DebugLineIns)instruction;
                    if (debugIns.LineNumber != 247) continue;

                    sendCode.RemoveRange(0, (i + 1));
                    int headerNameIndex = abc.Pool.AddConstant("id");
                    int valuesNameIndex = abc.Pool.AddConstant("values");
                    sendCode.InsertRange(0, new ASInstruction[]
                    {
                        new GetLocal0Ins(),
                        new PushScopeIns(),
                        new DebugIns(abc, headerNameIndex, 1, 0),
                        new DebugIns(abc, valuesNameIndex, 1, 1)
                    });
                    isTrimming = false;
                    i = 4;
                }
            }
        }

        private bool DisableEncryption()
        {
            ASClass socketConnClass = ABCFiles[2].GetClasses("SocketConnection").FirstOrDefault();
            if (socketConnClass == null) return false;
            ASInstance socketConnInstance = socketConnClass.Instance;

            ASMethod sendMethod = socketConnInstance.GetMethod(1, "send", "Boolean");
            if (sendMethod == null) return false;

            ASCode sendCode = sendMethod.Body.ParseCode();
            sendCode.Deobfuscate();

            ASInstruction[] writeInstructions = null;
            for (int i = sendCode.Count - 1; i >= 0; i--)
            {
                ASInstruction instruction = sendCode[i];
                if (instruction.OP == OPCode.IfEq)
                {
                    writeInstructions = sendCode.GetJumpBlock((Jumper)instruction);
                    foreach (ASInstruction blockInstruction in writeInstructions)
                    {
                        if (blockInstruction.OP != OPCode.GetLocal) continue;
                        ((GetLocalIns)blockInstruction).Register--;
                        break;
                    }
                    sendCode.RemoveRange(i -= 2, (writeInstructions.Length + 3));
                }
                else if (instruction.OP == OPCode.IfNe)
                {
                    int rc4NullCheckStart = (i - 3);
                    sendCode.RemoveRange(rc4NullCheckStart, sendCode.Count - rc4NullCheckStart);

                    sendCode.AddRange(writeInstructions
                        .Concat(new ASInstruction[]
                        {
                            new PushTrueIns(),
                            new ReturnValueIns()
                        }));
                    break;
                }
            }
            sendMethod.Body.Code = sendCode.ToArray();
            return true;
        }
        private bool DisableHostChanges()
        {
            ABCFile abc = ABCFiles[2];

            ASClass habboCommMngrClass = abc.GetClasses("HabboCommunicationManager").FirstOrDefault();
            if (habboCommMngrClass == null) return false;
            ASInstance habboCommMngrInstance = habboCommMngrClass.Instance;

            ASTrait infoHostSlot = habboCommMngrInstance
                .GetSlotTraits("String").FirstOrDefault();
            if (infoHostSlot == null) return false;

            int getPropertyNameIndex = abc.Pool
                .GetMultinameIndices("getProperty").FirstOrDefault();
            if (getPropertyNameIndex == 0) return false;

            ASMethod initComponentMethod =
                habboCommMngrInstance.GetMethod(0, "initComponent", "void");
            if (initComponentMethod == null) return false;

            string connectMethodName = string.Empty;
            ASCode initComponentCode = initComponentMethod.Body.ParseCode();
            for (int i = initComponentCode.Count - 1; i >= 0; i--)
            {
                ASInstruction instruction = initComponentCode[i];
                if (instruction.OP != OPCode.CallPropVoid) continue;

                var callPropVoidIns = (CallPropVoidIns)instruction;
                connectMethodName = callPropVoidIns.PropertyName.Name;
                break;
            }
            if (string.IsNullOrWhiteSpace(connectMethodName)) return false;

            ASMethod connectMethod = habboCommMngrInstance
                .GetMethod(0, connectMethodName, "void");
            if (connectMethod == null) return false;

            ASCode connectCode = connectMethod.Body.ParseCode();
            connectCode.InsertRange(4, new ASInstruction[]
            {
                new GetLocal0Ins(),
                new FindPropStrictIns(abc, getPropertyNameIndex),
                new PushStringIns(abc, "connection.info.host"),
                new CallPropertyIns(abc, getPropertyNameIndex, 1),
                new InitPropertyIns(abc, infoHostSlot.QNameIndex)
                // Inserts: this.Slot = getProperty("connection.info.host");
                // This portion ensures the host slot value stays vanilla(no prefixes/changes).
            });

            // This portion prevents any suffix from being added to the host slot.
            int magicInverseIndex = abc.Pool.AddConstant(65290);
            foreach (ASInstruction instruction in connectCode)
            {
                if (instruction.OP != OPCode.PushInt) continue;

                var pushIntIns = (PushIntIns)instruction;
                pushIntIns.ValueIndex = magicInverseIndex;
            }
            connectMethod.Body.Code = connectCode.ToArray();
            return true;
        }

        public override void Disassemble(Action<TagItem> callback)
        {
            base.Disassemble(callback);
            LoadMessages();
        }
        protected override void WriteTag(TagItem tag, FlashWriter output)
        {
            if (tag.Kind == TagKind.DoABC)
            {
                var doABCTag = (DoABCTag)tag;
                doABCTag.ABCData = _abcFileTags[doABCTag].ToArray();
            }
            base.WriteTag(tag, output);
        }
        protected override TagItem ReadTag(HeaderRecord header, FlashReader input)
        {
            TagItem tag = base.ReadTag(header, input);
            if (tag.Kind == TagKind.DoABC)
            {
                var doABCTag = (DoABCTag)tag;
                var abcFile = new ABCFile(doABCTag.ABCData);

                _abcFileTags[doABCTag] = abcFile;
                ABCFiles.Add(abcFile);
            }
            return tag;
        }

        public static bool IsValidIdentifier(string value, bool invalidOnSanitized = false)
        {
            value = value.ToLower();
            if (invalidOnSanitized &&
                (value.StartsWith("class_") ||
                value.StartsWith("iinterface_") ||
                value.StartsWith("namespace_") ||
                value.StartsWith("method_") ||
                value.StartsWith("constant_") ||
                value.StartsWith("slot_") ||
                value.StartsWith("param")))
            {
                return false;
            }

            return (!value.Contains("_-") &&
                !_reservedNames.Contains(value.Trim()));
        }
    }
    public class MessageItem
    {
        public ushort Header { get; }
        public bool IsOutgoing { get; }
        public string SHA1 { get; private set; }

        public ASClass Class { get; }
        public ASClass Parser { get; }
        public List<MessageReference> References { get; }

        public MessageItem()
        {
            References = new List<MessageReference>();
        }
        public MessageItem(ASClass @class, ushort header, bool isOutgoing)
            : this()
        {
            Header = header;
            IsOutgoing = isOutgoing;

            Class = @class;
            if (!IsOutgoing)
            {
                Parser = GetMessageParser(Class);
            }
        }

        public string GenerateSHA1()
        {
            using (var sha1 = new SHA1Managed())
            using (var sha1Mem = new MemoryStream())
            using (var crypto = new CryptoStream(sha1Mem, sha1, CryptoStreamMode.Write))
            using (var output = new BinaryWriter(crypto))
            {
                Write(output);
                crypto.FlushFinalBlock();

                SHA1 = BitConverter.ToString(sha1.Hash)
                    .Replace("-", string.Empty).ToLower();

                return SHA1;
            }
        }
        private ASClass GetMessageParser(ASClass @class)
        {
            ABCFile abc = @class.GetABC();
            ASInstance instance = @class.Instance;

            ASInstance superInstance = abc.GetFirstInstance(instance.Super.Name);
            ASMultiname parserType = superInstance.GetMethod(0, "parser").ReturnType;

            IEnumerable<ASMethod> methods = instance.GetMethods()
                .Concat(new[] { instance.Constructor });

            foreach (ASMethod method in methods)
            {
                ASCode code = method.Body.ParseCode();
                foreach (ASInstruction instruction in code)
                {
                    ASMultiname multiname = null;
                    if (instruction.OP == OPCode.FindPropStrict)
                    {
                        var findPropStrictIns = (FindPropStrictIns)instruction;
                        multiname = findPropStrictIns.PropertyName;
                    }
                    else if (instruction.OP == OPCode.GetLex)
                    {
                        var getLexIns = (GetLexIns)instruction;
                        multiname = getLexIns.TypeName;
                    }
                    else continue;

                    foreach (ASClass refClass in abc.GetClasses(multiname.Name))
                    {
                        ASInstance refInstance = refClass.Instance;
                        if (refInstance.ContainsInterface(parserType.Name))
                        {
                            return refClass;
                        }
                    }
                }
            }
            return null;
        }

        private void Write(BinaryWriter output)
        {
            output.Write(IsOutgoing);
            string name = Class.Instance.QName.Name;
            if (!HGame.IsValidIdentifier(name, true))
            {
                WriteMethod(output, Class.Constructor);
                WriteMethod(output, Class.Instance.Constructor);

                WriteContainer(output, Class);
                WriteContainer(output, Class.Instance);

                output.Write(References.Count);
                foreach (MessageReference reference in References)
                {
                    output.Write(reference.IsAnonymous);
                    output.Write(reference.ClassReferenceRank);
                    if (IsOutgoing || reference.InCallback == null)
                    {
                        output.Write(reference.MethodReferenceRank);
                    }
                    if (reference.InCallback != null)
                    {
                        WriteMethod(output, reference.InCallback);
                    }
                    else
                    {
                        WriteMethod(output, reference.FromMethod);
                    }
                    WriteContainer(output, reference.FromClass.Instance, false);
                }
                if (!IsOutgoing)
                {
                    WriteContainer(output, Parser.Instance);
                }
            }
            else output.Write(name);
        }
        private void WriteTrait(BinaryWriter output, ASTrait trait)
        {
            output.Write(trait.Id);
            output.Write(trait.IsStatic);
            WriteMultiname(output, trait.QName);
            output.Write((byte)trait.Attributes);

            output.Write((byte)trait.Kind);
            switch (trait.Kind)
            {
                case TraitKind.Slot:
                case TraitKind.Constant:
                {
                    WriteMultiname(output, trait.Type);
                    if (trait.Value != null)
                    {
                        output.Write((byte)trait.ValueKind);
                        WriteValue(output, trait.Value, trait.ValueKind);
                    }
                    break;
                }
                case TraitKind.Method:
                case TraitKind.Getter:
                case TraitKind.Setter:
                {
                    WriteMethod(output, trait.Method);
                    break;
                }
            }
        }
        private void WriteMethod(BinaryWriter output, ASMethod method)
        {
            output.Write(method.IsConstructor);
            if (!method.IsConstructor)
            {
                WriteMultiname(output, method.ReturnType);
            }

            output.Write(method.Parameters.Count);
            foreach (ASParameter parameter in method.Parameters)
            {
                WriteMultiname(output, parameter.Type);
                if (!string.IsNullOrWhiteSpace(parameter.Name) &&
                    HGame.IsValidIdentifier(parameter.Name, true))
                {
                    output.Write(parameter.Name);
                }

                output.Write(parameter.IsOptional);
                if (parameter.IsOptional)
                {
                    output.Write((byte)parameter.ValueKind);
                    WriteValue(output, parameter.Value, parameter.ValueKind);
                }
            }

            ASCode code = method.Body.ParseCode();
            foreach (OPCode op in code.GetOPGroups().Keys)
            {
                if (op == OPCode.Dup) continue;
                if (op == OPCode.Pop) continue;
                if (op == OPCode.PushFalse) continue;
                if (op == OPCode.PushTrue) continue;
                if (op == OPCode.IfFalse) continue;
                if (op == OPCode.IfTrue) continue;
                output.Write((byte)op);
            }
        }
        private void WriteMultiname(BinaryWriter output, ASMultiname multiname)
        {
            if (multiname?.Kind == MultinameKind.TypeName)
            {
                WriteMultiname(output, multiname.QName);
                output.Write(multiname.TypeIndices.Count);
                foreach (ASMultiname type in multiname.GetTypes())
                {
                    WriteMultiname(output, type);
                }
            }
            else if (multiname == null ||
                HGame.IsValidIdentifier(multiname.Name, true))
            {
                output.Write((multiname?.Name ?? "*"));
            }
        }
        private void WriteValue(BinaryWriter output, object value, ConstantKind kind)
        {
            switch (kind)
            {
                case ConstantKind.Double:
                output.Write((double)value);
                break;

                case ConstantKind.Integer:
                output.Write((int)value);
                break;

                case ConstantKind.UInteger:
                output.Write((uint)value);
                break;

                case ConstantKind.String:
                output.Write((string)value);
                break;

                case ConstantKind.Null:
                output.Write("null");
                break;

                case ConstantKind.True:
                output.Write(true);
                break;

                case ConstantKind.False:
                output.Write(false);
                break;
            }
        }
        private void WriteContainer(BinaryWriter output, ASContainer container, bool includeTraits = true)
        {
            output.Write(container.IsStatic);
            if (includeTraits)
            {
                output.Write(container.Traits.Count);
                container.Traits.ForEach(t => WriteTrait(output, t));
            }
        }
    }
    public class MessageReference
    {
        /// <summary>
        /// Gets the class that contains a method/instruction referencing the message.
        /// </summary>
        public ASClass FromClass { get; }
        /// <summary>
        /// Gets or sets the method that contains the instruction referencing the message.
        /// </summary>
        public ASMethod FromMethod { get; set; }
        /// <summary>
        /// Gets or sets the method that is passed as a parameter to the Incoming message handler class.
        /// </summary>
        public ASMethod InCallback { get; set; }

        /// <summary>
        /// Gets or sets whether the method body is owned by an anonymous method.
        /// </summary>
        public bool IsAnonymous { get; set; }
        /// <summary>
        /// Gets or sets the index at which point the message is being referneced by an instruction.
        /// </summary>
        public int ReferencedAt { get; set; }

        /// <summary>
        /// Gets or sets the rank of the reference relative to where the method referencing the message is located.
        /// </summary>
        public int ClassReferenceRank { get; set; }
        /// <summary>
        /// Gets or sets the rank of the reference relative to the order in which the message was referenced by an instruction.
        /// </summary>
        public int MethodReferenceRank { get; set; }

        public MessageReference(ASClass fromClass)
        {
            FromClass = fromClass;
        }
    }
}