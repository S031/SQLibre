using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using sqlite3 = System.IntPtr;
using sqlite3_stmt = System.IntPtr;
using hook_handle = System.IntPtr;
using sqlite3_blob = System.IntPtr;
using sqlite3_backup = System.IntPtr;
using sqlite3_snapshot = System.IntPtr;

namespace SQLibre.Core
{
	internal static class Raw
	{
		static readonly bool IsArm64cc =
			RuntimeInformation.ProcessArchitecture == Architecture.Arm64 &&
			(RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Create("IOS")));
		
		public static int sqlite3_config(int op)
		{
			return NativeMethods.sqlite3_config_none(op);
		}

		public static int sqlite3_config(int op, int val)
		{
			if (IsArm64cc)
				return NativeMethods.sqlite3_config_int_arm64cc(op, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, val);

			return NativeMethods.sqlite3_config_int(op, val);
		}

		public static unsafe int sqlite3_db_config(sqlite3 db, int op, Utf8z val)
		{
			fixed (byte* p_val = val)
			{
				if (IsArm64cc)
					return NativeMethods.sqlite3_db_config_charptr_arm64cc(db, op, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, p_val);

				return NativeMethods.sqlite3_db_config_charptr(db, op, p_val);
			}
		}

		public static unsafe int sqlite3_db_config(sqlite3 db, int op, int val, out int result)
		{
			int out_result = 0;
			int native_result;

			if (IsArm64cc)
				native_result = NativeMethods.sqlite3_db_config_int_outint_arm64cc(db, op, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, val, &out_result);
			else
				native_result = NativeMethods.sqlite3_db_config_int_outint(db, op, val, &out_result);

			result = out_result;

			return native_result;
		}

		public static int sqlite3_db_config(sqlite3 db, int op, IntPtr ptr, int int0, int int1)
		{
			if (IsArm64cc)
				return NativeMethods.sqlite3_db_config_intptr_int_int_arm64cc(db, op, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ptr, int0, int1);

			return NativeMethods.sqlite3_db_config_intptr_int_int(db, op, ptr, int0, int1);
		}
		
		public static unsafe int sqlite3_prepare_v2(sqlite3 db, ReadOnlySpan<byte> sql, out IntPtr stm, out ReadOnlySpan<byte> tail)
		{
			fixed (byte* p_sql = sql)
			{
				var rc = NativeMethods.sqlite3_prepare_v2(db, p_sql, sql.Length, out stm, out var p_tail);
				var len_consumed = (int)(p_tail - p_sql);
				int len_remain = sql.Length - len_consumed;
				if (len_remain > 0)
				{
					tail = sql.Slice(len_consumed, len_remain);
				}
				else
				{
					tail = ReadOnlySpan<byte>.Empty;
				}
				return rc;
			}
		}

		public static unsafe int sqlite3_bind_blob(sqlite3_stmt stm, int paramIndex, ReadOnlySpan<byte> blob)
		{
			if (blob.Length == 0)
			{
				// passing a zero-length blob to sqlite3_bind_blob() requires
				// a non-null pointer, even though conceptually, that pointer
				// point to zero things, ie nothing.

				var ba_fake = new byte[] { 42 };
				ReadOnlySpan<byte> span_fake = ba_fake;
				fixed (byte* p_fake = span_fake)
				{
					return NativeMethods.sqlite3_bind_blob(stm, paramIndex, p_fake, 0, new IntPtr(-1));
				}
			}
			else
			{
				fixed (byte* p = blob)
				{
					return NativeMethods.sqlite3_bind_blob(stm, paramIndex, p, blob.Length, new IntPtr(-1));
				}
			}
		}

		public static unsafe int sqlite3_bind_parameter_index(sqlite3_stmt stmt, string paramName)
			=> NativeMethods.sqlite3_bind_parameter_index(stmt, (Utf8z)paramName);
		
		public static unsafe int sqlite3_bind_text16(sqlite3_stmt stm, int paramIndex, ReadOnlySpan<char> t)
		{
			fixed (char* p_t = t)
			{
				// mul span length times 2 to get num bytes, which is what sqlite wants
				return NativeMethods.sqlite3_bind_text16(stm, paramIndex, p_t, t.Length * 2, new IntPtr(-1));
			}
		}

		public static unsafe int sqlite3_bind_text(sqlite3_stmt stm, int paramIndex, Utf8z t)
		{
			fixed (byte* p_t = t)
			{
				return NativeMethods.sqlite3_bind_text(stm, paramIndex, p_t, -1, new IntPtr(-1));
			}
		}

		public static unsafe ReadOnlySpan<byte> sqlite3_column_blob(sqlite3_stmt stm, int columnIndex)
		{
			IntPtr blobPointer = NativeMethods.sqlite3_column_blob(stm, columnIndex);
			if (blobPointer == IntPtr.Zero)
			{
				return null;
			}

			var length = NativeMethods.sqlite3_column_bytes(stm, columnIndex);
			unsafe
			{
				return new ReadOnlySpan<byte>(blobPointer.ToPointer(), length);
			}
		}

