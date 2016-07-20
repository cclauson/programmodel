using System;

namespace ProgramModel
{

	public interface CodeBlock<AssignmentT, ConditionT>
	{
		void addAssignment(AssignmentT assignment);
		CodeBlock<AssignmentT, ConditionT> addIf(ConditionT condition);
		Tuple<CodeBlock<AssignmentT, ConditionT>, CodeBlock<AssignmentT, ConditionT>> addIfElse(ConditionT condition);
		Tuple<CodeBlock<AssignmentT, ConditionT>, Loop> addWhile(ConditionT condition);
		Tuple<CodeBlock<AssignmentT, ConditionT>, Loop> addDoWhile(ConditionT condition);
		void addContinue ();
		void addContinue(Loop loop);
		void addBreak();
		void addBreak (Loop loop);
		void addReturn();
	}

}

