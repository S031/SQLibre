namespace System
{
	public static class SpanExtensions
	{
		public static unsafe string ToString(this ReadOnlySpan<char> chars, Func<char, bool> filter)
		{
			if (chars == ReadOnlySpan<char>.Empty)
				return string.Empty;

			Span<char> target = stackalloc char[chars.Length];
			int j = 0;
			foreach (var c in chars)
				if (filter(c))
					target[j++] = c;
			return target[0..j].ToString();
		}
	}
}
