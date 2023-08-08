using SQLibre;
using SQLibre.Core;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

const int loop_count = 10_000;
const string _connectionString = @"DatabasePath=DATA\chinook.db;Provider Name=SQLibre";
SQLiteConnectionOptions connectionOptions = new SQLiteConnectionOptions(_connectionString);
SQLiteConnectionOptions connectionOptionsNoMutex = new SQLiteConnectionOptions(_connectionString)
{
	OpenFlags = (SQLiteConnectionOptions.DefaultOpenFlags & ~SQLiteOpenFlags.SQLITE_OPEN_FULLMUTEX) | SQLiteOpenFlags.SQLITE_OPEN_NOMUTEX, 
	UsingAutoCommit = true
};

Console.WriteLine("Start create database test");
const string test_db = @"DatabasePath=DATA\test.db;LifeTime=Scoped";
const string memory_db = @"DatabasePath=:memory:";
SQLiteConnectionOptions testOptions = new SQLiteConnectionOptions(test_db);

SQLiteConnection.DropDb(new SQLiteConnectionOptions(test_db) { Pooling = false });
SQLiteConnection.CreateDb(testOptions, SQLiteEncoding.Utf16);
using (var c = new SQLiteConnection(testOptions))
{
	Console.WriteLine($"Database {c.Handle.FileName()}");
	Console.WriteLine($"created with encoding {c.ExecuteScalar<string>("PRAGMA encoding;")} and journal = {c.ExecuteScalar<string>("PRAGMA journal_mode;")}");
}
SQLiteConnection.DropDb(new SQLiteConnectionOptions(test_db));
Console.WriteLine("End create database test");

string sql = @"select * from invoices order by RowId desc Limit 2;";
Console.WriteLine("Connection open test");
DateTime d = DateTime.Now;
for (int i = 0; i < loop_count; i++)
{
	using (var c = new SQLiteConnection(connectionOptionsNoMutex))
	{
		//c.Execute(sql);
	}
}
Console.WriteLine($"Finished {loop_count} calls with {(DateTime.Now - d).TotalSeconds} ms");
GC.Collect();


Console.WriteLine("Memory connection open test");
d = DateTime.Now;
for (int i = 0; i < loop_count; i++)
{
	using (var testDb = new SQLiteConnection(memory_db))
		testDb.Execute("SELECT COUNT(*) FROM sqlite_master;");
}
Console.WriteLine($"Finished {loop_count} calls with {(DateTime.Now - d).TotalSeconds} ms");
GC.Collect();

sql = @"
SELECT json_object(
	'Id', InvoiceId
	,'InvoiceDate',InvoiceDate
	,'BillingAddress',BillingAddress
	,'BillingCity',BillingCity
	,'BillingState',BillingState
	,'BillingCountry',BillingCountry
	,'BillingPostalCode',BillingPostalCode
	,'Total',Total
	,'Customer', (
		select json_object(
			'CustomerId',CustomerId
			,'FirstName',FirstName
			,'LastName',LastName
			,'Company',Company
			,'Address',Address
			,'City',City
			,'State',State
			,' Country',Country
			,'PostalCode',PostalCode
			,'Phone',Phone
			,'Fax',Fax
			,' Email',Email
			,'SupportRepId', SupportRepId
		)
		FROM customers c
		where c.CustomerId = i.CustomerId )
	,'InvoiceItems', (
		select json_group_array(json_object(
			'InvoiceLineId',InvoiceLineId
			,'TrackId',TrackId
			,'UnitPrice',UnitPrice
			,'Quantity', Quantity))
		FROM invoice_items ii
		where ii.InvoiceId = i.InvoiceId )
	) as json
FROM invoices i
where i.InvoiceId = @id
order by rowid desc
Limit 1;";

string sql2 = @"select * from playlist_track Limit @limit;";
string sql3 = @"SELECT *, rowid as RowId FROM employees Order By rowid desc limit @limit;";

JsonElement r = default;
using var db = new SQLiteConnection(connectionOptions);
using (var cmd = db.CreateCommand(sql).Bind(1, 412))
	r = cmd.ExecuteJson();

GC.Collect();
Console.WriteLine($"{nameof(SQLiteCommand)}Reference count: {SQLiteCommand.RefCount}");

Console.WriteLine("Start normal select test");
d = DateTime.Now;
db.Execute("begin transaction;");
for (int i = 0; i < loop_count; i++)
{
	using (var cmd = db.CreateCommand(sql).Bind(1, 412))
		r = cmd.ExecuteJson();
}
db.Execute("commit transaction;");
Console.WriteLine($"Finished {loop_count} calls with {(DateTime.Now - d).TotalSeconds} ms");
GC.Collect();
Console.WriteLine($"{nameof(SQLiteCommand)}Reference count: {SQLiteCommand.RefCount}");

