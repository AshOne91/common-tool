using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace common_tool.Tools.Generate
{
    public class GenerateApplication: ActionBase
    {
        InfraApplication infraApplication = null;

        public GenerateApplication(Parameter param) : base(param)
        {
        }

        public override void Run()
        {
            if (_param._dicActionParam.ContainsKey("--target-path") == false)
            {
                throw new Exception($"not found parameter - \"--target-path\"");
            }

            string templateDir = Path.Combine(Directory.GetCurrentDirectory(), "Template");
            if (_param._dicActionParam.ContainsKey("--template-path") == true)
            {
                templateDir = new DirectoryInfo(_param._dicActionParam["--template-path"]).FullName;
            }

            string targetDir = new DirectoryInfo(_param._dicActionParam["--target-path"]).FullName;
            string applicationName = Helpers.SplitPath(targetDir)[Helpers.SplitPath(targetDir).Length - 1];

            // 프로젝트 생성
            string filePath = Path.Combine(targetDir, applicationName + ".csproj");
            if (File.Exists(filePath) == false)
            {
                string command = "new console -o " + targetDir;
                try
                {
                    Process.Start("dotnet", command).WaitForExit();
                }
                catch(Exception)
                {
                    Process.Start("dotnet.exe", command).WaitForExit();
                }

                File.Delete(Path.Combine(targetDir, "Program.cs"));
            }

            GenerateInfrastructureFile(targetDir, applicationName);
            string infraFilePath = Path.Combine(targetDir, "infrastructure-config.json");
            using (StreamReader r = new StreamReader(infraFilePath))
            {
                infraApplication = JsonConvert.DeserializeObject<InfraApplication>(r.ReadToEnd());
            }

            GenerateProjectFile(targetDir, applicationName);
            GenerateAppSettingFile(targetDir, applicationName.ToLower() + "-config.json", true);
            GenerateAppSettingFile(targetDir, applicationName.ToLower() + "-config_debug.json");
            GenerateAppFile(targetDir, templateDir, applicationName);
            GenerateEntryFile(targetDir, applicationName);
            GenerateControllers(targetDir, templateDir, infraApplication.applicationName, infraApplication.templates);
        }

        void GenerateInfrastructureFile(string targetDir, string applicationName)
        {
            string filePath = Path.Combine(targetDir, "infrastructure-config.json");
            if (File.Exists(filePath) == true)
            {
                return;
            }

            using (var streamWriter = new StreamWriter(filePath))
            {
                streamWriter.WriteLine("{");
                streamWriter.WriteLine("\t\"applicationName\" : \"{0}\",", applicationName);
                streamWriter.WriteLine("\t\"applicationVersion\" : \"\",");
                streamWriter.WriteLine("\t\"services\" : [");
                streamWriter.WriteLine("\t\t\"../../Service/Service.Core\",");
                streamWriter.WriteLine("\t\t\"../../Service/Service.Net\"");
                streamWriter.WriteLine("\t],");
                streamWriter.WriteLine("\t\"templates\" : []");
                streamWriter.WriteLine("}");
            }

            Console.WriteLine($"Generate InfrastructureFile : {filePath}");
        }

        void GenerateProjectFile(string targetDir, string applicationName)
        {
            string filePath = Path.Combine(targetDir, applicationName + ".csproj");

            using (var streamWriter = new StreamWriter(filePath))
            {
                streamWriter.WriteLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
                streamWriter.WriteLine("\t<PropertyGroup>");
                streamWriter.WriteLine("\t\t<OutputType>Exe</OutputType>");
                streamWriter.WriteLine("\t\t<TargetFramework>netcoreapp3.1</TargetFramework>");
                streamWriter.WriteLine("\t</PropertyGroup>");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t<ItemGroup>");
                foreach (var template in infraApplication.services)
                {
                    var words = Helpers.SplitPath(template);
                    var path = Path.Combine(template, words[words.Length - 1]);
                    streamWriter.WriteLine("<ProjectReference Include=\"{0}.csproj\" />", path);

                }
                streamWriter.WriteLine("\t</ItemGroup>");

                streamWriter.WriteLine("\t<ItemGroup>");
                streamWriter.WriteLine("\t\t<ProjectReference Include=\"../../Template/GameBase/GameBase.csproj\" />");

                foreach (var template in infraApplication.templates)
                {
                    var words = Helpers.SplitPath(template);
                    var path = Path.Combine(template, words[words.Length - 1]);
                    streamWriter.WriteLine("\t\t<ProjectReference Include=\"{0}.csproj\" />", path);
                }
                streamWriter.WriteLine("\t</ItemGroup>");
                streamWriter.WriteLine();

                /*streamWriter.WriteLine("\t<ItemGroup>");
                streamWriter.WriteLine("\t\t<Content Include=\"./Table/*.csv\" Link=\"./table/%(Filename)%(Extension)\">");
                streamWriter.WriteLine("\t\t\t<CopyToOutputDirectory>Always</CopyToOutputDirectory>");
                streamWriter.WriteLine("\t\t</Content>");
                streamWriter.WriteLine("\t\t<Content Include=\"./*.p12\" Link=\"./%(Filename)%(Extension)\">");
                streamWriter.WriteLine("\t\t\t<CopyToOutputDirectory>Always</CopyToOutputDirectory>");
                streamWriter.WriteLine("\t\t</Content>");
                streamWriter.WriteLine("\t</ItemGroup>");*/
                streamWriter.WriteLine("</Project>");
            }
        }

        void GenerateAppSettingFile(string targetDir, string configFileName, bool checkDataTableVersion = false)
        {
            string filePath = Path.Combine(targetDir, configFileName);
            if (File.Exists(filePath) == true)
            {
                return;
            }

            using (var streamWriter = new StreamWriter(filePath))
            {
                streamWriter.WriteLine("{");
                streamWriter.WriteLine("\t//하면서 채워넣기");
                streamWriter.WriteLine("}");
            }
        }

        void GenerateEntryFile(string targetDir, string applicationName)
        {
            string filePath = Path.Combine(targetDir, "Entry.cs");
            using (var streamWriter = new StreamWriter(filePath))
            {
                streamWriter.WriteLine("using System;");
                streamWriter.WriteLine("using System.Collections.Generic;");
                streamWriter.WriteLine("using Service.Core;");
                streamWriter.WriteLine("using Service.Net;");
                streamWriter.WriteLine();
                streamWriter.WriteLine("namespace {0}", applicationName);
                streamWriter.WriteLine("{");
                streamWriter.WriteLine("\tpublic sealed class {0}Entry", applicationName);
                streamWriter.WriteLine("\t{");
                streamWriter.WriteLine("\t\tpublic static {0}App serverApp = new {1}App();", applicationName, applicationName);
                streamWriter.WriteLine("\t\tpublic static {0}App GetApp() {{ return serverApp; }}", applicationName);
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tstatic void Main(string[] args)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\ttry");
                streamWriter.WriteLine("\t\t\t{");
                streamWriter.WriteLine("\t\t\t\tConsole.Title = \"{0} : \" + System.Diagnostics.Process.GetCurrentProcess().Id;", applicationName);
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\t\t\tServerConfig config = new ServerConfig();");
                streamWriter.WriteLine("\t\t\t\tconfig.PeerConfig.UseSessionEventQueue = true;");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\t\t\tLogger.Default = new Logger();");
                streamWriter.WriteLine("\t\t\t\tLogger.Default.Create(true, \"{0}\");", applicationName);
                streamWriter.WriteLine("\t\t\t\tLogger.Default.Log(ELogLevel.Always, \"Start {0}...\");", applicationName);
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\t\t\tbool result = serverApp.Create(config);");
                streamWriter.WriteLine("\t\t\t\tif (result == false)");
                streamWriter.WriteLine("\t\t\t\t{");
                streamWriter.WriteLine("\t\t\t\t\tLogger.Default.Log(ELogLevel.Fatal, \"Failed Create {0}.\");", applicationName);
                streamWriter.WriteLine("\t\t\t\t\treturn;");
                streamWriter.WriteLine("\t\t\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\t\t\tLogger.Default.Log(ELogLevel.Always, \"Start WaitForSessionEvent...\");");
                streamWriter.WriteLine("\t\t\t\tserverApp.Join();");
                streamWriter.WriteLine("\t\t\t}");
                streamWriter.WriteLine("\t\t\tcatch (Exception e)");
                streamWriter.WriteLine("\t\t\t{");
                streamWriter.WriteLine("\t\t\t\tLogger.WriteExceptionLog(e);");
                streamWriter.WriteLine("\t\t\t}");
                streamWriter.WriteLine("\t\t\tfinally");
                streamWriter.WriteLine("\t\t\t{");
                streamWriter.WriteLine("\t\t\t\tserverApp.Destroy();");
                streamWriter.WriteLine("\t\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\t\treturn;");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine("\t}");
                streamWriter.WriteLine("}");
            }
        }

        void GenerateAppFile(string targetDir, string templateDir, string applicationName)
        {
            string filePath = Path.Combine(targetDir, "App.cs");
            using (var streamWriter = new StreamWriter(filePath))
            {
                streamWriter.WriteLine("using System;");
                streamWriter.WriteLine("using System.Collections.Generic;");
                streamWriter.WriteLine("using System.Net;");
                streamWriter.WriteLine("using System.Net.Sockets;");
                foreach (var service in infraApplication.services)
                {
                    var words = Helpers.SplitPath(service);
                    streamWriter.WriteLine("using {0};", words[words.Length - 1]);
                }
                streamWriter.WriteLine("using GameBase.Template.GameBase;");
                foreach (var template in infraApplication.templates)
                {
                    var words = Helpers.SplitPath(template);
                    DirectoryInfo childDir = new DirectoryInfo(Path.Combine(templateDir, words[words.Length - 2], words[words.Length - 1]));
                    FileInfo[] files = childDir.GetFiles();
                    foreach (FileInfo file in files)
                    {
                        if (file.Extension.CompareTo(".json") != 0 || file.Name.StartsWith("infrastructure-config") == false)
                        {
                            continue;
                        }

                        using (StreamReader r = new StreamReader(file.FullName))
                        {
                            var templateWords = Helpers.SplitPath(file.DirectoryName);
                            var templateConfig = JsonConvert.DeserializeObject<InfraTemplateConfig>(r.ReadToEnd());
                            streamWriter.WriteLine("using GameBase.Template.{0}.{1};", templateWords[templateWords.Length - 2], templateWords[templateWords.Length - 1]);
                        }
                    }
                }

                streamWriter.WriteLine();
                streamWriter.WriteLine("namespace {0}", applicationName);
                streamWriter.WriteLine("{");
                streamWriter.WriteLine("\tpublic class {0}App : ServerApp", applicationName);
                streamWriter.WriteLine("\t{");
                streamWriter.WriteLine("\t\tpublic {0}App()", applicationName);
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\t~{0}App()", applicationName);
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\tDestroy();");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic override bool Create(ServerConfig config, int frame = 30)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\tbool result = base.Create(config, frame);");
                streamWriter.WriteLine();
                foreach (var template in infraApplication.templates)
                {
                    var words = Helpers.SplitPath(template);
                    DirectoryInfo childDir = new DirectoryInfo(Path.Combine(templateDir, words[words.Length - 2], words[words.Length - 1]));
                    FileInfo[] files = childDir.GetFiles();
                    foreach (FileInfo file in files)
                    {
                        if (file.Extension.CompareTo(".json") != 0 || file.Name.StartsWith("infrastructure-config") == false)
                        {
                            continue;
                        }

                        using (StreamReader r = new StreamReader(file.FullName))
                        {
                            var templateWords = Helpers.SplitPath(file.DirectoryName);
                            var templateConfig = JsonConvert.DeserializeObject<InfraTemplateConfig>(r.ReadToEnd());
                            streamWriter.WriteLine("\t\t\tGameBaseTemplateContext.AddTemplate(ETemplateType.{0}, new {1}Template());", templateWords[templateWords.Length - 2], templateWords[templateWords.Length - 1]);
                        }
                    }
                }
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\t\tTemplateConfig templateConfig = new TemplateConfig();");
                streamWriter.WriteLine("\t\t\tGameBaseTemplateContext.InitTemplate(templateConfig);");
                streamWriter.WriteLine("\t\t\tGameBaseTemplateContext.LoadDataTable(templateConfig);");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\t\tPerformanceCounter._WarningEvent += OnPerfWarning;");
                streamWriter.WriteLine("\t\t\treturn result;");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic void OnPerfWarning(int tick)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\tLogger.Default.Log(ELogLevel.Warn, \"OnPerfWarning\");");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic override void Destroy()");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\tbase.Destroy();");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\t\tPerformanceCounter.Print();");
                streamWriter.WriteLine("\t\t\tLogger.Default.Destroy();");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic override void OnAccept(SocketSession session, IPEndPoint localEP, IPEndPoint remoteEP)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\tUserObject obj = new UserObject();");
                streamWriter.WriteLine("\t\t\tsession.SetUserObject(obj);");
                streamWriter.WriteLine("\t\t\tobj.SetSocketSession(session);");
                streamWriter.WriteLine();
                foreach (var template in infraApplication.templates)
                {
                    var words = Helpers.SplitPath(template);
                    DirectoryInfo childDir = new DirectoryInfo(Path.Combine(templateDir, words[words.Length - 2], words[words.Length - 1]));
                    FileInfo[] files = childDir.GetFiles();
                    foreach (FileInfo file in files)
                    {
                        if (file.Extension.CompareTo(".json") != 0 || file.Name.StartsWith("infrastructure-config") == false)
                        {
                            continue;
                        }

                        using (StreamReader r = new StreamReader(file.FullName))
                        {
                            var templateWords = Helpers.SplitPath(file.DirectoryName);
                            var templateConfig = JsonConvert.DeserializeObject<InfraTemplateConfig>(r.ReadToEnd());
                            streamWriter.WriteLine("\t\t\tGameBaseTemplateContext.AddTemplate<UserObject>(obj, ETemplateType.{0}, new {1}Template());", templateWords[templateWords.Length - 2], templateWords[templateWords.Length - 1]);
                            streamWriter.WriteLine("\t\t\t{0}Controller.Add{1}Controller(session.GetUid());", templateWords[templateWords.Length - 2], templateWords[templateWords.Length - 2]);
                        }
                    }
                }
                streamWriter.WriteLine("\t\t\tGameBaseTemplateContext.CreateClient(session.GetUid());");
                streamWriter.WriteLine("\t\t\tobj.OnAccept(localEP);");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tprivate bool _bListenState = false;");
                streamWriter.WriteLine("\t\tprivate void ListenUsers(bool bNewState)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\tif (_bListenState == bNewState) return;");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\t\t_bListenState = bNewState;");
                streamWriter.WriteLine("\t\t\tif (_bListenState)");
                streamWriter.WriteLine("\t\t\t{");
                streamWriter.WriteLine("\t\t\t\tLogger.Default.Log(ELogLevel.Always, \"Start Listen {0} \", 10000/*TODO : 설정파일 읽는거로 변경하기*/);");
                streamWriter.WriteLine("\t\t\t\tIPEndPoint epClient = new IPEndPoint(IPAddress.Any, 10000);");
                streamWriter.WriteLine("\t\t\t\tBeginAcceptor(epClient);");
                streamWriter.WriteLine("\t\t\t}");
                streamWriter.WriteLine("\t\t\telse");
                streamWriter.WriteLine("\t\t\t{");
                streamWriter.WriteLine("\t\t\t\tif (_listeners != null)");
                streamWriter.WriteLine("\t\t\t\t{");
                streamWriter.WriteLine("\t\t\t\t\tforeach (TcpListener listener in _listeners)");
                streamWriter.WriteLine("\t\t\t\t\t{");
                streamWriter.WriteLine("\t\t\t\t\t\tlistener.Stop();");
                streamWriter.WriteLine("\t\t\t\t\t}");
                streamWriter.WriteLine("\t\t\t\t\t_listeners.Clear();");
                streamWriter.WriteLine("\t\t\t\t}");
                streamWriter.WriteLine("\t\t\t}");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic override void OnClose(SocketSession session)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\tUserObject userObj = session.GetUserObject();");
                streamWriter.WriteLine("\t\t\tif (userObj != null)");
                streamWriter.WriteLine("\t\t\t{");
                streamWriter.WriteLine("\t\t\t\tGameBaseTemplateContext.DeleteClient(userObj.GetSession().GetUid());");
                streamWriter.WriteLine("\t\t\t\tuserObj.OnClose();");
                streamWriter.WriteLine("\t\t\t\tuserObj.Dispose();");
                streamWriter.WriteLine("\t\t\t\tsession.SetUserObject(null);");
                streamWriter.WriteLine("\t\t\t}");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic override void OnSocketError(SocketSession session, string e)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\tLogger.Default.Log(ELogLevel.Err, \"OnSocketError = {{0}}\", e);");
                streamWriter.WriteLine("\t\t\tsession.Disconnect();");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic override void OnPacket(SocketSession session, Packet packet)");
                streamWriter.WriteLine("\t\t{");
                foreach (var template in infraApplication.templates)
                {
                    var words = Helpers.SplitPath(template);
                    DirectoryInfo childDir = new DirectoryInfo(Path.Combine(templateDir, words[words.Length - 2], words[words.Length - 1]));
                    FileInfo[] files = childDir.GetFiles();
                    foreach (FileInfo file in files)
                    {
                        if (file.Extension.CompareTo(".json") != 0 || file.Name.StartsWith("infrastructure-config") == false)
                        {
                            continue;
                        }

                        using (StreamReader r = new StreamReader(file.FullName))
                        {
                            var templateWords = Helpers.SplitPath(file.DirectoryName);
                            var templateConfig = JsonConvert.DeserializeObject<InfraTemplateConfig>(r.ReadToEnd());
                            streamWriter.WriteLine("\t\t\t{0}Controller.OnPacket(session.GetUserObject(), packet.GetId(), packet);", templateWords[templateWords.Length - 2]);
                        }
                    }
                }
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic override void OnTimer(TimerHandle timer)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic override void OnUpdate(float dt)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\tGameBaseTemplateContext.UpdateClient(dt);");
                streamWriter.WriteLine("\t\t\tGameBaseTemplateContext.UpdateTemplate(dt);");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine("\t}");
                streamWriter.WriteLine("}");
            }
        }

        void GenerateControllers(string targetDir, string templateDir, string applicationName, List<string> templates)
        {
            string controllerPath = Path.Combine(targetDir, "Controller");
            if (Directory.Exists(controllerPath) == false)
            {
                Directory.CreateDirectory(controllerPath);
            }

            foreach (var template in templates)
            {
                var words = Helpers.SplitPath(template);
                DirectoryInfo childDIr = new DirectoryInfo(Path.Combine(templateDir, words[words.Length - 2], words[words.Length - 1]));
                FileInfo[] files = childDIr.GetFiles();
                foreach (FileInfo file in files)
                {
                    if (file.Extension.CompareTo(".json") != 0 || file.Name.StartsWith("infrastructure-config") == false)
                    {
                        continue;
                    }

                    using (StreamReader r = new StreamReader(file.FullName))
                    {
                        var templateConfig = JsonConvert.DeserializeObject<InfraTemplateConfig>(r.ReadToEnd());
                        GenerateControllerFile(templateConfig, file.DirectoryName, controllerPath);
                    }
                }
            }
        }

        void GenerateControllerFile(InfraTemplateConfig templateConfig, string templatePath, string controllerPath)
        {
            var words = Helpers.SplitPath(templatePath);
            string filePath = Path.Combine(controllerPath, templateConfig.templateName + "Controller.cs");
            using (var streamWriter = new StreamWriter(filePath))
            {
                streamWriter.WriteLine("using System;");
                streamWriter.WriteLine("using System.Collections.Generic;");
                streamWriter.WriteLine("using Service.Net;");
                streamWriter.WriteLine("using GameBase.Template.GameBase;");
                streamWriter.WriteLine("using GameBase.Template.{0}.{1};", words[words.Length - 2], words[words.Length - 1]);
                streamWriter.WriteLine("using GameBase.Template.{0}.{1}.Common;", words[words.Length - 2], words[words.Length - 1]);
                streamWriter.WriteLine();
                streamWriter.WriteLine("namespace {0}", infraApplication.applicationName);
                streamWriter.WriteLine("{");
                streamWriter.WriteLine("\tpublic static class {0}Controller", templateConfig.templateType);
                streamWriter.WriteLine("\t{");
                streamWriter.WriteLine("\t\tstatic Dictionary<ulong, {0}Protocol> _protocolByUid = new Dictionary<ulong, {1}Protocol>();", words[words.Length - 1], words[words.Length - 1]);
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic static void Add{0}Controller(ulong uid)", words[words.Length - 2]);
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\t{0}Template template = GameBaseTemplateContext.GetTemplate<{1}Template>(uid, ETemplateType.{2});", words[words.Length - 1], words[words.Length - 1], words[words.Length - 2]);
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\t\tif (_protocolByUid.ContainsKey(uid) == true)");
                streamWriter.WriteLine("\t\t\t{");
                streamWriter.WriteLine("\t\t\t\tthrow new Exception(\"Duplication Add{0}Controller\");", words[words.Length - 2]);
                streamWriter.WriteLine("\t\t\t}");
                streamWriter.WriteLine("\t\t\t{0}Protocol protocol = new {1}Protocol();", words[words.Length - 1], words[words.Length - 1]);
                foreach (var protocol in templateConfig.protocols)
                {
                    if (protocol.method.ToLower() == "noti")
                    {
                        streamWriter.WriteLine("\t\t\tprotocol.ON_{0}_NOTI_CALLBACK = template.ON_{1}_NOTI_CALLBACK;", protocol.name, protocol.name);
                    }
                    else if (protocol.method.ToLower() == "react")
                    {
                        streamWriter.WriteLine("\t\t\tprotocol.ON_{0}_REQ_CALLBACK = template.ON_{1}_REQ_CALLBACK;", protocol.name, protocol.name);
                        streamWriter.WriteLine("\t\t\tprotocol.ON_{0}_RES_CALLBACK = template.ON_{1}_RES_CALLBACK;", protocol.name, protocol.name);
                    }
                }
                streamWriter.WriteLine("\t\t\t_protocolByUid.Add(uid, protocol);");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic static bool OnPacket(ImplObject obj, ushort protocolId, Packet packet)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\tulong uid = obj.GetSession().GetUid();");
                streamWriter.WriteLine("\t\t\tif (_protocolByUid.ContainsKey(uid) == false)");
                streamWriter.WriteLine("\t\t\t{");
                streamWriter.WriteLine("\t\t\t\treturn false;");
                streamWriter.WriteLine("\t\t\t}");
                streamWriter.WriteLine("\t\t\tvar Protocol = _protocolByUid[uid];");
                streamWriter.WriteLine("\t\t\treturn Protocol.OnPacket(obj, protocolId, packet);");
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine("\t}");
                streamWriter.WriteLine("}");
            }
        }
    }
}
