using System;
using System.IO;
using System.Collections.Generic;
using System.Data.Common;

namespace common_tool
{
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

			// 테이블 클래스 코드 생성
			string outputPath = Directory.GetCurrentDirectory();
			string targetPath = _param._dicActionParam["--target-path"];
			if (_param._dicActionParam.ContainsKey("--output") == true)
			{
				outputPath = new DirectoryInfo(_param._dicActionParam["--output"]).FullName;
			}

			if (GenerateCode(targetPath, outputPath) == false)
			{
				throw new Exception($"failed to generate files. path: {outputPath}");
			}
			Console.WriteLine($"Generate table. target: {targetPath}, output: {outputPath}");
		}

		bool GenerateCode(string sourcePath, string outputPath)
		{
			string filePath = string.Empty;
			try
			{
				string[] fileEntries = Directory.GetFiles(sourcePath);
                foreach (var fileName in fileEntries)
				{
					FileInfo fileInfo = new FileInfo(fileName);
					string name = fileInfo.Name.Substring(0, fileInfo.Name.IndexOf('.'));
					if (fileInfo.Extension != ".csv")
					{
						continue;
					}

					List<Column> columnList = new List<Column>();
					using (var reader = new StreamReader(fileInfo.FullName))
					{
						int line = 0;
						while (reader.EndOfStream == false)
						{
							string row = reader.ReadLine();
							List<string> rowList = new List<string>(row.Split(','));
							if (line == 0)
							{
								for (int i = 0; i < rowList.Count; ++i)
								{
                                    columnList.Add(new Column { Name = rowList[i] });
                                }
							}
							else if (line == 1)
							{
								for (int i = 0; i < rowList.Count; ++i)
								{
									columnList[i].Type = rowList[i];

                                }
							}
							++line;
						}
					}

					if (Directory.Exists(outputPath) == false)
					{
						Directory.CreateDirectory(outputPath);
                    }

					using (var streamWriter = new StreamWriter($"{outputPath}/{name}Table.cs"))
					{
						streamWriter.WriteLine($"using System;");
						streamWriter.WriteLine($"using System.Collections.Generic;");
						streamWriter.WriteLine($"using System.Linq;");

                        streamWriter.WriteLine($"using Service.Core;");
						streamWriter.WriteLine();
						streamWriter.WriteLine($"namespace GameBase.Template.GameBase.Table");
						streamWriter.WriteLine("{");
						streamWriter.WriteLine($"\tpublic class {name}Table : ITableData");
						streamWriter.WriteLine($"\t{{");
						for (int i = 0; i < columnList.Count; ++i)
						{
							if (columnList[i].Name.StartsWith("~") == true)
							{
								continue;
							}

							columnList[i].Type.ToLower();
							switch (columnList[i].Type)
							{
								case "array_bool":
								case "array_short":
								case "array_int":
								case "array_float":
								case "array_DateTime":
								case "array_Byte":
								case "array_string":
									string[] words = columnList[i].Type.Split("_");
									streamWriter.WriteLine($"\t\tpublic List<{words[1]}> {columnList[i].Name} = new List<{words[1]}>();");
									break;
								case "string":
									streamWriter.WriteLine($"\t\tpublic {columnList[i].Type} {columnList[i].Name} = string.Empty;");
									break;
								default:
									streamWriter.WriteLine($"\t\tpublic {columnList[i].Type} {columnList[i].Name} = new {columnList[i].Type}();");
									break;
							}
						}
						streamWriter.WriteLine();
						streamWriter.WriteLine($"\t\tpublic void Serialize(Dictionary<string, string> data)");
						streamWriter.WriteLine($"\t\t{{");
						for (int i = 0; i < columnList.Count; ++i)
						{
							if (columnList[i].Name.StartsWith("~") == true)
							{
								continue;
							}
							switch (columnList[i].Type)
							{
								case "int":
								case "bool":
								case "short":
								case "float":
								case "Byte":
									{
										streamWriter.WriteLine($"\t\t\tif (data.ContainsKey(\"{columnList[i].Name}\") == true) {{ {columnList[i].Name} = {columnList[i].Type}.Parse(data[\"{columnList[i].Name}\"]); }}");
									}
									break;
								case "DateTime":
									{
										streamWriter.WriteLine($"\t\t\tif (data.ContainsKey(\"{columnList[i].Name}\") == true) {{ {columnList[i].Name} = (data[\"{columnList[i].Name}\"] == \"-1\") ? default(DateTime) : DateTime.Parse(data[\"{columnList[i].Name}\"]); }}");
									}
									break;
								case "string":
									{
                                        streamWriter.WriteLine($"\t\t\tif (data.ContainsKey(\"{columnList[i].Name}\") == true) {{ {columnList[i].Name} = data[\"{columnList[i].Name}\"].Replace(\"{{$}}\", \",\"); }}");
                                    }
									break;
                                case "array_bool":
                                case "array_short":
                                case "array_int":
                                case "array_float":
                                case "array_DateTime":
                                case "array_Byte":
                                case "array_string":
									{
										string[] words = columnList[i].Type.Split("_");
										if (columnList[i].Type != "array_string")
										{
											streamWriter.WriteLine($"\t\t\tif (data.ContainsKey(\"{columnList[i].Name}\") == true) {{ if (data[\"{columnList[i].Name}\"] != \"-1\") {columnList[i].Name} = data[\"{columnList[i].Name}\"].Split('|').Select({words[1]}.Parse).ToList(); }}");
										}
										else
										{
                                            streamWriter.WriteLine($"\t\t\tif (data.ContainsKey(\"{columnList[i].Name}\") == true) {{ if (data[\"{columnList[i].Name}\"] != \"-1\") {columnList[i].Name} = data[\"{columnList[i].Name}\"].Split('|').ToList(); }}");
                                        }
									}
                                    break;
								default:
									{
										streamWriter.WriteLine($"\t\t\tif (data.ContainsKey(\"{columnList[i].Name}\") == true) {{ {columnList[i].Name} = ({columnList[i].Type})Enum.Parse(typeof({columnList[i].Type}), data[\"{columnList[i].Name}\"]); }}");
									}
									break;
                            }


						}
						streamWriter.WriteLine($"\t\t}}");
						streamWriter.WriteLine($"\t}}");
						streamWriter.WriteLine($"}}");

                    }
                }
            }
			catch (Exception ex)
			{
				Console.WriteLine($"Error GenerateCode. filePath: {filePath}");
				return false;
			}
			return true;
		}
	}
}