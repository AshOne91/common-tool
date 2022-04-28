using System;
using System.IO;
using System.Collections.Generic;
using common_tool.Code;


namespace common_tool
{
	/// <summary>
	/// 프로젝트 파일내의 csv파일을 읽고 자동으로 .cs을 생성합니다.<br />
	/// </summary>
	public class GenerateTable : BaseGenerateTool
	{

		public override bool Run(string sourcePath)
		{
			base.Run(sourcePath);

			try{
				//세부명령어 지정
				Console.WriteLine("-a: 모든 하위디렉토리작업");
				Console.WriteLine("-c: 현재 디렉토리만 작업");
				List<string> fileEntries;
				
				//세부명령어에 따른 작업할 파일 설정
				switch (ToolCommand.StayCommand("a", "c"))
				{
					case "a":
						fileEntries = new List<string>(Directory.GetFiles(_sourcePath, "*.csv", SearchOption.AllDirectories));
						break;
					case "c":
						fileEntries = new List<string>(Directory.GetFiles(_sourcePath, "*.csv"));
						break;
					default:
						fileEntries = new List<string>();
						break;
				}

				//찾은 파일에 대한 작업실행
				foreach (string filePath in fileEntries)
				{
					FileInfo fileInfo = new FileInfo(filePath);
					string outputCopySourcePath;
					if (_sourcePath.Length == fileInfo.DirectoryName.Length)
					{
						outputCopySourcePath = Path.Combine(_outputPath, fileInfo.DirectoryName.Substring(_sourcePath.Length));
					}
					else
					{
						outputCopySourcePath = Path.Combine(_outputPath, fileInfo.DirectoryName.Substring(_sourcePath.Length + 1));

					}
					Generate(filePath, outputCopySourcePath);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message); 
				return false;
			}
			return true;
		}

		public override bool Generate(string sourcePath, string outputPath)
		{
			try
			{
				if (Directory.Exists(outputPath) == false)
					Directory.CreateDirectory(outputPath);

				List<string> fields = new List<string>();
				List<string> fieldtypes = new List<string>();//요소타입
				List<int> exceptIndex = new List<int>();
				
				//1. 자료를 읽어올 파일들을 불러온다.
				using (var reader = new StreamReader($"{sourcePath}"))
				{
					int row = 0;
					//1. 파일에서 사용할 요소 이름과 자료타입 불러온다.
					while (reader.EndOfStream == false)
					{
						var line = reader.ReadLine();

						int replaceIndex = line.IndexOf("\"");
						while (replaceIndex != -1)
						{
							var originText = line.Substring(replaceIndex, line.IndexOf("\"", replaceIndex + 1) - replaceIndex + ("\"").Length);
							var replaceText = originText.Replace(",", "{$}");
							replaceText = replaceText.Replace("\"", "");
							line = line.Replace(originText, replaceText);
							replaceIndex = line.IndexOf("\"");
						};

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
									if (cols[i].Contains("enum"))
									{//enum 처리구문
										fieldtypes.Add(cols[i].Substring(4));
									}
									else
									{
										fieldtypes.Add(cols[i]);
									}
								}
								row++;
								break;
							case 2:
								continue;
						}
					}
				}
				//2. cs파일을 만든다.
				string title = sourcePath.Substring(sourcePath.LastIndexOf("\\")+1,sourcePath.LastIndexOf(".") - sourcePath.LastIndexOf("\\")-1);
				string template = $"TableData_{title}";
				using (var streamWriter = new StreamWriter($"{outputPath}/{template}.cs"))
				{
					//코드 템플릿 작성부
					CodeWriter cw = new CodeWriter();
					cw.StartWritingCode();
					cw._using("System");
					cw._using("System", "Collections", "Generic");
					cw._using("UndeadButterFly", "Tools");

					cw._namespace("UndeadButterFly", "DataBase");
					cw._class(AM.Public, "class", template, "ITableData");

					for (int i = 0; i < fieldtypes.Count; i++)
					{
						cw._var(AM.Public, fieldtypes[i], fields[i], "{ get; set;}");
					}
					cw.WriteLine();
					cw.WriteLine();

					string[] methodImplement = new string[fields.Count];
					for (int i = 0; i < fields.Count; i++)
					{
						string 할당문 = "";
						switch (fieldtypes[i])
						{
							case "int":
							case "float":
							case "short":
							case "long":
							case "byte":
							case "bool":
								할당문 = $"{fields[i]} = {fieldtypes[i]}.Parse(data[\"{fields[i]}\"]);";
								break;
							case "string":
								할당문 = $"{fields[i]} = data[\"{fields[i]}\"].Replace(\"{{$}}\", \",\");";
								break;

							case "int[]":
							case "float[]":
							case "short[]":
							case "long[]":
							case "byte[]":
							case "bool[]":
								할당문 = $"{fields[i]} = (data[\"{fields[i]}\"] == \"-1\") ? null : Array.ConvertAll(data[\"{fields[i]}\"].Split('|'), s => {fieldtypes[i]}.Parse(s));";
								break;
							case "string[]":
								할당문 = $"{fields[i]} = (data[\"{fields[i]}\"] == \"-1\") ? null : data[\"{fields[i]}\"].Split('|');";
								break;

							case "DateTime":
								할당문 = "try{" + $"{fields[i]} = DateTime.Parse(data[\"{fields[i]}\"]);" + "} catch {" + $"{fields[i]} = default;" + "}";
								break;
							default://enumrator
								할당문 = $"{fields[i]} = ({fieldtypes[i]})Enum.Parse(typeof({fieldtypes[i]}), data[\"{fields[i]}\"]);";
								break;
						}
						methodImplement[i] = $"if (data.ContainsKey(\"{fields[i]}\") == true) {할당문}";
					}
					cw._method(AM.Public, "void", "Serialize", true, "Dictionary<string, string> data", methodImplement);

					streamWriter.Write(cw.EndWritingCode());//마무리
					Console.WriteLine($"{sourcePath} 파일에 대한 {outputPath}/{ template}.cs 작업완료");
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return false;
			}
			return true;
		}
	}
}