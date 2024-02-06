using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace common_tool.Tools.Generate
{
    public class GenerateController: ActionBase
    {
        protected InfraApplication _infraApplication = null;

        public GenerateController(Parameter param) : base(param) 
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

            GenerateInfrastructureFile(targetDir, applicationName);
            string infraFilePath = Path.Combine(targetDir, "infrastructure-config.json");
            using (StreamReader reader = new StreamReader(infraFilePath))
            {
                _infraApplication = JsonConvert.DeserializeObject<InfraApplication>(reader.ReadToEnd());
            }

            GenerateControllers(targetDir, templateDir, _infraApplication.applicationName, _infraApplication.templates);
        }

        protected void GenerateInfrastructureFile(string targetDir, string applicationName)
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

        protected void GenerateControllers(string targetDir, string templateDir, string applicationName, List<string> templates)
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

        protected void GenerateControllerFile(InfraTemplateConfig templateConfig, string templatePath, string controllerPath)
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
                streamWriter.WriteLine("namespace {0}", _infraApplication.applicationName);
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
                streamWriter.WriteLine("\t\tpublic static void Remove{0}Controller(ulong uid)", words[words.Length - 2]);
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\tif (_protocolByUid.ContainsKey(uid) == true)");
                streamWriter.WriteLine("\t\t\t{");
                streamWriter.WriteLine("\t\t\t\t_protocolByUid.Remove(uid);");
                streamWriter.WriteLine("\t\t\t}");
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
