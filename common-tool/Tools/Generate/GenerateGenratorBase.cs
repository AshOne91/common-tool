using System;
using System.IO;
using common_tool.Code;

namespace common_tool
{
	public class GenerateGenratorBase : BaseGenerateTool
	{
		enum GenerateType {readwrite, write}
		GenerateType generateType;
		public override bool Run(string sourcePath)
		{
			base.Run(sourcePath);

			try
			{
				Console.WriteLine("-a: 읽고 쓰기용");
				Console.WriteLine("-w: 쓰기 전용");

				switch (ToolCommand.StayCommand("a", "w"))
				{
					case "a":
						generateType = GenerateType.readwrite;
						break;
					case "w":
						generateType = GenerateType.write;
						break;
					default:
						break;
				}

				Generate($"{_sourcePath}\\incomplete_GenerateTool.cs", _outputPath);
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

				//코드생성 작업
				string title = sourcePath.Substring((sourcePath.LastIndexOf("\\") + 1), (sourcePath.LastIndexOf(".") - 1) - sourcePath.LastIndexOf("\\"));
				string template = $"{title}";
				using (var streamWriter = new StreamWriter($"{outputPath}/{template}.cs"))
				{
					CodeWriter cw = new CodeWriter();
					cw.StartWritingCode();
					cw._using("System");
					cw._using("System","IO");
					cw._using("System","Collections","Generic");
					cw._using("common_tool","Code");

					cw._namespace("common_tool");
					cw.WriteLine("incomplete_GenerateTool  파일이름과 클래스이름 바꾸기;");
					cw._class(AM.Public, "", "incomplete_GenerateTool", "BaseGenerateTool");
					cw.WriteLine();
					cw.WriteLineWithTab("public override bool Run(string sourcePath)");
					cw.WriteOpenBracket();
						cw.WriteLineWithTab("base.Run(sourcePath);");
						cw.WriteLineWithTab("try");
							cw.WriteOpenBracket();
					//파일을 읽고 쓸건지, 바로 쓸건지///////////////////////////////////////
								cw.WriteLineWithTab("//세부명령어 지정");
					switch (generateType)
					{
						case GenerateType.readwrite:
								cw.WriteLineWithTab("Console.WriteLine(\"-a: 모든 하위디렉토리작업\");");
								cw.WriteLineWithTab("Console.WriteLine(\"-c: 현재 디렉토리만 작업\");");
								cw.WriteLineWithTab("List<string> fileEntries;"); 
								cw.WriteLine();
							cw.WriteLineWithTab("//세부명령어에 따른 작업파일 설정하기");
							cw.WriteLineWithTab("switch (ToolCommand.StayCommand(\"a\", \"c\"))");
									cw.WriteOpenBracket();
										cw.WriteLineWithTab("case \"a\":");
										cw.WriteLineWithTab("//searchPattern example : \"*.csv\"");
										cw.WriteLineWithTab("fileEntries = new List<string>(Directory.GetFiles(_sourcePath, searchPattern, SearchOption.AllDirectories));");
										cw.WriteLineWithTab("break;");
										cw.WriteLineWithTab("case \"c\":");
										cw.WriteLineWithTab("fileEntries = new List<string>(Directory.GetFiles(_sourcePath, searchPattern));");
										cw.WriteLineWithTab("break;");
										cw.WriteLineWithTab("default:");
										cw.WriteLineWithTab("fileEntries = new List<string>();");
										cw.WriteLineWithTab("break;");
									cw.WriteCloseBracket();
								cw.WriteLineWithTab("//해당하는 파일들에 대한 작업실행");
								cw.WriteLineWithTab("foreach (string filePath in fileEntries)");
								cw.WriteOpenBracket();
									cw.WriteLineWithTab("FileInfo fileInfo = new FileInfo(filePath);"); 
									cw.WriteLineWithTab("string outputCopySourcePath;");
									cw.WriteLineWithTab("if (_sourcePath.Length == fileInfo.DirectoryName.Length)");
									cw.WriteOpenBracket();
										cw.WriteLineWithTab("outputCopySourcePath = Path.Combine(_outputPath, fileInfo.DirectoryName.Substring(_sourcePath.Length));");
									cw.WriteCloseBracket();
									cw.WriteLineWithTab("else");
									cw.WriteOpenBracket();
										cw.WriteLineWithTab("outputCopySourcePath = Path.Combine(_outputPath, fileInfo.DirectoryName.Substring(_sourcePath.Length + 1));");
									cw.WriteCloseBracket();
								cw.WriteLineWithTab("Generate(filePath, outputCopySourcePath);");
							cw.WriteCloseBracket();

							break;
						case GenerateType.write:
							cw.WriteLineWithTab("Console.WriteLine(\"-a: 명령어 a에 대한 처리\");");
							cw.WriteLineWithTab("Console.WriteLine(\"-c: 명령어 c에 대한 처리\");");
							cw.WriteLineWithTab("switch (ToolCommand.StayCommand(\"a\", \"c\"))");
							cw.WriteOpenBracket(); 
							cw.WriteLineWithTab("case \"a\":");
							cw.WriteLineWithTab("break;");
							cw.WriteLineWithTab("case \"c\":");
							cw.WriteLineWithTab("break;");
							cw.WriteLineWithTab("default:");
							cw.WriteLineWithTab("break;");
							cw.WriteCloseBracket(); 
							cw.WriteLineWithTab("//fileName example : $\"___________{title}\"");
							cw.WriteLineWithTab("string title_template = fileName;");
							cw.WriteLineWithTab("//fileExtension example : \"cs\", \"txt\", \"json\"");
							cw.WriteLineWithTab("string extension_template = fileExtension;");
							cw.WriteLineWithTab("string filePath = $\"{_sourcePath}\\\\{title_template}.{extension_template}\";"); 
							cw.WriteLineWithTab("Generate(filePath,_outputPath);");
							break;
					}
					////////////////////////////////////////////////
					cw.WriteCloseBracket();
							cw.WriteLineWithTab("catch (Exception e)");
							cw.WriteOpenBracket();
								cw.WriteLineWithTab("Console.WriteLine(e);");
								cw.WriteLineWithTab("return false;");
							cw.WriteCloseBracket();
							cw.WriteLineWithTab("return true;");
						cw.WriteCloseBracket();


					cw.WriteLineWithTab("public override bool Generate(string sourcePath, string outputPath)");
					cw.WriteOpenBracket();
					cw.WriteLineWithTab("try");
					cw.WriteOpenBracket();
					////////////////////////////////////////////////
					cw.WriteLineWithTab("//0. 파일 output디렉토리 경로 생성");
					cw.WriteLineWithTab("if (Directory.Exists(outputPath) == false)");
					cw.WriteLineWithTab("\tDirectory.CreateDirectory(outputPath);");

					cw.WriteLineWithTab("//데이터 처리 저장필드");
					cw.WriteLineWithTab("//Example/////////////////////////");
					cw.WriteLineWithTab("//List<string> fields = new List<string>();");
					cw.WriteLineWithTab("//List<string> fieldtypes = new List<string>();");
					cw.WriteLineWithTab("//List<int> exceptIndex = new List<int>();");
					cw.WriteLineWithTab("//////////////////////////////////");
					cw.WriteLine();
					switch (generateType)
					{
						case GenerateType.readwrite:
							cw.WriteLineWithTab("//1. 쓰기에 필요한 파일 데이터 읽기");
							cw.WriteLineWithTab("using (var reader = new StreamReader($\"{sourcePath}\"))");
								cw.WriteOpenBracket();
									cw.WriteLineWithTab("//2. 파일 데이터를 한줄씩 읽기");
									cw.WriteLineWithTab("while (reader.EndOfStream == false)");
									cw.WriteOpenBracket();
									cw.WriteLineWithTab("var line = reader.ReadLine();");
									cw.WriteLineWithTab("//3. 데이터 처리 구문");
									cw.WriteLineWithTab("//");
									cw.WriteLineWithTab("//");
									cw.WriteLineWithTab("//");
									cw.WriteLineWithTab("//");
									cw.WriteLineWithTab("//");
									cw.WriteLineWithTab("////////////////////");
								cw.WriteCloseBracket();
							cw.WriteCloseBracket();
							break;
						case GenerateType.write:
							break;
					}
					cw.WriteLineWithTab("//1.파일제목 서식");
					cw.WriteLineWithTab("string title = sourcePath.Substring(sourcePath.LastIndexOf(\"\\\\\")+1,sourcePath.LastIndexOf(\".\") - sourcePath.LastIndexOf(\"\\\\\")-1);");
					cw.WriteLineWithTab("//fileName example : $\"___________{title}\"");
					cw.WriteLineWithTab("string title_template = fileName;"); 
					cw.WriteLineWithTab("//fileExtension example : \"cs\", \"txt\", \"json\"");
					cw.WriteLineWithTab("string extension_template = fileExtension;");
					cw.WriteLineWithTab("using (var streamWriter = new StreamWriter($\"{outputPath}/{title_template}.{extension_template}\"))");
					cw.WriteOpenBracket();
						cw.WriteLineWithTab("//2.파일 쓰기 작성 부분");
						cw.WriteLineWithTab("CodeWriter cw = new CodeWriter();");
						cw.WriteLineWithTab("//cw._using(\"System\");");
						cw.WriteLineWithTab("//cw._using(\"System\", \"IO\");");
						cw.WriteLineWithTab("//cw._using(\"System\", \"Collections\", \"Generic\");");
						cw.WriteLineWithTab("//cw._namespece(\"common_tool\");");
						cw.WriteLineWithTab("//cw._class();");
					cw.WriteLineWithTab("//cw.WriteLine(\"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\")");
						cw.WriteLineWithTab("//cw.WriteLine(\"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\")");
						cw.WriteLineWithTab("//cw.WriteLine(\"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\")");
						cw.WriteLineWithTab("//cw.WriteLine(\"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\")");
					cw.WriteLineWithTab("streamWriter.Write(cw.EndWritingCode());//마무리");
						cw.WriteLineWithTab("Console.WriteLine($\"{outputPath}/{title_template}.{extension_template} 파일 작업완료\");");
					cw.WriteCloseBracket();
					////////////////////////////////////////////////
					cw.WriteCloseBracket();
					cw.WriteLineWithTab("catch (Exception e)");
						cw.WriteOpenBracket();
						cw.WriteLineWithTab("Console.WriteLine(e);");
						cw.WriteLineWithTab("return false;");
						cw.WriteCloseBracket();
						cw.WriteLineWithTab("return true;");
					cw.WriteCloseBracket();
					streamWriter.Write(cw.EndWritingCode());//마무리
					Console.WriteLine($"{outputPath}/{template}.cs 파일 작업완료");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return false;
			}
			return true;
		}
	}
}

