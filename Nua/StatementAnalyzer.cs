using System.Threading.Channels;

namespace Nua.Transpiler;

public static class StatementAnalyzer {
	public class Statement(List<Lexer.Token> tokens) {
		public List<Lexer.Token> Tokens { get; set; } = tokens;

		public Statement Clone() {
			var newTokens = new List<Lexer.Token>();

			foreach (var t in this.Tokens) {
				newTokens.Add(new Lexer.Token(t.Type, t.Value));
			}

			var newStm = new Statement(newTokens);

			return newStm;
		}
	}

	static string CommentStatement(Statement stm) {
		var retStm = "";

		if (stm.Tokens[0].Value == "/" &&
			stm.Tokens[1].Value == "/") {
			retStm = "--";

			for (var i = 2; i < stm.Tokens.Count; i++) {
				retStm += stm.Tokens[i].Value + " ";
			}
		}

		return retStm;
	}

	static (string, string) DirectoryStatement(Statement stm) {
		var retStm = "";

		var dir = "";

		if (stm.Tokens.Count > 3) {
			if (stm.Tokens[0].Value == "dir" &&
				stm.Tokens[1].Value == "<" &&
				stm.Tokens[^1].Value == ">") {

				for (var i = 2; i < stm.Tokens.Count - 1; i++) {
					dir += stm.Tokens[i].Value;
				}

				dir = dir.Replace("\\", "\\\\");

				retStm = "package.path=package.path..\";" + dir + "?.lua\"";
			}
		}

		return (retStm, dir);
	}

	static string IncludeStatement(Statement stm, string dir) {
		var retStm = "";

		var path = "";

		if (stm.Tokens[0].Value == "inc" &&
			stm.Tokens[1].Value == "<" &&
			stm.Tokens[^1].Value == ">") {

			for (var i = 2; i < stm.Tokens.Count - 1; i++) {
				path += stm.Tokens[i].Value;
			}

			path = dir + "\\" + path;

			if (path.Substring(path.Length - 3) == "nua") {
				using (var sr = new StreamReader(path)) {
					var incNua = new Nua();

					string srRead = sr.ReadToEnd();

					var incOut = incNua.BasicTranspile(srRead);

					incOut = incOut.Remove(incOut.Length - 1);

					retStm = incOut;
				}
			}
		}

		return retStm;
	}

	static string AcquireStatement(Statement stm, int offset, string prefix) {
		var retStm = "";

        if (stm.Tokens.Count == 6) {
            if (stm.Tokens[0 + offset].Value == "acq" &&
				stm.Tokens[1 + offset].Value == "<" &&
				stm.Tokens[2 + offset].Type == Lexer.TokenType.Identifier &&
				stm.Tokens[3 + offset].Value == ">" &&
				stm.Tokens[4 + offset].Value == "@" &&
				stm.Tokens[5 + offset].Type == Lexer.TokenType.Identifier) {

				stm.Tokens[2 + offset].Value = stm.Tokens[2 + offset].Value.Replace("\\", "\\\\");

                var tag = stm.Tokens[5 + offset].Value;
				var lib = stm.Tokens[2 + offset].Value;

				var end = lib.Substring(lib.Length - 3);

				if (end == "lua") {
					retStm = prefix + stm.Tokens[5 + offset].Value + "=" + "require(\"" + lib[..^4] + "\")";
				}
			}
		}

		return retStm;
	}

	static string UnaryStatement(Statement stm, int offset, string prefix) {
		var retStm = "";


		var vari = "";

		if (stm.Tokens.Count >= 3) {
			if (stm.Tokens[^2].Value == "+" &&
				stm.Tokens[^1].Value == "+") {

				for (var i = 0; i < stm.Tokens.Count - 2; i++) {
					vari += stm.Tokens[i].Value;
				}

				retStm = prefix + vari + "=" + prefix + vari + "+1";
			}
			else if (stm.Tokens[^2].Value == "-" &&
				stm.Tokens[^1].Value == "-") {
				for (var i = 0; i < stm.Tokens.Count - 2; i++) {
					vari += stm.Tokens[i].Value;
				}

				retStm = prefix + vari + "=" + prefix + vari + "-1";
			}
		}

		return retStm;
	}

