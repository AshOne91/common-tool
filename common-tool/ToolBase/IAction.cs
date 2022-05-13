namespace common_tool
{
	public abstract class ActionBase
	{
		protected Parameter _param;

		public ActionBase(Parameter param)
		{
			_param = param;
		}
		public abstract void Run();
	}
}
