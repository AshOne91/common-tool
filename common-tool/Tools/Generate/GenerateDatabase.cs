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
                GenerateUserDB(commonPath, templateType, namespaceValue);
                GenerateDBSave(commonPath, templateType, namespaceValue);
                GenerateDBLoad(commonPath, templateName, namespaceValue);
            }
        }
        void GenerateUserDB(string targetDir, string templateType, string namespaceValue)
        {
            string filePath = Path.Combine(targetDir, _config.templateName + "UserDB.cs");
            using (var streamWriter = new StreamWriter(filePath))
            {
                streamWriter.WriteLine("using System;");
                streamWriter.WriteLine("using System.Collections.Generic;");
                streamWriter.WriteLine("using Service.Net;");
                streamWriter.WriteLine("using Service.Core;");
                streamWriter.WriteLine("using Service.DB;");
                streamWriter.WriteLine("using GameBase.Template.GameBase;");
                streamWriter.WriteLine();
                streamWriter.WriteLine("namespace GameBase.{0}", namespaceValue);
                streamWriter.WriteLine("{");
                streamWriter.WriteLine("\tpublic partial class {0}UserDB : GameBaseUserDB", _config.templateName);
                streamWriter.WriteLine("\t{");
                foreach (var database in _config.databases)
                {
                    if (database.tableType.ToLower() == "slot")
                    {
                        streamWriter.WriteLine("\t\tpublic DBSlotContainer_{0} _dbSlotContainer_{1} = new DBSlotContainer_{2}();", database.tableName, database.tableName, database.tableName);
                    }
                    else
                    {
                        streamWriter.WriteLine("\t\tpublic DBBaseContainer_{0} _dbBaseContainer_{1} = new DBBaseContainer_{2}();", database.tableName, database.tableName, database.tableName);
                    }
                }
                streamWriter.WriteLine();
                streamWriter.WriteLine("\t\tpublic override void Copy(UserDB userSrc, bool isChanged)");
                streamWriter.WriteLine("\t\t{");
                streamWriter.WriteLine("\t\t\t{0}UserDB userDB = userSrc.GetReadUserDB<{1}UserDB>(ETemplateType.{2});", _config.templateName, _config.templateName, _config.templateType);
                foreach (var database in _config.databases)
                {
                    if (database.tableType.ToLower() == "slot")
                    {
                        streamWriter.WriteLine("\t\t\t_dbSlotContainer_{0}.Copy(userDB._dbSlotContainer_{1}, isChanged);", database.tableName, database.tableName);
                    }
                    else
                    {
                        streamWriter.WriteLine("\t\t\t_dbBaseContainer_{0}.Copy(userDB._dbBaseContainer_{1}, isChanged);", database.tableName, database.tableName);
                    }
                }
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine("\t}");
                streamWriter.WriteLine("}");

            }
        }
        void GenerateDBLoad(string targetDir, string templateType, string namespaceValue)
        {
            string filePath = Path.Combine(targetDir, _config.templateName + "DBLoad.cs");
            using (var streamWriter = new StreamWriter(filePath))
            {
                streamWriter.WriteLine("using System;");
                streamWriter.WriteLine("using System.Collections.Generic;");
                streamWriter.WriteLine("using Service.Net;");
                streamWriter.WriteLine("using Service.Core;");
                streamWriter.WriteLine("using Service.DB;");
                streamWriter.WriteLine("using GameBase.Template.GameBase;");
                streamWriter.WriteLine();

                streamWriter.WriteLine("namespace GameBase.{0}", namespaceValue);
                streamWriter.WriteLine("{");
                streamWriter.WriteLine("\tpublic partial class {0}UserDB", _config.templateName);
                streamWriter.WriteLine("\t{");
                foreach (var database in _config.databases)
                {
                    int paramCount = 0;
                    if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                    {
                        ++paramCount;
                    }
                    if (string.IsNullOrEmpty(database.partitionKey_2) == false)
                    {
                        ++paramCount;
                    }

                    string parmStr = string.Empty;
                    for (int i = 0; i < paramCount; ++i)
                    {
                        parmStr += "?";
                        if ((i + 1) != paramCount)
                        {
                            parmStr += ",";
                        }
                    }

                    string funcStr = string.Format("\t\tprivate void _Run_LoadUser_{0}(AdoDB adoDB, UInt64 user_db_key, UInt64 player_db_key)", database.tableName);
                    /*if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                    {
                        funcStr += "UInt64 ";
                        funcStr += database.partitionKey_1;
                        funcStr += ", ";
                    }
                    if (string.IsNullOrEmpty(database.partitionKey_2) == false)
                    {
                        funcStr += "UInt64 ";
                        funcStr += database.partitionKey_2;
                        funcStr += ")";
                    }*/

                    streamWriter.WriteLine(funcStr);
                    streamWriter.WriteLine("\t\t{");
                    streamWriter.WriteLine("\t\t\ttry");
                    streamWriter.WriteLine("\t\t\t{");
                    if (database.tableType.ToLower() == "slot")
                    {
                        streamWriter.WriteLine("\t\t\t\tQueryBuilder query = new QueryBuilder(\"call gp_player_{0}_load({1})\");", database.tableName.ToLower(), parmStr);
                        if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                        {
                            streamWriter.WriteLine("\t\t\t\tquery.SetInputParam(\"@p_{0}\", {1});", database.partitionKey_1, database.partitionKey_1);
                        }
                        if (string.IsNullOrEmpty(database.partitionKey_2) == false)
                        {
                            streamWriter.WriteLine("\t\t\t\tquery.SetInputParam(\"@p_{0}\", {1});", database.partitionKey_2, database.partitionKey_2);
                        }
                        streamWriter.WriteLine();
                        streamWriter.WriteLine("\t\t\t\tadoDB.Execute(query);");
                        streamWriter.WriteLine();
                        streamWriter.WriteLine("\t\t\t\twhile (adoDB.RecordWhileNotEOF())");
                        streamWriter.WriteLine("\t\t\t\t{");
                        streamWriter.WriteLine("\t\t\t\t\tshort nSlot = adoDB.RecordGetValue(\"slot\");");
                        streamWriter.WriteLine();
                        streamWriter.WriteLine("\t\t\t\t\tDBSlot_{0} slot = _dbSlotContainer_{1}.Insert(nSlot, false);", database.tableName, database.tableName);
                        streamWriter.WriteLine();
                        streamWriter.WriteLine("\t\t\t\t\tslot._isDeleted = adoDB.RecordGetValue(\"deleted\");");
                        if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                        {
                            streamWriter.WriteLine("\t\t\t\t\tslot._DBData.{0} = adoDB.RecordGetValue(\"{1}\");", database.partitionKey_1, database.partitionKey_1);
                        }
                        if (string.IsNullOrEmpty(database.partitionKey_2) == false)
                        {
                            streamWriter.WriteLine("\t\t\t\t\tslot._DBData.{0} = adoDB.RecordGetValue(\"{1}\");", database.partitionKey_2, database.partitionKey_2);
                        }
                        streamWriter.WriteLine("\t\t\t\t\tslot._DBData.create_time = adoDB.RecordGetTimeValue(\"create_time\");");
                        streamWriter.WriteLine("\t\t\t\t\tslot._DBData.update_time = adoDB.RecordGetTimeValue(\"update_time\");");
                        streamWriter.WriteLine();
                        foreach (var member in database.members)
                        {
                            if (member.type == "string")
                            {
                                streamWriter.WriteLine("\t\t\t\t\tslot._DBData.{0} = adoDB.RecordGetStrValue(\"{1}\");", member.name, member.name);
                            }
                            else if (member.type == "DateTime")
                            {
                                streamWriter.WriteLine("\t\t\t\t\tslot._DBData.{0} = adoDB.RecordGetTimeValue(\"{1}\");", member.name, member.name);
                            }
                            else
                            {
                                streamWriter.WriteLine("\t\t\t\t\tslot._DBData.{0} = adoDB.RecordGetValue(\"{1}\");", member.name, member.name);
                            }
                        }
                        streamWriter.WriteLine("\t\t\t\t}");
                        streamWriter.WriteLine("\t\t\t\tadoDB.RecordEnd();");
                    }
                    else
                    {
                        streamWriter.WriteLine("\t\t\t\tstring strResult;");
                        streamWriter.WriteLine("\t\t\t\tQueryBuilder query = new QueryBuilder(\"call gp_player_{0}_load({1})\");", database.tableName.ToLower(), parmStr);
                        if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                        {
                            streamWriter.WriteLine("\t\t\t\tquery.SetInputParam(\"@p_{0}\", {1});", database.partitionKey_1, database.partitionKey_1);
                        }
                        if (string.IsNullOrEmpty(database.partitionKey_2) == false)
                        {
                            streamWriter.WriteLine("\t\t\t\tquery.SetInputParam(\"@p_{0}\", {1});", database.partitionKey_2, database.partitionKey_2);
                        }
                        streamWriter.WriteLine();
                        streamWriter.WriteLine("\t\t\t\tadoDB.Execute(query);");
                        streamWriter.WriteLine();
                        streamWriter.WriteLine("\t\t\t\tif (adoDB.RecordNotEOF())");
                        streamWriter.WriteLine("\t\t\t\t{");
                        streamWriter.WriteLine("\t\t\t\t\t{0} r{1} = _dbBaseContainer_{2}.GetWriteData(false)._DBData;", database.tableName, database.tableName, database.tableName);
                        streamWriter.WriteLine();
                        if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                        {
                            streamWriter.WriteLine("\t\t\t\t\tr{0}.{1} = adoDB.RecordGetValue(\"{2}\");", database.tableName, database.partitionKey_1, database.partitionKey_1);
                        }
                        if (string.IsNullOrEmpty(database.partitionKey_2) == false)
                        {
                            streamWriter.WriteLine("\t\t\t\t\tr{0}.{1} = adoDB.RecordGetValue(\"{2}\");", database.tableName, database.partitionKey_2, database.partitionKey_2);
                        }
                        streamWriter.WriteLine("\t\t\t\t\tr{0}.create_time = adoDB.RecordGetTimeValue(\"create_time\");", database.tableName);
                        streamWriter.WriteLine("\t\t\t\t\tr{0}.update_time = adoDB.RecordGetTimeValue(\"update_time\");", database.tableName);
                        streamWriter.WriteLine();
                        foreach (var member in database.members)
                        {
                            if (member.type == "string")
                            {
                                streamWriter.WriteLine("\t\t\t\t\tr{0}.{1} = adoDB.RecordGetStrValue(\"{2}\");", database.tableName, member.name, member.name);
                            }
                            else if (member.type == "DateTime")
                            {
                                streamWriter.WriteLine("\t\t\t\t\tr{0}.{1} = adoDB.RecordGetTimeValue(\"{2}\");", database.tableName, member.name, member.name);
                            }
                            else
                            {
                                streamWriter.WriteLine("\t\t\t\t\tr{0}.{1} = adoDB.RecordGetValue(\"{2}\");", database.tableName, member.name, member.name);
                            }
                        }
                        streamWriter.WriteLine("\t\t\t\t}");
                        streamWriter.WriteLine("\t\t\t\telse");
                        streamWriter.WriteLine("\t\t\t\t{");
                        streamWriter.WriteLine("\t\t\t\t\tstrResult = \"[gp_player_{0}_load] No Result!\";", database.tableName.ToLower());
                        streamWriter.WriteLine("\t\t\t\t}");
                        streamWriter.WriteLine("\t\t\t\tadoDB.RecordEnd();");
                    }
                    streamWriter.WriteLine("\t\t\t}");
                    streamWriter.WriteLine("\t\t\tcatch (Exception e)");
                    streamWriter.WriteLine("\t\t\t{");
                    streamWriter.WriteLine("\t\t\t\tadoDB.RecordEnd();", database.tableName.ToLower());
                    streamWriter.WriteLine("\t\t\t\tthrow new Exception(\"[gp_player_{0}_load]\" + e.Message);", database.tableName.ToLower());
                    streamWriter.WriteLine("\t\t\t}");
                    streamWriter.WriteLine("\t\t}");
                }
                streamWriter.WriteLine("\t\tpublic override void LoadRun(AdoDB adoDB, UInt64 user_db_key, UInt64 player_db_key)");
                streamWriter.WriteLine("\t\t{");
                foreach (var database in _config.databases)
                {
                    streamWriter.WriteLine("\t\t\t_Run_LoadUser_{0}(adoDB, user_db_key, player_db_key);", database.tableName);
                }
                /*foreach (var database in _config.databases)
                {
                    string funcStr = string.Format("\t\t\t_Run_LoadUser_{0}(adoDB, ", database.tableName);
                    if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                    {
                        funcStr += "partitionKey_1, ";
                    }
                    if (string.IsNullOrEmpty(database.partitionKey_2) == false)
                    {
                        funcStr += "partitionKey_2);";
                    }
                    streamWriter.WriteLine(funcStr);
                }*/
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine("\t}");
                streamWriter.WriteLine("}");
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
                streamWriter.WriteLine("using GameBase.Template.GameBase;");
                streamWriter.WriteLine();

                streamWriter.WriteLine("namespace GameBase.{0}", namespaceValue);
                streamWriter.WriteLine("{");
                streamWriter.WriteLine("\tpublic partial class {0}UserDB", _config.templateName);
                streamWriter.WriteLine("\t{");
                foreach (var database in _config.databases)
                {
                    int slotParamCount = 0;
                    int singleParamCount = 0;
                    if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                    {
                        ++slotParamCount;
                        ++singleParamCount;
                    }
                    if (string.IsNullOrEmpty(database.partitionKey_2) == false)
                    {
                        ++slotParamCount;
                        ++singleParamCount;
                    }

                    //p_slot
                    //p_deleted
                    //p_createTime
                    //p_updateTime
                    slotParamCount = slotParamCount + 4;
                    slotParamCount = slotParamCount + database.members.Count;
                    string slotParamStr = string.Empty;
                    for (int i = 0; i < slotParamCount; ++i)
                    {
                        slotParamStr += "?";
                        if (i + 1 != slotParamCount)
                        {
                            slotParamStr += ",";
                        }
                    }

                    //p_createTime
                    //p_updateTime
                    singleParamCount = singleParamCount + 2;
                    singleParamCount = singleParamCount + database.members.Count;
                    string singleParamStr = string.Empty;
                    for (int i = 0; i < singleParamCount; ++i)
                    {
                        singleParamStr += "?";
                        if (i + 1 != singleParamCount)
                        {
                            singleParamStr += ",";
                        }
                    }

                    /*string funcStr = string.Format("\t\tprivate void _Run_SaveUser_{0}(AdoDB adoDB, ", database.tableName);
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
                    }
                    funcStr = funcStr + ")";
                    streamWriter.WriteLine(funcStr);*/
                    streamWriter.WriteLine("\t\tprivate void _Run_SaveUser_{0}(AdoDB adoDB, UInt64 user_db_key, UInt64 player_db_key)", database.tableName);
                    streamWriter.WriteLine("\t\t{");
                    streamWriter.WriteLine("\t\t\ttry");
                    streamWriter.WriteLine("\t\t\t{");
                    if (database.tableType.ToLower() == "slot")
                    {
                        streamWriter.WriteLine("\t\t\t\t_dbSlotContainer_{0}.ForEach((DBSlot_{1} slot) =>", database.tableName, database.tableName);
                        streamWriter.WriteLine("\t\t\t\t{");
                        streamWriter.WriteLine("\t\t\t\t\tQueryBuilder query = new QueryBuilder(\"call gp_player_{0}_save({1})\");", database.tableName.ToLower(), slotParamStr);
                        if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                        {
                            streamWriter.WriteLine("\t\t\t\t\tquery.SetInputParam(\"@p_{0}\", {1});", database.partitionKey_1, database.partitionKey_1);
                        }
                        if (string.IsNullOrEmpty(database.partitionKey_2) == false)
                        {
                            streamWriter.WriteLine("\t\t\t\t\tquery.SetInputParam(\"@p_{0}\", {1});", database.partitionKey_2, database.partitionKey_2);
                        }
                        streamWriter.WriteLine();
                        streamWriter.WriteLine("\t\t\t\t\tif (!slot._isDeleted)");
                        streamWriter.WriteLine("\t\t\t\t\t{");
                        streamWriter.WriteLine("\t\t\t\t\t\tquery.SetInputParam(\"@p_slot\", slot._nSlot);");
                        streamWriter.WriteLine("\t\t\t\t\t\tquery.SetInputParam(\"@p_deleted\", 0);");
                        streamWriter.WriteLine("\t\t\t\t\t\tquery.SetInputParam(\"@p_create_time\", slot._DBData.create_time);");
                        streamWriter.WriteLine("\t\t\t\t\t\tquery.SetInputParam(\"@p_update_time\", DateTime.UtcNow);");
                        streamWriter.WriteLine();
                        foreach(var member in database.members)
                        {
                            streamWriter.WriteLine("\t\t\t\t\t\tquery.SetInputParam(\"@p_{0}\", slot._DBData.{1});", member.name, member.name);
                        }
                        streamWriter.WriteLine("\t\t\t\t\t}");
                        streamWriter.WriteLine("\t\t\t\t\telse");
                        streamWriter.WriteLine("\t\t\t\t\t{");
                        streamWriter.WriteLine("\t\t\t\t\t\tquery.SetInputParam(\"@p_slot\", slot._nSlot);");
                        streamWriter.WriteLine("\t\t\t\t\t\tquery.SetInputParam(\"@p_deleted\", 1);");
                        streamWriter.WriteLine("\t\t\t\t\t\tquery.SetInputParam(\"@p_create_time\", DateTime.UtcNow);");
                        streamWriter.WriteLine("\t\t\t\t\t\tquery.SetInputParam(\"@p_update_time\", DateTime.UtcNow);");
                        streamWriter.WriteLine();
                        foreach (var member in database.members)
                        {
                            streamWriter.WriteLine("\t\t\t\t\t\tquery.SetInputParam(\"@p_{0}\", default({1}));", member.name, member.type);
                        }
                        streamWriter.WriteLine("\t\t\t\t\t}");
                        streamWriter.WriteLine("\t\t\t\t\tadoDB.ExecuteNoRecords(query);");
                        streamWriter.WriteLine("\t\t\t\t}, true);");
                    }
                    else
                    {
                        streamWriter.WriteLine("\t\t\t\tif (_dbBaseContainer_{0}.GetReadData()._isChanged == false)", database.tableName);
                        streamWriter.WriteLine("\t\t\t\t{");
                        streamWriter.WriteLine("\t\t\t\t\treturn;");
                        streamWriter.WriteLine("\t\t\t\t}");
                        streamWriter.WriteLine();
                        streamWriter.WriteLine("\t\t\t\t{0} r{1} = _dbBaseContainer_{2}.GetReadData()._DBData;", database.tableName, database.tableName, database.tableName);
                        streamWriter.WriteLine();
                        streamWriter.WriteLine("\t\t\t\tQueryBuilder query = new QueryBuilder(\"call gp_player_{0}_save({1})\");", database.tableName.ToLower(), singleParamStr);
                        if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                        {
                            streamWriter.WriteLine("\t\t\t\tquery.SetInputParam(\"@p_{0}\", {1});", database.partitionKey_1, database.partitionKey_1);
                        }
                        if (string.IsNullOrEmpty(database.partitionKey_2) == false)
                        {
                            streamWriter.WriteLine("\t\t\t\tquery.SetInputParam(\"@p_{0}\", {1});", database.partitionKey_2, database.partitionKey_2);
                        }
                        streamWriter.WriteLine("\t\t\t\tquery.SetInputParam(\"@p_create_time\", r{0}.create_time);", database.tableName);
                        streamWriter.WriteLine("\t\t\t\tquery.SetInputParam(\"@p_update_time\", r{0}.update_time);", database.tableName);
                        streamWriter.WriteLine();
                        foreach (var member in database.members)
                        {
                            streamWriter.WriteLine("\t\t\t\tquery.SetInputParam(\"@p_{0}\", r{1}.{2});", member.name, database.tableName, member.name);
                        }
                        streamWriter.WriteLine();
                        streamWriter.WriteLine("\t\t\t\tadoDB.ExecuteNoRecords(query);");
                    }
                    streamWriter.WriteLine("\t\t\t}");
                    streamWriter.WriteLine("\t\t\tcatch (Exception e)");
                    streamWriter.WriteLine("\t\t\t{");
                    streamWriter.WriteLine("\t\t\t\tthrow new Exception(\"[gp_player_{0}_save]\" + e.Message);", database.tableName.ToLower());
                    streamWriter.WriteLine("\t\t\t}");
                    streamWriter.WriteLine("\t\t}");
                }
                streamWriter.WriteLine("\t\tpublic override void SaveRun(AdoDB adoDB, UInt64 user_db_key, UInt64 player_db_key)");
                streamWriter.WriteLine("\t\t{");
                foreach (var database in _config.databases)
                {
                    /*string funcStr = string.Format("\t\t\t_Run_SaveUser_{0}(adoDB, ", database.tableName);
                    if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                    {
                        funcStr += "partitionKey_1, ";
                    }
                    if (string.IsNullOrEmpty(database.partitionKey_2) == false)
                    {
                        funcStr += "partitionKey_2);";
                    }*/
                    streamWriter.WriteLine("\t\t\t_Run_SaveUser_{0}(adoDB, user_db_key, player_db_key);", database.tableName);
                }
                streamWriter.WriteLine("\t\t}");
                streamWriter.WriteLine("\t}");
                streamWriter.WriteLine("}");
            }
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
                streamWriter.WriteLine("using GameBase.Template.GameBase;");
                streamWriter.WriteLine();

                streamWriter.WriteLine("namespace GameBase.{0}", namespaceValue);
                streamWriter.WriteLine("{");
                foreach (var database in _config.databases)
                {
                    streamWriter.WriteLine("\tpublic class {0} : BaseDBClass", database.tableName);
                    streamWriter.WriteLine("\t{");
                    if (string.IsNullOrEmpty(database.partitionKey_1) && string.IsNullOrEmpty(database.partitionKey_2))
                    {
                        throw new Exception($"failed create : partitionKey empty. {database.tableName}");
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
                    streamWriter.WriteLine("\t\tpublic DateTime create_time;");

                    streamWriter.WriteLine("\t\t/// <sumary>");
                    streamWriter.WriteLine("\t\t/// 업데이트 시간");
                    streamWriter.WriteLine("\t\t/// <sumary>");
                    streamWriter.WriteLine("\t\tpublic DateTime update_time;");


                    foreach (var member in database.members)
                    {
                        streamWriter.WriteLine("\t\t/// <sumary>");
                        streamWriter.WriteLine("\t\t/// {0}", member.comment);
                        streamWriter.WriteLine("\t\t/// <sumary>");
                        streamWriter.WriteLine("\t\tpublic {0} {1};", member.type, member.name);
                    }
                    streamWriter.WriteLine("\t\tpublic {0}() {{ Reset(); }}", database.tableName);
                    streamWriter.WriteLine("\t\t~{0}() {{ Reset(); }}", database.tableName);
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
                    streamWriter.WriteLine("\t\t\tcreate_time = DateTime.UtcNow;");
                    streamWriter.WriteLine("\t\t\tupdate_time = DateTime.UtcNow;");
                    foreach (var member in database.members)
                    {
                        if (member.type == "string")
                        {
                            streamWriter.WriteLine("\t\t\t{0} = string.Empty;", member.name, member.type);
                        }
                        else
                        {
                            streamWriter.WriteLine("\t\t\t{0} = default({1});", member.name, member.type);
                        }
                    }
                    streamWriter.WriteLine("\t\t}");
                    streamWriter.WriteLine("\t}");
                    if (database.tableType.ToLower() == "slot")
                    {
                        streamWriter.WriteLine("\tpublic class DBSlot_{0} : DBSlot<{1}>{{}}", database.tableName, database.tableName);
                        streamWriter.WriteLine("\tpublic class DBSlotContainer_{0} : DBSlotContainer<DBSlot_{1}, {2}>{{}}", database.tableName, database.tableName, database.tableName);
                    }
                    else
                    {
                        streamWriter.WriteLine("\tpublic class DBBase_{0} : DBBase<{1}>{{}}", database.tableName, database.tableName);
                        streamWriter.WriteLine("\tpublic class DBBaseContainer_{0} : DBBaseContainer<DBBase_{1}, {2}>{{}}", database.tableName, database.tableName, database.tableName);
                    }
                }
                streamWriter.WriteLine("}");
            }
        }


    }
}