	static string VariableStatement(Statement stm, int offset, string prefix) {
		var retStm = "";

		var stmCln = stm.Clone();

		var expression = "(";

		if (stmCln.Tokens[0 + offset].Value == "loc" &&
			stmCln.Tokens[1 + offset].Type == Lexer.TokenType.Identifier &&
			stmCln.Tokens[2 + offset].Type == Lexer.TokenType.AssignmentOperator) {


			for (var i = 3 + offset; i < stmCln.Tokens.Count; i++) {
				if (stmCln.Tokens[i].Value == "{" && stm.Tokens[i + 1].Value != "[") {
					if (stmCln.Tokens[^1].Value == "0") {
						stmCln.Tokens[i].Value = "{ [0]=0";
						stmCln.Tokens[^1].Value = "";
					}
				}
				if (stmCln.Tokens[i].Value == "!") {
					stmCln.Tokens[i].Value = "_nua_program.";
				}

				expression += stmCln.Tokens[i].Value;
			}

			expression += ")";

			retStm = (prefix + stmCln.Tokens[1 + offset].Value + stmCln.Tokens[2 + offset].Value + expression + ";");
		}

		return retStm;
	}

	static string ModifyStatement(Statement stm, int offset, string prefix) {
		var stmCln = stm.Clone();
	
		var retStm = "";

		var expression = "(";

		if (stmCln.Tokens[0 + offset].Type == Lexer.TokenType.Identifier &&
			stmCln.Tokens[1 + offset].Value == ".." &&
			stmCln.Tokens[2 + offset].Value == "=") {

			for (var i = 3 + offset; i < stmCln.Tokens.Count; i++) {
				if (stmCln.Tokens[i].Value == "!") {
					stmCln.Tokens[i].Value = "_nua_program.";
				}
				expression += stmCln.Tokens[i].Value;
			}

			expression += ")";

			retStm = prefix + stmCln.Tokens[0 + offset].Value + "=" + stmCln.Tokens[0 + offset].Value + ".." + expression + ";";
		}

		if (stmCln.Tokens[0 + offset].Type == Lexer.TokenType.Identifier &&
			stmCln.Tokens[1 + offset].Type == Lexer.TokenType.AssignmentOperator) {

			for (var i = 2 + offset; i < stmCln.Tokens.Count; i++) {
				if (stmCln.Tokens[i].Value == "!") {
					stmCln.Tokens[i].Value = "_nua_program.";
				}
				if (stmCln.Tokens[i].Value == "{" &&
					stmCln.Tokens[i + 1].Value == "}" &&
					stmCln.Tokens[i + 2].Value == "0") {

					stmCln.Tokens[i].Value = "{[0]=0}";
                	stmCln.Tokens[i + 1].Value = "";
					stmCln.Tokens[i + 2].Value = "";
				}

				expression += stmCln.Tokens[i].Value;
			}

			expression += ")";


			if (stmCln.Tokens[1 + offset].Value.Length == 1)
				retStm = prefix + stmCln.Tokens[0 + offset].Value + stmCln.Tokens[1 + offset].Value + expression + ";";

			else if (stmCln.Tokens[1 + offset].Value.Length == 2)
				retStm = prefix + stmCln.Tokens[0 + offset].Value + stmCln.Tokens[1 + offset].Value[1] + stmCln.Tokens[0 + offset].Value + stmCln.Tokens[1 + offset].Value[0] + expression + ";";
		}

		return retStm;
	}

