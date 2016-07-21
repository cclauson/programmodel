using System;

namespace ProgramModel
{
	/// <summary>
	/// Either object, as is found in many programming
	/// languages and libraries.
	/// 
	/// The Either class has two type parameters, T1 and T2.
	/// Each Either instance is either a left either or a
	/// right Either.  If left, then it can be converted
	/// to a T1, otherwise it can be converted to a T2.
	/// </summary>
	public struct Either<T1, T2>
		where T1 : class
		where T2 : class
	{

		private readonly T1 v1;
		private readonly T2 v2;

		/// <summary>
		/// If this is a left instance, then convert to a
		/// T1.
		/// </summary>
		/// <value>The value of this Either as a T1.</value>
		public T1 Left
		{
			get
			{
				if (v1 == null) {
					throw new InvalidOperationException ("This is a right instance");
				} else {
					return v1;
				}
			}
		}

		/// <summary>
		/// If this is a Right instance, then convert to a
		/// T2.
		/// </summary>
		/// <value>The value of this Either as a T2.</value>
		public T2 Right
		{
			get
			{
				if (v2 == null) {
					throw new InvalidOperationException ("This is a left instance");
				} else {
					return v2;
				}
			}
		}

		private Either(T1 arg1, T2 arg2)
		{
			this.v1 = arg1;
			this.v2 = arg2;
		}

		/// <summary>
		/// Construct a new left Either object.
		/// </summary>
		/// <param name="arg">T1 object to which this either can be converted.</param>
		/// <typeparam name="T1">Type to which this instance can be converted.</typeparam>
		/// <typeparam name="T2">Type to which this instance would be convertible if it were a right instance, which it is not.</typeparam>
		public static Either<T1, T2> left<T1, T2>(T1 arg)
			where T1 : class
			where T2 : class
		{
			if (arg == null) {
				throw new ArgumentNullException ();
			}
			return new Either<T1, T2>(arg, null);
		}

		/// <summary>
		/// Construct a new right Either object.
		/// </summary>
		/// <param name="arg">T2 object to which this either can be converted.</param>
		/// <typeparam name="T1">Type to which this instance would be convertible if it were a left instance, which it is not.</typeparam>
		/// <typeparam name="T2">Type to which this instance can be converted.</typeparam>
		public static Either<T1, T2> right<T1, T2>(T2 arg)
			where T1 : class
			where T2 : class
		{
			if (arg == null) {
				throw new ArgumentNullException ();
			}
			return new Either<T1, T2>(null, arg);
		}

		/// <summary>
		/// True iff this object is a left instance.
		/// </summary>
		/// <returns><c>true</c>, if this instance is convertible to T1 with Left() <c>false</c> otherwise.</returns>
		public bool isLeft()
		{
			return v1 != null;
		}

		/// <summary>
		/// True iff this object is a right instance.
		/// </summary>
		/// <returns><c>true</c>, if this instance is convertible to T2 with Right() <c>false</c> otherwise.</returns>
		public bool isRight()
		{
			return v2 != null;
		}

	}
}

