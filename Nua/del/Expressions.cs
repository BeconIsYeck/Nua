//using System;
//using System.Linq.Expressions;
//using System.Threading.Channels;


//namespace Nua.Transpiler;

//public static class Expressions {

//	public abstract class Expression {
//		public static string FindExpression(Lexer.Token[] tokens) {
//			foreach (var token in tokens) {
//				//Console.WriteLine($"{token.Type} {token.Value}");
//			}

//			var expr = ParseExpression(tokens);

//			return expr.GetType().Name;
//		}
//	}

//	public static Expression ParseExpression(Lexer.Token[] tokens) {
//		return ParseBinaryExpression(tokens, 0).expression;
//	}

//	static (Expression expression, int nextPos) ParseBinaryExpression(Lexer.Token[] tokens, int pos) {
//		var (left, nextPos) = ParseLiteral(tokens, pos);

//		if (nextPos < tokens.Length) {
//			var op = tokens[nextPos].Value;

//			var (right, finalPos) = ParseBinaryExpression(tokens, nextPos + 1);

//			return (new BinaryExpression(left, op, right), finalPos);
//		}
//		else {
//			return (left, nextPos);
//		}
//	}

//	private static (Expression expression, int nextPos) ParseLiteral(Lexer.Token[] tokens, int pos) {
//		return (new LiteralExpression(tokens[pos].Value), pos + 1);
//	}

//	public class NoneExpression : Expression { }

//	// Binary Expression also include Logical, Arithmetic, & Comparison Expressions.
//	public class BinaryExpression : Expression {
//		public Expression Left { get; }
//		public string Operator { get; }
//		public Expression Right { get; }

//		public BinaryExpression(Expression left, string op, Expression right) {
//			this.Left = left;
//			this.Operator = op;
//			this.Right = right;
//		}
//	}

//	public class UnaryExpression : Expression {
//		public Expression Left { get; }
//		public string Operator { get; }

//		public UnaryExpression(Expression left, string op) {
//			this.Left = left;
//			this.Operator = op;
//		}
//	}

//	// Includes =, +=, -=, *=, /=, %=, ^=, and ..=
//	public class AssignmentExpression : Expression {
//		public string Variable { get; }
//		public string Operator { get; }
//		public Expression Value { get; }

//		public AssignmentExpression(string variable, string op, Expression val) {
//			this.Variable = variable;
//			this.Operator = op;
//			this.Value = val;
//		}
//	}

//	public class FunctionCallExpression : Expression {
//		public string Function { get; }
//		public Expression[] Arguments { get; }

//		public FunctionCallExpression(string function, Expression[] arguments) {
//			this.Function = function;
//			this.Arguments = arguments;
//		}
//	}

//	public class ArrayAccessExpression : Expression {
//		public Expression Array { get; }
//		public Expression Index { get; }

//		public ArrayAccessExpression(Expression array, Expression index) {
//			this.Array = array;
//			this.Index = index;
//		}
//	}

//	public class MemberAcccessExpression {
//		public Expression Base { get; }
//		public string Member { get; }

//		public MemberAcccessExpression(Expression @base, string member) {
//			Base = @base;
//			Member = member;
//		}
//	}

//	public class LiteralExpression : Expression {
//		public string Value { get; }

//		public LiteralExpression(string value) {
//			this.Value = value;
//		}
//	}
//}