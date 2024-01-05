//using System;
//using System.Collections.Generic;
//using System.Linq;


//namespace Nua.Transpiler; 

//public class Tree(Node root) {
//	public Node Root { get; set; } = root;

//	public static void Print(Node node, string indent = "") {
//		Console.WriteLine(indent + $"({node.Token.Type}, \"{node.Token.Value}\")");
//		foreach (var child in node.Children) {
//			Print(child, indent + "    ");
//		}
//	}
//}

//public class Node(Lexer.Token token) {
//	public Lexer.Token Token { get; set; } = token;

//	public List<Node> Children { get; set; } = new List<Node>();

//	public void AddChild(Node child) {
//		this.Children.Add(child);
//	}

//}
