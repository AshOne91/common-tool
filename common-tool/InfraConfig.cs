using System;
using System.Collections.Generic;
using System.Text;

namespace common_tool
{
    public class InfraTemplate
    {
        public string type = string.Empty;
        public string name = string.Empty;
        public string path = string.Empty;
        public Dictionary<string, string> tables;
    }

    public class InfraMember
    {
        public string type = string.Empty;
        public string name = string.Empty;
        public string comment = string.Empty;
    }

    public class InfraProtocol
    {
        public int id;
        public string method = "react"; //noti, react 
        public string name = string.Empty;
        public string protocolType = string.Empty; //admin
        public List<InfraMember> reqMembers;
        public List<InfraMember> resMembers;
        public List<InfraMember> notiMembers;
    }

    public class InfraModel
    {
        public string name = string.Empty;
        public string parent = string.Empty;
        public List<InfraMember> members;
    }

    public class InfraDatabase
    {
        public string databaseName = string.Empty; //데이터베이스 이름
        public string tableType = string.Empty; //slot(슬롯형식), base(싱글형식)
        public string tableName = string.Empty; //table명
        public string partitionKey_1 = string.Empty;
        public string partitionKey_2 = string.Empty;
        public List<InfraMember> members;
    }

    public class InfraTemplateConfig
    {
        public string templateType = string.Empty;
        public string templateName = string.Empty;
        public string templateVersion = string.Empty;
        public List<string> tables;
        public List<InfraDatabase> databases;
        public List<InfraProtocol> protocols;
        public List<InfraModel> models;
    }
}
