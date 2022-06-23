﻿using System;
using System.IO;
using System.Collections.Generic;
using common_tool.Code;


namespace common_tool
{
	/// <summary>
	/// 프로젝트 파일내의 csv파일을 읽고 자동으로 .cs을 생성합니다.<br />
	/// </summary>
	public class GenerateTable : ActionBase
	{
		public GenerateTable(Parameter param) : base(param)
		{
		}

		public override void Run()
		{
			if (_param._dicActionParam.ContainsKey("--target-path") == false)
			{
				throw new Exception($"not found parameter - \"--target-path\"");
			}

			bool isLagacy = false;
			if (_param._dicActionParam.ContainsKey("--legacy"))
			{
				isLagacy = true;
				Console.Write("Legacy Mode Generate !!!");
			}

			// 테이블 클래스 코드 생성
			string outputPath = Directory.GetCurrentDirectory();
			string targetPath = _param._dicActionParam["--target-path"];
			if (_param._dicActionParam.ContainsKey("--output") == true)
			{
				outputPath = new DirectoryInfo(_param._dicActionParam["--output"]).FullName;
			}

			if (GenerateCode(targetPath, outputPath, isLagacy) == false)
            {
				throw new Exception($"failed to generate files. path: {outputPath}");
            }
			Console.WriteLine($"Generate table. target: {targetPath}, output: {outputPath}");
		}

		private bool GenerateCode(string sourcePath, string outputPath, bool isLagacy)
		{
			try
			{
				string filePath = string.Empty;
				string[] fileEntries = Directory.GetFiles(sourcePath, "*.csv", SearchOption.TopDirectoryOnly);//최상위 디렉토리만
								  // = Directory.GetFiles(sourcePath, "*.csv", SearchOption.AllDirectories  );//모든디렉토리 모두
				foreach (string fileEntry in fileEntries)
				{
					FileInfo fileInfo = new FileInfo(fileEntry);

					if (fileInfo.Name.Contains("Enum") == true)
					{
						GenerateEnumTable_CSV(fileInfo, outputPath);
					}
					else
					{
						GenerateDataTable_CSV(fileInfo, outputPath);
					}
				}
			}
			catch(Exception e)
			{
				Console.WriteLine(e.ToString());
				return false;
			}
			return true;
		}