	static string MetaStatement(Statement stm, int offset, string prefix, string prefix2) {
		var retStm = "";

		if (stm.Tokens[0 + offset].Value == "meta" &&
			stm.Tokens[1 + offset].Type == Lexer.TokenType.Identifier) {

			retStm = prefix + stm.Tokens[1 + offset].Value + "=" + "{};";

			if (stm.Tokens[^2].Value != "*" ||
				stm.Tokens[^1].Value != "*") {

				retStm += "\n\t";
				retStm += prefix2 + stm.Tokens[1 + offset].Value + ".__index=" + prefix2 + stm.Tokens[1 + offset].Value;
			}

			if (stm.Tokens[2 + offset].Value == "<") {
				retStm += "\n\t" + "function " + prefix2 + stm.Tokens[1 + offset].Value + ".new" + "(";

				for (var i = 3 + offset; i < stm.Tokens.Count - 1; i++) {
					retStm += stm.Tokens[i].Value;
				}

				retStm += ")\n\t";

				retStm += "local self = setmetatable({}," + prefix2 + stm.Tokens[1 + offset].Value + ");\n\t";

				for (var i = 3 + offset; i < stm.Tokens.Count - 1; i++) {
					if (stm.Tokens[i].Value != ",")
						retStm += "self." + stm.Tokens[i].Value + "=" + stm.Tokens[i].Value + ";\n\t";
				}
				retStm += "return self;\n\t";

				retStm += "end";
			}
			else if (stm.Tokens[^1].Value != "*") {
				return "";
			}
		}

		return retStm;
	}

	static string FunctionDefStatement(Statement stm, int offset, string prefix) {
		var retStm = "";

		var parameters = "";

		if (stm.Tokens.Count > 2 &&
			stm.Tokens[0 + offset].Type == Lexer.TokenType.Identifier &&
			stm.Tokens[1 + offset].Type == Lexer.TokenType.LParen &&
			stm.Tokens[stm.Tokens.Count - 2].Type == Lexer.TokenType.RParen &&
			stm.Tokens[stm.Tokens.Count - 1].Type == Lexer.TokenType.LCurly) {

			for (var i = 2 + offset; i < stm.Tokens.Count - 2; i++) {
				if (stm.Tokens[i].Type != Lexer.TokenType.RParen) {

					parameters += stm.Tokens[i].Value;
				}
			}

			retStm = ("function " + prefix + stm.Tokens[0 + offset].Value + "(" + parameters + ")");
		}

		return retStm;
	}

	static string FunctionCallStatement(Statement stm, int offset, string prefix) {
		var stmCln = stm.Clone();

		var retStm = "";

		var arguments = "";

		if (stmCln.Tokens[0 + offset].Type == Lexer.TokenType.Identifier &&
			stmCln.Tokens[1 + offset].Type == Lexer.TokenType.LParen &&
			stmCln.Tokens[^1].Type == Lexer.TokenType.RParen) {

			for (var i = 2 + offset; i < stmCln.Tokens.Count - 1; i++) {
				if (stmCln.Tokens[i].Value == "{" &&
					stmCln.Tokens[i + 1].Value == "}" &&
					stmCln.Tokens[i + 2].Value == "0") {

					stmCln.Tokens[i].Value = "{[0]=0}";
					stmCln.Tokens[i + 1].Value = "";
					stmCln.Tokens[i + 2].Value = "";
				}
					     										
				arguments += stmCln.Tokens[i].Value;
			}

			retStm = (prefix + stm.Tokens[0 + offset].Value + "(" + arguments + ")");
		}

		return retStm;
	}

	static string ScopeStatement(Statement stm) {
		var retStm = "";

		if (stm.Tokens[0].Value == "scope" &&
			stm.Tokens[1].Value == "{") {

			retStm = "do";
		}

		return retStm;
	}

	static string CowrapStatement(Statement stm) {
		var retStm = "";

		if (stm.Tokens[0].Value == "cowrap" &&
			stm.Tokens[1].Value == "{") {

			retStm = "coroutine.wrap(function()";
		}

		return retStm;
	}

	static string IfStatement(Statement stm) {
		var retStm = "";

		var condition = "";

		if (stm.Tokens[0].Value == "if" &&
			stm.Tokens[1].Type == Lexer.TokenType.LParen &&
			stm.Tokens[^2].Type == Lexer.TokenType.RParen &&
			stm.Tokens[^1].Type == Lexer.TokenType.LCurly) {

			for (var i = 2; i < stm.Tokens.Count - 2; i++) {
				if (stm.Tokens[i].Value == "or") {
					stm.Tokens[i].Value = " or ";
				}
				if (stm.Tokens[i].Value == "and") {
					stm.Tokens[i].Value = " or ";
				}
				condition += stm.Tokens[i].Value;
			}

			retStm = "if (" + condition + ") then";
		}

		return retStm;
	}

