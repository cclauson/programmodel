﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ProgramModel
{

	public partial class ProgramBuilder<AssignmentT, ConditionT>
	{

		private interface ProgramNode {}

		private class ProgramReturn : ProgramNode {}

		private static readonly ProgramReturn PROGRAM_RETURN = new ProgramReturn();

		private class BasicBlock : ProgramNode
		{
			public readonly IList<AssignmentT> assignments;
			public ProgramNode coda;

			public BasicBlock()
			{
				this.assignments = new List<AssignmentT>();
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
				this.trueCoda = this.falseCoda = null;
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
			private readonly Either<BasicBlock, ProgramSubgraph> final;

			public BasicBlockProgramSubgraph(ProgramNode initial, Either<BasicBlock, ProgramSubgraph> final)
			{
				this.initial = initial;
				this.final = final;
			}

			public override ProgramNode GetEntryPoint()
			{
				return initial;
			}

			void SetExitDest(ProgramNode programNode)
			{
				if (final != null) {
					if (final.isLeft ()) {
						final.Left ().coda = programNode;
					} else {
						final.Right ().SetExitDest (programNode);
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

			public override ProgramNode GetEntryPoint()
			{
				return branchBlock;
			}

			void SetExitDest(ProgramNode programNode)
			{
				//for an if, the following block is gone to
				//if the condition is false, and unconditionally
				//after executing the contained code
				branchBlock.falseDest = programNode;
				programSubgraph.SetExitDest (programNode);
			}

		}

		private ProgramSubgraph processCodeBlock(CodeBlockImpl cb)
		{
			//initial and curr are either both null or neither null,
			//both null only on first loop iteration
			ProgramNode initial = null;
			Either<BasicBlock, ProgramSubgraph> curr = null;

			foreach (Either<AssignmentT, CodeBlockNonAssignment> el in cb.contents)
			{
				if (el.isLeft())
				{
					//we need to process an assignment
					AssignmentT assignment = el.Left ();
					BasicBlock bb;

					//if first loop iteration, initialize initial and curr
					//to non-null values
					if (initial == null) {
						bb = new BasicBlock ();
						initial = bb;
						curr = Either<BasicBlock, ProgramSubgraph>.left (bb);
					}

					if (curr.isLeft()) {
						bb = curr.Left ();
						curr = Either<BasicBlock, ProgramSubgraph>.left (bb);
					} else {
						bb = new BasicBlock ();
						curr.Right ().SetExitDest (bb);
						curr = Either<BasicBlock, ProgramSubgraph>.left (bb);
					}
					bb.assignments.Add (assignment);
				} else {
					CodeBlockNonAssignment elr = el.Right ();
					if (elr is Return) {
						if (initial == null) {
							initial = PROGRAM_RETURN;
						}
						return new BasicBlockProgramSubgraph (initial, null);
					} else if (elr is Continue) {

					} else if (elr is Break) {

					} else if (elr is If) {
						If iif = (If) elr;
						ProgramSubgraph ps = processCodeBlock(iif.codeBlock);
						if (ps == null) {
							//if empty body, we just ignore the if
							continue;
						}
						BranchBlock branchBlock = new BranchBlock (iif.condition);
						branchBlock.trueDest = ps.GetEntryPoint ();
						return new IfProgramSubgraph (branchBlock, ps);
					} else if (elr is IfElse) {
						//TO FIX: Never gets here, actually, since if-else is a
						//subtype of if!!!
						IfElse ifElse = (IfElse) elr;
						ProgramSubgraph ps = processCodeBlock(iif.codeBlock);
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

		}

		static void Main()
		{
			System.Console.WriteLine ("Hello");
		}

	}

}