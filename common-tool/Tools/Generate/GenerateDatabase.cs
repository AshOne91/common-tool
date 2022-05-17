using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace common_tool.Tools.Generate
{
    public class GenerateDatabase : ActionBase
    {
        readonly string _commonDir = "Common";
        InfraTemplateConfig _config = null;

        public GenerateDatabase(Parameter param) : base(param)
        {

        }

        public override void Run()
        {
            if (_param._dicActionParam.ContainsKey("--target-path") == false)
            {
                throw new Exception($"not found parameter - \"--target-path\"");
            }

            var targetDir = new DirectoryInfo(_param._dicActionParam["--target-path"]);
            string templateType = Helpers.SplitPath(targetDir.FullName)[Helpers.SplitPath(targetDir.FullName).Length - 2];
            string templateName = Helpers.SplitPath(targetDir.FullName)[Helpers.SplitPath(targetDir.FullName).Length - 1];

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

                GenerateTemplate.GenerateProjectFile(targetDir.FullName, templateType, templateName);
                GenerateTemplate.GenerateInfrastructureFile(targetDir.FullName, templateType, templateName);
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

                var commonPath = Path.Combine(targetDir.FullName, _commonDir);
                if (Directory.Exists(commonPath) == false)
                {
                    Directory.CreateDirectory(commonPath);
                    Console.WriteLine($"Create Directory: {commonPath}");
                }

                var namespaceValue = Helpers.GetNameSpace(commonPath);
                GenerateDBTable(commonPath, namespaceValue);
            }
        }
        void GenerateDBTable(string targetDir, string namespaceValue)
        {
            string filePath = Path.Combine(targetDir, _config.templateName + "DBTable.cs");
            using (var streamWriter = new StreamWriter(filePath))
            {
                streamWriter.WriteLine("using System;");
                streamWriter.WriteLine("using System.Collections.Generic;");
                streamWriter.WriteLine("using Service.Net");
                streamWriter.WriteLine("using Service.Core");
                streamWriter.WriteLine();

                streamWriter.WriteLine("namespace GameBase.{0}", namespaceValue);
                streamWriter.WriteLine("{");
                foreach (var model in _config.models)
                {
                    streamWriter.WriteLine("\tpublic class {0} : BaseDBClass", model.name);
                    streamWriter.WriteLine("\t{");
                    foreach (var member in model.members)
                    {
                        streamWriter.WriteLine("\t\t/// <sumary>");
                        streamWriter.WriteLine("\t\t/// {0}", member.comment);
                        streamWriter.WriteLine("\t\t/// <sumary>");
                        streamWriter.WriteLine("\t\tpublic {0} {1};", member.type, member.name);
                    }
                    streamWriter.WriteLine("\t\tpublic {0}() { Reset(); }", model.name);
                    streamWriter.WriteLine("\t\t~{0}() { Reset(); }", model.name);
                    streamWriter.WriteLine("\t\tpublic override void Reset()");
                    streamWriter.WriteLine("\t\t{");
                    bool isContainSlot = false;
                    foreach (var member in model.members)
                    {
                        if (member.name.ToLower() == "slot")
                        {
                            isContainSlot = true;
                        }
                        streamWriter.WriteLine("\t\t\t{0} = default({1});", member.name, member.type);
                    }
                    streamWriter.WriteLine("\t\t}");
                    if (isContainSlot == true)
                    {
                        streamWriter.WriteLine("\tpublic class DBSlot_{0} : DBSlot<{1}>{}", model.name, model.name);
                        streamWriter.WriteLine("\tpublic class DBSlotContainer_{0} : DBSlotContainer<DBSlot_{1}>{}", model.name, model.name);
                    }
                    else
                    {
                        streamWriter.WriteLine("\tpublic class DBBase_{0} : DBBase<{1}>{}", model.name, model.name);
                        streamWriter.WriteLine("\tpublic class DBBaseContainer_{0} : DBBaseContainer<DBBase_{1}>()", model.name, model.name);
                    }
                }
                streamWriter.WriteLine("}");
            }
        }


    }
}
