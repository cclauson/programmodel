using System;

namespace ProgramModel
{
	/// <summary>
	/// A CodeBlock object represents a block of code.  It has
	/// two type parameters, AssignmentT and ConditionT.  Abstract
	/// assignments and conditions are discussed in the doc for
	/// the ProgramBuilder class.
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
	public interface CodeBlock<AssignmentT, ConditionT>
	{
		/// <summary>
		/// Append an abstract assignment to the end of this CodeBlock.
		/// </summary>
		/// <param name="assignment">Assignment to append to the end of this CodeBlock.</param>
		void addAssignment(AssignmentT assignment);
		/// <summary>
		/// Appends an if structure to the end of this code block, which uses the
		/// given condition to guard its CodeBlock.  The CodeBlock guarded
		/// by the if is returned, which can be used to define what code is
		/// to be executed conditionally.
		/// </summary>
		/// <returns>CodeBlock object which can be used to define the code to be executed conditionally by this if.</returns>
		/// <param name="condition">Condition that protects the enclosed CodeBlock.</param>
		CodeBlock<AssignmentT, ConditionT> addIf(ConditionT condition);
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
		Tuple<CodeBlock<AssignmentT, ConditionT>, CodeBlock<AssignmentT, ConditionT>> addIfElse(ConditionT condition);
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
		Tuple<CodeBlock<AssignmentT, ConditionT>, Loop> addWhile(ConditionT condition);
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
		Tuple<CodeBlock<AssignmentT, ConditionT>, Loop> addDoWhile(ConditionT condition);
		/// <summary>
		/// Appends a "continue" statement to the end of this CodeBlock, which targets the
		/// nearest enclosing loop.  If there is no enclosing loop, calling this method will
		/// cause a runtime exception.  Note that it will never make sense to add any more
		/// code to the CodeBlock after a "continue," since any such code would never be
		/// executed.
		/// </summary>
		void addContinue ();
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
		void addContinue(Loop loop);
		/// <summary>
		/// Appends a "break" statement to the end of this CodeBlock, which targets the
		/// nearest enclosing loop.  If there is no enclosing loop, calling this method will
		/// cause a runtime exception.  Note that it will never make sense to add any more
		/// code to the CodeBlock after a "break," since any such code would never be
		/// executed.
		/// </summary>
		void addBreak();
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
		void addBreak (Loop loop);
		/// <summary>
		/// Appends a "return" statement to the end of this CodeBlock.  Note that it never
		/// makes sense to add any more code after a "return," since any such code would never
		/// be executed.
		/// </summary>
		void addReturn();
	}

}

