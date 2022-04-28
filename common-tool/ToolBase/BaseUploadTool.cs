using System.IO;

namespace common_tool
{
	public class BaseUploadTool : BaseTool
	{
		public override bool Run(string sourcePath)
		{
			_sourcePath = Directory.GetCurrentDirectory() + sourcePath;
			return true;
		}
		public virtual bool Upload(string sourcePath, string addressPath)
		{
			return false;
		}
	}
}
