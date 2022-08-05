namespace System
{
	public static class SpanExtensions
	{
		/// <summary>
		/// Fast return subsequence of source span with used index && separator
		/// </summary>
		/// <param name="str"></param>
		/// <param name="index"></param>
		/// <param name="separator"></param>
		/// <returns></returns>
		/// <remarks>
		/// Use StringComparison.Ordinal
		/// </remarks>
		public static ReadOnlySpan<char> GetToken(this ReadOnlySpan<char> str, int index, ReadOnlySpan<char> separator)
		{
			int start = 0;
			int len = separator.Length;

			for (int i = 0; i < index && start != -1; i++)
			{
				int pos = str[start..].IndexOf(separator);
				if (pos < 0)
				{
					return ReadOnlySpan<char>.Empty;
				}
				else
				{
					start += (len + pos);
				}
			}

			int finish = str[start..].IndexOf(separator);
			if (finish > 0)
			{
				return str.Slice(start, finish);
			}
			else
			{
				return str[start..];
			}
		}

		public static ReadOnlySpan<T> Left<T>(this ReadOnlySpan<T> source, int lenght)
		{
			if (source.IsEmpty)
				return ReadOnlySpan<T>.Empty;

			return source[..(lenght > source.Length ? source.Length : lenght)];
		}

		public static ReadOnlySpan<T> Right<T>(this ReadOnlySpan<T> source, int lenght)
		{
			if (source.IsEmpty)
				return ReadOnlySpan<T>.Empty;

			return source[^(lenght > source.Length ? source.Length : lenght)..];
		}

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
