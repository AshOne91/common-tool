using System.Text;
using System.Collections.Generic;

namespace common_tool.Code
{
	using System.Text;
	using System.Collections.Generic;

	public enum AccessModifier { Public, Private, Protected, Internal, None }
	public enum ClassType { None, Sealed, Abstract, Static }
	public class CodeGenerator
	{
		private StringBuilder _codeGenerator;
		private List<string> _front;
		private Stack<string> _end;
		private int _tabTemplate = 0;
		private string _code = "";
		public string Code { get { return _code; } }

		/////////////////////////////////////////////////////
		public void StartWritingCode()
		{
			_codeGenerator = new StringBuilder();
			_front = new List<string>();
			_end = new Stack<string>();
			_tabTemplate = 0;
			_code = string.Empty;
		}
		public void EndWritingCode()
		{
			_codeGenerator.Clear();
			foreach (string s in _front)
			{
				_codeGenerator.Append(s);
			}
			foreach (string s in _end)
			{
				_codeGenerator.Append(s);
			}
			_code = _codeGenerator.ToString();
		}
		public string GetTab()
		{
			string tabsize = "";
			for (int i = 0; i < _tabTemplate; i++)
			{
				tabsize += "\t";
			}
			return tabsize;
		}

		/////////////////////////////////////////////////////
		public void Write(string code = "")
		{
			_codeGenerator = new StringBuilder();
			_codeGenerator.Append(code);
			_front.Add(_codeGenerator.ToString());
		}
		public void WriteLine(string code = "")
		{
			_codeGenerator = new StringBuilder();
			_codeGenerator.Append(code);
			_codeGenerator.Append("\n");
			_front.Add(_codeGenerator.ToString());
		}
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
			_codeGenerator.Clear();
			_codeGenerator.Append(GetTab());
			_codeGenerator.Append("{\n");
			_tabTemplate += 1;
			_front.Add(_codeGenerator.ToString());
		}
		public void WriteCloseBracket()
		{
			_codeGenerator.Clear();
			_tabTemplate -= 1;
			_codeGenerator.Append(GetTab());
			_codeGenerator.Append("}\n");
			_front.Add(_codeGenerator.ToString());
		}

