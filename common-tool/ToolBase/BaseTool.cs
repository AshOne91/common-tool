namespace common_tool
{
	public abstract class BaseTool
	{	
		//작업시작 디렉토리
		protected string _sourcePath = string.Empty;
		public abstract bool Run(string sourcePath);
	}
}
