using System.Text;
using System.Collections.Generic;

namespace common_tool.Code
{
	public enum AM { Public, Private, Protected, Internal, None }

	/// <summary>
	/// cs를 스크립트로 생성하는 것을 생산성 좋게 하기 위해 만든 클래스입니다.
	/// </summary>
	public class CodeWriter
	{
		private int _tabTemplate = 0;
		private StringBuilder _codeGenerator = new StringBuilder();
		private List<string> _front = new List<string>();
		private Stack<string> _end = new Stack<string>();
		private string _code = "";
		public string Code { get { return _code; } }
		
		//start, end
		public void StartWritingCode()
		{
			_codeGenerator.Clear();
			_tabTemplate = 0;
			_front.Clear();
			_end.Clear(); 
			_code = "";
		}
		public string EndWritingCode()
		{
			_codeGenerator = new StringBuilder();
			foreach (string s in _front)
			{
				_codeGenerator.Append(s);
			}
			foreach (string s in _end)
			{
				_codeGenerator.Append(s);
			}
			_code = _codeGenerator.ToString(); 
			return _code;
		}

		//basic
		/// <summary>
		/// 기본적인 작성메소드입니다.
		/// </summary>
		public void WriteLine(string code = "")
		{
			_codeGenerator = new StringBuilder();
			_codeGenerator.Append(code);
			_codeGenerator.Append("\n");
			_front.Add(_codeGenerator.ToString());
		}
		/// <summary>
		/// 들여쓰기 처리가 들어있는 작성메소드입니다.
		/// </summary>
		public void WriteLineWithTab(string code)
		{
			_codeGenerator = new StringBuilder();
			_codeGenerator.Append(GetTab());
			_codeGenerator.Append(code);
			_codeGenerator.Append("\n");
			_front.Add(_codeGenerator.ToString());
		}
		public void WriteOpenBracket()
		{
			_codeGenerator = new StringBuilder();
			_codeGenerator.Append(GetTab()); 
			_codeGenerator.Append("{\n"); 
			_tabTemplate += 1; 
			_front.Add(_codeGenerator.ToString());
		}
		public void WriteCloseBracket()
		{
			_codeGenerator = new StringBuilder(); 
			_tabTemplate -= 1;
			_codeGenerator.Append(GetTab());
			_codeGenerator.Append("}\n");
			_front.Add(_codeGenerator.ToString());
		}


		//code template
		/// <summary>
		/// 현재 탭수준에 맞는 탭양식을 반환합니다.
		/// </summary>
		public string GetTab()
		{
			string tabsize = "";
			for (int i = 0; i < _tabTemplate; i++)
			{
				tabsize += "\t";
			}
			return tabsize;
		}

		//basic code template
		public void _using(params string[] _namespaces)
		{
			_codeGenerator = new StringBuilder();
			_codeGenerator.Append("using ");
			for (int i = 0; i < _namespaces.Length; i++)
			{
				_codeGenerator.Append(_namespaces[i]);
				if (i == _namespaces.Length - 1)
				{
					_codeGenerator.Append(";\n");
				}
				else
				{
					_codeGenerator.Append(".");
				}
			}
			_front.Add(_codeGenerator.ToString());
		}
		public void _namespace(params string[] _namespaces)
		{
			_codeGenerator = new StringBuilder();
			_codeGenerator.Append("\n");
			_codeGenerator.Append("namespace ");
			for (int i = 0; i < _namespaces.Length; i++)
			{
				_codeGenerator.Append(_namespaces[i]);
				if (i == _namespaces.Length - 1)
				{
					_codeGenerator.Append(" {\n");
				}
				else
				{
					_codeGenerator.Append(".");
				}
			}
			_front.Add(_codeGenerator.ToString());
			_end.Push("\n}");

			_tabTemplate = 1;
		}
		public void _interface(AM accessModifier, string _interfaceName)
		{
			_codeGenerator = new StringBuilder();
			_codeGenerator.Append("\n");
			_codeGenerator.Append(GetTab());
			//접근제한자
			switch (accessModifier)
			{
				case AM.Public:
					_codeGenerator.Append("public ");
					break;
				case AM.Private:
					_codeGenerator.Append("private ");
					break;
				case AM.Protected:
					_codeGenerator.Append("protected ");
					break;
				case AM.Internal:
					_codeGenerator.Append("internal ");
					break;
				case AM.None:
					break;
			}
			_codeGenerator.Append("interface");
			_codeGenerator.Append(" ");
			_codeGenerator.Append(_interfaceName);
			_front.Add(_codeGenerator.ToString());

			_codeGenerator = new StringBuilder();
			_codeGenerator.Append("\n");
			_codeGenerator.Append(GetTab());
			_codeGenerator.Append("}");
			_end.Push(_codeGenerator.ToString());
			_tabTemplate = 2;
		}
		public void _class(AM accessModifier, string _classType, string _className, params string[] _inheritances)
		{
			_codeGenerator = new StringBuilder();
			_codeGenerator.Append("\n");
			_codeGenerator.Append(GetTab());

			//접근제한자
			switch (accessModifier)
			{
				case AM.Public:
					_codeGenerator.Append("public ");
					break;
				case AM.Private:
					_codeGenerator.Append("private ");
					break;
				case AM.Protected:
					_codeGenerator.Append("protected ");
					break;
				case AM.Internal:
					_codeGenerator.Append("internal ");
					break;
				case AM.None:
					break;
			}
			_codeGenerator.Append(_classType);
			_codeGenerator.Append(" ");
			_codeGenerator.Append("class");
			_codeGenerator.Append(" ");
			_codeGenerator.Append(_className);

			//상속클래스, 인터페이스
			if (_inheritances.Length == 0)
			{
				_codeGenerator.Append(" {\n");
			}
			else
			{
				for (int i = 0; i < _inheritances.Length; i++)
				{
					if (i == 0)
					{
						_codeGenerator.Append(" : ");
					}
					_codeGenerator.Append(_inheritances[i]);
					if (i == _inheritances.Length - 1)
					{
						_codeGenerator.Append(" {\n");
					}
					else
					{
						_codeGenerator.Append(", ");
					}
				}
			}
			_front.Add(_codeGenerator.ToString());

			_codeGenerator = new StringBuilder();
			_codeGenerator.Append("\n");
			_codeGenerator.Append(GetTab());
			_codeGenerator.Append("}");
			_end.Push(_codeGenerator.ToString());
			_tabTemplate = 2;
		}