		//////////////////////////////////////////////////////basic code template
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:명명 스타일", Justification = "<보류 중>")]
		public void _using(params string[] _namespaces)
		{
			_codeGenerator.Clear();
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
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:명명 스타일", Justification = "<보류 중>")]
		public void _namespace(params string[] _namespaces)
		{
			_codeGenerator.Clear();
			_codeGenerator.Append("\n");
			_codeGenerator.Append("namespace ");
			for (int i = 0; i < _namespaces.Length; i++)
			{
				_codeGenerator.Append(_namespaces[i]);
				if (i == _namespaces.Length - 1)
				{
					_codeGenerator.Append("\n{\n");
				}
				else
				{
					_codeGenerator.Append(".");
				}
			}
			_front.Add(_codeGenerator.ToString());
			_end.Push("\n}");

			_tabTemplate += 1;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:명명 스타일", Justification = "<보류 중>")]
		public void _interface(AccessModifier accessModifier, string _interfaceName)
		{
			_codeGenerator.Clear();
			_codeGenerator.Append("\n");
			_codeGenerator.Append(GetTab());

			switch (accessModifier)
			{
				case AccessModifier.Public:
					_codeGenerator.Append("public ");
					break;
				case AccessModifier.Private:
					_codeGenerator.Append("private ");
					break;
				case AccessModifier.Protected:
					_codeGenerator.Append("protected ");
					break;
				case AccessModifier.Internal:
					_codeGenerator.Append("internal ");
					break;
				case AccessModifier.None:
					break;
			}
			_codeGenerator.Append("interface ");
			_codeGenerator.Append(_interfaceName);
			_codeGenerator.Append("\n");
			_codeGenerator.Append(GetTab());
			_codeGenerator.Append("{");
			_front.Add(_codeGenerator.ToString());

			_codeGenerator.Clear();
			_codeGenerator.Append("\n");
			_codeGenerator.Append(GetTab());
			_codeGenerator.Append("}");
			_end.Push(_codeGenerator.ToString());

			_tabTemplate += 1;
		}
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:명명 스타일", Justification = "<보류 중>")]
		public void _class(AccessModifier accessModifier, ClassType classType, string _className, params string[] _inheritances)
		{
			_codeGenerator.Clear();
			_codeGenerator.Append("\n");
			_codeGenerator.Append(GetTab());

			switch (accessModifier)
			{
				case AccessModifier.Public:
					_codeGenerator.Append("public ");
					break;
				case AccessModifier.Private:
					_codeGenerator.Append("private ");
					break;
				case AccessModifier.Protected:
					_codeGenerator.Append("protected ");
					break;
				case AccessModifier.Internal:
					_codeGenerator.Append("internal ");
					break;
				case AccessModifier.None:
					break;
			}
			switch (classType)
			{
				case ClassType.None:
					_codeGenerator.Append("class ");
					break;
				case ClassType.Abstract:
					_codeGenerator.Append("abstract class");
					break;
				case ClassType.Static:
					_codeGenerator.Append("static class ");
					break;
				case ClassType.Sealed:
					_codeGenerator.Append("sealed class ");
					break;
			}
			_codeGenerator.Append(_className);

			//상속클래스, 인터페이스
			if (_inheritances.Length == 0)
			{
				_codeGenerator.Append("\n");
				_codeGenerator.Append(GetTab());
				_codeGenerator.Append("{");
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

					//마지막이라면
					if (i == _inheritances.Length - 1)
					{
						_codeGenerator.Append("\n");
						_codeGenerator.Append(GetTab());
						_codeGenerator.Append("{\n");
					}
					else
					{
						_codeGenerator.Append(", ");
					}
				}
			}
			_front.Add(_codeGenerator.ToString());

			_codeGenerator.Clear();
			_codeGenerator.Append("\n");
			_codeGenerator.Append(GetTab());
			_codeGenerator.Append("}");
			_end.Push(_codeGenerator.ToString());

			_tabTemplate += 1;
		}

		////////////////////////////////////////////////////////member, method template
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:명명 스타일", Justification = "<보류 중>")]
		public void _var(AccessModifier accessModifier, string varType, string varName, string varAssignment = "{ get; set; }")
		{
			_codeGenerator.Clear();
			_codeGenerator.Append(GetTab());
			switch (accessModifier)
			{
				case AccessModifier.Public:
					_codeGenerator.Append("public ");
					break;
				case AccessModifier.Private:
					_codeGenerator.Append("private ");
					break;
				case AccessModifier.Protected:
					_codeGenerator.Append("protected ");
					break;
				case AccessModifier.Internal:
					_codeGenerator.Append("internal ");
					break;
				case AccessModifier.None:
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

		/// <param name="returnType">메소드 리턴타입 void, int, etc...</param>
		/// <param name="arguments">"Type 인자명, Type 인자명"식으로 기입</param>
		/// <param name="implementMethods">각 요소들 끝에 ; 기입</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:명명 스타일", Justification = "<보류 중>")]
		public void _method(AccessModifier accessModifier, string returnType, string methodName, string arguments, params string[] implementMethods)
		{
			_codeGenerator.Clear();
			_codeGenerator.Append(GetTab());
			switch (accessModifier)
			{
				case AccessModifier.Public:
					_codeGenerator.Append("public ");
					break;
				case AccessModifier.Private:
					_codeGenerator.Append("private ");
					break;
				case AccessModifier.Protected:
					_codeGenerator.Append("protected ");
					break;
				case AccessModifier.Internal:
					_codeGenerator.Append("internal ");
					break;
				case AccessModifier.None:
					break;
			}
			_codeGenerator.Append(returnType);
			_codeGenerator.Append(" ");
			_codeGenerator.Append(methodName);
			_codeGenerator.Append("(");
			_codeGenerator.Append(arguments);
			_codeGenerator.Append(")\n");
			_codeGenerator.Append(GetTab());
			_codeGenerator.Append("{\n");
			_tabTemplate += 1;
			foreach (string s in implementMethods)
			{
				_codeGenerator.Append(GetTab());
				_codeGenerator.Append(s);
				_codeGenerator.Append("\n");
			}
			_tabTemplate -= 1;
			_codeGenerator.Append(GetTab());
			_codeGenerator.Append("}\n");

			_front.Add(_codeGenerator.ToString());
		}
	}
}

