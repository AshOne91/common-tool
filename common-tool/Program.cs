using System;
using System.Collections.Generic;


namespace common_tool
{
	class Program
	{
		private static string inputCommand = string.Empty;
		public static void CommandProgram(string command)
		{
			inputCommand = command;
		}
		static void Main(string[] args)
		{
			string inputPath = string.Empty;
			try
			{
				Console.WriteLine("________________________UndeadButterFly_Tools");
				Console.WriteLine("______________________________version : 1.0.0");
				Console.WriteLine("");
				Console.WriteLine("명령어를 입력하세요. 도움말 help, 종료 escape");

				//도구 등록부분
				Dictionary<string, BaseTool> Tools = new Dictionary<string, BaseTool>
				{
					{"gen.basegen", new GenerateGenratorBase()},
					//{"gen.base.upload", new BaseTool()},

					{"gen.table", new GenerateTable()},
					//{"gen.application", new BaseTool()},
					//{"gen.template", new BaseTool()},

					//{"up.table", new BaseTool()},
					//{"up.file", new BaseTool()},
                };
				BaseTool currentTool;
				while (inputCommand != "escape")
				{
					inputCommand = Console.ReadLine();
					if (inputCommand == "help")
					{
						Console.WriteLine("모든 명령어 목록");
						foreach (string command in Tools.Keys)
						{
							Console.WriteLine("- "+command);
						}
						Console.WriteLine("- help");
						Console.WriteLine("- escape");
						continue;
					}
					else if (inputCommand == "escape")
					{
						Console.WriteLine("프로그램을 종료합니다.");
						break;
					}


					if (Tools.TryGetValue(inputCommand, out currentTool) == false)
					{
						Console.WriteLine($"{inputCommand} 명령어를 찾을 수 없습니다. 도움말이 필요하면 help를 입력하세요.");
					}
					else
					{
						Console.WriteLine($"도구를 불러오는 중입니다. 도구 로딩 완료 {currentTool.GetType().Name} 도구");
						Console.WriteLine("경로를 입력해주세요. 1. 현재작업경로 \\ 2. 완료된작업경로 \\output 3. 지정작업경로 \\***");
						string inputpath = Console.ReadLine();
						switch (inputpath)
						{
							case "\\":
								inputPath = "";
								Console.WriteLine("현재 작업경로 선택");
								break;
							case "\\output"://음음
								inputPath = inputpath;
								Console.WriteLine("완료된 작업경로 선택");
								break;
							default:
								inputPath = inputpath;
								Console.WriteLine("지정한 작업경로 선택");
								break;
						}
						try{
							if (currentTool.Run(inputPath))
							{
								Console.WriteLine($"{currentTool.GetType().Name} 작업완료.");
								Console.WriteLine("다른 작업을 이어서 하려면, 명령어를 입력하세요.");
							} 
						}
						catch(Exception e)
						{
							Console.WriteLine(e);
						}
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("ERROR : " + e.Message);
				Console.WriteLine("ERROR : " + e.StackTrace);
				throw;
			}
		}
	}
}
