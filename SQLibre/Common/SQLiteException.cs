using SQLibre.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SQLibre.Core.Raw;
using static SQLibre.Core.Raw.NativeMethods;

namespace SQLibre
{
    /// <summary>
    ///     Represents a SQLite error.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/database-errors">Database Errors</seealso>
	public class SQLiteException : Exception
	{
        public const string default_native_error = @"For more information on this error code see https://www.sqlite.org/rescode.html</value>";
        public const string call_requires_open_connection = @"{0} can only be called when the connection is open.";

        public int ErrorCode { get; }
		public int ExtendedErrorCode { get; }

		public SQLiteException(int r, string? message) : base(message)
		{
			ErrorCode = (int)r;
		}

        public SQLiteException(string message, int errorCode, int extendedErrorCode) : base(message)
        {
            ErrorCode = (int)errorCode;
            ExtendedErrorCode = (int)extendedErrorCode;
        }

        public static void CheckOK(int r)
            => CheckOK(DbHandle.Empty, r);

        public static unsafe void CheckOK(DbHandle db, int r)
		{
            if (r == SQLITE_OK
                || r == SQLITE_ROW
                || r == SQLITE_DONE)
            {
                return;
            }
            
            string? message;
            int extendedErrorCode;
            if (db.IsEmpty()
                || r != sqlite3_errcode(db))
            {
                message = (Utf8z)sqlite3_errstr(r) + " " + default_native_error;
                extendedErrorCode = r;
            }
            else
            {
                var p = sqlite3_errmsg(db);
                if (p != null)
                    message = (Utf8z)p;
                else
                    message = $"SQLite native error with code={r}. {default_native_error}";
                extendedErrorCode = sqlite3_extended_errcode(db);
            }

            throw new SQLiteException(message ?? string.Empty, r, extendedErrorCode);
        }
    }
}
