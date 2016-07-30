using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

namespace ProgramModel
{

	public partial class CodeBlock<MutationT, ConditionT>
	{

		private static void IndentToLevel(StringBuilder sb, int indentLevel)
		{
			for (int i = 0; i < indentLevel; ++i) {
				sb.Append ("  ");
			}
		}

		//does a newline after the closing brace
		private string ToStringWithIndent(int indentLevel)
		{
			StringBuilder sb = new StringBuilder ();

			IndentToLevel (sb, indentLevel);
			sb.Append ("{\n");

			foreach (Either<MutationT, CodeBlockNonAssignment> el in this.contents) {
				IndentToLevel (sb, indentLevel + 1);
				if (el.isLeft ()) {
					//we need to process an assignment
					MutationT mutation = el.Left;
					sb.Append (mutation.ToString () + ";\n");
				} else { //isRight
					//something that's not an assignment
					CodeBlockNonAssignment elr = el.Right;
					if (elr is Return) {
						sb.Append ("return;\n");
					} else if (elr is ContinueBreakBase) {
						ContinueBreakBase continueOrBreak = (ContinueBreakBase)elr;
						Loop loop = continueOrBreak.loop;
						if (!(loop is WhileDoWhileBase)) {
							throw new ArgumentException("Found loop that is not While/Do-While, but this should never happen");
						}
						WhileDoWhileBase whileDoWhile = (WhileDoWhileBase) loop;
						string keywordStr;
						if (continueOrBreak is Continue) {
							keywordStr = "continue";
						} else if (continueOrBreak is Break) {
							keywordStr = "break";
						} else {
							throw new ArgumentException("Found bad subtype of ContinueBreakBase");
						}
						if (whileDoWhile.label == null) {
							sb.Append (keywordStr + ";\n");
						} else {
							sb.Append (keywordStr + " " + whileDoWhile.label + ";\n");
						}
					} else if (elr is IfElse) {
						IfElse ifElse = (IfElse) elr;
						sb.Append ("if (" + ifElse.condition + ")\n");
						sb.Append(ifElse.codeBlock.ToStringWithIndent(indentLevel + 1));
						IndentToLevel (sb, indentLevel + 1);
						sb.Append ("else\n");
						sb.Append(ifElse.elseCodeBlock.ToStringWithIndent(indentLevel + 1));
					} else if (elr is If) {
						If iif = (If) elr;
						sb.Append ("if (" + iif.condition + ")\n");
						sb.Append(iif.codeBlock.ToStringWithIndent(indentLevel + 1));
					} else if (elr is WhileDoWhileBase) {
						WhileDoWhileBase whileOrDoWhile = (WhileDoWhileBase) elr;
						if (whileOrDoWhile.label != null) {
							sb.Append (whileOrDoWhile.label + ":\n");
							IndentToLevel (sb, indentLevel + 1);
						}
						String labelString = (whileOrDoWhile.label == null) ? "" : whileOrDoWhile.label + ":\n";
						if (elr is While) {
							sb.Append ("while (" + whileOrDoWhile.condition + ")\n");
							sb.Append (whileOrDoWhile.codeBlock.ToStringWithIndent (indentLevel + 1));
						} else if (elr is DoWhile) {
							sb.Append ("do\n");
							sb.Append (whileOrDoWhile.codeBlock.ToStringWithIndent (indentLevel + 1));
							IndentToLevel (sb, indentLevel + 1);
							sb.Append ("while (" + whileOrDoWhile.condition + ");\n");
						} else {
							Debug.Fail("WhileDoWhileBase object of unknown type: " + elr);
						}
					} else {
						throw new InvalidOperationException ("Code block contains " + elr + ", but no handler for this type");
					}

				}
			}

			IndentToLevel (sb, indentLevel);
			sb.Append ("}\n");

			return sb.ToString ();

		}

		public override string ToString()
		{
			return ToStringWithIndent(0);
		}



		private partial class ProgramImpl : Program<MutationT, ConditionT>
		{

			//type for keeping track of symbolic references
			//for program nodes.
			//Each program node needs a "name" that can be
			//used to refer to it, here we name nodes and
			//keep track of the names we've assigned
			private class ProgramNodeReferencer
			{
				private readonly Dictionary<ProgramNode, int> dict = new Dictionary<ProgramNode, int> ();
				private int currNum = 0;

				public String nameForProgramNode(ProgramNode programNode)
				{
					if (programNode == null) {
						throw new ArgumentNullException("programNode is null");
					}
					if (programNode is ProgramReturn) {
						return "RETURN";
					}
					if (!dict.ContainsKey(programNode)) {
						dict[programNode] = currNum;
						++currNum;
					}
					return dict [programNode].ToString();
				}
			}

			//print a representation of the program node to the
			//string builder, and return a list of the child nodes
			private IList<ProgramNode> printProgramNode(
				ProgramNode programNode, StringBuilder sb, ProgramNodeReferencer pnr)
			{
				Debug.Assert (!(programNode is ProgramReturn),
					"we should never be trying to print a ProgramReturn node");

				String nodeName = pnr.nameForProgramNode (programNode);
				IList<ProgramNode> ret = new List<ProgramNode> ();
				if (programNode is BasicBlock) {
					BasicBlock basicBlock = (BasicBlock) programNode;
					sb.Append("===== " + nodeName + ": BASIC BLOCK ======\n");
					foreach(MutationT mutation in basicBlock.assignments) {
						sb.Append ("  " + mutation + "\n");
					}
					sb.Append(" GOTO: " + pnr.nameForProgramNode(basicBlock.coda) + "\n");
					ret.Add (basicBlock.coda);
				} else if (programNode is BranchBlock) {
					BranchBlock branchBlock = (BranchBlock) programNode;
					sb.Append("===== " + nodeName + ": BRANCH BLOCK =====\n");
					sb.Append (" CONDITION: " + branchBlock.condition + "\n");
					sb.Append (" TRUE  DEST: " + pnr.nameForProgramNode (branchBlock.trueDest) + "\n");
					sb.Append (" FALSE DEST: " + pnr.nameForProgramNode (branchBlock.falseDest) + "\n");
					ret.Add (branchBlock.trueDest);
					ret.Add (branchBlock.falseDest);
				} else {
					throw new Exception ("Unknown subtype of ProgramNode: " + programNode);
				}
				return ret;
			}

			public override string ToString()
			{
				ProgramNodeReferencer pnr = new ProgramNodeReferencer ();
				//this doesn't have to be a stack, it could be a queue, or
				//actually almost any collection, it would just cause the graph
				//to be traversed in a different order
				Stack<ProgramNode> stack = new Stack<ProgramNode> ();
				HashSet<ProgramNode> queuedToPrint = new HashSet<ProgramNode> ();

				if (this.initialBlock is ProgramReturn) {
					return "(EMPTY PROGRAM GRAPH)";
				}

				StringBuilder sb = new StringBuilder ();

				stack.Push (this.initialBlock);
				queuedToPrint.Add (this.initialBlock);

				while (stack.Count != 0) {
					ProgramNode programNode = stack.Pop ();
					IList<ProgramNode> children = printProgramNode (programNode, sb, pnr);
					foreach (ProgramNode child in children) {
						if (child == null)
							continue;
						if (child is ProgramReturn)
							continue;
						if (queuedToPrint.Contains(child))
							continue;
						queuedToPrint.Add (child);
						stack.Push (child);
					}
				}

				return sb.ToString ();

			}

		}


	}
}
