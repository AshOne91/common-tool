using System;
using System.Collections.Generic;
using System.Text;

namespace common_tool.ToolBase
{
    public class DBFactory
    {
        public const string _type_mysql = "MySql.Data.MySqlClient.MySqlConnection";
        public const string _type_mssql = "System.Data.SqlClient.SqlConnection";

		public delegate void DbCreateCallback();
		public delegate void DbResultCallback(System.Data.Common.DbDataReader rd);
		public delegate void DbFinishCallback(string CommandText, object state);

		public static System.Data.Common.DbConnection GetConnection(string dbtype, string connection_string)
		{
			System.Data.Common.DbConnection conn = null;
			switch (dbtype.ToLower())
			{
				case "mysql":
					conn = new MySql.Data.MySqlClient.MySqlConnection();
					conn.ConnectionString = connection_string;
					break;
				case "mssql":
				default:
					conn = new System.Data.SqlClient.SqlConnection();
					conn.ConnectionString = connection_string;
					break;
			}
			return conn;
		}

		public static System.Data.Common.DbConnection GetConnection(string dbtype, string hostname, string database, string id, string pwd, string port)
		{
			System.Data.Common.DbConnection conn = null;
			switch (dbtype.ToLower())
			{
				case "mysql":
					conn = new MySql.Data.MySqlClient.MySqlConnection();
					conn.ConnectionString = "Server=" + hostname + ";Port=" + port + ";Database=" + database + ";Uid=" + id + ";Pwd=" + pwd + ";Old GUIDs=true";
					break;
				case "mssql":
				default:
					conn = new System.Data.SqlClient.SqlConnection();
					conn.ConnectionString = "Data Source=" + hostname + ";Initial Catalog=" + database + ";User id=" + id + "; password=" + pwd + "; Asynchronous Processing=true; MultipleActiveResultSets=True;";
					break;
			}
			return conn;
		}

		public static System.Data.Common.DbCommand GetDBCommand(string query, System.Data.Common.DbConnection conn, System.Data.CommandType commandtype)
		{
			System.Data.Common.DbCommand dbcommand = null;
			switch (conn.GetType().ToString())
			{
				case DBFactory._type_mysql:
					dbcommand = new MySql.Data.MySqlClient.MySqlCommand(query, conn as MySql.Data.MySqlClient.MySqlConnection);
					dbcommand.CommandType = commandtype;
					dbcommand.Connection = conn;
					break;
				case DBFactory._type_mssql:
				default:
					dbcommand = new System.Data.SqlClient.SqlCommand(query, conn as System.Data.SqlClient.SqlConnection);
					dbcommand.CommandType = commandtype;
					break;
			}

			return dbcommand;
		}

		public static System.Data.Common.DbParameter GetDBParameter(System.Data.Common.DbConnection conn, string name, object arg)
		{
			System.Data.Common.DbParameter param = null;
			switch (conn.GetType().ToString())
			{
				case DBFactory._type_mysql:
					{
						var dbtype = MySql.Data.MySqlClient.MySqlDbType.VarString;
						switch (arg.GetType().ToString())
						{
							case "System.Byte":
								dbtype = MySql.Data.MySqlClient.MySqlDbType.Byte;
								break;
							case "System.Int16":
								dbtype = MySql.Data.MySqlClient.MySqlDbType.Int16;
								break;
							case "System.Int32":
								dbtype = MySql.Data.MySqlClient.MySqlDbType.Int32;
								break;
							case "System.Int64":
								dbtype = MySql.Data.MySqlClient.MySqlDbType.Int64;
								break;
							case "System.UInt64":
								dbtype = MySql.Data.MySqlClient.MySqlDbType.UInt64;
								break;
							case "System.Single":
								dbtype = MySql.Data.MySqlClient.MySqlDbType.Float;
								break;
							case "System.Double":
								dbtype = MySql.Data.MySqlClient.MySqlDbType.Double;
								break;
							case "System.Decimal":
								dbtype = MySql.Data.MySqlClient.MySqlDbType.Decimal;
								break;
							case "System.String":
								dbtype = MySql.Data.MySqlClient.MySqlDbType.VarString;
								break;
							case "System.DateTime":
								dbtype = MySql.Data.MySqlClient.MySqlDbType.DateTime;
								break;
						}
						param = new MySql.Data.MySqlClient.MySqlParameter("?" + name, dbtype);
						param.Value = arg;
					}
					break;
				case DBFactory._type_mssql:
				default:
					{
						//var dbtype = System.Data.SqlDbType.NVarChar;
						//switch (arg.GetType().ToString())
						//{
						//    case "System.Byte":
						//        dbtype = System.Data.SqlDbType.TinyInt;
						//        break;
						//    case "System.Int16":
						//        dbtype = System.Data.SqlDbType.SmallInt;
						//        break;
						//    case "System.Int32":
						//        dbtype = System.Data.SqlDbType.Int;
						//        break;
						//    case "System.Int64":
						//        dbtype = System.Data.SqlDbType.BigInt;
						//        break;
						//    case "System.Single":
						//        dbtype = System.Data.SqlDbType.Float;
						//        break;
						//    case "System.Double":
						//        dbtype = System.Data.SqlDbType.Float;
						//        break;
						//    case "System.String":
						//        dbtype = System.Data.SqlDbType.NVarChar;
						//        break;
						//    case "System.DateTime":
						//        dbtype = System.Data.SqlDbType.DateTime;
						//        break;
						//}
						//param = new System.Data.SqlClient.SqlParameter("@" + name, dbtype);
						//param.Value = arg;
						param = new System.Data.SqlClient.SqlParameter("@" + name, arg);
						param.Direction = System.Data.ParameterDirection.Input;
					}
					break;
			}
			return param;
		}

