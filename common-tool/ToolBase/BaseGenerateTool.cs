using System.IO;

namespace common_tool
{
	public class BaseGenerateTool : BaseTool
	{	
		//작업완료 디렉토리
		protected string _outputPath = string.Empty;
		public override bool Run(string sourcePath)
		{
			//작업시작 디렉토리 경로설정
			_sourcePath = Directory.GetCurrentDirectory() + sourcePath;

			//작업완료 디렉토리 경로설정
			_outputPath = Directory.GetCurrentDirectory() + "\\" + "output";
			if (Directory.Exists(_outputPath) == false)
				Directory.CreateDirectory(_outputPath);

			//작업할 파일경로설정
			return true;
		}
		public virtual bool Generate(string sourcePath, string outputPath)
		{
			return false;
		}
	}
}