		private static void GenerateDataTable_CSV(FileInfo fileInfo, string outputPath)
		{
			List<string> fields = new List<string>();//요소명
			List<string> fieldTypes = new List<string>();//요소타입
			List<int> exceptIndex = new List<int>();//제외요소

			//1. 파일에서 사용할 요소 이름과 자료타입 불러온다.
			using (var reader = new StreamReader(fileInfo.FullName))
			{
				int row = 0;
				while (reader.EndOfStream == false)
				{
					var line = reader.ReadLine();

					//"를 표현하기 위함
					line = line.Replace("\"\"", "{&}");
					//,를 표현하기 위함
					int replaceIndex = line.IndexOf("\"");
					//줄에 "가 없을때까지(csv는 요소에 ,가 추가되면 요소를 ""로 감쌈)
					while (replaceIndex != -1)
					{
						var originText = line.Substring(replaceIndex, line.IndexOf("\"", replaceIndex + 1) - replaceIndex + 1);
						var replaceText = originText.Replace(",", "{$}");//요소로 검색되지않게 처리
						replaceText = replaceText.Replace("\"", "");//" 삭제처리
						line = line.Replace(originText, replaceText);// 처리후 변경
																	 //다음 "" 검색
						replaceIndex = line.IndexOf("\"");
					}
					line = line.Replace("{&}", "\"");

					List<string> cols = new List<string>(line.Split(','));
					switch (row)
					{
						case 0:
							for (int i = cols.Count - 1; i >= 0; i--)
							{
								if (cols[i].StartsWith("~") == true)
								{
									exceptIndex.Add(i);
								}
								else
								{
									fields.Add(cols[i]);
								}
							}
							fields.Reverse();
							row++;
							break;
						case 1:
							foreach (var index in exceptIndex)
							{
								cols.RemoveAt(index);
							}
							for (var i = 0; i < cols.Count; i++)
							{
								if (cols[i].StartsWith("enum") == true)
								{
									//enumType 처리구문
									//enum을때고 저장
									fieldTypes.Add(cols[i].Substring(4));
								}
								else
								{
									fieldTypes.Add(cols[i]);
								}
							}
							row++;
							break;
						case 2:
							continue;
					}
				}
			}

			//파일출력저장소 처리
			if (Directory.Exists(outputPath) == false)
			{
				Directory.CreateDirectory(outputPath);
			}

			//파일명 처리
			string title = fileInfo.Name.Substring(0, fileInfo.Name.IndexOf("."));
			#region 구글스프레드시트 파일명 처리
			//2022.06.16 이현수
			title = title.Replace(" ", "");
			title = title.Replace("(", "");
			title = title.Replace(")", "");
			title = title.Replace("-", "_");
			//유니티엔진 인식불가 파일명제거
			title = title.Replace(".", "");
			title = title.Replace("~", "");

			int tableClassIndex = title.IndexOf("_");
			string tableClass = string.Empty;
			string titleTemplate = string.Empty;
			if (tableClassIndex != -1)
			{
				tableClass = title.Substring(0, tableClassIndex);
				titleTemplate = $"DataRow_{title.Substring(tableClassIndex + 1)}";
			}
			else
				#endregion
				titleTemplate = $"DataRow_{title}";

			//2. 읽은 자료에 맞는 Table Cs파일을 만든다.
			using (var streamWriter = new StreamWriter($"{outputPath}/{titleTemplate}.cs"))
			{
				CodeGenerator cg = new CodeGenerator();
				cg.StartWritingCode();
				cg._using("System");
				cg._using("System", "Collections", "Generic");

				if (tableClass == string.Empty)
				{
					cg._namespace("UBF", "Table");
				}
				else
				{
					cg._namespace("UBF", "Table", tableClass);
				}
				cg._class(AccessModifier.Public, ClassType.None, titleTemplate, $"IDataRow");

				for (int i = 0; i < fieldTypes.Count; i++)
				{
					cg._var(AccessModifier.Public, fieldTypes[i], fields[i], "{ get; set;}");
				}
				cg.WriteLine();
				cg.WriteLine();

				string[] methodImplements = new string[fields.Count];
				for (int i = 0; i < fields.Count; i++)
				{
					string Implement = string.Empty;
					switch (fieldTypes[i])
					{
						case "bool":
						case "byte":
						case "short":
						case "int":
						case "long":
						case "float":
						case "double":
							Implement = $"{fields[i]} = {fieldTypes[i]}.Parse(data[\"{fields[i]}\"]);";
							break;
						case "string":
							Implement = $"{fields[i]} = data[\"{fields[i]}\"].Replace(\"{{$}}\", \",\");";
							break;

						//배열처리
						case "bool[]":
						case "byte[]":
						case "short[]":
						case "int[]":
						case "long[]":
						case "float[]":
						case "double[]":
							Implement = $"{fields[i]} = (data[\"{fields[i]}\"] == \"-1\") ? null : Array.ConvertAll(data[\"{fields[i]}\"].Split('|'), s => {fieldTypes[i].Replace("[]", "")}.Parse(s));";
							break;
						case "string[]":
							Implement = $"{fields[i]} = (data[\"{fields[i]}\"] == \"-1\") ? null : data[\"{fields[i]}\"].Replace(\"{{$}}\", \",\").Split('|');";
							break;

						case "DateTime":
							Implement = $"{fields[i]} = (data[\"{fields[i]}\"] == \"-1\") ? default(DateTime) : DateTime.Parse(data[\"{fields[i]}\"]);";
							break;
						default://enumrator
							Implement = $"{fields[i]} = ({fieldTypes[i]})Enum.Parse(typeof({fieldTypes[i]}), data[\"{fields[i]}\"]);";
							break;
					}
					methodImplements[i] = $"if (data.ContainsKey(\"{fields[i]}\") == true) {Implement}";
				}
				cg._method(AccessModifier.Public, "void", "Serialize", "Dictionary<string, string> data", methodImplements);

				cg.EndWritingCode();
				streamWriter.Write(cg.Code);
				Console.WriteLine($"{fileInfo.FullName} 파일에 대한 {outputPath}{titleTemplate}.cs 작업완료");
			}
		}
		private static void GenerateEnumTable_CSV(FileInfo fileInfo, string outputPath)
		{
			List<string> fieldTypes = new List<string>();//요소형식 : 데이터 읽을때 필요
			Dictionary<string, string> enumByID = new Dictionary<string, string>();//ID별 enum: 데이터 읽을때 필요

			List<string> enumNames = new List<string>();//Enum 이름 : 데이터를 작성할때 필요
			List<string> enumTypes = new List<string>();//Enum 형식 : 데이터를 작성할때 필요
			Dictionary<string, List<string>> valuesByEnum = new Dictionary<string, List<string>>();//enum별 값들: 데이터를 작성할때 필요

			List<int> enumIDsWithStringID = new List<int>();//StringID를 가진 enum의 ClassID

			//1. 데이터를 읽어올 파일들을 불러온다.
			using (var reader = new StreamReader(fileInfo.FullName))
			{
				int row = 0;
				while (reader.EndOfStream == false)
				{
					var line = reader.ReadLine();

					//"를 표현하기 위함
					line = line.Replace("\"\"", "{&}");
					//,를 표현하기 위함
					int replaceIndex = line.IndexOf("\"");
					//줄에 "가 없을때까지(csv는 요소에 ,가 추가되면 요소를 ""로 감쌈)
					while (replaceIndex != -1)
					{
						var originText = line.Substring(replaceIndex, line.IndexOf("\"", replaceIndex + 1) - replaceIndex + 1);
						var replaceText = originText.Replace(",", "{$}");//요소로 검색되지않게 처리
						replaceText = replaceText.Replace("\"", "");//" 삭제처리
						line = line.Replace(originText, replaceText);// 처리후 변경
																	 //다음 "" 검색
						replaceIndex = line.IndexOf("\"");
					}
					line = line.Replace("{&}", "\"");

					//데이터처리
					List<string> cols = new List<string>(line.Split(','));
					switch (row)
					{
						case 0://필드요소 분리
							for (int i = 0; i < cols.Count; i++)
							{
								fieldTypes.Add(cols[i]);
								//fields
								//{ID, Enum, EnumType, ~Comment, StringID}
							}
							row++;
							break;
						default://테이블 작업
								//DataBind ID(int), enumName(string)
							if (enumByID.ContainsKey(cols[0]) == false)
							{
								//enumID , enumName DataBind
								enumByID.Add(cols[0], cols[1]);
								//아랫줄에서 읽기위해서 저장
							}
							//enumName = enumByID[cols[0]]

							//가독성
							string enumID = cols[0];
							string enumName = enumByID[enumID];

							//새로운 enum을 작업
							if (enumNames.Contains(enumName) == false)
							{
								//cols[0] = enumClassID (Bind) enumName
								//enumName >> enumClassID Add
								enumNames.Add(enumName);
								//////////////////////////////////////

								//////////////////////////////////////////////////////////////////////////
								///enum Data
								//////////////////////////////////////////////////////////////////////////
								//cols[1] enumName
								if (valuesByEnum.ContainsKey($"{enumName}") == false)
								{
									valuesByEnum.Add(enumName, new List<string>());
									//enumMember 저장용
								}
								//cols[2] enumType
								if (valuesByEnum.ContainsKey($"{enumName}_Type") == false)
								{
									valuesByEnum.Add($"{enumName}_Type", new List<string>());
									//enumMemberConnect 저장용

									//enumType저장 (DataBind ID => EnumName, EnumType)
									enumTypes.Add(cols[2]);
								}
								//cols[3] enumComment
								if (valuesByEnum.ContainsKey($"{enumName}_EnumComment") == false)
								{
									valuesByEnum.Add($"{enumName}_EnumComment", new List<string>(1));
									valuesByEnum[$"{enumName}_EnumComment"].Add(cols[3]);
									//enum의 comment(주의: enumMember의 Comment가 아니다.)
								}
								//cols[4] stringID
								if (valuesByEnum.ContainsKey($"{enumName}_StringID") == false)
								{
									valuesByEnum.Add($"{enumName}_StringID", new List<string>());

									if (cols[4] != "")//EnumName 분류열에 StringID가 기입된 경우 StringID를 쓰는 Enum
									{
										//stringID가 있는 enum의 classID에 저장
										enumIDsWithStringID.Add(int.Parse(enumID));
										valuesByEnum.Add($"{enumName}_StringIDComment", new List<string>());
										valuesByEnum[$"{enumName}_StringIDComment"].Add(cols[4]);
									}

									//stringIDComment입니다 StringID
								}
								//////////////////////////////////////////////////////////////////////////
							}
							//같은 enum을 작업
							else
							{
								//가독성
								enumID = cols[0];
								enumName = enumByID[enumID];

								for (int i = 0; i < cols.Count; i++)
								{
									if (fieldTypes[i].StartsWith("StringID"))
									{
										valuesByEnum[$"{enumName}_StringID"].Add(cols[i]);
									}
									else if (fieldTypes[i].StartsWith("~"))
									{
										if (valuesByEnum.ContainsKey($"{enumName}_MemberComment") == false)
										{
											valuesByEnum.Add($"{enumName}_MemberComment", new List<string>());
										}
										valuesByEnum[$"{enumName}_MemberComment"].Add(cols[i]);
									}
									else if (fieldTypes[i].StartsWith("EnumType"))
									{
										//가독성
										int enumNumber = enumNames.IndexOf(enumName);

										switch (enumTypes[enumNumber])
										{
											case "default":
												valuesByEnum[$"{enumName}_Type"].Add($"{valuesByEnum[enumName].Count - 1}");
												break;
											case "int":
											case "flags":
												valuesByEnum[$"{enumName}_Type"].Add(cols[i]);
												break;
											default:
												Console.WriteLine($"{fileInfo.FullName}의 {fieldTypes[i]}_AssociatedType이 잘못되었습니다. flags, int, default 3가지 유형중 하나여야 합니다.");
												break;

										}
									}
									else if (fieldTypes[i].StartsWith("Enum"))
									{
										valuesByEnum[enumName].Add(cols[i]);
									}
								}
							}
							//
							break;
					}
				}
			}

			//파일출력저장소 처리
			if (Directory.Exists(outputPath) == false)
			{
				Directory.CreateDirectory(outputPath);
			}

			//파일명 처리
			string title = fileInfo.Name.Substring(0, fileInfo.Name.IndexOf("."));
			#region 구글스프레드시트 파일명 처리
			//2022.06.16 이현수
			title = title.Replace(" ", "");
			title = title.Replace("(", "");
			title = title.Replace(")", "");
			title = title.Replace("-", "_");
			//유니티엔진 인식불가 파일명제거
			title = title.Replace(".", "");
			title = title.Replace("~", "");

			int tableClassIndex = title.IndexOf("_");
			string tableClass = string.Empty;
			string titleTemplate = string.Empty;
			if (tableClassIndex != -1)
			{
				tableClass = title.Substring(0, tableClassIndex);
				titleTemplate = $"EnumTable_{title.Substring(tableClassIndex + 5)}";
			}
			else
				#endregion
				titleTemplate = $"EnumTable_{title.Substring(4)}";
			//2. 읽은 자료에 맞는 Table Cs파일을 만든다.
			using (var streamWriter = new StreamWriter($"{outputPath}/{titleTemplate}.cs"))
			{
				CodeGenerator cg = new CodeGenerator();
				cg.StartWritingCode();

				//using
				cg._using("System");

				//namespace
				if (tableClass == "")//테이블 클래스가 없다면
				{
					cg._namespace("UBF", "Table");
				}
				else
				{
					cg._namespace("UBF", "Table", tableClass);
				}

				//enums
				for (int i = 0; i < enumNames.Count; i++)
				{
					//가독성
					string enumName = enumNames[i];
					string enumType = enumTypes[i];
					int enumMemberCount = valuesByEnum[enumName].Count;

					//each enum comment
					if (valuesByEnum.ContainsKey($"{enumName}_EnumComment"))
					{
						if (valuesByEnum[$"{enumName}_EnumComment"][0] != "")
						{
							cg.WriteLineTab($"// {valuesByEnum[$"{enumName}_EnumComment"][0]}");
						}
					}

					//each enum type option
					if (enumType == "flags")
					{
						cg.WriteLineTab("[Flags]");
						cg.WriteLineTab($"public enum {enumName} : uint");//uint64 ushort32
					}
					else
					{
						cg.WriteLineTab($"public enum {enumName}");
					}

					//each enum implements
					cg.WriteOpenBracket();

					for (int n = 0; n < enumMemberCount; n++)
					{
						cg.WriteLineTab($"{valuesByEnum[enumName][n]} = {valuesByEnum[$"{enumName}_Type"][n]}, // {valuesByEnum[$"{enumName}_MemberComment"][n]}");
					}
					cg.WriteClosedBracket();
				}


				cg.EndWritingCode();
				streamWriter.Write(cg.Code);
				Console.WriteLine($"{fileInfo.FullName} 파일에 대한 {outputPath}{titleTemplate}.cs 작업완료");
			}
			titleTemplate = $"StringID_{tableClass}";
			//3. 읽은 자료에 맞는 StringID Cs파일을 만든다.
			using (var streamWriter2 = new StreamWriter($"{outputPath}/{titleTemplate}.cs"))
			{
				CodeGenerator cg = new CodeGenerator();
				cg.StartWritingCode();

				//using
				cg._using("System.Collections");
				cg._using("System.Collections.Generic");

				//namespace
				if (tableClass == "")//테이블 클래스가 없다면
				{
					cg._namespace("UBF", "Table");
				}
				else
				{
					cg._namespace("UBF", "Table", tableClass);
				}

				cg._class(AccessModifier.Public, ClassType.Static, "StringID");

				cg.WriteLineTab("//Enum별 StringID묶음");
				cg._var(AccessModifier.Private, "readonly static Dictionary<string, IDictionary>",
					"STRINGID_BINDING_BY_ENUM", " = new Dictionary<string, IDictionary>();");

				cg.WriteLine();


				List<string> methodImplements = new List<string>();
				methodImplements.Add("IDictionary enumStringID;");
				methodImplements.Add("");

				//stringID를 가진 enum의 수
				for (int i = 0; i < enumIDsWithStringID.Count; i++)
				{
					string enumName = enumNames[enumIDsWithStringID[i]];
					int enumMemberCount = valuesByEnum[enumName].Count;

					methodImplements.Add($"//enum : {valuesByEnum[$"{enumName}_EnumComment"][0]}");
					methodImplements.Add($"//stringID : {valuesByEnum[$"{enumName}_StringIDComment"][0]}");
					methodImplements.Add($"Dictionary<{enumName}, string> stringIDByEnumMember_{enumName} = new Dictionary<{enumName}, string>();");
					methodImplements.Add($"enumStringID = stringIDByEnumMember_{enumName};");
					methodImplements.Add($"STRINGID_BINDING_BY_ENUM[nameof({enumName})] = enumStringID; ");
					methodImplements.Add("");

					//enum의 멤버수
					for (int n = 0; n < enumMemberCount; n++)
					{
						methodImplements.Add($"enumStringID[{enumName}.{valuesByEnum[enumName][n]}] = \"{valuesByEnum[$"{enumName}_StringID"][n]}\"; //{valuesByEnum[$"{enumName}_MemberComment"][n]}");
					}
					methodImplements.Add("");
					methodImplements.Add("");
					methodImplements.Add("");
				}
				string[] methodImplement = methodImplements.ToArray();
				cg._method(AccessModifier.None, "static", "StringID", "", methodImplement);

				//stringID를 가진 enum의 수
				for (int i = 0; i < enumIDsWithStringID.Count; i++)
				{
					string enumName = enumNames[enumIDsWithStringID[i]];
					methodImplement = new string[1];
					methodImplement[0] = $"return STRINGID_BINDING_BY_ENUM[nameof({enumName})][args{enumName}] as string;";
					cg._method(AccessModifier.Public, "static string", "GetFrom", $"{enumName} args{enumName}", methodImplement);
				}


				cg.EndWritingCode();
				streamWriter2.Write(cg.Code);
				Console.WriteLine($"{fileInfo.FullName} 파일에 대한 {outputPath}{titleTemplate}_StringID.cs 작업완료");
			}
		}


	}
}