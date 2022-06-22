using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace common_tool.Tools.Generate
{
    public class GenerateTemplate : ActionBase
    {
        readonly string _commonDir = "Common";
        readonly string _controllerDir = "Controller";
        InfraTemplateConfig _config = null;

        public GenerateTemplate(Parameter param) : base(param)
        {
        }

        public override void Run()
        {
            if (_param._dicActionParam.ContainsKey("--target-path") == false)
            {
                throw new Exception($"not found parameter - \"--target-path\"");
            }

            string clientPath = string.Empty;
            if (_param._dicActionParam.ContainsKey("--client-path") == true)
            {
                clientPath = new DirectoryInfo(_param._dicActionParam["--client-path"]).FullName;
            }

            var targetDir = new DirectoryInfo(_param._dicActionParam["--target-path"]);
            //경로 이름(Mail, Friend)
            string templateType = Helpers.SplitPath(targetDir.FullName)[Helpers.SplitPath(targetDir.FullName).Length - 2];
            //경로 (GameBaseMailBox..)
            string templateName = Helpers.SplitPath(targetDir.FullName)[Helpers.SplitPath(targetDir.FullName).Length - 1];

            // 프로젝트 생성
            string filePath = Path.Combine(targetDir.FullName, templateName + ".csproj");
            if (File.Exists(filePath) == false)
            {
                string command = "new classlib -o " + targetDir.FullName;
                try
                {
                    Process.Start("dotnet", command).WaitForExit();
                }
                catch (Exception)
                {
                    Process.Start("dotnet.exe", command).WaitForExit();
                }


                GenerateProjectFile(targetDir.FullName, templateType, templateName);
                GenerateInfrastructureFile(targetDir.FullName, templateType, templateName);
                File.Delete(Path.Combine(targetDir.FullName, "Class1.cs"));
            }

            FileInfo[] files = targetDir.GetFiles();
            foreach (FileInfo file in files)
            {
                if (file.Extension.CompareTo(".json") != 0 || file.Name.StartsWith("infrastructure-config") == false)
                {
                    continue;
                }

                using (StreamReader r = new StreamReader(file.FullName))
                {
                    string json = r.ReadToEnd();
                    _config = JsonConvert.DeserializeObject<InfraTemplateConfig>(json);
                }

                if (_config == null)
                {
                    throw new Exception($"failed to read file. {file.FullName}");
                }

                // 코드 생성 폴더 생성
                var commonPath = Path.Combine(targetDir.FullName, _commonDir);
                if (Directory.Exists(commonPath) == false)
                {
                    Directory.CreateDirectory(commonPath);
                    Console.WriteLine($"Create Directory: {commonPath}");
                }

                var namespaceValue = Helpers.GetNameSpace(commonPath);
                GenerateModel(commonPath, true, namespaceValue);
                GenerateProtocol(commonPath, true, namespaceValue);
                GeneratePacket(commonPath, true, namespaceValue);

                if (string.IsNullOrEmpty(clientPath) == false)
                {
                    var clientCommonPath = Path.Combine(clientPath, _commonDir);
                    if (Directory.Exists(clientCommonPath) == false)
                    {
                        Directory.CreateDirectory(clientCommonPath);
                    }

                    GenerateModel(clientCommonPath, false, namespaceValue);
                    GenerateProtocol(clientCommonPath, false, namespaceValue);
                    GeneratePacket(clientCommonPath, false, namespaceValue);
                }

                GenerateController(targetDir.FullName, true);
                GenerateTemplateFile(targetDir.FullName, true);
                GenerateDefine(targetDir.FullName, true);
                GenerateImpl(targetDir.FullName, true);

                if (string.IsNullOrEmpty(clientPath) == false)
                {
                    GenerateController(clientPath, false);
                    GenerateTemplateFile(clientPath, false);
                    GenerateDefine(clientPath, false);
                    GenerateImpl(clientPath, false);
                }
            }
        }
        void GenerateController(string targetDir, bool defineServer)
        {
            if (_config.protocols.Count == 0)
            {
                return;
            }

            var controllerPath = Path.Combine(targetDir, _controllerDir);
            if (Directory.Exists(controllerPath) == false)
            {
                Directory.CreateDirectory(controllerPath);
                Console.WriteLine($"Create Directory: {controllerPath}");
            }

            var namespaceValue = Helpers.GetNameSpace(targetDir);
            var commonNamespaceValue = Helpers.GetNameSpace(Path.Combine(targetDir, _commonDir));
            foreach (var protocol in _config.protocols)
            {
                string filePath = Path.Combine(controllerPath, protocol.name + "Controller.cs");
                using (var streamWriter = new StreamWriter(filePath))
                {
                    if (defineServer == true)
                        streamWriter.WriteLine("#define SERVER");

                    streamWriter.WriteLine("using System;");
                    streamWriter.WriteLine("using System.Collections.Generic;");
                    streamWriter.WriteLine("using Service.Core;");
                    streamWriter.WriteLine("using Service.Net;");
                    streamWriter.WriteLine("using GameBase.Template.GameBase;");
                    streamWriter.WriteLine("using GameBase.Template.GameBase.Common;");
                    streamWriter.WriteLine("using GameBase.{0};", commonNamespaceValue);
                    streamWriter.WriteLine();

                    streamWriter.WriteLine("namespace GameBase.{0}", namespaceValue);
                    streamWriter.WriteLine("{");
                    streamWriter.WriteLine("\tpublic partial class {0}Template", _config.templateName);
                    streamWriter.WriteLine("\t{");
                    switch(protocol.method.ToLower())
                    {
                        case "noti":
                            {
                                streamWriter.WriteLine("\t\tpublic void ON_{0}_NOTI_CALLBACK(ImplObject userObject, PACKET_{1}_NOTI packet)", protocol.name, protocol.name);
                                streamWriter.WriteLine("\t\t{");
                                streamWriter.WriteLine("\t\t\t");
                                streamWriter.WriteLine("\t\t}");
                            }
                            break;
                        case "react":
                            streamWriter.WriteLine("\t\tpublic void ON_{0}_REQ_CALLBACK(ImplObject userObject, PACKET_{1}_REQ packet)", protocol.name, protocol.name);
                            streamWriter.WriteLine("\t\t{");
                            streamWriter.WriteLine("\t\t}");
                            streamWriter.WriteLine("\t\tpublic void ON_{0}_RES_CALLBACK(ImplObject userObject, PACKET_{1}_RES packet)", protocol.name, protocol.name);
                            streamWriter.WriteLine("\t\t{");
                            streamWriter.WriteLine("\t\t}");
                            break;
                    }
                    streamWriter.WriteLine("\t}");
                    streamWriter.WriteLine("}");

                }
            }
        }
        void GenerateTemplateFile(string targetDir, bool defineServer)
        {
            string filePath = Path.Combine(targetDir, _config.templateName + "Template.cs");
            if (File.Exists(filePath)==true)
            {
                return;
            }

            var namespaceValue = Helpers.GetNameSpace(targetDir);
            var commonNamespaceValue = Helpers.GetNameSpace(Path.Combine(targetDir, _commonDir));
            var words = namespaceValue.Split(".");

            using (var streamWriter = new StreamWriter(filePath))
            {
                if (defineServer == true)
                    streamWriter.WriteLine("#define SERVER");

                streamWriter.WriteLine("using System;");
                streamWriter.WriteLine("using System.Collections.Generic;");
                streamWriter.WriteLine("using Service.Core;");
                streamWriter.WriteLine("using Service.Net;");
                streamWriter.WriteLine("using GameBase.Template.GameBase;");
                streamWriter.WriteLine("using GameBase.Template.GameBase.Common;");
                streamWriter.WriteLine("using GameBase.{0};", commonNamespaceValue);
                streamWriter.WriteLine();
                streamWriter.WriteLine("namespace GameBase.{0}", namespaceValue);
                streamWriter.WriteLine("{");

                streamWriter.WriteLine("\tpublic partial class {0}Template : {1}Template", _config.templateName, words[1]);
                streamWriter.WriteLine("\t{");
                streamWriter.WriteLine("\t\tImplObject _obj = null;");
                streamWriter.WriteLine("\t\tstatic {0}Impl _Impl = null", _config.templateName);
                streamWriter.WriteLine("\t\tpublic override void Init(TemplateConfig config)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\tbase.Init(config);");
                streamWriter.WriteLine("\t\t\t_Impl = new {0}Impl(null);", _config.templateName);
                streamWriter.WriteLine("\t\t\t//OnLoadData(config)");
                streamWriter.WriteLine("\t\t\t// TODO : 서버 기동시 실행할 템플릿 관련 로직을 아래에 작성");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic override void OnLoadData(TemplateConfig config)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\t// TODO : 로드할 데이터를 연결");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic override void OnClientCreate(ImplObject userObject)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\t_obj = userObject;");
                streamWriter.WriteLine("\t\t\tswitch ((ObjectType)userObject.GetObjectID())");
                streamWriter.WriteLine("\t\t\t{");
                streamWriter.WriteLine("\t\t\t\tcase ObjectType.Master:");
                streamWriter.WriteLine("\t\t\t\t\t{");
                streamWriter.WriteLine("\t\t\t\t\t\t_obj.{0}Impl = new {1}MasterImpl(_obj);", words[1], _config.templateName);
                streamWriter.WriteLine("\t\t\t\t\t}");
                streamWriter.WriteLine("\t\t\t\t\tbreak;");
                streamWriter.WriteLine("\t\t\t\tcase ObjectType.User:");
                streamWriter.WriteLine("\t\t\t\t\t{");
                streamWriter.WriteLine("\t\t\t\t\t\t_obj.{0}Impl = new {1}UserImpl(_obj);", words[1], _config.templateName);
                streamWriter.WriteLine("\t\t\t\t\t}");
                streamWriter.WriteLine("\t\t\t\t\tbreak;");
                streamWriter.WriteLine("\t\t\t\tcase ObjectType.Login:");
                streamWriter.WriteLine("\t\t\t\t\t{");
                streamWriter.WriteLine("\t\t\t\t\t\t_obj.{0}Impl = new {1}LoginImpl(_obj);", words[1], _config.templateName);
                streamWriter.WriteLine("\t\t\t\t\t}");
                streamWriter.WriteLine("\t\t\t\t\tbreak;");
                streamWriter.WriteLine("\t\t\t\tcase ObjectType.Game:");
                streamWriter.WriteLine("\t\t\t\t\t{");
                streamWriter.WriteLine("\t\t\t\t\t\t_obj.{0}Impl = new {1}GameImpl(_obj);", words[1], _config.templateName);
                streamWriter.WriteLine("\t\t\t\t\t}");
                streamWriter.WriteLine("\t\t\t\t\tbreak;");
                streamWriter.WriteLine("\t\t\t}");
                streamWriter.WriteLine("\t\t\t// TODO : 유저의 최초 생성시 필요한 DB관련 로직을 작성");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic T Get{0}Impl<T>() where T : {1}Impl", _config.templateName, words[1]);
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\treturn _obj.{0}Impl as T", words[1]);
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic static {0}Impl Get{1}Impl<T>()", _config.templateName, _config.templateName);
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\treturn _Impl;");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic virtual void OnTemplateUpdate(float dt)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\t// TODO : 템플릿 업데이트 사항 작성(유저X)");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic override void OnClientUpdate(float dt)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\t// TODO : 템플릿 업데이트 사항 작성");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic override void OnClientDelete(ImplObject userObject)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\t// TODO : 계정 초기화시 사용 템플릿에 보유한 내역듣 삭제");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic override (List<ItemBaseInfo> listItemInfo, List<QuestCompleteParam> listQuestCompleteParam) OnAddItem(ImplObject userObject, int itemId, long value, int parentItemId, int groupIndex)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\treturn (null, null);");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic override (List<ItemBaseInfo> listItemInfo, List<QuestCompleteParam> listQuestCompleteParam) OnDeleteItem(ImplObject userObject, int itemId, long value, int parentItemId, int groupIndex)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\treturn (null, null);");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic override (List<ItemBaseInfo> listItemInfo, List<QuestCompleteParam> listQuestCompleteParam) AddRandomReward(ImplObject userObject, int classId, int grade, int kind, long value, int parentItemId, int groupIndex)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\treturn (null, null);");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic override bool OnHasItemId(ImplObject userObject, int itemId)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\treturn false;");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic override bool OnHasItemType(ImplObject userObject, int itemType)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\treturn false;");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic override bool OnHasItemSubType(ImplObject userObject, int subType)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\treturn false;");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine("\t}");
                streamWriter.WriteLine("}");
            }
        }
        void GenerateDefine(string targetDir, bool defineServer)
        {
            string filePath = Path.Combine(targetDir, _config.templateName + "Define.cs");
            if (File.Exists(filePath) == true)
            {
                return;
            }

            var namespaceValue = Helpers.GetNameSpace(targetDir);
            var commonNamespaceValue = Helpers.GetNameSpace(Path.Combine(targetDir, _commonDir));
            using (var streamWriter = new StreamWriter(filePath))
            {
                if (defineServer == true)
                    streamWriter.WriteLine("#define SERVER");

                streamWriter.WriteLine("using System;");
                streamWriter.WriteLine();

                streamWriter.WriteLine("namespace GameBase.{0}", namespaceValue);
                streamWriter.WriteLine("{");
                streamWriter.WriteLine("\t// TODO : 템플릿에서 사용할 열거형을 정의합니다.");
                streamWriter.WriteLine("}");
            }
        }
        void GenerateImpl(string targetDir, bool defineServer)
        {
            string filePath = Path.Combine(targetDir, _config.templateName + "Impl.cs");
            if (File.Exists(filePath) == true)
            {
                return;
            }

            var namespaceValue = Helpers.GetNameSpace(targetDir);
            var commonNamespaceValue = Helpers.GetNameSpace(Path.Combine(targetDir, _commonDir));
            var words = namespaceValue.Split(".");

            using (var streamWriter = new StreamWriter(filePath))
            {
                if (defineServer == true)
                    streamWriter.WriteLine("#define SERVER");

                streamWriter.WriteLine("using System;");
                streamWriter.WriteLine("using System.Collections.Generic;");
                streamWriter.WriteLine("using Service.Core;");
                streamWriter.WriteLine("using Service.Net;");
                streamWriter.WriteLine("using GameBase.Template.GameBase;");
                streamWriter.WriteLine("using GameBase.Template.GameBase.Common;");
                streamWriter.WriteLine("using GameBase.{0};", commonNamespaceValue);
                streamWriter.WriteLine();

                streamWriter.WriteLine("namespace GameBase.{0}", namespaceValue);
                streamWriter.WriteLine("{");
                streamWriter.WriteLine("\tpublic partial class {0}Impl : {1}Impl", _config.templateName, words[1]);
                streamWriter.WriteLine("\t{");
                streamWriter.WriteLine("\t\tpublic {0}Impl(ServerType type) : base(type){{}}", _config.templateName);
                streamWriter.WriteLine("\t\t// TODO : 서버에서 사용 될 변수 선언 및 함수 구현");
                streamWriter.WriteLine("\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\tpublic partial class {0}MasterImpl : {1}Impl", _config.templateName, words[1]);
                streamWriter.WriteLine("\t{");
                streamWriter.WriteLine("\t\tpublic {0}MasterImpl(ImplObject obj) : base(obj){{}}", _config.templateName);
                streamWriter.WriteLine("\t\t// TODO : ImplObject에서 사용 될 변수 선언 및 함수 구현");
                streamWriter.WriteLine("\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\tpublic partial class {0}UserImpl : {1}Impl", _config.templateName, words[1]);
                streamWriter.WriteLine("\t{");
                streamWriter.WriteLine("\t\tpublic {0}UserImpl(ImplObject obj) : base(obj){{}}", _config.templateName);
                streamWriter.WriteLine("\t\t// TODO : ImplObject에서 사용 될 변수 선언 및 함수 구현");
                streamWriter.WriteLine("\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\tpublic partial class {0}LoginImpl : {1}Impl", _config.templateName, words[1]);
                streamWriter.WriteLine("\t{");
                streamWriter.WriteLine("\t\tpublic {0}LoginImpl(ImplObject obj) : base(obj){{}}", _config.templateName);
                streamWriter.WriteLine("\t\t// TODO : ImplObject에서 사용 될 변수 선언 및 함수 구현");
                streamWriter.WriteLine("\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\tpublic partial class {0}GameImpl : {1}Impl", _config.templateName, words[1]);
                streamWriter.WriteLine("\t{");
                streamWriter.WriteLine("\t\tpublic {0}GameImpl(ImplObject obj) : base(obj){{}}", _config.templateName);
                streamWriter.WriteLine("\t\t// TODO : ImplObject에서 사용 될 변수 선언 및 함수 구현");
                streamWriter.WriteLine("\t}");
                streamWriter.WriteLine("}");
            }
        }
        void GenerateModel(string targetDir, bool defineServer, string namespaceValue)
        {
            string filePath = Path.Combine(targetDir, _config.templateName + "Model.cs");
            using (var streamWriter = new StreamWriter(filePath))
            {
                if (defineServer == true)
                    streamWriter.WriteLine("#define SERVER");

                streamWriter.WriteLine("using System;");
                streamWriter.WriteLine("using System.Collections.Generic;");
                streamWriter.WriteLine("using System.Reflection;");
                streamWriter.WriteLine("using System.Numerics;");
                streamWriter.WriteLine("using Service.Net;");
                streamWriter.WriteLine("using Service.Core;");
                streamWriter.WriteLine();

                streamWriter.WriteLine("namespace GameBase.{0}", namespaceValue);
                streamWriter.WriteLine("{");
                foreach (var model in _config.models)
                {
                    streamWriter.WriteLine("\tpublic class {0} : IPacketSerializable", model.name);
                    streamWriter.WriteLine("\t{");
                    foreach (var member in model.members)
                    {
                        streamWriter.WriteLine("\t\t/// <sumary>");
                        streamWriter.WriteLine("\t\t/// {0}", member.comment);
                        streamWriter.WriteLine("\t\t/// </sumary>");
                        if (member.type.CompareTo("string") == 0)
                        {
                            streamWriter.WriteLine("\t\tpublic {0} {1} = string.Empty;", member.type, member.name);
                        }
                        else
                        {
                            streamWriter.WriteLine("\t\tpublic {0} {1} = new {2}();", member.type, member.name, member.type);
                        }
                    }
                    streamWriter.WriteLine("\t\tpublic void Serialize(Packet packet)");
                    streamWriter.WriteLine("\t\t{");
                    foreach (var member in model.members)
                    {
                        member.type = member.type.Replace(" ", "");
                        member.name = member.name.Replace(" ", "");
                        if (member.type.StartsWith("List<") == true)
                        {
                            streamWriter.WriteLine("\t\t\tint length{0} = ({1} == null) ? 0 : {2}.Count;", member.name, member.name, member.name);
                            streamWriter.WriteLine("\t\t\tpacket.Write(length{0});", member.name);
                            streamWriter.WriteLine("\t\t\tfor (int i = 0; i < length{0}; ++i)", member.name);
                            streamWriter.WriteLine("\t\t\t{");
                            //string tempType = member.type.Substring(member.type.IndexOf('<') + 1, member.type.IndexOf('>') - member.type.IndexOf('<') - 1);
                            streamWriter.WriteLine("\t\t\t\tpacket.Write({0}[i]);", member.name);
                            streamWriter.WriteLine("\t\t\t}");

                        }
                        else if (member.type.StartsWith("Dictionary<") == true)
                        {
                            streamWriter.WriteLine("\t\t\tpacket.Write({0}.Count);", member.name);
                            streamWriter.WriteLine("\t\t\tforeach (var pair in {0})", member.name);
                            streamWriter.WriteLine("\t\t\t{");
                            streamWriter.WriteLine("\t\t\t\tpacket.Write(pair.Key);");
                            streamWriter.WriteLine("\t\t\t\tpacket.Write(pair.Value);");
                            streamWriter.WriteLine("\t\t\t}");
                        }
                        else
                        {
                            streamWriter.WriteLine("\t\t\tpacket.Write({0});", member.name);
                        }
                    }
                    streamWriter.WriteLine("\t\t}");
                    streamWriter.WriteLine("\t\tpublic void Deserialize(Packet packet)");
                    streamWriter.WriteLine("\t\t{");
                    foreach (var member in model.members)
                    {
                        member.type = member.type.Replace(" ","");
                        member.name = member.name.Replace(" ", "");
                        if (member.type.StartsWith("List<") == true)
                        {
                            streamWriter.WriteLine("\t\t\tint length{0} = ({1} == null) ? 0 : {2}.Count;", member.name, member.name, member.name);
                            streamWriter.WriteLine("\t\t\tpacket.Read(ref length{0});", member.name);
                            streamWriter.WriteLine("\t\t\tfor (int i = 0; i < length{0}; ++i)", member.name);
                            streamWriter.WriteLine("\t\t\t{");
                            string tempType = member.type.Substring(member.type.IndexOf('<') + 1, member.type.IndexOf('>') - member.type.IndexOf('<') - 1);
                            if (tempType.CompareTo("string") == 0)
                            {
                                streamWriter.WriteLine("\t\t\t\tstring element = string.Empty");
                            }
                            else
                            {
                                streamWriter.WriteLine("\t\t\t\t{0} element = new {1}();", tempType, tempType);
                            }
                            streamWriter.WriteLine("\t\t\t\tpacket.Read({0});", GetModelDerializeInputParameter(tempType, "element"));
                            streamWriter.WriteLine("\t\t\t\t{0}.Add(element);", member.name);
                            streamWriter.WriteLine("\t\t\t}");

                        }
                        else if (member.type.StartsWith("Dictionary<") == true)
                        {
                            string[] words = member.type.Substring(member.type.IndexOf('<') + 1, member.type.IndexOf('>') - member.type.IndexOf('<') - 1).Split(",");
                            string dicKey = words[0];
                            string dicValue = words[1];

                            streamWriter.WriteLine("\t\t\tint length{0} = 0;", member.name);
                            streamWriter.WriteLine("\t\t\tpacket.Read(ref length{0});", member.name);
                            streamWriter.WriteLine("\t\t\tfor (int i = 0; i < length{0}; ++i)", member.name);
                            streamWriter.WriteLine("\t\t\t{");
                            if (dicKey.CompareTo("string") == 0)
                                streamWriter.WriteLine("\t\t\t\tstring tempKey = string.Empty;", dicKey, dicKey);
                            else
                                streamWriter.WriteLine("\t\t\t\t{0} tempKey = new {1}();", dicKey, dicKey);
                            if (dicValue.CompareTo("string") == 0)
                                streamWriter.WriteLine("\t\t\t\tstring tempValue = string.Empty;", dicValue, dicValue);
                            else
                                streamWriter.WriteLine("\t\t\t\t{0} tempValue = new {1}();", dicValue, dicValue);
                            streamWriter.WriteLine("\t\t\t\tpacket.Read(ref tempKey);");
                            streamWriter.WriteLine("\t\t\t\tpacket.Read(ref tempValue);");
                            streamWriter.WriteLine("\t\t\t\t{0}.Add(tempKey, tempValue);", member.name);
                            streamWriter.WriteLine("\t\t\t}");
                        }
                        else
                        {
                            streamWriter.WriteLine("\t\t\tpacket.Read({0});", GetModelDerializeInputParameter(member.type, member.name));
                        }
                    }
                    streamWriter.WriteLine("\t\t}");
                    streamWriter.WriteLine("\t\tpublic string GetLog()");
                    streamWriter.WriteLine("\t\t{");
                    streamWriter.WriteLine("\t\t\tstring log = \"\";");
                    streamWriter.WriteLine("\t\t\tFieldInfo[] fields = this.GetType().GetFields();");
                    streamWriter.WriteLine("\t\t\tforeach (FieldInfo field in fields)");
                    streamWriter.WriteLine("\t\t\t{");
                    streamWriter.WriteLine("\t\t\t\tlog += string.Format(\"{{0}}={{1}}\\r\\n\", field.Name, field.GetValue(this).ToString());");
                    streamWriter.WriteLine("\t\t\t}");
                    streamWriter.WriteLine("\t\t\treturn log;");
                    streamWriter.WriteLine("\t\t}");
                    streamWriter.WriteLine("\t}");
                }
                streamWriter.WriteLine("}");
            }
        }
        void GenerateProtocol(string targetDir, bool defineServer, string namespaceValue)
        {
            string filePath = Path.Combine(targetDir, _config.templateName + "Protocol.cs");
            using (var streamWriter = new StreamWriter(filePath))
            {
                if (defineServer == true)
                    streamWriter.WriteLine("#define SERVER");

                streamWriter.WriteLine("using System;");
                streamWriter.WriteLine("using System.Collections.Generic;");
                streamWriter.WriteLine("using Service.Net;");
                streamWriter.WriteLine("using Service.Core;");
                streamWriter.WriteLine("using GameBase.Template.GameBase;");
                streamWriter.WriteLine();

                streamWriter.WriteLine("namespace GameBase.{0}", namespaceValue);
                streamWriter.WriteLine("{");
                streamWriter.WriteLine("\tpublic partial class {0}Protocol", _config.templateName);
                streamWriter.WriteLine("\t{");

                streamWriter.WriteLine("\t\tDictionary<ushort, ControllerDelegate> MessageControllers = new Dictionary<ushort, ControllerDelegate>();");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic {0}Protocol()", _config.templateName);
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\tInit();");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tvoid Init()");
                streamWriter.WriteLine("\t\t{");
                foreach (var protocol in _config.protocols)
                {
                    switch (protocol.method.ToLower())
                    {
                        case "noti":
                            {
                                streamWriter.WriteLine("\t\t\tMessageControllers.Add(PACKET_{0}_NOTI.ProtocolId, {1}_NOTI_CONTROLLER);", protocol.name, protocol.name);
                            }
                            break;
                        case "react":
                            {
                                streamWriter.WriteLine("\t\t\tMessageControllers.Add(PACKET_{0}_REQ.ProtocolId, {1}_REQ_CONTROLLER);", protocol.name, protocol.name);
                                streamWriter.WriteLine("\t\t\tMessageControllers.Add(PACKET_{0}_RES.ProtocolId, {1}_RES_CONTROLLER);", protocol.name, protocol.name);
                            }
                            break;
                        default:
                            throw new Exception($"failed to generate protocol. {protocol.method}");
                    }
                }
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic virtual bool OnPacket(ImplObject userObject, ushort protocolId, Packet packet)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\tControllerDelegate controllerCallback;");
                streamWriter.WriteLine("\t\t\tif(MessageControllers.TryGetValue(protocolId, out controllerCallback) == false)");
                streamWriter.WriteLine("\t\t\t{");
                streamWriter.WriteLine("\t\t\t\treturn false;");
                streamWriter.WriteLine("\t\t\t}");
                streamWriter.WriteLine("\t\t\tcontrollerCallback(userObject, packet);");
                streamWriter.WriteLine("\t\t\treturn true;");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();

                foreach (var protocol in _config.protocols)
                {
                    if (protocol.protocolType.ToLower() == "admin")
                    {
                        streamWriter.WriteLine("#if SERVER");
                    }
                    switch (protocol.method.ToLower())
                    {
                        case "noti":
                            {
                                streamWriter.WriteLine("\t\tpublic delegate void {0}_NOTI_CALLBACK(ImplObject userObject, PACKET_{1}_NOTI packet);", protocol.name, protocol.name);
                                streamWriter.WriteLine("\t\tpublic {0}_NOTI_CALLBACK ON_{1}_NOTI_CALLBACK;", protocol.name, protocol.name);
                                streamWriter.WriteLine("\t\tpublic void {0}_NOTI_CONTROLLER(ImplObject obj, Packet packet)", protocol.name);
                                streamWriter.WriteLine("\t\t{");
                                streamWriter.WriteLine("\t\t\tPACKET_{0}_NOTI recvPacket = new PACKET_{1}_NOTI();", protocol.name, protocol.name);
                                streamWriter.WriteLine("\t\t\trecvPacket.Deserialize(packet);");
                                streamWriter.WriteLine("\t\t\tON_{0}_NOTI_CALLBACK(obj, recvPacket);", protocol.name);
                                streamWriter.WriteLine("\t\t}");
                            }
                            break;
                        case "react":
                            {
                                streamWriter.WriteLine("\t\tpublic delegate void {0}_REQ_CALLBACK(ImplObject userObject, PACKET_{1}_REQ packet);", protocol.name, protocol.name);
                                streamWriter.WriteLine("\t\tpublic {0}_REQ_CALLBACK ON_{1}_REQ_CALLBACK;", protocol.name, protocol.name);
                                streamWriter.WriteLine("\t\tpublic void {0}_REQ_CONTROLLER(ImplObject obj, Packet packet)", protocol.name);
                                streamWriter.WriteLine("\t\t{");
                                streamWriter.WriteLine("\t\t\tPACKET_{0}_REQ recvPacket = new PACKET_{1}_REQ();", protocol.name, protocol.name);
                                streamWriter.WriteLine("\t\t\trecvPacket.Deserialize(packet);");
                                streamWriter.WriteLine("\t\t\tON_{0}_REQ_CALLBACK(obj, recvPacket);", protocol.name);
                                streamWriter.WriteLine("\t\t}");

                                streamWriter.WriteLine("\t\tpublic delegate void {0}_RES_CALLBACK(ImplObject userObject, PACKET_{1}_RES packet);", protocol.name, protocol.name);
                                streamWriter.WriteLine("\t\tpublic {0}_RES_CALLBACK ON_{1}_RES_CALLBACK;", protocol.name, protocol.name);
                                streamWriter.WriteLine("\t\tpublic void {0}_RES_CONTROLLER(ImplObject obj, Packet packet)", protocol.name);
                                streamWriter.WriteLine("\t\t{");
                                streamWriter.WriteLine("\t\t\tPACKET_{0}_RES recvPacket = new PACKET_{1}_RES();", protocol.name, protocol.name);
                                streamWriter.WriteLine("\t\t\trecvPacket.Deserialize(packet);");
                                streamWriter.WriteLine("\t\t\tON_{0}_RES_CALLBACK(obj, recvPacket);", protocol.name);
                                streamWriter.WriteLine("\t\t}");
                            }
                            break;
                    }

                    if (protocol.protocolType.ToLower() == "admin")
                    {
                        streamWriter.WriteLine("#endif");
                        streamWriter.WriteLine();
                    }
                }
                streamWriter.WriteLine("\t}");
                streamWriter.WriteLine("}");
            }
        }
        void GeneratePacket(string targetDir, bool defineServer, string namespaceValue)
        {
            string filePath = Path.Combine(targetDir, _config.templateName + "Packet.cs");
            using (var streamWriter = new StreamWriter(filePath))
            {
                if (defineServer == true)
                    streamWriter.WriteLine("#define SERVER");

                streamWriter.WriteLine("using System;");
                streamWriter.WriteLine("using System.Collections.Generic;");
                streamWriter.WriteLine("using Service.Net;");
                streamWriter.WriteLine("using Service.Core;");
                streamWriter.WriteLine("using GameBase.Template.GameBase;");
                streamWriter.WriteLine("using GameBase.Template.GameBase.Common;");
                streamWriter.WriteLine();

                streamWriter.WriteLine("namespace GameBase.{0}", namespaceValue);
                streamWriter.WriteLine("{");
                foreach (var protocol in _config.protocols)
                {
                    string packetName = string.Empty;
                    if (protocol.protocolType.ToLower() == "admin")
                    {
                        streamWriter.WriteLine("#if SERVER");
                    }
                    switch (protocol.method.ToLower())
                    {
                        case "noti":
                            packetName = String.Format("PACKET_{0}_NOTI", protocol.name);
                            streamWriter.WriteLine("\tpublic sealed class {0} : PacketBaseNotification", packetName);
                            streamWriter.WriteLine("\t{");
                            streamWriter.WriteLine("\t\tpublic static readonly ushort ProtocolId = {0};", protocol.id);
                            streamWriter.WriteLine("\t\t");
                            GeneratePacketMember(streamWriter, packetName, protocol.notiMembers);
                            streamWriter.WriteLine("\t}");
                            break;
                        case "react":
                            packetName = String.Format("PACKET_{0}_REQ", protocol.name);
                            streamWriter.WriteLine("\tpublic sealed class {0} : PacketBaseRequest", packetName);
                            streamWriter.WriteLine("\t{");
                            streamWriter.WriteLine("\t\tpublic static readonly ushort ProtocolId = {0};", protocol.id);
                            GeneratePacketMember(streamWriter, packetName, protocol.reqMembers);
                            streamWriter.WriteLine("\t}");
                            packetName = String.Format("PACKET_{0}_RES", protocol.name);
                            streamWriter.WriteLine("\tpublic sealed class {0} : PacketBaseResponse", packetName);
                            streamWriter.WriteLine("\t{");
                            streamWriter.WriteLine("\t\tpublic static readonly ushort ProtocolId = {0};", protocol.id+1);
                            GeneratePacketMember(streamWriter, packetName, protocol.resMembers);
                            streamWriter.WriteLine("\t};");
                            break;
                        default:
                            throw new Exception($"failed to generate packet. {protocol.method}");
                    }

                    if (protocol.protocolType.ToLower() == "admin")
                    {
                        streamWriter.WriteLine("#endif");
                        streamWriter.WriteLine();
                    }
                }
                streamWriter.WriteLine("}");
            }
        }

        void GeneratePacketMember(StreamWriter streamWriter, string className, List<InfraMember> members)
        {
            foreach (var member in members)
            {
                streamWriter.WriteLine("\t\t/// <summary>");
                streamWriter.WriteLine("\t\t/// {0}", member.comment);
                streamWriter.WriteLine("\t\t/// </summary>");
                if (member.type.CompareTo("string") == 0)
                {
                    streamWriter.WriteLine("\t\tpublic {0} {1} = string.Empty;", member.type, member.name);
                }
                else
                {
                    streamWriter.WriteLine("\t\tpublic {0} {1} = new {2}();", member.type, member.name, member.type);
                }
            }
            streamWriter.WriteLine("\t\tpublic {0}():base(ProtocolId){{}}", className);
            streamWriter.WriteLine("\t\tpublic override void Serialize(Packet packet)");
            streamWriter.WriteLine("\t\t{");
            streamWriter.WriteLine("\t\t\tbase.Serialize(packet);");
            foreach (var member in members)
            {
                member.type = member.type.Replace(" ", "");
                member.name = member.name.Replace(" ", "");
                if (member.type.StartsWith("List<") == true)
                {
                    streamWriter.WriteLine("\t\t\tint length{0} = ({1} == null) ? 0 : {2}.Count;", member.name, member.name, member.name);
                    streamWriter.WriteLine("\t\t\tpacket.Write(length{0});", member.name);
                    streamWriter.WriteLine("\t\t\tfor (int i = 0; i < length{0}; ++i)", member.name);
                    streamWriter.WriteLine("\t\t\t{");
                    //string tempType = member.type.Substring(member.type.IndexOf('<') + 1, member.type.IndexOf('>') - member.type.IndexOf('<') - 1);
                    streamWriter.WriteLine("\t\t\t\tpacket.Write({0}[i]);", member.name);
                    streamWriter.WriteLine("\t\t\t}");

                }
                else if (member.type.StartsWith("Dictionary<") == true)
                {
                    streamWriter.WriteLine("\t\t\tpacket.Write({0}.Count);", member.name);
                    streamWriter.WriteLine("\t\t\tforeach (var pair in {0})", member.name);
                    streamWriter.WriteLine("\t\t\t{");
                    streamWriter.WriteLine("\t\t\t\tpacket.Write(pair.Key);");
                    streamWriter.WriteLine("\t\t\t\tpacket.Write(pair.Value);");
                    streamWriter.WriteLine("\t\t\t}");
                }
                else
                {
                    streamWriter.WriteLine("\t\t\tpacket.Write({0});", member.name);
                }
            }

            streamWriter.WriteLine("\t\t}");
            streamWriter.WriteLine("\t\tpublic override void Deserialize(Packet packet)");
            streamWriter.WriteLine("\t\t{");
            streamWriter.WriteLine("\t\t\tbase.Deserialize(packet);");
            foreach (var member in members)
            {
                member.type = member.type.Replace(" ", "");
                member.name = member.name.Replace(" ", "");
                if (member.type.StartsWith("List<") == true)
                {
                    streamWriter.WriteLine("\t\t\tint length{0} = ({1} == null) ? 0 : {2}.Count;", member.name, member.name, member.name);
                    streamWriter.WriteLine("\t\t\tpacket.Read(ref length{0});", member.name);
                    streamWriter.WriteLine("\t\t\tfor (int i = 0; i < length{0}; ++i)", member.name);
                    streamWriter.WriteLine("\t\t\t{");
                    string tempType = member.type.Substring(member.type.IndexOf('<') + 1, member.type.IndexOf('>') - member.type.IndexOf('<') - 1);
                    if (tempType.CompareTo("string") == 0)
                    {
                        streamWriter.WriteLine("\t\t\t\tstring element = string.Empty");
                    }
                    else
                    {
                        streamWriter.WriteLine("\t\t\t\t{0} element = new {1}();", tempType, tempType);
                    }
                    streamWriter.WriteLine("\t\t\t\tpacket.Read({0});", GetModelDerializeInputParameter(tempType, "element"));
                    streamWriter.WriteLine("\t\t\t\t{0}.Add(element);", member.name);
                    streamWriter.WriteLine("\t\t\t}");

                }
                else if (member.type.StartsWith("Dictionary<") == true)
                {
                    string[] words = member.type.Substring(member.type.IndexOf('<') + 1, member.type.IndexOf('>') - member.type.IndexOf('<') - 1).Split(",");
                    string dicKey = words[0];
                    string dicValue = words[1];

                    streamWriter.WriteLine("\t\t\tint length{0} = 0;", member.name);
                    streamWriter.WriteLine("\t\t\tpacket.Read(ref length{0});", member.name);
                    streamWriter.WriteLine("\t\t\tfor (int i = 0; i < length{0}; ++i)", member.name);
                    streamWriter.WriteLine("\t\t\t{");
                    if (dicKey.CompareTo("string") == 0)
                        streamWriter.WriteLine("\t\t\t\tstring tempKey = string.Empty;", dicKey, dicKey);
                    else
                        streamWriter.WriteLine("\t\t\t\t{0} tempKey = new {1}();", dicKey, dicKey);
                    if (dicValue.CompareTo("string") == 0)
                        streamWriter.WriteLine("\t\t\t\tstring tempValue = string.Empty;", dicValue, dicValue);
                    else
                        streamWriter.WriteLine("\t\t\t\t{0} tempValue = new {1}();", dicValue, dicValue);
                    streamWriter.WriteLine("\t\t\t\tpacket.Read(ref tempKey);");
                    streamWriter.WriteLine("\t\t\t\tpacket.Read(ref tempValue);");
                    streamWriter.WriteLine("\t\t\t\t{0}.Add(tempKey, tempValue);", member.name);
                    streamWriter.WriteLine("\t\t\t}");
                }
                else
                {
                    streamWriter.WriteLine("\t\t\tpacket.Read({0});", GetModelDerializeInputParameter(member.type, member.name));
                }
            }
            streamWriter.WriteLine("\t\t}");
        }

        public static void GenerateProjectFile(string targetDir, string templateType, string templateName)
        {
            string filePath = Path.Combine(targetDir, templateName + ".csproj");
            using (var streamWriter = new StreamWriter(filePath))
            {
                streamWriter.WriteLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t<PropertyGroup>");
                streamWriter.WriteLine("\t\t<TargetFramework>netcoreapp3.1</TargetFramework>");
                streamWriter.WriteLine("\t</PropertyGroup>");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t<ItemGroup>");
                streamWriter.WriteLine("\t\t<ProjectReference Include=\"../../../Service/Service.Core/Service.Core.csproj\" />");
                streamWriter.WriteLine("\t\t<ProjectReference Include=\"../../../Service/Service.DB/Service.DB.csproj\" />");
                streamWriter.WriteLine("\t\t<ProjectReference Include=\"../../../Service/Service.Net/Service.Net.csproj\" />");
                streamWriter.WriteLine("\t\t<ProjectReference Include=\"../../../Template/GameBase/GameBase.csproj\" />");
                streamWriter.WriteLine("\t</ItemGroup>");
                streamWriter.WriteLine();
                streamWriter.WriteLine("</Project>");
            }
        }

        public static void GenerateInfrastructureFile(string targetDir, string templateType, string templateName)
        {
            string filePath = Path.Combine(targetDir, "infrastructure-config.json");
            using (var fs = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                using (var streamWriter = new StreamWriter(fs, Encoding.UTF8))
                {
                    streamWriter.WriteLine("{");
                    streamWriter.WriteLine("\t\"templateType\" : \"{0}\",", templateType);
                    streamWriter.WriteLine("\t\"templateName\" : \"{0}\",", templateName);
                    streamWriter.WriteLine("\t\"templateVersion\" : \"1.0.0\",");
                    streamWriter.WriteLine("\t\"databases\" : [],");
                    streamWriter.WriteLine("\t\"protocols\" : [],");
                    streamWriter.WriteLine("\t\"models\" : []");
                    streamWriter.WriteLine("}");
                }
            }

            Console.WriteLine($"Generate InfrastructureFile : {filePath}");
        }

        public static string GetModelDerializeInputParameter(string type, string name)
        {
            string str = name;
            switch(type)
            {
                case "sbyte":
                case "SByte":
                case "byte":
                case "Byte":
                case "short":
                case "ushort":
                case "int":
                case "Int32":
                case "uint":
                case "UInt32":
                case "long":
                case "Int64":
                case "ulong":
                case "UInt64":
                case "float":
                case "Single":
                case "double":
                case "Double":
                case "decimal":
                case "Decimal":
                case "char":
                case "string":
                case "bool":
                case "DateTime":
                    str = "ref " + name;
                    break;
            }
            return str;
        }

    }
}
