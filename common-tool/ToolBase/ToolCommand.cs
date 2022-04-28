using System;
using System.Collections.Generic;
using System.Text;

namespace common_tool
{
	public static class ToolCommand
	{
		public static string StayCommand(params string[] cmd){
			while (true)
			{
				string inputCommand = Console.ReadLine();
				foreach (string s in cmd)
				{
					if (inputCommand == s)
					{
						return s;
					}
					else if(inputCommand == "-"+s)
					{
						return s;
					}
				}
				Console.WriteLine("잘못된 명령어입니다.");
			}
		}
	}
}
