﻿using common_tool.ToolBase;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace common_tool.Tools.Generate
{
    public class GenerateSql : ActionBase
    {
        readonly string _commonDir = "Common";
        InfraTemplateConfig _config = null;

        public GenerateSql(Parameter param) : base(param)
        {

        }

        public override void Run()
        {
            if (_param._dicActionParam.ContainsKey("--target-path") == false)
            {
                throw new Exception($"not found parameter - \"--target-path\"");
            }
            var targetDir = new DirectoryInfo(_param._dicActionParam["--target-path"]);
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
                if (_param._dicActionParam.ContainsKey("--id") == false)
                {
                    throw new Exception($"not found parameter - \"--id\"");
                }
                if (_param._dicActionParam.ContainsKey("--pw") == false)
                {
                    throw new Exception($"not found parameter - \"--pw\"");
                }
                if (_param._dicActionParam.ContainsKey("--ip") == false)
                {
                    throw new Exception($"not found parameter - \"--ip\"");
                }
                if (_param._dicActionParam.ContainsKey("--port") == false)
                {
                    throw new Exception($"not found parameter - \"--port\"");
                }
                if (_param._dicActionParam.ContainsKey("--name") == false)
                {
                    throw new Exception($"not found parameter - \"--name\"");
                }
                if (_param._dicActionParam.ContainsKey("--type") == false)
                {
                    throw new Exception($"not found parameter - \"--type\"");
                }

                //string str = "DELIMITER $$\r\n\r\n";
                foreach (var database in _config.databases)
                {
                    if (database.databaseType.ToLower().Trim() != _param._dicActionParam["--type"].ToLower().Trim())
                    {
                        continue;
                    }
                    Process(database, _param._dicActionParam["--id"], _param._dicActionParam["--pw"], _param._dicActionParam["--ip"], _param._dicActionParam["--port"], _param._dicActionParam["--name"], _param._dicActionParam["--type"], targetDir.FullName);
                }
                //str += "\r\nDELIMITER ;";
            }
        }

        void Process(InfraDatabase database, string id, string pw, string ip, string port, string name, string type, string targetDir)
        {
            bool IsSuccess = true;
            try
            {
                string filePath = Path.Combine(targetDir, _config.templateName + "_" + name + "_" + type + "DB.sql");

                string sql = string.Empty;
                using (var streamWriter = new StreamWriter(filePath))
                {
                    sql += "USE " + name + ";\r\n";
                    sql += DeleteTable(database);
                    sql += CreateTable(database);
                    sql += AddUniqueIndex(database);
                    sql += DeleteLoadProc(database);
                    sql += "DELIMITER $$\r\n\r\n";
                    sql += CreateLoadProc(database);
                    sql += "\r\nDELIMITER ;";
                    sql += DeleteSaveProc(database);
                    sql += "DELIMITER $$\r\n\r\n";
                    sql += CreateSaveProc(database);
                    sql += "\r\nDELIMITER ;";

                    streamWriter.Write(sql);
                }
                var connection = DBFactory.GetConnection("mysql", ip, name, id, pw, port);
                connection.Open();

                MySqlScript script = new MySqlScript((MySqlConnection)connection, sql);
                script.Execute();
                connection.Close();
            }
            catch (Exception e)
            {
                IsSuccess = false;
                Console.WriteLine(e.Message);
            }

            if (!IsSuccess)
            {
                Console.ReadKey(true);
                System.Environment.Exit(1);
            }
        }

        static string MakeProc()
        {
            /*string str = "DELIMITER $$\r\n\r\n";
            foreach (var)*/
            //!!FIXME
            return string.Empty;
        }
        static string DeleteTable(InfraDatabase database)
        {
            string str = "DROP TABLE if exists " + "table_auto_" + database.tableName.ToLower() + ";\r\n";
            return str;
        }
        static string CreateTable(InfraDatabase database)
        {
            string str = "CREATE TABLE " + "table_auto_" + database.tableName.ToLower() + " (\t\n";
            str += "\tidx BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,\r\n";
            if (string.IsNullOrEmpty(database.partitionKey_1) == false)
            {
                str += "\t" + database.partitionKey_1 + " BIGINT UNSIGNED NOT NULL DEFAULT 0,\r\n";
            }
            if (string.IsNullOrEmpty(database.partitionKey_2) == false)
            {
                str += "\t" + database.partitionKey_2 + " BIGINT UNSIGNED NOT NULL DEFAULT 0,\r\n";
            }
            if (database.tableType == "slot")
            {
                str += "\tslot SMALLINT NOT NULL DEFAULT 0 COMMENT '슬롯 번호',\r\n";
                str += "\tdeleted BIT NOT NULL DEFAULT b'0' COMMENT '삭제 여부',\r\n";
            }
            str += "\tcreate_time DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '생성시간',\r\n";
            str += "\tupdate_time DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '수정시간',\r\n";
            int count = 0;
            foreach (var member in database.members)
            {
                count++;
                str += "\t" + member.name +" "+ GetTableType(member.type) +" COMMENT " +"'"+ member.comment + "'" +",\r\n";
            }
            str += "\tPRIMARY KEY(idx)\r\n";
            str += ")\r\n";
            str += "ENGINE = INNODB,\r\n";
            str += "CHARACTER SET utf8mb4,\r\n";
            str += "COLLATE utf8mb4_general_ci;\r\n";
            return str;
        }
        static string AddUniqueIndex(InfraDatabase database)
        {
            string str = "ALTER TABLE " + "table_auto_" + database.tableName.ToLower() + "\r\n";
            str += "ADD UNIQUE INDEX ix_" + database.tableName.ToLower() + (string.IsNullOrEmpty(database.partitionKey_1) == false ? "_" + database.partitionKey_1 : "")
                + (string.IsNullOrEmpty(database.partitionKey_2) == false ? "_" + database.partitionKey_2 : "") + (database.tableType == "slot" ? "_slot" : "") + " (";

            if (string.IsNullOrEmpty(database.partitionKey_1) == false)
            {
                str += database.partitionKey_1;
            }
            if (string.IsNullOrEmpty(database.partitionKey_2) == false)
            {
                if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                {
                    str += ",";
                }
                str += database.partitionKey_2;
            }
            if (database.tableType == "slot")
            {
                str += ",slot";
            }
            str += ");\r\n";
            return str;
        }
        static string GetTableType(string type)
        {
            string ret = string.Empty;
            switch (type.Trim())
            {
                case "bool":
                case "Boolean":
                    ret = "BIT NOT NULL DEFAULT b'0'";
                    break;
                case "Byte":
                case "byte":
                    ret = "TINYINT UNSIGNED NOT NULL DEFAULT 0";
                    break;
                case "SByte":
                case "sbyte":
                    ret = "TINYINT NOT NULL DEFAULT 0";
                    break;
                case "short":
                case "Int16":
                    ret = "SMALLINT NOT NULL DEFAULT 0";
                    break;
                case "ushort":
                case "UInt16":
                    ret = "SMALLINT UNSIGNED NOT NULL DEFAULT 0";
                    break;
                case "Int32":
                case "int":
                    ret = "INT NOT NULL DEFAULT 0";
                    break;
                case "UInt32":
                case "uint":
                    ret = "INT UNSIGNED NOT NULL DEFAULT 0";
                    break;
                case "Int64":
                case "long":
                    ret = "BIGINT NOT NULL DEFAULT 0";
                    break;
                case "UInt64":
                case "ulong":
                    ret = "BIGINT UNSIGNED NOT NULL DEFAULT 0";
                    break;
                case "char":
                case "string":
                    ret = "VARCHAR(100) NOT NULL DEFAULT ''";
                    break;
                case "float":
                case "Single":
                    ret = "FLOAT NOT NULL DEFAULT 0";
                    break;
                case "double":
                case "Double":
                    ret = "DOUBLE NOT NULL DEFAULT 0";
                    break;
                case "DateTime":
                    ret = "DATETIME  NOT NULL DEFAULT CURRENT_TIMESTAMP";
                    break;
            }
            return ret;
        }


        static string DeleteLoadProc(InfraDatabase database)
        {
            string str = "DROP PROCEDURE if exists " + "gp_player_" + database.tableName.ToLower() + "_load;\r\n";
            return str;
        }
        static string CreateLoadProc(InfraDatabase database)
        {
            string str = "";
            str += "CREATE PROCEDURE " + "gp_player_" + database.tableName.ToLower() + "_load(\r\n";
            if (string.IsNullOrEmpty(database.partitionKey_1) == false)
            {
                str += "    IN p_" + database.partitionKey_1 + " BIGINT UNSIGNED";
            }
            if (string.IsNullOrEmpty(database.partitionKey_2) == false)
            {
                if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                {
                    str += ",\r\n";
                }
                str += "    IN p_" + database.partitionKey_2 + " BIGINT UNSIGNED\r\n";
            }
            str += ")\r\n";
            str += "BEGIN\r\n";
            str += "\r\n";
            str += "	DECLARE ProcParam varchar(4000);\r\n";
            str += "\r\n";
            str += "	DECLARE EXIT HANDLER FOR SQLEXCEPTION\r\n";
            str += "	BEGIN\r\n";
            str += "		GET DIAGNOSTICS @cno = NUMBER;\r\n";
            str += "			GET DIAGNOSTICS CONDITION @cno\r\n";
            str += "			@p_ErrorState = RETURNED_SQLSTATE, @p_ErrorNo = MYSQL_ERRNO, @p_ErrorMessage = MESSAGE_TEXT;\r\n";
            if (string.IsNullOrEmpty(database.partitionKey_1) == false)
            {
                str += "		SET ProcParam = CONCAT(p_" + database.partitionKey_1;
            }
            if (string.IsNullOrEmpty(database.partitionKey_2) == false)
            {
                if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                {
                    str += ",', ', p_" + database.partitionKey_2 ;
                }
                else
                {
                    str += "		SET ProcParam = CONCAT(p_" + database.partitionKey_2;
                }
            }
            str += ");\r\n";
            str += "		INSERT INTO table_errorlog(procedure_name, error_state, error_no, error_message, param) VALUES('gp_player_" + database.tableName.ToLower() + "_load', @p_ErrorState, @p_ErrorNo, @p_ErrorMessage, ProcParam);\r\n";
            str += "		RESIGNAL;\r\n";
            str += "	END;\r\n";
            str += "\r\n";

            string selectColum = string.Empty;
            string param = string.Empty;
            if (string.IsNullOrEmpty(database.partitionKey_1) == false)
            {
                selectColum += database.partitionKey_1;
                param += "p_" + database.partitionKey_1;
            }
            if (string.IsNullOrEmpty(database.partitionKey_2) == false)
            {
                if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                {
                    selectColum += ",";
                    param += ",";
                }
                selectColum += database.partitionKey_2;
                param += "p_" + database.partitionKey_2;
            }

            string condition = string.Empty;
            if (string.IsNullOrEmpty(database.partitionKey_1) == false)
            {
                condition = " WHERE " + database.partitionKey_1 + " = p_" + database.partitionKey_1;

            }
            if (string.IsNullOrEmpty(database.partitionKey_2) == false)
            {
                if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                {
                    condition += " AND " + database.partitionKey_2 + " = p_" + database.partitionKey_2;
                }
                else
                {
                    condition = " WHERE " + database.partitionKey_2 + " = p_" + database.partitionKey_2;
                }
            }

            if (!(database.tableType == "slot"))
            {
                str += "    IF NOT EXISTS(SELECT " + selectColum + " FROM table_auto_" + database.tableName.ToLower() + condition + ") THEN\r\n";
                str += "        INSERT INTO table_auto_" + database.tableName.ToLower() + "(" + selectColum + ")" + "VALUES(" + param + ");\r\n";
                str += "    END IF;\r\n";
                str += "\r\n";
            }

            str += "    SELECT * FROM table_auto_" + database.tableName.ToLower() + condition + ((database.tableType == "slot") ? " AND deleted = 0" : "") + ";\r\n";
            str += "\r\n";
            str += "END\r\n";
            str += "$$";
            return str;
        }
        static string DeleteSaveProc(InfraDatabase database)
        {
            string str = @"DROP PROCEDURE if exists " + "gp_player_" + database.tableName.ToLower() + "_save;\r\n";
            return str;
        }
        static string CreateSaveProc(InfraDatabase database)
        {
            string str = string.Empty;
            str += "CREATE PROCEDURE " + "gp_player_" + database.tableName.ToLower() + "_save(\r\n";
            str += "" + GetStringInputParam(database) + "\r\n)\r\n";
            str += "BEGIN\r\n";
            str += "\r\n";
            str += "    DECLARE ProcParam varchar(4000);\r\n";
            str += "\r\n";
            str += "    DECLARE EXIT HANDLER FOR SQLEXCEPTION\r\n";
            str += "    BEGIN\r\n";
            str += "        GET DIAGNOSTICS @cno = NUMBER;\r\n";
            str += "			GET DIAGNOSTICS CONDITION @cno\r\n";
            str += "			@p_ErrorState = RETURNED_SQLSTATE, @p_ErrorNo = MYSQL_ERRNO, @p_ErrorMessage = MESSAGE_TEXT;\r\n";
            str += "		SET ProcParam = CONCAT(" + GetStringLogParam(database) + ");\r\n";
            str += "		INSERT INTO table_errorlog(procedure_name, error_state, error_no, error_message, param)\r\n";
            str += "			VALUES('gp_player_" + database.tableName.ToLower() + "_save', @p_ErrorState, @p_ErrorNo, @p_ErrorMessage, ProcParam);\r\n";
            str += "		RESIGNAL;\r\n";
            str += "	END;\r\n";
            str += "\r\n";
            str += "	" + GetStringInsertParam(database) + "\r\n";
            str += "\r\n";
            str += "END\r\n";
            str += "$$";
            return str;
        }
        static string GetStringInsertParam(InfraDatabase database)
        {
            string str = string.Empty;
            if (database.tableType == "slot")
            {
                str += "INSERT INTO " + "table_auto_" + database.tableName.ToLower() + " (\r\n";
                if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                {
                    str += "\t\t" + database.partitionKey_1;
                    if (string.IsNullOrEmpty (database.partitionKey_2) == true) 
                    {
                        str += ",\r\n";
                    }
                }
                if (string.IsNullOrEmpty(database.partitionKey_2) == false)
                {
                    if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                    {
                        str += ",\r\n";
                    }
                    str += "\t\t" + database.partitionKey_2 + ",\r\n";
                }
                str += "\t\tslot,\r\n";
                str += "\t\tdeleted,\r\n";
                str += "\t\tupdate_time,\r\n";
                int count = 0;
                foreach (var member in database.members)
                {
                    count++;
                    str += "\t\t" + member.name + ((count < database.members.Count) ? ",\r\n" : "\r\n");
                }
                str += "\t)\r\n";
                str += "\tVALUES (\r\n";
                if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                {
                    str += "\t\tp_" + database.partitionKey_1;
                    if (string.IsNullOrEmpty(database.partitionKey_2) == true)
                    {
                        str += ",\r\n";
                    }
                }
                if (string.IsNullOrEmpty(database.partitionKey_2) == false)
                {
                    if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                    {
                        str += ",\r\n";
                    }
                    str += "\t\tp_" + database.partitionKey_2 + ",\r\n";
                }
                str += "\t\tp_slot,\r\n";
                str += "\t\tp_deleted,\r\n";
                str += "\t\tp_update_time,\r\n";
                count = 0;
                foreach (var member in database.members)
                {
                    count++;
                    str += "\t\tp_" + member.name + ((count < database.members.Count) ? ",\r\n" : "\r\n");
                }
                str += "\t)\r\n";
                str += "\tON DUPLICATE KEY\r\n";
                str += "\tUPDATE\r\n";
                str += "\t\tcreate_time = CASE WHEN deleted = 1 AND p_deleted = 0 THEN CURRENT_TIMESTAMP() ELSE create_time END,\r\n";
                str += "\t\tdeleted = p_deleted,\r\n";
                str += "\t\tupdate_time = p_update_time,\r\n";
                count = 0;
                foreach (var member in database.members)
                {
                    count++;
                    str += "\t\t" + member.name + " = p_" + member.name + ((count < database.members.Count) ? ",\r\n" : "");
                }
                str += ";";
            }
            else
            {
                str += "UPDATE " + "table_auto_" + database.tableName.ToLower() + "\r\n";
                str += "    SET\r\n";
                str += "\t\t" + GetCombineKey(database, ",") + ",\r\n";
                str += "\t\tcreate_time = p_create_time,\r\n";
                str += "\t\tupdate_time = p_update_time,\r\n";
                int count = 0;
                foreach (var member in database.members)
                {
                    count++;
                    str += "\t\t" + member.name + " = p_" + member.name + ((count < database.members.Count) ? "," : "") + "\r\n";
                }
                str += "\t\tWHERE " + GetCombineKey(database, " AND") + ";\r\n";
            }
            return str;
        }
        static string GetCombineKey(InfraDatabase database, string sep)
        {
            string strKey = string.Empty;
            if (string.IsNullOrEmpty(database.partitionKey_1) == false)
            {
                strKey += database.partitionKey_1 + " = p_" + database.partitionKey_1;
            }
            if (string.IsNullOrEmpty(database.partitionKey_2) == false)
            {
                if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                {
                    strKey += sep + "\r\n";
                }
                strKey += "\t\t" + database.partitionKey_2 + " = p_" + database.partitionKey_2;
            }
            return strKey;
        }
        static string GetStringLogParam(InfraDatabase database)
        {
            string str = string.Empty;
            int count = 0;
            if (string.IsNullOrEmpty(database.partitionKey_1) == false)
            {
                str += "p_" + database.partitionKey_1 +",','";
                if (string.IsNullOrEmpty(database.partitionKey_2) == true)
                {
                    str += ",";
                }
            }
            if (string.IsNullOrEmpty(database.partitionKey_2) == false)
            {
                if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                {
                    str += ",";
                }
                str += "p_" + database.partitionKey_2 + ",',',";
            }
            if (database.tableType == "slot")
            {
                str += "p_slot, ',', p_deleted, ',', p_create_time, ',', p_update_time, ',', ";
            }
            foreach (var member in database.members)
            {
                count++;
                str += "p_" + member.name + ((count < database.members.Count) ? ",','," : "");
            }
            return str;
        }
        static string GetStringInputParam(InfraDatabase database)
        {
            string str = string.Empty;
            if (string.IsNullOrEmpty(database.partitionKey_1) == false)
            {
                str += "    IN p_" + database.partitionKey_1 + " BIGINT UNSIGNED";
            }
            if (string.IsNullOrEmpty(database.partitionKey_2) == false)
            {
                if (string.IsNullOrEmpty(database.partitionKey_1) == false)
                {
                    str += ",\r\n";
                }
                str += "    IN p_" + database.partitionKey_2 + " BIGINT UNSIGNED";
            }
            if (database.tableType == "slot")
            {
                str += ",\r\n";
                str += "    IN p_slot SMALLINT";
                str += ",\r\n";
                str += "    IN p_deleted BIT";
            }
            str += ",\r\n";
            str += "    IN p_create_time DATETIME";
            str += ",\r\n";
            str += "    IN p_update_time DATETIME";
            foreach (var member in database.members)
            {
                str += ",\r\n";
                str += "    IN p_" + member.name + " " + GetStringInputParamType(member.type);
            }
            return str;
        }

        static string GetStringInputParamType(string type)
        {
            string ret = string.Empty;
            switch (type.Trim())
            {
                case "bool":
                case "Boolean":
                    ret = "BIT";
                    break;
                case "Byte":
                case "byte":
                    ret = "TINYINT UNSIGNED";
                    break;
                case "SByte":
                case "sbyte":
                    ret = "TINYINT";
                    break;
                case "short":
                case "Int16":
                    ret = "SMALLINT";
                    break;
                case "ushort":
                case "UInt16":
                    ret = "SMALLINT UNSIGNED";
                    break;
                case "Int32":
                case "int":
                    ret = "INT";
                    break;
                case "UInt32":
                case "uint":
                    ret = "INT UNSIGNED";
                    break;
                case "Int64":
                case "long":
                    ret = "BIGINT";
                    break;
                case "UInt64":
                case "ulong":
                    ret = "BIGINT UNSIGNED";
                    break;
                case "char":
                case "string":
                    ret = "VARCHAR(100)";
                    break;
                case "float":
                case "Single":
                    ret = "FLOAT";
                    break;
                case "double":
                case "Double":
                    ret = "DOUBLE";
                    break;
                case "DateTime":
                    ret = "DATETIME";
                    break;
            }
            return ret;
        }
    }
}
