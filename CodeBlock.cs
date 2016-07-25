using System;
using System.Collections.Generic;

namespace ProgramModel
{
	/// <summary>
	/// A CodeBlock object represents a block of code.  It has
	/// two type parameters, MutationT and ConditionT, which
	/// are the types of the mutations and conditions for
	/// this programming language respectively.
	/// 
	/// Note that "mutations" and "conditions" are
	/// treated as opaque by a program builder, this
	/// allows a program builder to be used for various
	/// kinds of imperative programs.  The general
	/// guideline for intended use, though, is:
	/// *An "mutation" represents an expression in
	/// the language that changes the state of the
	/// world in some way
	/// *A "condition" is something that evaluates to
	/// true or false based on the current state of the
	/// world.
	/// Note that here "the world" could mean machine
	/// memory, but "changing the state of the world"
	/// could also mean producing output or reading
	/// input off of an input stream.
	///
	/// CodeBlock objects contain various mutator methods, each
	/// adds additional things to the code block.  Things that
	/// can be added are assignments, if structures, if/else
	/// structures, while and do/while loops, continue, break
	/// and return statements, where continue and break
	/// target enclosing loops.
	/// 
	/// Note that control structures (if/if-else/while/do-while)
	/// have CodeBlock objects associated with them, so CodeBlocks
	/// contain control structures, which have CodeBlocks below them,
	/// which can have control structures with CodeBlocks below
	/// them, etc.
	/// </summary>
	public partial class CodeBlock<MutationT, ConditionT>
		where MutationT : class
		where ConditionT : class
	{

		//sequence of contents in this code block
		private readonly IList<Either<MutationT, CodeBlockNonAssignment>> contents;
		//loop that immediately encloses this code block, might be null
		private readonly Loop enclosingLoop;
		//parent code block to this code block, may be null
		private CodeBlock<MutationT, ConditionT> parent;

		/// <summary>
		/// Create a new CodeBlock object, which models an empty
		/// block of code.  Mutator methods should be called to
		/// add contents to the code block.
		/// </summary>
		public CodeBlock() : this(null) {}

		private CodeBlock(CodeBlock<MutationT, ConditionT> parentBlock) : this(parentBlock, null) {}

		private CodeBlock(CodeBlock<MutationT, ConditionT> parentBlock, Loop enclosingLoop)
		{
			contents = new List<Either<MutationT, CodeBlockNonAssignment>>();
			this.parent = parentBlock;
			this.enclosingLoop = enclosingLoop;
		}

		/// <summary>
		/// Append an abstract assignment to the end of this CodeBlock.
		/// </summary>
		/// <param name="assignment">Assignment to append to the end of this CodeBlock.</param>
		public void addAssignment(MutationT assignment)
		{
			contents.Add (Either<MutationT, CodeBlockNonAssignment>
				.left<MutationT, CodeBlockNonAssignment>(assignment));
		}

		private void addNonAssignment(CodeBlockNonAssignment nonAssignment)
		{
			contents.Add (Either<MutationT, CodeBlockNonAssignment>
				.right<MutationT, CodeBlockNonAssignment>(nonAssignment));
		}

		/// <summary>
		/// Appends an if structure to the end of this code block, which uses the
		/// given condition to guard its CodeBlock.  The CodeBlock guarded
		/// by the if is returned, which can be used to define what code is
		/// to be executed conditionally.
		/// </summary>
		/// <returns>CodeBlock object which can be used to define the code to be executed conditionally by this if.</returns>
		/// <param name="condition">Condition that protects the enclosed CodeBlock.</param>
		public CodeBlock<MutationT, ConditionT> addIf(ConditionT condition)
		{
			If iif = new If (condition, this);
			addNonAssignment (iif);
			return iif.codeBlock;
		}

		/// <summary>
		/// Appends an if-else structure to this CodeBlock, which of its sub blocks
		/// will be executed is controlled by the condition object passed as a method
		/// parameter.
		/// 
		/// This method returns a pair of CodeBlock objects, the first is the CodeBlock
		/// that will be executed if the condition is true, the second is the CodeBlock
		/// that will be executed if the condition is false.  The CodeBlock object references
		/// can be used to define the actual code that is to be conditionally executed.
		/// </summary>
		/// <returns>The two code blocks which are underneath this if-else, first that to be executed
		/// on the "if," the second to be executed on the "else."</returns>
		/// <param name="condition">Condition that will be used to decide whether the first or second
		/// code block is executed.</param>
		public Tuple<CodeBlock<MutationT, ConditionT>, CodeBlock<MutationT, ConditionT>> addIfElse(ConditionT condition)
		{
			IfElse ifElse = new IfElse (condition, this);
			addNonAssignment (ifElse);
			return new Tuple<CodeBlock<MutationT, ConditionT>, CodeBlock<MutationT, ConditionT>> (ifElse.codeBlock, ifElse.elseCodeBlock);
		}

		/// <summary>
		/// Appends a while structure to the end of this CodeBlock, which uses the given
		/// condition as the loop condition.
		/// 
		/// This method returns a (CodeBlock, Loop) pair.  The CodeBlock handle can be used
		/// to define the code contained in this while loop, the Loop object can be used as
		/// a target for "break" and "continue" statements in the contained code block, or
		/// any CodeBlocks contained underneath it hierarchically.
		/// </summary>
		/// <returns>A CodeBlock/Loop pair, the CodeBlock defines the code protected by the
		/// loop, the Loop object can be used as a target for continue/break.</returns>
		/// <param name="condition">Condition to check to decide whether or not to loop.</param>
		public Tuple<CodeBlock<MutationT, ConditionT>, Loop> addWhile(ConditionT condition)
		{
			While whiile = new While (condition, this);
			addNonAssignment (whiile);
			return new Tuple<CodeBlock<MutationT, ConditionT>, Loop> (whiile.codeBlock, whiile);
		}

		/// <summary>
		/// Appends a do-while structure to the end of this CodeBlock, which uses the given
		/// condition as the loop condition.
		/// 
		/// This method returns a (CodeBlock, Loop) pair.  The CodeBlock handle can be used
		/// to define the code contained in this do-while loop, the Loop object can be used as
		/// a target for "break" and "continue" statements in the contained code block, or
		/// any CodeBlocks contained underneath it hierarchically.
		/// </summary>
		/// <returns>A CodeBlock/Loop pair, the CodeBlock defines the code protected by the
		/// loop, the Loop object can be used as a target for continue/break.</returns>
		/// <param name="condition">Condition to check to decide whether or not to loop.</param>
		public Tuple<CodeBlock<MutationT, ConditionT>, Loop> addDoWhile(ConditionT condition)
		{
			DoWhile doWhile = new DoWhile (condition, this);
			addNonAssignment (doWhile);
			return new Tuple<CodeBlock<MutationT, ConditionT>, Loop> (doWhile.codeBlock, doWhile);
		}

		private Loop getNearestEnclosingLoop()
		{
			for (CodeBlock<MutationT, ConditionT> currBlock = this; currBlock != null; currBlock = currBlock.parent) {
				if (currBlock.enclosingLoop != null)
					return currBlock.enclosingLoop;
			}
			return null;
		}

		private void checkLoopIsEnclosing(Loop loop)
		{
			for (CodeBlock<MutationT, ConditionT> currBlock = this; currBlock != null; currBlock = currBlock.parent) {
				if (currBlock.enclosingLoop == loop)
					return;
			}
			throw new ArgumentException ("loop is not enclosing loop of this block");
		}

		/// <summary>
		/// Appends a "continue" statement to the end of this CodeBlock, which targets the
		/// nearest enclosing loop.  If there is no enclosing loop, calling this method will
		/// cause a runtime exception.  Note that it will never make sense to add any more
		/// code to the CodeBlock after a "continue," since any such code would never be
		/// executed.
		/// </summary>
		public void addContinue ()
		{
			Loop nearestEnclosingLoop = getNearestEnclosingLoop ();
			if (nearestEnclosingLoop == null)
				throw new InvalidOperationException ("Can't add continue, no enclosing loop");
			else
				addContinue (nearestEnclosingLoop);
		}

		/// <summary>
		/// Appends a "continue" statement to the end of this CodeBlock, which targets the
		/// enclosing loop corresponding to the given Loop object.  Note that if "loop" does
		/// not refer to an enclosing loop, this method will
		/// cause a runtime exception.  Also note that it will never make sense to add any more
		/// code to the CodeBlock after a "continue," since any such code would never be
		/// executed.
		/// </summary>
		/// <param name="loop">Enclosing loop to which we would like to continue.  If "loop"
		/// is not an enclosing loop, a runtime exception will be generated..</param>
		public void addContinue(Loop loop)
		{
			checkLoopIsEnclosing (loop);
			addNonAssignment (new Continue (loop));
		}

		/// <summary>
		/// Appends a "break" statement to the end of this CodeBlock, which targets the
		/// nearest enclosing loop.  If there is no enclosing loop, calling this method will
		/// cause a runtime exception.  Note that it will never make sense to add any more
		/// code to the CodeBlock after a "break," since any such code would never be
		/// executed.
		/// </summary>
		public void addBreak()
		{
			Loop nearestEnclosingLoop = getNearestEnclosingLoop ();
			if (nearestEnclosingLoop == null)
				throw new InvalidOperationException ("Can't add break, no enclosing loop");
			else
				addBreak (nearestEnclosingLoop);
		}

		/// <summary>
		/// Appends a "break" statement to the end of this CodeBlock, which targets the
		/// enclosing loop corresponding to the given Loop object.  Note that if "loop" does
		/// not refer to an enclosing loop, this method will
		/// cause a runtime exception.  Also note that it will never make sense to add any more
		/// code to the CodeBlock after a "break," since any such code would never be
		/// executed.
		/// </summary>
		/// <param name="loop">Enclosing loop out of which we would like to break.  If "loop"
		/// is not an enclosing loop, a runtime exception will be generated..</param>
		public void addBreak (Loop loop)
		{
			checkLoopIsEnclosing (loop);
			addNonAssignment (new Break (loop));
		}

		/// <summary>
		/// Appends a "return" statement to the end of this CodeBlock.  Note that it never
		/// makes sense to add any more code after a "return," since any such code would never
		/// be executed.
		/// </summary>
		public void addReturn()
		{
			addNonAssignment (RETURN);
		}

		private static readonly Return RETURN = new Return();

		private interface CodeBlockNonAssignment {}

		private class Return : CodeBlockNonAssignment {}

		private abstract class ContinueBreakBase : CodeBlockNonAssignment
		{
			public readonly Loop loop;

			public ContinueBreakBase(Loop loop)
			{
				this.loop = loop;
			}
		}

		private class Continue : ContinueBreakBase
		{
			public Continue(Loop loop) : base(loop) {}
		}

		private class Break : ContinueBreakBase
		{
			public Break(Loop loop) : base(loop) {}
		}

		private abstract class IfWhileDoWhileBase : CodeBlockNonAssignment
		{
			public readonly ConditionT condition;
			public readonly CodeBlock<MutationT, ConditionT> codeBlock;

			public IfWhileDoWhileBase(ConditionT condition, CodeBlock<MutationT, ConditionT> parent, Boolean isLoop)
			{
				this.condition = condition;
				this.codeBlock = new CodeBlock<MutationT, ConditionT>(parent, isLoop? (Loop) this : null);
			}
		}

		private class If : IfWhileDoWhileBase
		{
			public If(ConditionT condition, CodeBlock<MutationT, ConditionT> parent) : base(condition, parent, false) {}
		}

		private class IfElse : If
		{
			public readonly CodeBlock<MutationT, ConditionT> elseCodeBlock;

			public IfElse(ConditionT condition, CodeBlock<MutationT, ConditionT> parent) : base(condition, parent)
			{
				this.elseCodeBlock = new CodeBlock<MutationT, ConditionT>();
			}
		}

		private abstract class WhileDoWhileBase : IfWhileDoWhileBase, Loop
		{
			public WhileDoWhileBase(ConditionT condition, CodeBlock<MutationT, ConditionT> parent) : base(condition, parent, true) {}
		}

		private class While : WhileDoWhileBase
		{
			public While(ConditionT condition, CodeBlock<MutationT, ConditionT> parent) : base(condition, parent) {}
		}

		private class DoWhile : WhileDoWhileBase
		{
			public DoWhile(ConditionT condition, CodeBlock<MutationT, ConditionT> parent) : base(condition, parent) {}
		}

	}

}
