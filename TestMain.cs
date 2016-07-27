using System;

namespace ProgramModel
{
	public class TestMain
	{
		private TestMain () {}

		static void Main()
		{
			System.Console.WriteLine ("Hello");

			CodeBlock<String, String> codeBlock = new CodeBlock<String, String> ();
			System.Console.WriteLine (codeBlock);

		}

	}
}

