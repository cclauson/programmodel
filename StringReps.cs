using System;
using System.Text;
using System.Diagnostics;

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

			public override string ToString()
			{
				return "Program as string";
			}

		}


	}
}