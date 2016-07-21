using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ProgramModel
{

	/// <summary>
	/// A program builder is a mutable object that
	/// represents a program, in order to describe
	/// a program, the user code will construct
	/// a program builder object, and describe the
	/// program through a series of method calls
	/// on the builder object.  At the end, the
	/// builder object will represent a description
	/// of a program.
	/// 
	/// Note that "assignments" and "conditions" are
	/// treated as opaque by a program builder, this
	/// allows a program builder to be used for various
	/// kinds of imperative programs.  The general
	/// guideline for intended use, though, is:
	/// *An "assignment" represents an expression in
	/// the language that changes the state of the
	/// world in some way
	/// *A "condition" is something that evaluates to
	/// true or false based on the current state of the
	/// world.
	/// Note that here "the world" could mean machine
	/// memory, but "changing the state of the world"
	/// could also mean producing output or reading
	/// input off of an input stream.
	/// </summary>
	public partial class ProgramBuilder<AssignmentT, ConditionT>
		where AssignmentT : class
		where ConditionT : class
	{

		private readonly CodeBlock<AssignmentT, ConditionT> rootCodeBlock;
		private static readonly Return RETURN = new ProgramBuilder<AssignmentT, ConditionT>.Return();

		interface CodeBlockNonAssignment {}

		class Return : CodeBlockNonAssignment {}

		abstract class ContinueBreakBase : CodeBlockNonAssignment
		{
			public readonly Loop loop;

			public ContinueBreakBase(Loop loop)
			{
				this.loop = loop;
			}
		}

		class Continue : ContinueBreakBase
		{
			public Continue(Loop loop) : base(loop) {}
		}

		class Break : ContinueBreakBase
		{
			public Break(Loop loop) : base(loop) {}
		}

		abstract class IfWhileDoWhileBase : CodeBlockNonAssignment
		{
			public readonly ConditionT condition;
			public readonly CodeBlock<AssignmentT, ConditionT> codeBlock;

			public IfWhileDoWhileBase(ConditionT condition, CodeBlockImpl parent, Boolean isLoop)
			{
				this.condition = condition;
				this.codeBlock = new CodeBlockImpl(parent, isLoop? (Loop) this : null);
			}
		}

		class If : IfWhileDoWhileBase
		{
			public If(ConditionT condition, CodeBlockImpl parent) : base(condition, parent, false) {}
		}

		class IfElse : If
		{
			public readonly CodeBlock<AssignmentT, ConditionT> elseCodeBlock;

			public IfElse(ConditionT condition, CodeBlockImpl parent) : base(condition, parent)
			{
				this.elseCodeBlock = new CodeBlockImpl();
			}
		}

		abstract class WhileDoWhileBase : IfWhileDoWhileBase, Loop
		{
			public WhileDoWhileBase(ConditionT condition, CodeBlockImpl parent) : base(condition, parent, true) {}
		}

		class While : WhileDoWhileBase
		{
			public While(ConditionT condition, CodeBlockImpl parent) : base(condition, parent) {}
		}

		class DoWhile : WhileDoWhileBase
		{
			public DoWhile(ConditionT condition, CodeBlockImpl parent) : base(condition, parent) {}
		}

		class CodeBlockImpl : CodeBlock<AssignmentT, ConditionT>
		{

			public readonly IList<Either<AssignmentT, CodeBlockNonAssignment>> contents;
			public readonly Loop enclosingLoop;
			public CodeBlockImpl parent;

			public CodeBlockImpl() : this(null) {}

			public CodeBlockImpl(CodeBlockImpl parentBlock) : this(parentBlock, null) {}

			public CodeBlockImpl(CodeBlockImpl parentBlock, Loop enclosingLoop)
			{
				contents = new List<Either<AssignmentT, CodeBlockNonAssignment>>();
				this.parent = parentBlock;
				this.enclosingLoop = enclosingLoop;
			}

			public override void addAssignment(AssignmentT assignment)
			{
				contents.Add (Either<AssignmentT, CodeBlockNonAssignment>
					.left<AssignmentT, CodeBlockNonAssignment>(assignment));
			}

			private void addNonAssignment(CodeBlockNonAssignment nonAssignment)
			{
				contents.Add (Either<AssignmentT, CodeBlockNonAssignment>
					.right<AssignmentT, CodeBlockNonAssignment>(nonAssignment));
			}

			public override CodeBlock<AssignmentT, ConditionT> addIf(ConditionT condition)
			{
				If iif = new If (condition, this);
				addNonAssignment (iif);
				return iif.codeBlock;
			}

			public override Tuple<CodeBlock<AssignmentT, ConditionT>, CodeBlock<AssignmentT, ConditionT>> addIfElse(ConditionT condition)
			{
				IfElse ifElse = new IfElse (condition, this);
				addNonAssignment (ifElse);
				return new Tuple<CodeBlock<AssignmentT, ConditionT>, CodeBlock<AssignmentT, ConditionT>> (ifElse.codeBlock, ifElse.elseCodeBlock);
			}

			public override Tuple<CodeBlock<AssignmentT, ConditionT>, Loop> addWhile(ConditionT condition)
			{
				While whiile = new While (condition, this);
				addNonAssignment (whiile);
				return new Tuple<CodeBlock<AssignmentT, ConditionT>, Loop> (whiile.codeBlock, whiile);
			}

			public override Tuple<CodeBlock<AssignmentT, ConditionT>, Loop> addDoWhile(ConditionT condition)
			{
				DoWhile doWhile = new DoWhile (condition, this);
				addNonAssignment (doWhile);
				return new Tuple<CodeBlock<AssignmentT, ConditionT>, Loop> (doWhile.codeBlock, doWhile);
			}

			private Loop getNearestEnclosingLoop()
			{
				for (CodeBlockImpl currBlock = this; currBlock != null; currBlock = currBlock.parent) {
					if (currBlock.enclosingLoop != null)
						return currBlock.enclosingLoop;
				}
				return null;
			}

			private void checkLoopIsEnclosing(Loop loop)
			{
				for (CodeBlockImpl currBlock = this; currBlock != null; currBlock = currBlock.parent) {
					if (currBlock.enclosingLoop == loop)
						return;
				}
				throw new ArgumentException ("loop is not enclosing loop of this block");
			}

			public override void addContinue ()
			{
				Loop nearestEnclosingLoop = getNearestEnclosingLoop ();
				if (nearestEnclosingLoop == null)
					throw new InvalidOperationException ("Can't add continue, no enclosing loop");
				else
					addContinue (nearestEnclosingLoop);
			}

			public override void addContinue(Loop loop)
			{
				checkLoopIsEnclosing (loop);
				addNonAssignment (new Continue (loop));
			}

			public override void addBreak()
			{
				Loop nearestEnclosingLoop = getNearestEnclosingLoop ();
				if (nearestEnclosingLoop == null)
					throw new InvalidOperationException ("Can't add break, no enclosing loop");
				else
					addBreak (nearestEnclosingLoop);
			}

			public override void addBreak (Loop loop)
			{
				checkLoopIsEnclosing (loop);
				addNonAssignment (new Break (loop));
			}

			public override void addReturn()
			{
				addNonAssignment (RETURN);
			}

		}

		public ProgramBuilder ()
		{
			rootCodeBlock = new CodeBlockImpl();
		}

		public CodeBlock<AssignmentT, ConditionT> getRootCodeBlock()
		{
			return rootCodeBlock;
		}

	}

}
