using System;

namespace ProgramModel
{
	/// <summary>
	/// A loop object is a handle to a loop
	/// structure in a code block.  Loops object
	/// references can be obtained by defining
	/// loops in code blocks, and can be used
	/// later as targets for break and continue
	/// statements.  The correspond roughly to
	/// labels in Java.
	/// 
	/// Loop has no methods, and is thus a marker
	/// interface.  There is never any reason for
	/// client code to implement Loop, loop objectz
	/// should only ever be obtained from CodeBlocks.
	/// </summary>
	public interface Loop {}
}

