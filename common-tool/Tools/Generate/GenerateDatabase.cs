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
                GenerateDBSave(commonPath, templateType, namespaceValue);
                GenerateDBLoad(commonPath, templateName, namespaceValue);
            }
        }
        void GenerateDBSave(string targetDir, string templateType, string namespaceValue)
        {
            string filePath = Path.Combine(targetDir, _config.templateName + "DBSave.cs");
            using (var streamWriter = new StreamWriter(filePath))
            {
                streamWriter.WriteLine("using System;");
                streamWriter.WriteLine("using System.Collections.Generic;");
                streamWriter.WriteLine("using Service.Net;");
                streamWriter.WriteLine("using Service.Core;");
                streamWriter.WriteLine("using Service.DB;");
                streamWriter.WriteLine();

                streamWriter.WriteLine("namespace GameBase.{0}", namespaceValue);
                streamWriter.WriteLine("{");
                streamWriter.WriteLine("\tpublic partial class DBGameUserSave");
                streamWriter.WriteLine("\t{");
                foreach (var database in _config.databases)
                {
                    string funcStr = string.Format("\t\tprivate void _Run_SaveUser_{0}:(Ado adoDB, ", database.tableName);
                    if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                    {
                        funcStr += "UInt64 ";
                        funcStr += database.partitionKey_1;
                        funcStr += ", ";
                    }
                    if (string.IsNullOrEmpty(database.partitionKey_2) == false)
                    {
                        funcStr += "UInt64 ";
                        funcStr += database.partitionKey_2;
                        funcStr += ", ";
                    }
                    if (database.tableType.ToLower() == "slot")
                    {
                        funcStr += string.Format("DBSlotContainer_{0} container)", database.tableName);
                    }
                    else
                    {
                        funcStr += string.Format("DBBaseContainer_{0} container)", database.tableName);
                    }
                    streamWriter.WriteLine(funcStr);
                    streamWriter.WriteLine("\t\t{");
                    if (database.tableType.ToLower() == "slot")
                    {
                        streamWriter.WriteLine("\t\t\ttry");
                        streamWriter.WriteLine("\t\t\t{");
                        streamWriter.WriteLine("\t\t\t\tcontainer.ForEach((DBSlot_{0} slot) =>", database.tableName);
                        streamWriter.WriteLine("\t\t\t\t{");
                        streamWriter.WriteLine("\t\t\t\t\tQueryBuilder query = new QueryBuilder(\"gp_player_{0}_save\");", database.tableName.ToLower());
                        if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                        {
                            streamWriter.WriteLine("\t\t\t\t\tquery.SetInputParam(\"p_{0}\", {1});", database.partitionKey_1, database.partitionKey_1);
                        }
                        if (string.IsNullOrEmpty(database.partitionKey_2) == false)
                        {
                            streamWriter.WriteLine("\t\t\t\t\tquery.SetInputParam(\"p_{0}\", {1});", database.partitionKey_2, database.partitionKey_2);
                        }
                        streamWriter.WriteLine();
                        streamWriter.WriteLine("\t\t\t\t\tif (slot._isDeleted)");
                        streamWriter.WriteLine("\t\t\t\t\t{");
                        streamWriter.WriteLine("\t\t\t\t\t\tquery.SetInputParam(\"p_slot\", slot._nSlot);");
                        streamWriter.WriteLine("\t\t\t\t\t\tquery.SetInputParam(\"p_deleted\", 0);");
                        streamWriter.WriteLine("\t\t\t\t\t\tquery.SetInputParam(\"p_createTime\", slot._DBData.createTime);");
                        streamWriter.WriteLine("\t\t\t\t\t\tquery.SetInputParam(\"p_updateTime\", DateTime.UtcNow);");
                        streamWriter.WriteLine();
                        foreach(var member in database.members)
                        {
                            streamWriter.WriteLine("\t\t\t\t\t\tquery.SetInputParam(\"p_{0}\");");
                        }
                        streamWriter.WriteLine("\t\t\t\t\t}");
                        streamWriter.WriteLine("\t\t\t\t\telse");
                        streamWriter.WriteLine("\t\t\t\t\t{");
                        streamWriter.WriteLine("\t\t\t\t\t}");
                        streamWriter.WriteLine("\t\t\t\t\tadoDB.ExecuteNoRecords(query);");
                        streamWriter.WriteLine("\t\t\t\t}, true);");
                        streamWriter.WriteLine("\t\t\t}");
                        streamWriter.WriteLine("\t\t\tcatch (Exception e)");
                        streamWriter.WriteLine("\t\t\t{");
                        streamWriter.WriteLine("\t\t\t\tthrow new Exception(\"[gp_player_{0}_save]\" + e.Message);", database.tableName.ToLower());
                        streamWriter.WriteLine("\t\t\t}");
                    }
                    else
                    {

                    }
                    streamWriter.WriteLine("\t\t}");
                }
                streamWriter.WriteLine("\t\tpartial void {0}Run(UserDB userDB, AdoDB adoDB, UInt64 partitionKey_1, UInt64 partitionKey_2)", templateType);
                streamWriter.WriteLine("\t\t{");
                foreach (var database in _config.databases)
                {
                    streamWriter.WriteLine("\t\t\t", )
                }
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine("\t}");
                
            }

        }
        void GenerateDBLoad()
        {

        }
        void GenerateDBTable(string targetDir, string namespaceValue)
        {
            string filePath = Path.Combine(targetDir, _config.templateName + "DBTable.cs");
            using (var streamWriter = new StreamWriter(filePath))
            {
                streamWriter.WriteLine("using System;");
                streamWriter.WriteLine("using System.Collections.Generic;");
                streamWriter.WriteLine("using Service.Net;");
                streamWriter.WriteLine("using Service.Core;");
                streamWriter.WriteLine("using Service.DB;");
                streamWriter.WriteLine();

                streamWriter.WriteLine("namespace GameBase.{0}", namespaceValue);
                streamWriter.WriteLine("{");
                foreach (var database in _config.databases)
                {
                    streamWriter.WriteLine("\tpublic class {0} : BaseDBClass", database.tableName);
                    streamWriter.WriteLine("\t{");
                    if (string.IsNullOrEmpty(database.partitionKey_1) || string.IsNullOrEmpty(database.partitionKey_2))
                    {
                        throw new Exception($"failed create : partitionKey empty. {database.databaseName}");
                    }

                    if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                    {
                        streamWriter.WriteLine("\t\t/// <sumary>");
                        streamWriter.WriteLine("\t\t/// 파티션키_1");
                        streamWriter.WriteLine("\t\t/// <sumary>");
                        streamWriter.WriteLine("\t\tpublic UInt64 {0};", database.partitionKey_1);
                    }

                    if (string.IsNullOrEmpty(database.partitionKey_2) == false)
                    {
                        streamWriter.WriteLine("\t\t/// <sumary>");
                        streamWriter.WriteLine("\t\t/// 파티션키_2");
                        streamWriter.WriteLine("\t\t/// <sumary>");
                        streamWriter.WriteLine("\t\tpublic UInt64 {0};", database.partitionKey_2);
                    }

                    /*if (database.tableType.ToLower() == "slot")
                    {
                        streamWriter.WriteLine("\t\t/// <sumary>");
                        streamWriter.WriteLine("\t\t/// 슬롯");
                        streamWriter.WriteLine("\t\t/// <sumary>");
                        streamWriter.WriteLine("\t\tpublic short nSlot;");
                    }*/

                    streamWriter.WriteLine("\t\t/// <sumary>");
                    streamWriter.WriteLine("\t\t/// 생성시간");
                    streamWriter.WriteLine("\t\t/// <sumary>");
                    streamWriter.WriteLine("\t\t///public DateTime createTime;");

                    streamWriter.WriteLine("\t\t/// <sumary>");
                    streamWriter.WriteLine("\t\t/// 업데이트 시간");
                    streamWriter.WriteLine("\t\t/// <sumary>");
                    streamWriter.WriteLine("\t\t///public DateTime updateTime;");


                    foreach (var member in database.members)
                    {
                        streamWriter.WriteLine("\t\t/// <sumary>");
                        streamWriter.WriteLine("\t\t/// {0}", member.comment);
                        streamWriter.WriteLine("\t\t/// <sumary>");
                        streamWriter.WriteLine("\t\tpublic {0} {1};", member.type, member.name);
                    }
                    streamWriter.WriteLine("\t\tpublic {0}() { Reset(); }", database.tableName);
                    streamWriter.WriteLine("\t\t~{0}() { Reset(); }", database.tableName);
                    streamWriter.WriteLine("\t\tpublic override void Reset()");
                    streamWriter.WriteLine("\t\t{");
                    if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                    {
                        streamWriter.WriteLine("\t\t\t{0} = default(UInt64);", database.partitionKey_1);
                    }
                    if (string.IsNullOrEmpty(database.partitionKey_2) == false)
                    {
                        streamWriter.WriteLine("\t\t\t{0} = default(UInt64);", database.partitionKey_2);
                    }
                    /*if (database.tableType.ToLower() == "slot")
                    {
                        streamWriter.WriteLine("\t\t\tnSlot = default(short);");
                    }*/
                    streamWriter.WriteLine("\t\t\tcreateTime = DateTime.UtcNow");
                    streamWriter.WriteLine("\t\t\tcreateTime = DateTime.UtcNow");
                    foreach (var member in database.members)
                    {
                        streamWriter.WriteLine("\t\t\t{0} = default({1});", member.name, member.type);
                    }
                    streamWriter.WriteLine("\t\t}");
                    if (database.tableType.ToLower() == "slot")
                    {
                        streamWriter.WriteLine("\tpublic class DBSlot_{0} : DBSlot<{1}>{}", database.tableName, database.tableName);
                        streamWriter.WriteLine("\tpublic class DBSlotContainer_{0} : DBSlotContainer<DBSlot_{1}>{}", database.tableName, database.tableName);
                    }
                    else
                    {
                        streamWriter.WriteLine("\tpublic class DBBase_{0} : DBBase<{1}>{}", database.tableName, database.tableName);
                        streamWriter.WriteLine("\tpublic class DBBaseContainer_{0} : DBBaseContainer<DBBase_{1}>()", database.tableName, database.tableName);
                    }
                }
                streamWriter.WriteLine("}");
            }
        }


    }
}