	static string ElseStatement(Statement stm) {
		var retStm = "";

		if (stm.Tokens[0].Value == "else" &&
			stm.Tokens[1].Type == Lexer.TokenType.LCurly &&
			stm.Tokens.Count == 2) {

			retStm = "else";
		}

		return retStm;
	}

	static string ElseIfStatement(Statement stm) {
		var retStm = "";

		var stmCln = stm.Clone();

		if (stmCln.Tokens[0].Value.Length > 4) {
			stmCln.Tokens[0].Value = stmCln.Tokens[0].Value.Remove(0, 4);

			retStm = IfStatement(stmCln);

			if (retStm != "")
				retStm = "else" + retStm;
		}

		return retStm;
	}

	static string SwitchStatement(Statement stm, Statement nxStm) {
		var retStm = "";

		if (stm.Tokens[0].Value == "switch" &&
			stm.Tokens[1].Type == Lexer.TokenType.LParen &&
			stm.Tokens[^2].Type == Lexer.TokenType.RParen &&
			stm.Tokens[^1].Type == Lexer.TokenType.LCurly) {

			if (nxStm.Tokens[0].Value == "case" &&
				nxStm.Tokens[1].Type == Lexer.TokenType.LParen &&
				nxStm.Tokens[^2].Type == Lexer.TokenType.RParen &&
				nxStm.Tokens[^1].Type == Lexer.TokenType.LCurly) {

				retStm = "if ";

				for (var i = 2; i < stm.Tokens.Count - 2; i++) {
					retStm += stm.Tokens[i].Value;
				}

				retStm += " then";
			}
		}

		return retStm;
	}

	static string CaseStatement(Statement stm, Stack<Statement> switches, ref Stack<Statement> cases) {
		var retStm = "";

		var curSwitch = "";

		if (stm.Tokens[0].Value == "case" &&
			stm.Tokens[1].Type == Lexer.TokenType.LParen &&
			stm.Tokens[^2].Type == Lexer.TokenType.RParen &&
			stm.Tokens[^1].Type == Lexer.TokenType.LCurly) {

			if (cases.Count > 0) {
				retStm = "elseif ";
			}
			else {
				retStm = "if ";
			}

			for (var i = 2; i < switches.Peek().Tokens.Count - 2; i++) {
				curSwitch += switches.Peek().Tokens[i].Value;
			}

			retStm += curSwitch + " == ";

			for (var i = 2; i < stm.Tokens.Count - 2; i++) {
				retStm += stm.Tokens[i].Value;
			}

			retStm += " then";

			cases.Push(stm);
		}

		return retStm;
	}

	static string DefaultStatement(Statement stm, ref Stack<Statement> cases) {
		var retStm = "";

		if (stm.Tokens.Count == 1 &&
			stm.Tokens[0].Value == "default") {

			cases.Clear();

			retStm = "end";
		}
		else if (stm.Tokens.Count == 2 &&
			stm.Tokens[0].Value == "default" &&
			stm.Tokens[1].Type == Lexer.TokenType.LCurly) {

			cases.Clear();

			retStm = "else";
		}

		return retStm;
	}

	static string WhileStatement(Statement stm) {
		var retStm = "";

		var condition = "";

		if (stm.Tokens[0].Value == "while" &&
			stm.Tokens[1].Type == Lexer.TokenType.LParen &&
			stm.Tokens[^2].Type == Lexer.TokenType.RParen &&
			stm.Tokens[^1].Type == Lexer.TokenType.LCurly) {

			for (var i = 2; i < stm.Tokens.Count - 2; i++) {
				condition += stm.Tokens[i].Value;
			}

			retStm = "while" + "(" + condition + ")" + "do";
		}

		return retStm;
	}

