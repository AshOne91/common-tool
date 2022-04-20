using System;

namespace common_tool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
			//일반 도구
			//UBF 게임개발시에 모든 프로젝트에서 사용가능한 도구
			//ex csv <=> json 포팅도구같은 것?

			//테이블 생성
			//템플릿 생성
			//업로드 파일
			//업로드 테이블
        }
    }
}
/*using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

 * public class GaenrateTable : ActionBase
    {
        public GaenrateTable(Parameter param) : base(param)
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
                outputPath = new DirectoryInfo( _param._dicActionParam["--output"]).FullName;
            }

            if (GenerateCode(targetPath, outputPath, isLagacy) == false)
            {
                throw new Exception($"failed to generate files. path: {outputPath}");
            }
            Console.WriteLine($"Generate table. target: {targetPath}, output: {outputPath}");
        }


        // 코드 생성
        bool GenerateCode(string sourcePath, string outputPath, bool isLagacy)
        {
            string filePath = string.Empty;
            try
            {
                string[] fileEntries = Directory.GetFiles(sourcePath);
                foreach (string fileName in fileEntries)
                {
                    FileInfo fi = new FileInfo(fileName);
                    string title = fi.Name.Substring(0, fi.Name.IndexOf("."));
                    filePath = fileName;
                    if (fi.Extension != ".csv")
                    {
                        continue;
                    }

                    List<Column> cols = new List<Column>();
                    List<KeyValuePair<string, int>> enumKeys = new List<KeyValuePair<string, int>>();
                    using (var reader = new StreamReader(fi.FullName))
                    {
                        int row = 0;
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            List<string> words = new List<string>(line.Split(','));
                            if (row == 0)
                            {
                                for (var i = 0; i < words.Count; i++)
                                {
                                    cols.Add(new Column { Name = words[i] });
                                }
                            }
                            else if (row == 1)
                            {
                                for (var i = 0; i < words.Count; i++)
                                {
                                    cols[i].Type = words[i];        
                                }
                            }
                            else
                            {
                                // int id = 0;
                                // for (var i = 0; i < words.Count; i++)
                                // {
                                //     if (cols[i].Name == "id")
                                //     {
                                //         id = int.Parse(words[i]);
                                //     }
                                //     else if (cols[i].Name == "constKey" && cols[i].Type == "string")
                                //     {
                                //         enumKeys.Add(new KeyValuePair<string, int>(words[i], id));
                                //         break;
                                //     }
                                // }
                            }   

                            row++;
                        }
                    }   


                    if (Directory.Exists(outputPath) == false)
                    {
                        Directory.CreateDirectory(outputPath);
                    }


                    using (var streamWriter = new StreamWriter($"{outputPath}/TData{title}.cs"))
                    {
                        streamWriter.WriteLine("using System;");
                        streamWriter.WriteLine("using System.Collections.Generic;");
                        streamWriter.WriteLine("using Percent.GameBase.Service.Core;");
                        streamWriter.WriteLine();

                        streamWriter.WriteLine("namespace Percent.GameBase.Template.GameBase.Table");
                        streamWriter.WriteLine("{");
                        streamWriter.WriteLine($"\tpublic class TData{title} : ITableData");
                        streamWriter.WriteLine("\t{");

                        for (var i = 0; i < cols.Count; i++)
                        {
                            if (cols[i].Name.StartsWith("~") == true)
                            {
                                continue;
                            }

                            if (cols[i].Type.ToLower() == "array_int")
                            {
                                streamWriter.WriteLine($"\t\tpublic int[] {cols[i].Name} {{ get; set; }}");
                            }
                            else if (cols[i].Type.ToLower() == "array_long")
                            {
                                streamWriter.WriteLine($"\t\tpublic long[] {cols[i].Name} {{ get; set; }}");
                            }
                            else if (cols[i].Type.ToLower() == "array_string")
                            {
                                streamWriter.WriteLine($"\t\tpublic string[] {cols[i].Name} {{ get; set; }}");
                            }
                            else if (cols[i].Type.ToLower() == "date_time")
                            {
                                streamWriter.WriteLine($"\t\tpublic DateTime {cols[i].Name} {{ get; set; }}");
                            }
                            else
                            {
                                streamWriter.WriteLine($"\t\tpublic {cols[i].Type} {cols[i].Name} {{ get; set; }}");
                            }
                        }


                        streamWriter.WriteLine();
                        streamWriter.WriteLine();

                        if (isLagacy)
                        {
                            streamWriter.WriteLine("\t\tpublic void Serialize(string[] cols)");
                            streamWriter.WriteLine("\t\t{");

                            int index = 0;
                            for (var i = 0; i < cols.Count; i++)
                            {
                                if (cols[i].Name.StartsWith("~") == true)
                                {
                                    continue;
                                }

                                if (cols[i].Type == "bool"
                                || cols[i].Type == "float"
                                || cols[i].Type == "int"
                                || cols[i].Type == "long")
                                {
                                    streamWriter.WriteLine($"\t\t\t{cols[i].Name} = {cols[i].Type}.Parse(cols[{index}]);");
                                }
                                else if (cols[i].Type == "string")
                                {
                                    streamWriter.WriteLine($"\t\t\t{cols[i].Name} = cols[{index}].Replace(\"{{$}}\", \",\");");
                                }
                                else if (cols[i].Type.ToLower() == "date_time")
                                {
                                    streamWriter.WriteLine($"\t\t\t{cols[i].Name} = (cols[{index}] == \"-1\") ? default(DateTime) : DateTime.Parse(cols[{index}]);");
                                }
                                else if (cols[i].Type.ToLower() == "array_int")
                                {
                                    streamWriter.WriteLine($"\t\t\t{cols[i].Name} = (cols[{index}] == \"-1\") ? null : Array.ConvertAll(cols[{index}].Split('|'), s => int.Parse(s));");
                                }
                                else if (cols[i].Type.ToLower() == "array_long")
                                {
                                    streamWriter.WriteLine($"\t\t\t{cols[i].Name} = (cols[{index}] == \"-1\") ? null : Array.ConvertAll(cols[{index}].Split('|'), s => long.Parse(s));");
                                }
                                else
                                {
                                    streamWriter.WriteLine($"\t\t\t{cols[i].Name} = ({cols[i].Type})Enum.Parse(typeof({cols[i].Type}), cols[{index}]);");
                                }
                                index++;
                            }
                        }
                        else
                        {
                            streamWriter.WriteLine("\t\tpublic void Serialize(Dictionary<string, string> data)");
                            streamWriter.WriteLine("\t\t{");

                            int index = 0;
                            for (var i = 0; i < cols.Count; i++)
                            {
                                if (cols[i].Name.StartsWith("~") == true)
                                {
                                    continue;
                                }

                                if (cols[i].Type == "float"
                                || cols[i].Type == "long"
                                || cols[i].Type == "int"
                                || cols[i].Type == "short"
                                || cols[i].Type == "byte")
                                {
                                    streamWriter.WriteLine($"\t\t\tif (data.ContainsKey(\"{cols[i].Name}\") == true) {cols[i].Name} = {cols[i].Type}.Parse(data[\"{cols[i].Name}\"]);");
                                }
                                else if (cols[i].Type == "bool")
                                {
                                    streamWriter.WriteLine($"\t\t\tif (data.ContainsKey(\"{cols[i].Name}\") == true) {cols[i].Name} = {cols[i].Type}.Parse(data[\"{cols[i].Name}\"]);");
                                }
                                else if (cols[i].Type == "string")
                                {
                                    streamWriter.WriteLine($"\t\t\tif (data.ContainsKey(\"{cols[i].Name}\") == true) {cols[i].Name} = data[\"{cols[i].Name}\"].Replace(\"{{$}}\", \",\");");
                                }
                                else if (cols[i].Type.ToLower() == "date_time")
                                {
                                    streamWriter.WriteLine($"\t\t\tif (data.ContainsKey(\"{cols[i].Name}\") == true) {cols[i].Name} = (data[\"{cols[i].Name}\"] == \"-1\") ? default(DateTime) : DateTime.Parse(data[\"{cols[i].Name}\"]);");
                                }
                                else if (cols[i].Type.ToLower() == "array_int")
                                {
                                    streamWriter.WriteLine($"\t\t\tif (data.ContainsKey(\"{cols[i].Name}\") == true) {cols[i].Name} = (data[\"{cols[i].Name}\"] == \"-1\") ? null : Array.ConvertAll(data[\"{cols[i].Name}\"].Split('|'), s => int.Parse(s));");
                                }
                                else if (cols[i].Type.ToLower() == "array_long")
                                {
                                    streamWriter.WriteLine($"\t\t\tif (data.ContainsKey(\"{cols[i].Name}\") == true) {cols[i].Name} = (data[\"{cols[i].Name}\"] == \"-1\") ? null : Array.ConvertAll(data[\"{cols[i].Name}\"].Split('|'), s => long.Parse(s));");
                                }
                                else if (cols[i].Type.ToLower() == "array_string")
                                {
                                    streamWriter.WriteLine($"\t\t\tif (data.ContainsKey(\"{cols[i].Name}\") == true) {cols[i].Name} = (data[\"{cols[i].Name}\"] == \"-1\") ? null : data[\"{cols[i].Name}\"].Split('|');");
                                }
                                else
                                {
                                    streamWriter.WriteLine($"\t\t\tif (data.ContainsKey(\"{cols[i].Name}\") == true) {cols[i].Name} = ({cols[i].Type})Enum.Parse(typeof({cols[i].Type}), data[\"{cols[i].Name}\"]);");
                                }
                                index++;
                            }
                        }
                        
                        streamWriter.WriteLine("\t\t}");                            
                        streamWriter.WriteLine("\t}");                            
                        streamWriter.WriteLine("}");                            
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine($"Error GenerateCode. filePath: {filePath}");
                return false;
            }

            return true;
        }  
 */
