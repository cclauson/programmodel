using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

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

		private partial class ProgramImpl : Program<MutationT, ConditionT>
		{
			private ProgramNode initialBlock;

			public ProgramImpl(ProgramNode initialBlock)
			{
				this.initialBlock = initialBlock;
			}

		}

		//this could easily be an interface but I
		//only ended up subtyping this once
		private class ProgramSubgraph
		{
			private readonly ProgramNode entryPoint;
			private readonly Action<ProgramNode> exitDestSetter;

			public ProgramSubgraph(ProgramNode programNode, Action<ProgramNode> exitDestSetter)
			{
				if(programNode == null) throw new ArgumentNullException("programNode is null");
				if(exitDestSetter == null) throw new ArgumentNullException("exitDestSetter is null");
				this.entryPoint = programNode;
				this.exitDestSetter = exitDestSetter;
			}

			public ProgramNode GetEntryPoint()
			{
				return entryPoint;
			}

			public void SetExitDest(ProgramNode programNode)
			{
				this.exitDestSetter (programNode);
			}
		}

		//for information associated with a loop
		//specifically, we need a reference to
		//the branching node associated with the
		//loop (for continue), and we also include
		//a mutable set that can be used to set
		//a destination node for anything breaking
		//from the loop (for break)
		private class LoopRecord
		{
			public readonly BranchBlock branchBlock;
			public readonly HashSet<Action<ProgramNode>> breakDestinationSetters;

			public LoopRecord(BranchBlock branchBlock) {
				this.branchBlock = branchBlock;
				this.breakDestinationSetters = new HashSet<Action<ProgramNode>>();
			}
		}

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

				//commented because in the case of do-while, we
				//need to jump directly to the body
				//if (programNode is BasicBlock) {
				//	throw new ArgumentException ("Basic block node not allowed");
				//}

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
			private void handleIf(If iif, Dictionary<Loop, LoopRecord> loopLabelMap) {
				ProgramSubgraph ps = processCodeBlock(iif.codeBlock, loopLabelMap);
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

			public void buildFromCodeBlock(CodeBlock<MutationT, ConditionT> cb, Dictionary<Loop, LoopRecord> loopLabelMap)
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
						} else if (elr is ContinueBreakBase) {
							ContinueBreakBase continueOrBreak = (ContinueBreakBase)elr;
							if (loopLabelMap.ContainsKey (continueOrBreak.loop)) {
								LoopRecord loopRecord = loopLabelMap[continueOrBreak.loop];
								if (elr is Continue) {
									//in continue case we just go directly to the
									//branch node
									setNextProgramNode(loopRecord.branchBlock, delegate(ProgramNode obj) {
										//nothing can follow a continue, do nothing
									});
								} else if (elr is Break) {
									loopRecord.breakDestinationSetters.Add (delegate(ProgramNode programNode) {
										//this is what is called once we have a node to break to
										setNextProgramNode(programNode, delegate(ProgramNode programNode2) {
											//nothing can follow a break, do nothing
										});
									});
								} else {
									throw new ArgumentException ("Unknown continue/break subtype");
								}
								//if continue or break occurs in a basic block,
								//we just ignore the rest of the contents;
								//they're unreachable
								return;
							} else {
								String continueOrBreakStr;
								if (elr is Continue) {
									continueOrBreakStr = "continue";
								} else if (elr is Break) {
									continueOrBreakStr = "break";
								} else {
									throw new ArgumentException ("Unknown continue/break subtype");
								}
								throw new ArgumentException ("Invalid loop target found for " + continueOrBreakStr);
							}
						} else if (elr is IfElse) {
							IfElse ifElse = (IfElse) elr;
							ProgramSubgraph ps2 = processCodeBlock(ifElse.elseCodeBlock, loopLabelMap);
							if (ps2 == null) {
								//if second block is empty, then it's
								//basically an if
								handleIf (ifElse, loopLabelMap);
							} else {
								ProgramSubgraph ps1 = processCodeBlock(ifElse.codeBlock, loopLabelMap);
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
							handleIf (iif, loopLabelMap);
						} else if (elr is WhileDoWhileBase) {
							WhileDoWhileBase whileOrDoWhile = (WhileDoWhileBase)elr;
							BranchBlock branchBlock = new BranchBlock (whileOrDoWhile.condition);
							LoopRecord loopRecord = new LoopRecord (branchBlock);
							//Put loop record in loopLabelMap, so it's visible when processing subblock
							loopLabelMap.Add(whileOrDoWhile, loopRecord);
							ProgramSubgraph ps = processCodeBlock (whileOrDoWhile.codeBlock, loopLabelMap);
							//Now remove so it's no longer visible
							loopLabelMap.Remove(whileOrDoWhile);
							//loopRecord now has setters that need to be invoked when the exit
							//node is set for this loop
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
								if (whileOrDoWhile is While) {
									initialNode = branchBlock;
								} else if (elr is DoWhile) {
									initialNode = ps.GetEntryPoint();
								} else {
									throw new ArgumentException ("Unknown while/do-while subtype");
								}
							}
							setNextProgramNode (initialNode, delegate(ProgramNode programNode) {
								branchBlock.falseDest = programNode;
								foreach (Action<ProgramNode> exitSetter in loopRecord.breakDestinationSetters) {
									exitSetter.Invoke(programNode);
								}
							});
						} else {
							throw new InvalidOperationException ("Code block contains " + elr + ", but no handler for this type");
						}
					}
				}
			}

			//returns null if the block passed was empty
			//otherwise, returns a ProgramSubgraph that
			//can be used to get the entry point for the
			//subgraph, and can set the 
			public ProgramSubgraph AsProgramSubgraph()
			{
				Action<ProgramNode> setter;
				if (initial == null) {
					return null;
				} else if (bb != null) {
					setter = delegate(ProgramNode programNode) {
						bb.coda = programNode;
					};
				} else if (setNextNode != null) {
					setter = setNextNode;
				} else {
					throw new Exception ("Initial non-null, but bb and setNextNode both null, but this should never happen");
					//Debug.Fail ("Initial non-null, but bb and setNextNode both null, but this should never happen");
					//return null; //never execu
				}
				return new ProgramSubgraph (initial, setter);
			}

		}
			
		private static ProgramSubgraph processCodeBlock(CodeBlock<MutationT, ConditionT> cb, Dictionary<Loop, LoopRecord> loopLabelMap)
		{
			//code that would theoretically go here moved to instance method of
			//ProgramGraphBuilder, so that subroutining can be used
			ProgramGraphBuilder pgb = new ProgramGraphBuilder ();
			pgb.buildFromCodeBlock (cb, loopLabelMap);
			//NOTE: This will return null if the block
			//was empty
			return pgb.AsProgramSubgraph ();
		}

		public Program<MutationT, ConditionT> ToProgram()
		{
			Dictionary<Loop, LoopRecord> loopLabelMap = new Dictionary<Loop, LoopRecord> ();
			ProgramSubgraph mainProgramGraph = processCodeBlock (this, loopLabelMap);
			ProgramNode rootProgramNode;
			if (mainProgramGraph == null) {
				//the entire block is empty, so main node is "return" node
				rootProgramNode = PROGRAM_RETURN;
			} else {
				mainProgramGraph.SetExitDest (PROGRAM_RETURN);
				rootProgramNode = mainProgramGraph.GetEntryPoint ();
			}
			return new ProgramImpl (rootProgramNode);
		}

	}

}