	static string ForStatement(Statement stm) {
		var retStm = "";

		var arguments = "";

		if (stm.Tokens[0].Value == "for" &&
			stm.Tokens[1].Type == Lexer.TokenType.LParen &&
			stm.Tokens[^2].Type == Lexer.TokenType.RParen &&
			stm.Tokens[^1].Type == Lexer.TokenType.LCurly) {

			for (var i = 2; i < stm.Tokens.Count - 2; i++) {
				arguments += stm.Tokens[i].Value + " ";
			}

			retStm = "for" + " " + arguments + " " + "do";
		}

		return retStm;
	}

	static string CitforStatement(Statement stm) {
		var retStm = "";

		var passedIn = false;

		var arguments = "";

		if (stm.Tokens[0].Value == "citfor" &&
			stm.Tokens[1].Type == Lexer.TokenType.LParen &&
			stm.Tokens[^2].Type == Lexer.TokenType.RParen &&
			stm.Tokens[^1].Type == Lexer.TokenType.LCurly) {

            for (var i = 2; i < stm.Tokens.Count - 2; i++) {
				arguments += stm.Tokens[i].Value == "`" ? " " : stm.Tokens[i].Value;

			//	arguments += passedIn ? " " : " ";
				
				if (stm.Tokens[i].Value == "in") {
					passedIn = true;
				}
			}

			retStm = "for " + arguments + " do";
		}

        return retStm;
	}

	static string ForeachStatement(Statement stm) {
		var retStm = "";

		var i = stm.Tokens[0].Value == "iforeach";

		if (stm.Tokens[0].Value == "foreach" || stm.Tokens[0].Value == "iforeach" &&
			stm.Tokens[1].Type == Lexer.TokenType.LParen &&
			stm.Tokens[2].Type == Lexer.TokenType.Identifier &&
			stm.Tokens[3].Value == "," &&
			stm.Tokens[4].Type == Lexer.TokenType.Identifier &&
			stm.Tokens[5].Value == "in" &&
			stm.Tokens[6].Type == Lexer.TokenType.Identifier &&
			stm.Tokens[^2].Type == Lexer.TokenType.RParen &&
			stm.Tokens[^1].Type == Lexer.TokenType.LCurly) {

			if (!i)
				retStm = "for " + stm.Tokens[2].Value + "," + stm.Tokens[4].Value + " in " + "pairs(" + stm.Tokens[6].Value + ")" + "do";
			else
				retStm = "for " + stm.Tokens[2].Value + "," + stm.Tokens[4].Value + " in " + "_nua_program_iforeach(" + stm.Tokens[6].Value + ")" + "do";
		}

		return retStm;
	}

	static string BreakStatement(Statement stm) {
		var retStm = "";

		if (stm.Tokens[0].Value == "break" && stm.Tokens.Count == 1) {
			retStm = "break";
		}

		return retStm;
	}

	static string ContinueStatement(Statement stm, int curCont) {
		var retStm = "";

		if (stm.Tokens[0].Value == "continue" && stm.Tokens.Count == 1) {
			retStm = "goto " + "cont_" + String.Format("{0:D4}", curCont);
		}

		return retStm;
	}

	static string ReturnStatement(Statement stm) {
        var stmCln = stm.Clone();
	
		var retStm = "";

		if (stmCln.Tokens[0].Value == "return") {
			if (stmCln.Tokens.LastOrDefault()?.Value != "return") {
				retStm += "return ";

				for (var i = 1; i < stmCln.Tokens.Count; i++) {
                    if (stmCln.Tokens[i].Value == "{" &&
						stmCln.Tokens[i + 1].Value == "}" &&
						stmCln.Tokens[i + 2].Value == "0") {

                        stmCln.Tokens[i].Value = "{[0]=0}";
						stmCln.Tokens[i + 1].Value = "";
						stmCln.Tokens[i + 2].Value = "";
					}
						
					retStm += stmCln.Tokens[i].Value;
				}
			}

			else retStm += "return";
		}

		return retStm;
	}

