using System;
using System.Runtime.InteropServices;
using NLua;


namespace Nua.Transpiler;

public static class Lexer {
	public enum TokenType {
		None,
		Keyword,
		ArithmeticOperator,
		LogicalOperator,
		AssignmentOperator,
		ComparisonOperator,
		LParen,
		RParen,
		LCurly,
		RCurly,
		LBrack,
		RBrack,
		Delimiter,
		Identifier,
		StringLiteral,
		NumberLiteral,
		BooleanLiteral,
		NullLiteral,
		EOL,
		EOF,
		SOP
	}

	static readonly string[] Whitespace = [" ", "\t", "\r"];
	static readonly string[] Delimiters = ["(", ")", /* "[", "]",*/  /*"\\",*/ ",", /*".",*/ "{", "}", ";", "@", "`"];
	static readonly string[] Operators = ["+", "-", "*", "/", "%", "^", "!", ">", "<", "=", "~"];

	static bool IsWhitespace(char s) => Whitespace.Contains(s.ToString());

	static bool IsSeperator(string s) =>
		Delimiters.Contains(s) || Operators.Contains(s);

	public class Token {
		public TokenType Type { get; set; } = TokenType.None;
		public string Value { get; set; } = "";

		public Token(TokenType type, string value) {
			this.Type = type;
			this.Value = value;
		}
	}

	public static bool IsLiteral(TokenType t) =>
		t == TokenType.NullLiteral || t == TokenType.StringLiteral || t == TokenType.NumberLiteral || t == TokenType.BooleanLiteral;

	public static TokenType FindTokenType(string str) {
		if (str == null)
			return TokenType.None;

		switch (str) {
			case "\n":
				return TokenType.EOL;
			case "\\EOL\\" or ";":
				return TokenType.EOL;
			case var s when s.StartsWith('\"') && s.EndsWith('\"'):
				return TokenType.StringLiteral;
			case var s when s.StartsWith('\'') && s.EndsWith('\''):
				return TokenType.StringLiteral;
			case var s when s.All(char.IsDigit):
				return TokenType.NumberLiteral;
			case "(":
				return TokenType.LParen;
			case ")":
				return TokenType.RParen;
			case "[":
				return TokenType.LBrack;
			case "]":
				return TokenType.RBrack;
			case "{":
				return TokenType.LCurly;
			case "}":
				return TokenType.RCurly;
			case "." or "," or ":" or "[" or "]" or "{" or "}" or "\\" or "@":
				return TokenType.Delimiter;
			case "+" or "-" or "*" or "/" or "%" or "^":
				return TokenType.ArithmeticOperator;
			case "!" or "&&" or "||":
				return TokenType.LogicalOperator;
			case "=" or "+=" or "-=" or "*=" or "/=" or "%=" or "^=" or "..=":
				return TokenType.AssignmentOperator;
			case ">" or "<" or "==" or "!=" or ">=" or "<=" or "~=":
				return TokenType.ComparisonOperator;
			case "true":
				return TokenType.BooleanLiteral;
			case "false":
				return TokenType.BooleanLiteral;
			case "null":
				return TokenType.NullLiteral;
			case "loc" or "meta" or "switch" or "case" or "default" or "if" or "else" or "elseif" or "for" or "citfor" or "foreach" or "iforeach" or "while" or "return" or "break" or "continue" or "in" or "acq" or "inc" or "dir" or "scope":
				return TokenType.Keyword;
			default:
				return TokenType.Identifier;
		}
	}

	public static Token[] Tokenize(string src) {
		var curTokStr = "";

		var tokStrs = new List<string>();
		var tokens = new List<Token>();

		tokens.Add(new Token(TokenType.SOP, ""));

		void addRefresh() {
			if (curTokStr != null)
				tokStrs?.Add(curTokStr);

			curTokStr = "";
		}

		for (var i = 0; i < src.Length; i++) {
			var c = src.ToCharArray()[i];

			void add() {
				curTokStr += c;
			}

			if (c == '\n') {
				tokStrs.Add("\\EOL\\");
				curTokStr = "";

				continue;
			}

			if (!IsWhitespace(c)) {
				if (IsSeperator(c.ToString())) {
					if (curTokStr.Length > 0) {
						addRefresh();

						add();
					}
					else {
						if (src.ToCharArray()[i + 1] == '=') {
							add();

							c = src.ToCharArray()[i + 1];

							add();
							addRefresh();

							i += 1;
						}
						else {
							add();
							addRefresh();
						}
					}
				}
				else {
					if (IsSeperator(curTokStr)) {
						addRefresh();

						add();
					}
					else {
						add();
					}
				}
			}
			else {
				if (curTokStr.Length > 0) {
					if (curTokStr.ToCharArray()[0] == '"') {
						add();
						addRefresh();
					}
					else {

						addRefresh();
					}

					if (curTokStr.Length > 0) {
						if (curTokStr.ToCharArray()[^1] == '"') {
							addRefresh();
						}
					}
				}
			}
		}

		var ji = 0;

        foreach (var t in tokStrs) {
            var token = new Token(FindTokenType(t), t);

			tokens.Add(token);

			ji++;
		}

		tokens.Add(new Token(TokenType.EOF, ""));

		return tokens.ToArray();
	}
}
