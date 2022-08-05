using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLibre
{
	internal ref struct KeyValuePairReader
	{
		private ReadOnlySpan<char> _buffer;
		private ReadOnlySpan<char> _sep1;
		private ReadOnlySpan<char> _sep2;
		int _offset;
		private readonly Func<char, bool> _keyCharFilter;
		private readonly Func<char, bool> _valueCharFilter;
		private static readonly Func<char, bool> _defaultCharFilter = c => true;


		public KeyValuePairReader(string text,
			string itemSeparator = ";",
			string keyValueSeparator = "=",
			Func<char, bool>? keyCharFilter = null,
			Func<char, bool>? valueCharFilter = null)
		{
			_buffer = text.AsSpan();
			_sep1 = itemSeparator.AsSpan();
			_sep2 = keyValueSeparator.AsSpan();
			_offset = 0;
			_keyCharFilter = keyCharFilter ?? _defaultCharFilter;
			_valueCharFilter = valueCharFilter ?? _defaultCharFilter;
		}

		public bool Read(out KeyValuePair<string, string> pair)
		{
			pair = new();
			int len = _buffer.Length;
			if (_offset >= len)
				return false;

			int pos = _buffer[_offset..].IndexOf(_sep1);
			if (pos == -1)
				pos = len - _offset;

			var token = _buffer.Slice(_offset, pos);
			if (token.Length > 0)
			{
				pos = token.IndexOf(_sep2);
				if (pos == -1)
					throw new InvalidOperationException($"Wrong string format");
				pair = new(token[0..pos].ToString(_keyCharFilter), token[(pos + _sep2.Length)..].ToString(_valueCharFilter));
			}
			_offset += token.Length + _sep1.Length;
			return true;
		}
	}
}