		public static System.IAsyncResult BeginExecuteReader(System.AsyncCallback async_callback, System.Data.Common.DbCommand cmd)
		{
			System.IAsyncResult ir = null;
			var sqlcommand = cmd as System.Data.SqlClient.SqlCommand;
			if (sqlcommand != null)
			{
				ir = sqlcommand.BeginExecuteReader(async_callback, cmd);
			}
			var mysqlcmd = cmd as MySql.Data.MySqlClient.MySqlCommand;
			if (mysqlcmd != null)
			{
				ir = mysqlcmd.BeginExecuteReader();
				async_callback.BeginInvoke(ir, async_callback, cmd);
			}
			return ir;
		}

		public static System.Data.Common.DbDataReader EndExecuteReader(System.IAsyncResult async_result, System.Data.Common.DbCommand cmd)
		{
			System.Data.Common.DbDataReader rd = null;
			var sqlcommand = cmd as System.Data.SqlClient.SqlCommand;
			if (sqlcommand != null)
			{
				try
				{
					rd = sqlcommand.EndExecuteReader(async_result);
				}
				catch (System.Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
			}
			var mysqlcmd = cmd as MySql.Data.MySqlClient.MySqlCommand;
			if (mysqlcmd != null && async_result.IsCompleted == false)
			{
				try
				{
					rd = mysqlcmd.EndExecuteReader(async_result);
				}
				catch (System.Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
			}
			return rd;
		}

		public static DBFactory.DbCreateCallback SetAsyncExecuteReader(DBFactory.DbResultCallback recv_process, System.Data.Common.DbCommand cmd)
		{
			DBFactory.DbCreateCallback send_process;
			System.AsyncCallback async_result = (ir) =>
			{
				var rd = DBFactory.EndExecuteReader(ir, cmd);
				recv_process(rd);
				cmd.Dispose();
			};
			send_process = () =>
			{
				DBFactory.BeginExecuteReader(async_result, cmd);
			};
			return send_process;
		}

		public static System.IAsyncResult BeginExecuteNonQuery(System.AsyncCallback async_callback, System.Data.Common.DbCommand cmd)
		{
			System.IAsyncResult ir = null;
			var sqlcommand = cmd as System.Data.SqlClient.SqlCommand;
			if (sqlcommand != null)
			{
				ir = sqlcommand.BeginExecuteNonQuery(async_callback, cmd);
			}
			var mysqlcmd = cmd as MySql.Data.MySqlClient.MySqlCommand;
			if (mysqlcmd != null)
			{
				ir = mysqlcmd.BeginExecuteNonQuery(async_callback, cmd);
				async_callback.BeginInvoke(ir, async_callback, cmd);
			}
			return ir;
		}
		public static int EndExecuteNonQuery(System.IAsyncResult async_result, System.Data.Common.DbCommand cmd)
		{
			int rtn = 0;
			var sqlcommand = cmd as System.Data.SqlClient.SqlCommand;
			if (sqlcommand != null)
			{
				try
				{
					rtn = sqlcommand.EndExecuteNonQuery(async_result);
				}
				catch (System.Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
			}
			var mysqlcmd = cmd as MySql.Data.MySqlClient.MySqlCommand;
			if (mysqlcmd != null)
			{
				try
				{
					rtn = mysqlcmd.EndExecuteNonQuery(async_result);
				}
				catch (System.Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
			}
			return rtn;
		}


		public static void SetAsyncExecuteNonQuery(List<DBFactory.DbCreateCallback> send_processes, DBFactory.DbFinishCallback result_callback, System.Data.Common.DbCommand command,
				System.Data.Common.DbTransaction tran)
		{
			DBFactory.DbCreateCallback create_callback = null;
			SetAsyncExecuteNonQuery(out create_callback, result_callback, command, tran);
			send_processes.Add(create_callback);
		}
		public static void SetAsyncExecuteNonQuery(out DBFactory.DbCreateCallback create_callback, DBFactory.DbFinishCallback result_callback, System.Data.Common.DbCommand command, System.Data.Common.DbTransaction tran)
		{
			System.AsyncCallback async_result = (ir) =>
			{
				System.Data.Common.DbCommand cmd = null;
				int result = -1;
				string command_text = "";
				try
				{
					cmd = ir.AsyncState as System.Data.Common.DbCommand;
					result = (int)DBFactory.EndExecuteNonQuery(ir, cmd);
				}
				catch (System.Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
				if (cmd != null)
				{
					command_text = cmd.CommandText;
				}
				if (result == -1)
				{
					result_callback(command_text, result);
				}
				else
				{
					result_callback(command_text, result);
				}
				try
				{
					if (cmd != null)
					{
						cmd.Dispose();
					}
				}
				catch (System.Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
			};
			create_callback = () =>
			{
				if (tran != null)
				{
					command.Transaction = tran;
				}
				DBFactory.BeginExecuteNonQuery(async_result, command);
			};
		}
	}
}