		//member, method template
		/// <summary>
		/// varType
		/// <br />
		/// "sealed varType"
		/// "static varType"
		/// "override varType"
		/// "abstract varType"
		/// "virtual varType"
		/// <br />
		/// varAssignment
		/// <br />
		/// 선언 ";"
		/// 할당 "= sentence;"
		/// 프로퍼티 "{ get; set;}"
		/// </summary>
		public void _var(AM accessModifier, string varType, string varName, string varAssignment = "{ get; set; }")
		{
			_codeGenerator = new StringBuilder();
			_codeGenerator.Append(GetTab());
			switch (accessModifier)
			{
				case AM.Public:
					_codeGenerator.Append("public ");
					break;
				case AM.Private:
					_codeGenerator.Append("private ");
					break;
				case AM.Protected:
					_codeGenerator.Append("protected ");
					break;
				case AM.Internal:
					_codeGenerator.Append("internal ");
					break;
				case AM.None:
					break;
			}
			_codeGenerator.Append(varType);
			_codeGenerator.Append(" ");
			_codeGenerator.Append(varName);
			_codeGenerator.Append(" ");
			_codeGenerator.Append(varAssignment);
			_codeGenerator.Append("\n");

			_front.Add(_codeGenerator.ToString());
		}

		/// <summary>
		/// implementMethods의 각 요소들은 ;로 끝을 맺어야 합니다.
		/// </summary>
		/// <param name="returnType">메소드 리턴타입</param>
		/// <param name="methodName">메소드 이름</param>
		/// <param name="haveArgu">인자값을 가지는지</param>
		/// <param name="arguments">"Type 인자명"식으로 기입</param>
		/// <param name="implementMethods"></param>
		public void _method(AM accessModifier, string returnType, string methodName, bool haveArgu, string arguments, params string[] implementMethods)
		{
			_codeGenerator = new StringBuilder();
			_codeGenerator.Append(GetTab());
			switch (accessModifier)
			{
				case AM.Public:
					_codeGenerator.Append("public ");
					break;
				case AM.Private:
					_codeGenerator.Append("private ");
					break;
				case AM.Protected:
					_codeGenerator.Append("protected ");
					break;
				case AM.Internal:
					_codeGenerator.Append("internal ");
					break;
				case AM.None:
					break;
			}
			_codeGenerator.Append(returnType);
			_codeGenerator.Append(" ");
			_codeGenerator.Append(methodName);
			_codeGenerator.Append("(");
			if (haveArgu == true)
			{
				_codeGenerator.Append(arguments);
			}
			_codeGenerator.Append(")");
			_codeGenerator.Append("{\n");
			_tabTemplate += 1;
			foreach (string s in implementMethods)
			{
				_codeGenerator.Append(GetTab());
				_codeGenerator.Append(s);
				_codeGenerator.Append("\n");
			}
			_tabTemplate -= 1;
			_codeGenerator.Append("}\n");
			_front.Add(_codeGenerator.ToString());

		}

	}
}

