using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ProgramModel
{

	public partial class CodeBlock<MutationT, ConditionT>
	{

		private interface ProgramNode {}

		private class ProgramReturn : ProgramNode {}

		private static readonly ProgramReturn PROGRAM_RETURN = new ProgramReturn();

		private class BasicBlock : ProgramNode
		{
			public readonly IList<MutationT> assignments;
			public ProgramNode coda;

			public BasicBlock()
			{
				this.assignments = new List<MutationT>();
				this.coda = null;
			}

		}

		private class BranchBlock : ProgramNode
		{
			public readonly ConditionT condition;
			public ProgramNode trueDest;
			public ProgramNode falseDest;

			public BranchBlock(ConditionT condition)
			{
				this.condition = condition;
				this.trueDest = this.falseDest = null;
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

		private interface ProgramSubgraph
		{
			ProgramNode GetEntryPoint();
			void SetExitDest(ProgramNode programNode);
		}

		private class BasicBlockProgramSubgraph : ProgramSubgraph
		{

			private readonly ProgramNode initial;
			private readonly Nullable<Either<BasicBlock, ProgramSubgraph>> final;

			public BasicBlockProgramSubgraph(ProgramNode initial, Either<BasicBlock, ProgramSubgraph> final)
			{
				this.initial = initial;
				this.final = final;
			}

			public ProgramNode GetEntryPoint()
			{
				return initial;
			}

			public void SetExitDest(ProgramNode programNode)
			{
				if (final != null) {
					Either<BasicBlock, ProgramSubgraph> f = final.Value;
					if (f.isLeft ()) {
						f.Left.coda = programNode;
					} else {
						f.Right.SetExitDest (programNode);
					}
				}
			}
		}

		private class IfProgramSubgraph : ProgramSubgraph
		{

			private readonly BranchBlock branchBlock;
			private readonly ProgramSubgraph programSubgraph;

			public IfProgramSubgraph(BranchBlock branchBlock, ProgramSubgraph programSubgraph)
			{
				this.branchBlock = branchBlock;
				this.programSubgraph = programSubgraph;
			}

			public ProgramNode GetEntryPoint()
			{
				return branchBlock;
			}

			public void SetExitDest(ProgramNode programNode)
			{
				//for an if, the following block is gone to
				//if the condition is false, and unconditionally
				//after executing the contained code
				branchBlock.falseDest = programNode;
				programSubgraph.SetExitDest (programNode);
			}

		}

		private class UnlessProgramSubgraph : ProgramSubgraph
		{

			private readonly BranchBlock branchBlock;
			private readonly ProgramSubgraph programSubgraph;

			public UnlessProgramSubgraph(BranchBlock branchBlock, ProgramSubgraph programSubgraph)
			{
				this.branchBlock = branchBlock;
				this.programSubgraph = programSubgraph;
			}

			public ProgramNode GetEntryPoint()
			{
				return branchBlock;
			}

			public void SetExitDest(ProgramNode programNode)
			{
				//like if, but block executed on negative
				branchBlock.trueDest = programNode;
				programSubgraph.SetExitDest (programNode);
			}

		}


		private ProgramSubgraph processCodeBlock(CodeBlock<MutationT, ConditionT> cb)
		{
			//initial and curr are either both null or neither null,
			//both null only on first loop iteration
			ProgramNode initial = null;
			Either<BasicBlock, ProgramSubgraph> curr = null;

			foreach (Either<MutationT, CodeBlockNonAssignment> el in cb.contents)
			{
				if (el.isLeft())
				{
					//we need to process an assignment
					MutationT assignment = el.Left ();
					BasicBlock bb;

					//if first loop iteration, initialize initial and curr
					//to non-null values
					if (initial == null) {
						bb = new BasicBlock ();
						initial = bb;
						curr = Either<BasicBlock, ProgramSubgraph>.left (bb);
					}

					if (curr.isLeft()) {
						bb = curr.Left;
						curr = Either<BasicBlock, ProgramSubgraph>.left (bb);
					} else {
						bb = new BasicBlock ();
						curr.Right.SetExitDest (bb);
						curr = Either<BasicBlock, ProgramSubgraph>.left (bb);
					}
					bb.assignments.Add (assignment);
				} else {
					CodeBlockNonAssignment elr = el.Right;
					if (elr is Return) {
						if (initial == null) {
							initial = PROGRAM_RETURN;
						}
						return new BasicBlockProgramSubgraph (initial, null);
					} else if (elr is Continue) {

					} else if (elr is Break) {

					} else if (elr is IfElse) {
						IfElse ifElse = (IfElse) elr;
						ProgramSubgraph ps1 = processCodeBlock(ifElse.codeBlock);
						ProgramSubgraph ps2 = processCodeBlock(ifElse.elseCodeBlock);
						if (ps1 == null && ps2 == null) {
							//ignore if both bodies are empty
							continue;
						} else if (ps1 == null) {
							//if first body is empty then we model it as "unless"
							BranchBlock branchBlock = new BranchBlock (ifElse.condition);
							branchBlock.falseDest = ps2.GetEntryPoint ();
							//TODO: Don't return here
							return new UnlessProgramSubgraph (branchBlock, ps1);
						} else if (ps2 == null) {
							//if second body is empty then it's effectively an if block
							BranchBlock branchBlock = new BranchBlock (ifElse.condition);
							branchBlock.trueDest = ps1.GetEntryPoint ();
							//TODO: Don't return here
							return new IfProgramSubgraph (branchBlock, ps1);
						} else {

						}
					} else if (elr is If) {
						If iif = (If) elr;
						ProgramSubgraph ps = processCodeBlock(iif.codeBlock);
						if (ps == null) {
							//if empty body, we just ignore the if
							continue;
						}
						BranchBlock branchBlock = new BranchBlock (iif.condition);
						branchBlock.trueDest = ps.GetEntryPoint ();
						//TODO: Don't return here
						return new IfProgramSubgraph (branchBlock, ps);
					} else if (elr is While) {

					} else if (elr is DoWhile) {

					} else {
						throw new InvalidOperationException ("Code block contains " + elr + ", but no handler for this type");
					}
				}
			}
		}

		private ProgramImpl ToProgram()
		{
			return null;
		}

	}

}