		public static unsafe Utf8z sqlite3_column_text(sqlite3_stmt stm, int columnIndex)
		{
			byte* p = NativeMethods.sqlite3_column_text(stm, columnIndex);
			var length = NativeMethods.sqlite3_column_bytes(stm, columnIndex);
			return Utf8z.FromPtrLen(p, length);
		}

		public static class NativeMethods
		{
			private const string SQLITE_DLL = "sqlite3";
			private const CallingConvention CALLING_CONVENTION = CallingConvention.Cdecl;

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_close(IntPtr db);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_close_v2(IntPtr db);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_enable_shared_cache(int enable);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe void sqlite3_interrupt(sqlite3 db);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_finalize(IntPtr stmt);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_reset(sqlite3_stmt stmt);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_clear_bindings(sqlite3_stmt stmt);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_stmt_status(sqlite3_stmt stm, int op, int resetFlg);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe byte* sqlite3_bind_parameter_name(sqlite3_stmt stmt, int index);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe byte* sqlite3_column_database_name(sqlite3_stmt stmt, int index);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe byte* sqlite3_column_decltype(sqlite3_stmt stmt, int index);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe byte* sqlite3_column_name(sqlite3_stmt stmt, int index);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe byte* sqlite3_column_origin_name(sqlite3_stmt stmt, int index);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe byte* sqlite3_column_table_name(sqlite3_stmt stmt, int index);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe byte* sqlite3_column_text(sqlite3_stmt stmt, int index);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe byte* sqlite3_errmsg(sqlite3 db);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_db_readonly(sqlite3 db, byte* dbName);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe byte* sqlite3_db_filename(sqlite3 db, byte* att);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_prepare_v2(sqlite3 db, byte* pSql, int nBytes, out IntPtr stmt, out byte* ptrRemain);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_prepare_v3(sqlite3 db, byte* pSql, int nBytes, uint flags, out IntPtr stmt, out byte* ptrRemain);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_db_status(sqlite3 db, int op, out int current, out int highest, int resetFlg);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_complete(byte* pSql);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_compileoption_used(byte* pSql);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe byte* sqlite3_compileoption_get(int n);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_table_column_metadata(sqlite3 db, byte* dbName, byte* tblName, byte* colName, out byte* ptrDataType, out byte* ptrCollSeq, out int notNull, out int primaryKey, out int autoInc);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe byte* sqlite3_value_text(IntPtr p);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_enable_load_extension(sqlite3 db, int enable);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_limit(sqlite3 db, int id, int newVal);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_initialize();

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_shutdown();

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe byte* sqlite3_libversion();

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_libversion_number();

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_threadsafe();

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe byte* sqlite3_sourceid();

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe IntPtr sqlite3_malloc(int n);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe IntPtr sqlite3_realloc(IntPtr p, int n);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe void sqlite3_free(IntPtr p);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_stricmp(IntPtr p, IntPtr q);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_strnicmp(IntPtr p, IntPtr q, int n);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_open(byte* filename, out IntPtr db);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_open_v2(byte* filename, out IntPtr db, int flags, byte* vfs);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe IntPtr sqlite3_vfs_find(byte* vfs);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe long sqlite3_last_insert_rowid(sqlite3 db);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_changes(sqlite3 db);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_total_changes(sqlite3 db);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe long sqlite3_memory_used();

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe long sqlite3_memory_highwater(int resetFlag);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe long sqlite3_soft_heap_limit64(long n);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe long sqlite3_hard_heap_limit64(long n);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_status(int op, out int current, out int highwater, int resetFlag);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_busy_timeout(sqlite3 db, int ms);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_bind_blob(sqlite3_stmt stmt, int index, byte* val, int nSize, IntPtr nTransient);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_bind_zeroblob(sqlite3_stmt stmt, int index, int size);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_bind_double(sqlite3_stmt stmt, int index, double val);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_bind_int(sqlite3_stmt stmt, int index, int val);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_bind_int64(sqlite3_stmt stmt, int index, long val);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_bind_null(sqlite3_stmt stmt, int index);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_bind_text(sqlite3_stmt stmt, int index, byte* val, int nlen, IntPtr pvReserved);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_bind_text16(sqlite3_stmt stmt, int index, char* val, int nlen, IntPtr pvReserved);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_bind_parameter_count(sqlite3_stmt stmt);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_bind_parameter_index(sqlite3_stmt stmt, byte* strName);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_column_count(sqlite3_stmt stmt);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_data_count(sqlite3_stmt stmt);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_step(sqlite3_stmt stmt);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe byte* sqlite3_sql(sqlite3_stmt stmt);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe double sqlite3_column_double(sqlite3_stmt stmt, int index);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_column_int(sqlite3_stmt stmt, int index);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe long sqlite3_column_int64(sqlite3_stmt stmt, int index);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe IntPtr sqlite3_column_blob(sqlite3_stmt stmt, int index);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_column_bytes(sqlite3_stmt stmt, int index);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_column_type(sqlite3_stmt stmt, int index);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_aggregate_count(IntPtr context);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe IntPtr sqlite3_value_blob(IntPtr p);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_value_bytes(IntPtr p);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe double sqlite3_value_double(IntPtr p);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_value_int(IntPtr p);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe long sqlite3_value_int64(IntPtr p);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_value_type(IntPtr p);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe IntPtr sqlite3_user_data(IntPtr context);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe void sqlite3_result_blob(IntPtr context, IntPtr val, int nSize, IntPtr pvReserved);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe void sqlite3_result_double(IntPtr context, double val);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe void sqlite3_result_error(IntPtr context, byte* strErr, int nLen);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe void sqlite3_result_int(IntPtr context, int val);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe void sqlite3_result_int64(IntPtr context, long val);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe void sqlite3_result_null(IntPtr context);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe void sqlite3_result_text(IntPtr context, byte* val, int nLen, IntPtr pvReserved);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe void sqlite3_result_zeroblob(IntPtr context, int n);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe void sqlite3_result_error_toobig(IntPtr context);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe void sqlite3_result_error_nomem(IntPtr context);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe void sqlite3_result_error_code(IntPtr context, int code);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe IntPtr sqlite3_aggregate_context(IntPtr context, int nBytes);