	static string EndStatement(Statement stm, Statement nxStm, ref int curCont, Stack<bool> blocks, Stack<bool> cowraps, ref Stack<Statement> cases) {
		var retStm = "";
		var el = false;

		if (nxStm.Tokens[0].Value == "else" || nxStm.Tokens[0].Value == "elseif" || nxStm.Tokens[0].Value == "case" || nxStm.Tokens[0].Value == "default") {
			el = true;
		}

		if (stm.Tokens[0].Type == Lexer.TokenType.RCurly &&
			stm.Tokens.Count == 1) {

			if (blocks.Count > 0) {
				if (blocks.Peek()) {
					retStm = el ? "" : "::cont_" + String.Format("{0:D4}", curCont) + "::end";
					curCont++;
				}
				else {
                    retStm = el ? "" : "end";
				}
			}
			if (cowraps.Count > 0) {
				if (cowraps.Peek()) {
					retStm += ")()";
				}
			}
		}

		return retStm;
	}

	public static string[] Analyze(Lexer.Token[] tokens) {
		bool IsLoop(string val) =>
			val == "while" ||
			val == "for" ||
			val == "foreach" ||
			val == "iforeach";

		bool IsCowrap(string val) =>
			val == "cowrap";

		bool IsSwitch(string val) =>
			val == "switch";

		var curCont = 0;
		var blocks = new Stack<bool>();
		var cowraps = new Stack<bool>();

		var cases = new Stack<Statement>();
		var switchStms = new Stack<Statement>();

		var switches = new Stack<bool>();

		var allBlocks = new Stack<Statement>();

		var statements = new List<Statement>();
		var luaStatements = new List<string>();

		var curStm = new Statement(new List<Lexer.Token>());

		foreach (var tkn in tokens) {
			if (tkn.Type == Lexer.TokenType.EOL) {
				if (curStm.Tokens.Count > 0) {
					statements.Add(curStm);

					curStm = new Statement(new List<Lexer.Token>());
				}
			}
			else {
				curStm.Tokens.Add(tkn);
			}
		}

		var i = 0;

		string dir = "";

		string dirBuff;

		string luaCom;

		string luaDir;
		string luaInc;
		string luaAcq;

		string luaUnr;
		string luaVar;
		string luaMod;

		string luaMeta;

		string luaFuncDef;
		string luaFuncCall;

		string luaScope;
		string luaCowrap;

		string luaSwitch;
		string luaCase;
		string luaDefault;

		string luaIf;
		string luaElse;
		string luaElseIf;

		string luaWhile;
		string luaFor;
		string luaCitFor;
		string luaForeach;

		string luaBreak;
		string luaReturn;
		string luaContinue;

		string luaEnd;

		var stmExclaMarkPrefix = "_nua_program.";
		var stmRegPrefix = "local ";

		foreach (var stm in statements) {
			luaCom = CommentStatement(stm);

			if (dir == "") {
				(luaDir, dirBuff) = DirectoryStatement(stm);
				dir = dirBuff;
			}
			else {
				(luaDir, dirBuff) = DirectoryStatement(stm);
			}

			luaInc = IncludeStatement(stm, dir);

			if (stm.Tokens[0].Value == "!") {
				luaAcq = AcquireStatement(stm, 1, stmExclaMarkPrefix);

				luaUnr = UnaryStatement(stm, 1, stmExclaMarkPrefix);
				luaVar = VariableStatement(stm, 1, stmExclaMarkPrefix);
				luaMod = ModifyStatement(stm, 1, stmExclaMarkPrefix);

				luaMeta = MetaStatement(stm, 1, stmExclaMarkPrefix, stmExclaMarkPrefix);

				luaFuncDef = FunctionDefStatement(stm, 1, stmExclaMarkPrefix);
				luaFuncCall = FunctionCallStatement(stm, 1, stmExclaMarkPrefix);
			}
			else {
				luaAcq = AcquireStatement(stm, 0, stmRegPrefix);

				luaUnr = UnaryStatement(stm, 0, "");
				luaVar = VariableStatement(stm, 0, stmRegPrefix);
				luaMod = ModifyStatement(stm, 0, "");

				luaMeta = MetaStatement(stm, 0, stmRegPrefix, "");

				luaFuncDef = FunctionDefStatement(stm, 0, "");
				luaFuncCall = FunctionCallStatement(stm, 0, "");
			}

			luaScope = ScopeStatement(stm);
			luaCowrap = CowrapStatement(stm);

			luaIf = IfStatement(stm);
			luaElse = ElseStatement(stm);
			luaElseIf = ElseIfStatement(stm);
			luaWhile = WhileStatement(stm);

			luaFor = ForStatement(stm);
			luaCitFor = CitforStatement(stm);
			luaForeach = ForeachStatement(stm);

			luaBreak = BreakStatement(stm);
			luaReturn = ReturnStatement(stm);
			luaContinue = ContinueStatement(stm, curCont);

			void allPush() {
				cowraps.Push(IsCowrap(stm.Tokens[0].Value));
                blocks.Push(IsLoop(stm.Tokens[0].Value));
				allBlocks.Push(stm);
				switches.Push(IsSwitch(stm.Tokens[0].Value));
			}

			if (i + 1 < statements.Count) {
				luaSwitch = SwitchStatement(stm, statements[i + 1]);
				luaCase = CaseStatement(stm, switchStms, ref cases);

				luaEnd = EndStatement(stm, statements[i + 1], ref curCont, blocks, cowraps, ref cases);
			}
			else {
				luaSwitch = SwitchStatement(stm, stm);
				luaCase = CaseStatement(stm, switchStms, ref cases);

				luaEnd = EndStatement(stm, stm, ref curCont, blocks, cowraps, ref cases);
			}

			luaDefault = DefaultStatement(stm, ref cases);

			if (luaCom != "")
				luaStatements.Add(luaCom);

			if (luaDir != "")
				luaStatements.Add(luaDir);
			if (luaInc != "")
				luaStatements.Add(luaInc);
			if (luaAcq != "")
				luaStatements.Add(luaAcq);

			if (luaUnr != "")
				luaStatements.Add(luaUnr);
			if (luaVar != "")
				luaStatements.Add(luaVar);
			if (luaMod != "")
				luaStatements.Add(luaMod);

			if (luaMeta != "")
				luaStatements.Add(luaMeta);

			if (luaFuncDef != "") {
				luaStatements.Add(luaFuncDef);
				allPush();
			}
			if (luaFuncCall != "")
				luaStatements.Add(luaFuncCall);

			if (luaScope != "") {
				luaStatements.Add(luaScope);
				allPush();
			}
			if (luaCowrap != "") {
				luaStatements.Add(luaCowrap);
				allPush();
			}
			if (luaIf != "") {
				luaStatements.Add(luaIf);
				allPush();
			}
			if (luaElse != "") {
				luaStatements.Add(luaElse);
			}
			if (luaElseIf != "") {
				luaStatements.Add(luaElseIf);
			}

			if (luaWhile != "") {
				luaStatements.Add(luaWhile);
				allPush();
			}
			if (luaFor != "") {
				luaStatements.Add(luaFor);
				allPush();
			}
			if (luaCitFor != "") {
				luaStatements.Add(luaCitFor);
				allPush();
			}
			if (luaForeach != "") {
				luaStatements.Add(luaForeach);
				allPush();
			}

			if (luaBreak != "")
				luaStatements.Add(luaBreak);
			if (luaReturn != "")
				luaStatements.Add(luaReturn);
			if (luaContinue != "")
				luaStatements.Add(luaContinue);

			if (luaSwitch != "") {
				switchStms.Push(stm);
				luaStatements.Add(luaSwitch);
				allPush();
			}
			if (luaCase != "") {
				luaStatements.Add(luaCase);
				allPush();
			}
			if (luaDefault != "") {
				luaStatements.Add(luaDefault);
				allPush();
			}

			if (luaEnd != "") {
				luaStatements.Add(luaEnd);
				cowraps.Pop();
				blocks.Pop();
				switches.Pop();
				allBlocks.Pop();
			}

			i++;
		}

		return luaStatements.ToArray();
	}
}
