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

		/*
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
		*/


		//while building our program graph by recursively descending
		//code blocks, we need to track the top level node, also
		//the exit points.  We create a type here that tracks
		//this state
		private class ProgramGraphBuilder
		{
			//initial and curr are either both null or neither null,
			//both null only on first loop iteration
			private ProgramNode initial = null;
			private BasicBlock bb = null;
			private Action<ProgramNode> setNextNode = null;

			public ProgramGraphBuilder() {}

			private void setNextProgramNode(ProgramNode programNode, Action<ProgramNode> setNextNode)
			{

				if (programNode is BasicBlock) {
					throw new ArgumentException ("Basic block node not allowed");
				}

				if (this.initial == null) {
					initial = programNode;
				} else if (this.setNextNode != null) {
					setNextNode (programNode);
				} else if (this.bb != null) {
					bb.coda = programNode;
				}

				//in any case, we won't have a basic block, set to null
				bb = null;
				//strategy for setting next node
				this.setNextNode = setNextNode;

			}

			//put in subroutine because there are two
			//cases in which this needs to happen
			private void handleIf(If iif) {
				ProgramSubgraph ps = processCodeBlock(iif.codeBlock);
				if (ps == null) {
					//if empty body, we just ignore the if
					return;
				}
				BranchBlock branchBlock = new BranchBlock (iif.condition);
				branchBlock.trueDest = ps.GetEntryPoint ();

				setNextProgramNode(branchBlock, delegate(ProgramNode programNode) {
					branchBlock.falseDest = programNode;
					ps.SetExitDest(programNode);
				});
			}

			public void buildFromCodeBlock(CodeBlock<MutationT, ConditionT> cb)
			{
				foreach (Either<MutationT, CodeBlockNonAssignment> el in cb.contents)
				{
					if (el.isLeft())
					{
						//we need to process an assignment
						MutationT mutation = el.Left;
						if (bb == null) {
							bb = new BasicBlock();
							if (initial == null) {
								initial = bb;
							}
							setNextNode = null;
						}
						bb.assignments.Add (mutation);
					} else {
						//something that's not an assignment
						CodeBlockNonAssignment elr = el.Right;
						if (elr is Return) {
							setNextProgramNode(PROGRAM_RETURN, delegate(ProgramNode obj) {
								//nothing can follow a return, do nothing
							});
							//once we hit a return all code afterwards
							//can be ignored, so stop processing
							return;
						} else if (elr is Continue) {
							//TODO: Handle this case, and return
						} else if (elr is Break) {
							//TODO: Handle this case, and return
						} else if (elr is IfElse) {
							IfElse ifElse = (IfElse) elr;
							ProgramSubgraph ps2 = processCodeBlock(ifElse.elseCodeBlock);
							if (ps2 == null) {
								//if second block is empty, then it's
								//basically an if
								handleIf (ifElse);
							} else {
								ProgramSubgraph ps1 = processCodeBlock(ifElse.codeBlock);
								BranchBlock branchBlock = new BranchBlock (ifElse.condition);
								if (ps1 == null) {
									//first code block is empty, this is where if-else
									//acts like "unless"
									branchBlock.falseDest = ps2.GetEntryPoint ();
									setNextProgramNode(branchBlock, delegate(ProgramNode programNode) {
										branchBlock.trueDest = programNode;
										ps2.SetExitDest(programNode);
									});
								} else {
									//neither code block is empty, this is a true if-else
									branchBlock.trueDest = ps1.GetEntryPoint();
									branchBlock.falseDest = ps2.GetEntryPoint();
									setNextProgramNode(branchBlock, delegate(ProgramNode programNode) {
										ps1.SetExitDest(programNode);
										ps2.SetExitDest(programNode);
									});
								}
							}
						} else if (elr is If) {
							If iif = (If) elr;
							handleIf (iif);
						} else if (elr is WhileDoWhileBase) {
							WhileDoWhileBase whileDoWhileBase = (WhileDoWhileBase)elr;
							ProgramSubgraph ps = processCodeBlock (whileDoWhileBase.codeBlock);
							BranchBlock branchBlock = new BranchBlock (whileDoWhileBase.condition);
							ProgramNode initialNode;
							if (ps == null) {
								//if body is empty, then branch destination
								//points to the same branch block
								branchBlock.trueDest = branchBlock;
								initialNode = branchBlock;
							} else {
								//otherwise it's a cycle, true condition
								//goes to block, block back to condition
								branchBlock.trueDest = ps.GetEntryPoint();
								ps.SetExitDest (branchBlock);
								//the only difference between while and
								//do-while is the entry, either condition
								//or block
								if (whileDoWhileBase is While) {
									initialNode = branchBlock;
								} else if (elr is DoWhile) {
									initialNode = ps.GetEntryPoint();
								} else {
									throw new ArgumentException ("Unknown while/do-while subtype");
								}
							}
							setNextProgramNode (initialNode, delegate(ProgramNode programNode) {
								branchBlock.falseDest = programNode;
							});
						} else {
							throw new InvalidOperationException ("Code block contains " + elr + ", but no handler for this type");
						}
					}
				}
			}
		}

		private static ProgramSubgraph processCodeBlock(CodeBlock<MutationT, ConditionT> cb)
		{

			ProgramGraphBuilder pgb = new ProgramGraphBuilder ();

			return null;
		}

		private ProgramImpl ToProgram()
		{
			return null;
		}

	}

}
