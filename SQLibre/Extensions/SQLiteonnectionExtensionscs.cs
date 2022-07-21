using SQLibre.Core;
using System.Text;
using System.Text.Json;
using static SQLibre.Core.Raw;

namespace SQLibre
{
	public static class SQLiteonnectionExtensions
	{
		public static JsonElement ExecuteJson(this SQLiteCommand command, JsonSerializerOptions? options = null)
		{
			using (var r = command.ExecuteReader())
			{
				if (r.Read())
					return JsonSerializer.Deserialize<JsonElement>((ReadOnlySpan<byte>)r.GetUtf8String(0), options);
				return default;
			}
		}
		public static T? ExecuteJson<T>(this SQLiteCommand command, JsonSerializerOptions? options = null)
		{
			using (var r = command.ExecuteReader())
			{
				if (r.Read())
					return JsonSerializer.Deserialize<T>((ReadOnlySpan<byte>)r.GetUtf8String(0), options);
				return default;
			}
		}
		
	}
}
