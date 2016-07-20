using System;

namespace ProgramModel
{
	public struct Either<T1, T2>
		where T1 : class
		where T2 : class
	{

		private readonly T1 v1;
		private readonly T2 v2;

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

		public static Either<T1, T2> left<T1, T2>(T1 arg)
			where T1 : class
			where T2 : class
		{
			if (arg == null) {
				throw new ArgumentNullException ();
			}
			return new Either<T1, T2>(arg, null);
		}

		public static Either<T1, T2> right<T1, T2>(T2 arg)
			where T1 : class
			where T2 : class
		{
			if (arg == null) {
				throw new ArgumentNullException ();
			}
			return new Either<T1, T2>(null, arg);
		}

		public bool isFirst()
		{
			return v1 != null;
		}

		public bool isSecond()
		{
			return v2 != null;
		}

	}
}