			[DllImport(SQLITE_DLL, ExactSpelling = true, EntryPoint = "sqlite3_config", CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_config_none(int op);

			[DllImport(SQLITE_DLL, ExactSpelling = true, EntryPoint = "sqlite3_config", CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_config_int(int op, int val);

			[DllImport(SQLITE_DLL, ExactSpelling = true, EntryPoint = "sqlite3_config", CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_config_int_arm64cc(int op, IntPtr dummy1, IntPtr dummy2, IntPtr dummy3, IntPtr dummy4, IntPtr dummy5, IntPtr dummy6, IntPtr dummy7, int val);

			[DllImport(SQLITE_DLL, ExactSpelling = true, EntryPoint = "sqlite3_config", CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_config_log(int op, IntPtr func, hook_handle pvUser);

			[DllImport(SQLITE_DLL, ExactSpelling = true, EntryPoint = "sqlite3_config", CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_config_log_arm64cc(int op, IntPtr dummy1, IntPtr dummy2, IntPtr dummy3, IntPtr dummy4, IntPtr dummy5, IntPtr dummy6, IntPtr dummy7, IntPtr func, hook_handle pvUser);

			[DllImport(SQLITE_DLL, ExactSpelling = true, EntryPoint = "sqlite3_db_config", CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_db_config_charptr(sqlite3 db, int op, byte* val);

			[DllImport(SQLITE_DLL, ExactSpelling = true, EntryPoint = "sqlite3_db_config", CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_db_config_charptr_arm64cc(sqlite3 db, int op, IntPtr dummy2, IntPtr dummy3, IntPtr dummy4, IntPtr dummy5, IntPtr dummy6, IntPtr dummy7, byte* val);

			[DllImport(SQLITE_DLL, ExactSpelling = true, EntryPoint = "sqlite3_db_config", CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_db_config_int_outint(sqlite3 db, int op, int val, int* result);

			[DllImport(SQLITE_DLL, ExactSpelling = true, EntryPoint = "sqlite3_db_config", CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_db_config_int_outint_arm64cc(sqlite3 db, int op, IntPtr dummy2, IntPtr dummy3, IntPtr dummy4, IntPtr dummy5, IntPtr dummy6, IntPtr dummy7, int val, int* result);

			[DllImport(SQLITE_DLL, ExactSpelling = true, EntryPoint = "sqlite3_db_config", CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_db_config_intptr_int_int(sqlite3 db, int op, IntPtr ptr, int int0, int int1);

			[DllImport(SQLITE_DLL, ExactSpelling = true, EntryPoint = "sqlite3_db_config", CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_db_config_intptr_int_int_arm64cc(sqlite3 db, int op, IntPtr dummy2, IntPtr dummy3, IntPtr dummy4, IntPtr dummy5, IntPtr dummy6, IntPtr dummy7, IntPtr ptr, int int0, int int1);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_create_collation(sqlite3 db, byte[] strName, int nType, hook_handle pvUser, IntPtr func);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe IntPtr sqlite3_update_hook(sqlite3 db, IntPtr func, hook_handle pvUser);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe IntPtr sqlite3_commit_hook(sqlite3 db, IntPtr func, hook_handle pvUser);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe IntPtr sqlite3_profile(sqlite3 db, IntPtr func, hook_handle pvUser);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe void sqlite3_progress_handler(sqlite3 db, int instructions, IntPtr func, hook_handle pvUser);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe IntPtr sqlite3_trace(sqlite3 db, IntPtr func, hook_handle pvUser);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe IntPtr sqlite3_rollback_hook(sqlite3 db, IntPtr func, hook_handle pvUser);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe IntPtr sqlite3_db_handle(IntPtr stmt);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe IntPtr sqlite3_next_stmt(sqlite3 db, IntPtr stmt);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_stmt_isexplain(sqlite3_stmt stmt);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_stmt_busy(sqlite3_stmt stmt);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_stmt_readonly(sqlite3_stmt stmt);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_exec(sqlite3 db, byte* strSql, IntPtr cb, hook_handle pvParam, out IntPtr errMsg);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_get_autocommit(sqlite3 db);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_extended_result_codes(sqlite3 db, int onoff);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_errcode(sqlite3 db);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_extended_errcode(sqlite3 db);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe byte* sqlite3_errstr(int rc);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe void sqlite3_log(int iErrCode, byte* zFormat);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_file_control(sqlite3 db, byte[] zDbName, int op, IntPtr pArg);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe sqlite3_backup sqlite3_backup_init(sqlite3 destDb, byte* zDestName, sqlite3 sourceDb, byte* zSourceName);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_backup_step(sqlite3_backup backup, int nPage);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_backup_remaining(sqlite3_backup backup);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_backup_pagecount(sqlite3_backup backup);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_backup_finish(IntPtr backup);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_snapshot_get(sqlite3 db, byte* schema, out IntPtr snap);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_snapshot_open(sqlite3 db, byte* schema, sqlite3_snapshot snap);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_snapshot_recover(sqlite3 db, byte* name);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_snapshot_cmp(sqlite3_snapshot p1, sqlite3_snapshot p2);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe void sqlite3_snapshot_free(IntPtr snap);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_blob_open(sqlite3 db, byte* sdb, byte* table, byte* col, long rowid, int flags, out sqlite3_blob blob);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_blob_write(sqlite3_blob blob, byte* b, int n, int offset);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_blob_read(sqlite3_blob blob, byte* b, int n, int offset);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_blob_bytes(sqlite3_blob blob);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_blob_reopen(sqlite3_blob blob, long rowid);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_blob_close(IntPtr blob);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_wal_autocheckpoint(sqlite3 db, int n);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_wal_checkpoint(sqlite3 db, byte* dbName);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_wal_checkpoint_v2(sqlite3 db, byte* dbName, int eMode, out int logSize, out int framesCheckPointed);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_set_authorizer(sqlite3 db, IntPtr cb, hook_handle pvUser);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_win32_set_directory8(uint directoryType, byte* directoryPath);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_create_function_v2(sqlite3 db, byte[] strName, int nArgs, int nType, hook_handle pvUser, IntPtr func, IntPtr fstep, IntPtr ffinal, IntPtr fdestroy);

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_keyword_count();

			[DllImport(SQLITE_DLL, ExactSpelling = true, CallingConvention = CALLING_CONVENTION)]
			public static extern unsafe int sqlite3_keyword_name(int i, out byte* name, out int length);

		}

		#region constants
		public const int SQLITE_UTF8 = 1;
		public const int SQLITE_UTF16LE = 2;
		public const int SQLITE_UTF16BE = 3;
		public const int SQLITE_UTF16 = 4;  /* Use native byte order */
		public const int SQLITE_ANY = 5;  /* sqlite3_create_function only */
		public const int SQLITE_UTF16_ALIGNED = 8;  /* sqlite3_create_function only */

		public const int SQLITE_DETERMINISTIC = 0x800;

		public const int SQLITE_LIMIT_LENGTH = 0;
		public const int SQLITE_LIMIT_SQL_LENGTH = 1;
		public const int SQLITE_LIMIT_COLUMN = 2;
		public const int SQLITE_LIMIT_EXPR_DEPTH = 3;
		public const int SQLITE_LIMIT_COMPOUND_SELECT = 4;
		public const int SQLITE_LIMIT_VDBE_OP = 5;
		public const int SQLITE_LIMIT_FUNCTION_ARG = 6;
		public const int SQLITE_LIMIT_ATTACHED = 7;
		public const int SQLITE_LIMIT_LIKE_PATTERN_LENGTH = 8;
		public const int SQLITE_LIMIT_VARIABLE_NUMBER = 9;
		public const int SQLITE_LIMIT_TRIGGER_DEPTH = 10;
		public const int SQLITE_LIMIT_WORKER_THREADS = 11;

		public const int SQLITE_CONFIG_SINGLETHREAD = 1;  /* nil */
		public const int SQLITE_CONFIG_MULTITHREAD = 2;  /* nil */
		public const int SQLITE_CONFIG_SERIALIZED = 3;  /* nil */
		public const int SQLITE_CONFIG_MALLOC = 4;  /* sqlite3_mem_methods* */
		public const int SQLITE_CONFIG_GETMALLOC = 5;  /* sqlite3_mem_methods* */
		public const int SQLITE_CONFIG_SCRATCH = 6;  /* void*, int utf8z, int N */
		public const int SQLITE_CONFIG_PAGECACHE = 7;  /* void*, int utf8z, int N */
		public const int SQLITE_CONFIG_HEAP = 8;  /* void*, int nByte, int min */
		public const int SQLITE_CONFIG_MEMSTATUS = 9;  /* boolean */
		public const int SQLITE_CONFIG_MUTEX = 10;  /* sqlite3_mutex_methods* */
		public const int SQLITE_CONFIG_GETMUTEX = 11;  /* sqlite3_mutex_methods* */
		/* previously SQLITE_CONFIG_CHUNKALLOC 12 which is now unused. */
		public const int SQLITE_CONFIG_LOOKASIDE = 13;  /* int int */
		public const int SQLITE_CONFIG_PCACHE = 14;  /* no-op */
		public const int SQLITE_CONFIG_GETPCACHE = 15;  /* no-op */
		public const int SQLITE_CONFIG_LOG = 16;  /* xFunc, void* */
		public const int SQLITE_CONFIG_URI = 17;  /* int */
		public const int SQLITE_CONFIG_PCACHE2 = 18;  /* sqlite3_pcache_methods2* */
		public const int SQLITE_CONFIG_GETPCACHE2 = 19;  /* sqlite3_pcache_methods2* */
		public const int SQLITE_CONFIG_COVERING_INDEX_SCAN = 20;  /* int */
		public const int SQLITE_CONFIG_SQLLOG = 21;  /* xSqllog, void* */

		public const int SQLITE_DBCONFIG_MAINDBNAME = 1000; /* const char* */
		public const int SQLITE_DBCONFIG_LOOKASIDE = 1001; /* void* int int */
		public const int SQLITE_DBCONFIG_ENABLE_FKEY = 1002; /* int int* */
		public const int SQLITE_DBCONFIG_ENABLE_TRIGGER = 1003; /* int int* */
		public const int SQLITE_DBCONFIG_ENABLE_FTS3_TOKENIZER = 1004; /* int int* */
		public const int SQLITE_DBCONFIG_ENABLE_LOAD_EXTENSION = 1005; /* int int* */
		public const int SQLITE_DBCONFIG_NO_CKPT_ON_CLOSE = 1006; /* int int* */
		public const int SQLITE_DBCONFIG_ENABLE_QPSG = 1007; /* int int* */
		public const int SQLITE_DBCONFIG_TRIGGER_EQP = 1008; /* int int* */
		public const int SQLITE_DBCONFIG_RESET_DATABASE = 1009; /* int int* */
		public const int SQLITE_DBCONFIG_DEFENSIVE = 1010; /* int int* */
		public const int SQLITE_DBCONFIG_WRITABLE_SCHEMA = 1011; /* int int* */
		public const int SQLITE_DBCONFIG_LEGACY_ALTER_TABLE = 1012; /* int int* */
		public const int SQLITE_DBCONFIG_DQS_DML = 1013; /* int int* */
		public const int SQLITE_DBCONFIG_DQS_DDL = 1014; /* int int* */
		public const int SQLITE_DBCONFIG_ENABLE_VIEW = 1015; /* int int* */
		public const int SQLITE_DBCONFIG_LEGACY_FILE_FORMAT = 1016; /* int int* */
		public const int SQLITE_DBCONFIG_TRUSTED_SCHEMA = 1017; /* int int* */
		public const int SQLITE_DBCONFIG_MAX = 1017; /* Largest DBCONFIG */

		public const int SQLITE_OPEN_READONLY = 0x00000001;  /* Ok for sqlite3_open_v2() */
		public const int SQLITE_OPEN_READWRITE = 0x00000002;  /* Ok for sqlite3_open_v2() */
		public const int SQLITE_OPEN_CREATE = 0x00000004;  /* Ok for sqlite3_open_v2() */
		public const int SQLITE_OPEN_DELETEONCLOSE = 0x00000008;  /* VFS only */
		public const int SQLITE_OPEN_EXCLUSIVE = 0x00000010;  /* VFS only */
		public const int SQLITE_OPEN_AUTOPROXY = 0x00000020;  /* VFS only */
		public const int SQLITE_OPEN_URI = 0x00000040;  /* Ok for sqlite3_open_v2() */
		public const int SQLITE_OPEN_MEMORY = 0x00000080;  /* Ok for sqlite3_open_v2() */
		public const int SQLITE_OPEN_MAIN_DB = 0x00000100;  /* VFS only */
		public const int SQLITE_OPEN_TEMP_DB = 0x00000200;  /* VFS only */
		public const int SQLITE_OPEN_TRANSIENT_DB = 0x00000400;  /* VFS only */
		public const int SQLITE_OPEN_MAIN_JOURNAL = 0x00000800;  /* VFS only */
		public const int SQLITE_OPEN_TEMP_JOURNAL = 0x00001000;  /* VFS only */
		public const int SQLITE_OPEN_SUBJOURNAL = 0x00002000;  /* VFS only */
		public const int SQLITE_OPEN_MASTER_JOURNAL = 0x00004000;  /* VFS only */
		public const int SQLITE_OPEN_NOMUTEX = 0x00008000;  /* Ok for sqlite3_open_v2() */
		public const int SQLITE_OPEN_FULLMUTEX = 0x00010000;  /* Ok for sqlite3_open_v2() */
		public const int SQLITE_OPEN_SHAREDCACHE = 0x00020000;  /* Ok for sqlite3_open_v2() */
		public const int SQLITE_OPEN_PRIVATECACHE = 0x00040000;  /* Ok for sqlite3_open_v2() */
		public const int SQLITE_OPEN_WAL = 0x00080000;  /* VFS only */

		public const int SQLITE_PREPARE_PERSISTENT = 0x01;
		public const int SQLITE_PREPARE_NORMALIZE = 0x02;
		public const int SQLITE_PREPARE_NO_VTAB = 0x04;

		public const int SQLITE_INTEGER = 1;
		public const int SQLITE_FLOAT = 2;
		public const int SQLITE_TEXT = 3;
		public const int SQLITE_BLOB = 4;
		public const int SQLITE_NULL = 5;

		public const int SQLITE_OK = 0;
		public const int SQLITE_ERROR = 1;
		public const int SQLITE_INTERNAL = 2;
		public const int SQLITE_PERM = 3;
		public const int SQLITE_ABORT = 4;
		public const int SQLITE_BUSY = 5;
		public const int SQLITE_LOCKED = 6;
		public const int SQLITE_NOMEM = 7;
		public const int SQLITE_READONLY = 8;
		public const int SQLITE_INTERRUPT = 9;
		public const int SQLITE_IOERR = 10;
		public const int SQLITE_CORRUPT = 11;
		public const int SQLITE_NOTFOUND = 12;
		public const int SQLITE_FULL = 13;
		public const int SQLITE_CANTOPEN = 14;
		public const int SQLITE_PROTOCOL = 15;
		public const int SQLITE_EMPTY = 16;
		public const int SQLITE_SCHEMA = 17;
		public const int SQLITE_TOOBIG = 18;
		public const int SQLITE_CONSTRAINT = 19;
		public const int SQLITE_MISMATCH = 20;
		public const int SQLITE_MISUSE = 21;
		public const int SQLITE_NOLFS = 22;
		public const int SQLITE_AUTH = 23;
		public const int SQLITE_FORMAT = 24;
		public const int SQLITE_RANGE = 25;
		public const int SQLITE_NOTADB = 26;
		public const int SQLITE_NOTICE = 27;
		public const int SQLITE_WARNING = 28;
		public const int SQLITE_ROW = 100;
		public const int SQLITE_DONE = 101;

		public const int SQLITE_IOERR_READ = (SQLITE_IOERR | (1 << 8));
		public const int SQLITE_IOERR_SHORT_READ = (SQLITE_IOERR | (2 << 8));
		public const int SQLITE_IOERR_WRITE = (SQLITE_IOERR | (3 << 8));
		public const int SQLITE_IOERR_FSYNC = (SQLITE_IOERR | (4 << 8));
		public const int SQLITE_IOERR_DIR_FSYNC = (SQLITE_IOERR | (5 << 8));
		public const int SQLITE_IOERR_TRUNCATE = (SQLITE_IOERR | (6 << 8));
		public const int SQLITE_IOERR_FSTAT = (SQLITE_IOERR | (7 << 8));
		public const int SQLITE_IOERR_UNLOCK = (SQLITE_IOERR | (8 << 8));
		public const int SQLITE_IOERR_RDLOCK = (SQLITE_IOERR | (9 << 8));
		public const int SQLITE_IOERR_DELETE = (SQLITE_IOERR | (10 << 8));
		public const int SQLITE_IOERR_BLOCKED = (SQLITE_IOERR | (11 << 8));
		public const int SQLITE_IOERR_NOMEM = (SQLITE_IOERR | (12 << 8));
		public const int SQLITE_IOERR_ACCESS = (SQLITE_IOERR | (13 << 8));
		public const int SQLITE_IOERR_CHECKRESERVEDLOCK = (SQLITE_IOERR | (14 << 8));
		public const int SQLITE_IOERR_LOCK = (SQLITE_IOERR | (15 << 8));
		public const int SQLITE_IOERR_CLOSE = (SQLITE_IOERR | (16 << 8));
		public const int SQLITE_IOERR_DIR_CLOSE = (SQLITE_IOERR | (17 << 8));
		public const int SQLITE_IOERR_SHMOPEN = (SQLITE_IOERR | (18 << 8));
		public const int SQLITE_IOERR_SHMSIZE = (SQLITE_IOERR | (19 << 8));
		public const int SQLITE_IOERR_SHMLOCK = (SQLITE_IOERR | (20 << 8));
		public const int SQLITE_IOERR_SHMMAP = (SQLITE_IOERR | (21 << 8));
		public const int SQLITE_IOERR_SEEK = (SQLITE_IOERR | (22 << 8));
		public const int SQLITE_IOERR_DELETE_NOENT = (SQLITE_IOERR | (23 << 8));
		public const int SQLITE_IOERR_MMAP = (SQLITE_IOERR | (24 << 8));
		public const int SQLITE_IOERR_GETTEMPPATH = (SQLITE_IOERR | (25 << 8));
		public const int SQLITE_IOERR_CONVPATH = (SQLITE_IOERR | (26 << 8));
		public const int SQLITE_LOCKED_SHAREDCACHE = (SQLITE_LOCKED | (1 << 8));
		public const int SQLITE_BUSY_RECOVERY = (SQLITE_BUSY | (1 << 8));
		public const int SQLITE_BUSY_SNAPSHOT = (SQLITE_BUSY | (2 << 8));
		public const int SQLITE_CANTOPEN_NOTEMPDIR = (SQLITE_CANTOPEN | (1 << 8));
		public const int SQLITE_CANTOPEN_ISDIR = (SQLITE_CANTOPEN | (2 << 8));
		public const int SQLITE_CANTOPEN_FULLPATH = (SQLITE_CANTOPEN | (3 << 8));
		public const int SQLITE_CANTOPEN_CONVPATH = (SQLITE_CANTOPEN | (4 << 8));
		public const int SQLITE_CORRUPT_VTAB = (SQLITE_CORRUPT | (1 << 8));
		public const int SQLITE_READONLY_RECOVERY = (SQLITE_READONLY | (1 << 8));
		public const int SQLITE_READONLY_CANTLOCK = (SQLITE_READONLY | (2 << 8));
		public const int SQLITE_READONLY_ROLLBACK = (SQLITE_READONLY | (3 << 8));
		public const int SQLITE_READONLY_DBMOVED = (SQLITE_READONLY | (4 << 8));
		public const int SQLITE_ABORT_ROLLBACK = (SQLITE_ABORT | (2 << 8));
		public const int SQLITE_CONSTRAINT_CHECK = (SQLITE_CONSTRAINT | (1 << 8));
		public const int SQLITE_CONSTRAINT_COMMITHOOK = (SQLITE_CONSTRAINT | (2 << 8));
		public const int SQLITE_CONSTRAINT_FOREIGNKEY = (SQLITE_CONSTRAINT | (3 << 8));
		public const int SQLITE_CONSTRAINT_FUNCTION = (SQLITE_CONSTRAINT | (4 << 8));
		public const int SQLITE_CONSTRAINT_NOTNULL = (SQLITE_CONSTRAINT | (5 << 8));
		public const int SQLITE_CONSTRAINT_PRIMARYKEY = (SQLITE_CONSTRAINT | (6 << 8));
		public const int SQLITE_CONSTRAINT_TRIGGER = (SQLITE_CONSTRAINT | (7 << 8));
		public const int SQLITE_CONSTRAINT_UNIQUE = (SQLITE_CONSTRAINT | (8 << 8));
		public const int SQLITE_CONSTRAINT_VTAB = (SQLITE_CONSTRAINT | (9 << 8));
		public const int SQLITE_CONSTRAINT_ROWID = (SQLITE_CONSTRAINT | (10 << 8));
		public const int SQLITE_NOTICE_RECOVER_WAL = (SQLITE_NOTICE | (1 << 8));
		public const int SQLITE_NOTICE_RECOVER_ROLLBACK = (SQLITE_NOTICE | (2 << 8));
		public const int SQLITE_WARNING_AUTOINDEX = (SQLITE_WARNING | (1 << 8));

		public const int SQLITE_CREATE_INDEX = 1;    /* Index Name      Table Name      */
		public const int SQLITE_CREATE_TABLE = 2;    /* Table Name      NULL            */
		public const int SQLITE_CREATE_TEMP_INDEX = 3;    /* Index Name      Table Name      */
		public const int SQLITE_CREATE_TEMP_TABLE = 4;    /* Table Name      NULL            */
		public const int SQLITE_CREATE_TEMP_TRIGGER = 5;    /* Trigger Name    Table Name      */
		public const int SQLITE_CREATE_TEMP_VIEW = 6;    /* View Name       NULL            */
		public const int SQLITE_CREATE_TRIGGER = 7;    /* Trigger Name    Table Name      */
		public const int SQLITE_CREATE_VIEW = 8;    /* View Name       NULL            */
		public const int SQLITE_DELETE = 9;    /* Table Name      NULL            */
		public const int SQLITE_DROP_INDEX = 10;   /* Index Name      Table Name      */
		public const int SQLITE_DROP_TABLE = 11;   /* Table Name      NULL            */
		public const int SQLITE_DROP_TEMP_INDEX = 12;   /* Index Name      Table Name      */
		public const int SQLITE_DROP_TEMP_TABLE = 13;   /* Table Name      NULL            */
		public const int SQLITE_DROP_TEMP_TRIGGER = 14;   /* Trigger Name    Table Name      */
		public const int SQLITE_DROP_TEMP_VIEW = 15;   /* View Name       NULL            */
		public const int SQLITE_DROP_TRIGGER = 16;   /* Trigger Name    Table Name      */
		public const int SQLITE_DROP_VIEW = 17;   /* View Name       NULL            */
		public const int SQLITE_INSERT = 18;   /* Table Name      NULL            */
		public const int SQLITE_PRAGMA = 19;   /* Pragma Name     1st arg or NULL */
		public const int SQLITE_READ = 20;   /* Table Name      Column Name     */
		public const int SQLITE_SELECT = 21;   /* NULL            NULL            */
		public const int SQLITE_TRANSACTION = 22;   /* Operation       NULL            */
		public const int SQLITE_UPDATE = 23;   /* Table Name      Column Name     */
		public const int SQLITE_ATTACH = 24;   /* Filename        NULL            */
		public const int SQLITE_DETACH = 25;   /* Database Name   NULL            */
		public const int SQLITE_ALTER_TABLE = 26;   /* Database Name   Table Name      */
		public const int SQLITE_REINDEX = 27;   /* Index Name      NULL            */
		public const int SQLITE_ANALYZE = 28;   /* Table Name      NULL            */
		public const int SQLITE_CREATE_VTABLE = 29;   /* Table Name      Module Name     */
		public const int SQLITE_DROP_VTABLE = 30;   /* Table Name      Module Name     */
		public const int SQLITE_FUNCTION = 31;   /* NULL            Function Name   */
		public const int SQLITE_SAVEPOINT = 32;   /* Operation       Savepoint Name  */
		public const int SQLITE_COPY = 0;    /* No longer used */
		public const int SQLITE_RECURSIVE = 33;   /* NULL            NULL            */

		public const int SQLITE_CHECKPOINT_PASSIVE = 0;
		public const int SQLITE_CHECKPOINT_FULL = 1;
		public const int SQLITE_CHECKPOINT_RESTART = 2;
		public const int SQLITE_CHECKPOINT_TRUNCATE = 3;

		public const int SQLITE_DBSTATUS_LOOKASIDE_USED = 0;
		public const int SQLITE_DBSTATUS_CACHE_USED = 1;
		public const int SQLITE_DBSTATUS_SCHEMA_USED = 2;
		public const int SQLITE_DBSTATUS_STMT_USED = 3;
		public const int SQLITE_DBSTATUS_LOOKASIDE_HIT = 4;
		public const int SQLITE_DBSTATUS_LOOKASIDE_MISS_SIZE = 5;
		public const int SQLITE_DBSTATUS_LOOKASIDE_MISS_FULL = 6;
		public const int SQLITE_DBSTATUS_CACHE_HIT = 7;
		public const int SQLITE_DBSTATUS_CACHE_MISS = 8;
		public const int SQLITE_DBSTATUS_CACHE_WRITE = 9;
		public const int SQLITE_DBSTATUS_DEFERRED_FKS = 10;

		public const int SQLITE_STATUS_MEMORY_USED = 0;
		public const int SQLITE_STATUS_PAGECACHE_USED = 1;
		public const int SQLITE_STATUS_PAGECACHE_OVERFLOW = 2;
		public const int SQLITE_STATUS_SCRATCH_USED = 3;
		public const int SQLITE_STATUS_SCRATCH_OVERFLOW = 4;
		public const int SQLITE_STATUS_MALLOC_SIZE = 5;
		public const int SQLITE_STATUS_PARSER_STACK = 6;
		public const int SQLITE_STATUS_PAGECACHE_SIZE = 7;
		public const int SQLITE_STATUS_SCRATCH_SIZE = 8;
		public const int SQLITE_STATUS_MALLOC_COUNT = 9;

		public const int SQLITE_STMTSTATUS_FULLSCAN_STEP = 1;
		public const int SQLITE_STMTSTATUS_SORT = 2;
		public const int SQLITE_STMTSTATUS_AUTOINDEX = 3;
		public const int SQLITE_STMTSTATUS_VM_STEP = 4;

		// Authorizer Return Codes
		public const int SQLITE_DENY = 1;   /* Abort the SQL statement with an error */
		public const int SQLITE_IGNORE = 2;   /* Don't allow access, but don't generate an error */

		public const int SQLITE_TRACE_STMT = 0x01;
		public const int SQLITE_TRACE_PROFILE = 0x02;
		public const int SQLITE_TRACE_ROW = 0x04;
		public const int SQLITE_TRACE_CLOSE = 0x08;

		#endregion constants
	}
}