Console.WriteLine("Start parallel select test");
d = DateTime.Now;
using var db2 = new SQLiteConnection(connectionOptions);
var t = Parallel.For(0, loop_count, i =>
{
		using (var cmd = db2.CreateCommand(sql).Bind(1, 412))
			r = cmd.ExecuteJson();
});
Console.WriteLine($"Finished {loop_count} calls with {(DateTime.Now - d).TotalSeconds} ms");
Console.WriteLine(r.GetProperty("BillingCountry").GetString());
GC.Collect();
Console.WriteLine($"{nameof(SQLiteCommand)}Reference count: {SQLiteCommand.RefCount}");

Console.WriteLine("Start select reader test");
d = DateTime.Now;
int count = 0;
using (var cmd = db2.CreateCommand(sql2).Bind("@limit", 20000))
using (SQLiteReader? r1 = cmd.ExecuteReader())
{ 
	for (; r1?.Read() ?? false;)
	{
		_ = r1.GetInt32(0);
		count++;
	}
};
Console.WriteLine($"Finished {count} calls with {(DateTime.Now - d).TotalSeconds} ms");
GC.Collect();
Console.WriteLine($"{nameof(SQLiteCommand)}Reference count: {SQLiteCommand.RefCount}");

using (var cmd = db.CreateCommand(sql3).Bind("@limit", 5))
using (SQLiteReader? r1 = cmd.ExecuteReader())
{
	for (; r1?.Read() ?? false;)
	{
		using (var source = r1.GetStream(1))
		using (var taget = new MemoryStream(Convert.ToInt32(source.Length)))
		{
			source.CopyTo(taget);
			var lastName = Encoding.Default.GetString(taget.ToArray());
			Console.WriteLine($"Id = {r1.GetInt32(0)} LastName = {lastName} FirstName = {r1.GetString(2)} Phone = {r1.GetString(12)}..");
		}
	}
}
GC.Collect();
Console.WriteLine($"{nameof(SQLiteCommand)}Reference count: {SQLiteCommand.RefCount}");


const string sql4 = @"
	create table if not exists Test (
     ID    integer     not null    primary key
     ,Name    text    not null
     ,CreationTime    datetime
 );";
const string sql5 = @"drop table if exists Test;";
const string sql7 = @"insert into Test (ID, Name, CreationTime)
	Values (@ID, @Name, @CreationTime);";


Console.WriteLine("Start bulk insert rows test");
using (var ctx = new SQLiteConnection(connectionOptions))
{
	ctx.Execute(sql5);
	ctx.Execute(sql4);
	d = DateTime.Now;
	ctx.Execute("begin transaction;");
	using (var stmt = ctx.CreateCommand(sql7))
		for (int i = 0; i < loop_count * 10; i++)
			//Parallel.For(0, loop_count * 10, i =>
			await stmt.Bind("@ID", i)
				.Bind("@Name", $"This is a name of {i}")
				.Bind("@CreationTime", DateTime.Now)
				.ExecuteAsync(CancellationToken.None);
	ctx.Execute("commit transaction;");
	var total = (DateTime.Now - d).TotalSeconds;
	count = Convert.ToInt32(ctx.CreateCommand("Select count(*) from Test").ExecuteScalar<long>());
	Console.WriteLine($"Finished {count} calls with {total} ms");
	Console.WriteLine($"{nameof(SQLiteCommand)}Reference count: {SQLiteCommand.RefCount}");

	using (var r1 = ctx.CreateCommand("Select Count(value) from json_each(?) where value = 300.14")
		//.Bind(1, new string[] { "five", "six", "seven", "eight" })
		.Bind(1, new double[] { 1.11, 2.12, 300.14, 40.99 })
		.ExecuteReader())
		if (r1.Read())
			Console.WriteLine($"CArray test return {r1.GetInt32(0)} rows count");
		else
			Console.WriteLine($"CArray test return ops(((");
	//db.Execute(sql5);
}

GC.Collect();
Console.WriteLine($"{nameof(SQLiteCommand)}Reference count: {SQLiteCommand.RefCount}");


Console.WriteLine("Start reader next result test");
using (SQLiteReader? r1 = db.CreateCommand(sql2 + sql3 + "PRAGMA encoding;" + sql2)
.Bind("@limit", 5)
.ExecuteReader())
{
	do
	{

		Console.WriteLine($"Sql Command: {r1.Sql}");
		for (; r1?.Read() ?? false;)
		{
			Console.WriteLine($"FirstField = {r1.GetInt32(0)}\tSecondField = {r1.GetInt32(1)}");
		}
	} while (r1?.NextResult() ?? false);
}
db?.Dispose();
GC.Collect();
Console.WriteLine($"{nameof(SQLiteCommand)}Reference count: {SQLiteCommand.RefCount}");
Console.ReadLine();
