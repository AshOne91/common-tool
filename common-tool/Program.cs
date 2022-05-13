using common_tool.Tools.Generate;
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
            try
            {
                Parameter param = new Parameter(args);
                Console.WriteLine($"[gamebase] startup gamebase tool");
                Console.WriteLine($"[gamebase] version : v1.0.9");
                Console.WriteLine($"[gamebase] args : {string.Join(" ", args)}");

                Dictionary<string, ActionBase> dicAction = new Dictionary<string, ActionBase>
            {
				{"gen:table", new GenerateTable(param) },
				{"gen:template", new GenerateTemplate(param)}
            };

                if (args.Length <= 2)
                {
                    throw new Exception($"not found action. actionName: {string.Join(" ", args)}");
                }

                string actionName = args[0] + ":" + args[1];

                ActionBase action;
                if (dicAction.TryGetValue(actionName, out action) == false)
                {
                    throw new Exception($"not found action. actionName: {actionName}");
                }

                action.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[gamebase] ERROR : " + ex.Message);
                Console.WriteLine("[gamebase] ERROR : " + ex.StackTrace);
                throw;
            }
        }
    }
}
