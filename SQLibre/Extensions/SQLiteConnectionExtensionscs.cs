using SQLibre.Core;
using System.Text;
using System.Text.Json;
using static SQLibre.Core.Raw;

namespace SQLibre
{
	public static class SQLiteConnectionExtensions
	{
		private static readonly byte[] null_utf8_string = UTF8Encoding.UTF8.GetBytes("null");

		public static JsonElement ExecuteJson(this SQLiteCommand command, JsonSerializerOptions? options = null)
		{
			using (var r = command.ExecuteReader())
			{
				if (r.Read())
					return JsonSerializer.Deserialize<JsonElement>(r.GetBytes(), options);
				return default;
			}
		}

		public static async ValueTask<JsonElement> ExecuteJsonAsync(this SQLiteCommand command, JsonSerializerOptions? options = null)
			=> await ValueTask.FromResult(ExecuteJson(command, options));
		
		public static async ValueTask<T?> ExecuteJsonAsync<T>(this SQLiteCommand command, JsonSerializerOptions? options = null)
			=> await ValueTask.FromResult(ExecuteJson<T>(command, options));

		public static T? ExecuteJson<T>(this SQLiteCommand command, JsonSerializerOptions? options = null)
		{
			using (var r = command.ExecuteReader())
			{
				if (r.Read())
					return JsonSerializer.Deserialize<T>(r.GetBytes(), options);
				return default;
			}
		}
		private static ReadOnlySpan<byte> GetBytes(this SQLiteReader  reader) 
		{
			var s = reader.GetUtf8String(0);
			if (s.Length > 0)
				return (ReadOnlySpan<byte>)s;
			return null_utf8_string; ;
		}
	}
}
