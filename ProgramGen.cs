using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ProgramModel
{


	public partial class ProgramBuilder<AssignmentT, ConditionT>
	{

		private class BasicBlock
		{
			public readonly IList<AssignmentT> assignments;
			//NOTE ON REPRESENTATION:
			//A null BasicBlock destination means
			//return, a null condition means
			//unconditional return
			public readonly ConditionT condition;
			public readonly BasicBlock trueDest;
			public readonly BasicBlock falseDest;

			public BasicBlock(IList<AssignmentT> assignments,
				ConditionT condition, BasicBlock trueDest, BasicBlock falseDest) {
				this.assignments = assignments.ToImmutableList();
				this.condition = condition;
				this.trueDest = trueDest;
				this.falseDest = falseDest;
			}

		}

		private class ProgramImpl
		{
			private BasicBlock initialBlock;
			private IList<BasicBlock> allBlocks;

			public ProgramImpl(BasicBlock initialBlock, IList<BasicBlock> allBlocks)
			{
				this.initialBlock = initialBlock;
				this.allBlocks = allBlocks.ToImmutableList();
			}

		}

		private ProgramImpl toProgram()
		{

		}

		static void Main()
		{
			System.Console.WriteLine ("Hello");
		}

	}

}
