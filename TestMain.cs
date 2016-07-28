using System;

namespace ProgramModel
{
	public class TestMain
	{
		private TestMain () {}

		static void Main()
		{
			//System.Console.WriteLine ("Hello");

			CodeBlock<String, String> codeBlock = new CodeBlock<String, String> ();

			codeBlock.addMutation ("mutation1");
			codeBlock.addMutation ("mutation2");
			codeBlock.addMutation ("mutation3");

			CodeBlock<String, String> ifBody = codeBlock.addIf ("ifcond1");
			ifBody.addMutation ("mutation4");
			ifBody.addMutation ("mutation5");
			ifBody.addMutation ("mutation6");

			codeBlock.addMutation ("mutation7");
			codeBlock.addMutation ("mutation8");
			codeBlock.addMutation ("mutation9");

			CodeBlock<String, String> whileBody;

			//whileBody = codeBlock.addWhile("whilecond1");

			Tuple<CodeBlock<String, String>, Loop> whileRes = codeBlock.addWhile ("whilecond1", "mywhile");
			whileBody = whileRes.Item1;
			Loop mywhile = whileRes.Item2;

			whileBody.addMutation ("mutation10");
			whileBody.addMutation ("mutation11");

			CodeBlock<String, String> innerDoWhileBody = whileBody.addDoWhile("doWhileCond");
			innerDoWhileBody.addMutation ("mutation17");
			innerDoWhileBody.addMutation ("mutation18");
			innerDoWhileBody.addMutation ("mutation19");

			Tuple<CodeBlock<String, String>, CodeBlock<String, String>> ifElseResult = innerDoWhileBody.addIfElse ("ifelsecond");
			ifBody = ifElseResult.Item1;
			ifBody.addMutation ("mutation22");
			ifBody.addMutation ("mutation23");
			ifBody.addContinue ();
			ifBody.addMutation ("mutation24");

			CodeBlock<String, String> elseBody = ifElseResult.Item2;
			elseBody.addMutation ("mutation25");
			elseBody.addMutation ("mutation26");
			elseBody.addBreak (mywhile);
			elseBody.addMutation ("mutation27");

			innerDoWhileBody.addMutation ("mutation20");
			innerDoWhileBody.addMutation ("mutation21");

			whileBody.addMutation ("mutation12");
			whileBody.addMutation ("mutation13");

			codeBlock.addMutation ("mutation14");
			codeBlock.addMutation ("mutation15");
			codeBlock.addMutation ("mutation16");

			System.Console.WriteLine (codeBlock);

			codeBlock.ToProgram ();

		}

	}
}

