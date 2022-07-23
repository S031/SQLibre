using SQLibre;
using SQLibre.Core;
using System.Text.Json;

const int loop_count = 10_000;
const string _connectionString = @"DatabasePath=DATA\chinook.db";
SQLiteConnectionOptions connectionOptions = new SQLiteConnectionOptions(_connectionString);
SQLiteConnectionOptions connectionOptionsNoMutex = new SQLiteConnectionOptions(_connectionString)
{
	OpenFlags = (SQLiteConnectionOptions.DefaultOpenFlags & ~SQLiteOpenFlags.SQLITE_OPEN_FULLMUTEX) | SQLiteOpenFlags.SQLITE_OPEN_NOMUTEX, 
	UsingAutoCommit = true
};

Console.WriteLine("Start create database test");
const string test_db = @"DatabasePath=DATA\test.db;LifeTime=Scoped";
SQLiteConnectionOptions testOptions = new SQLiteConnectionOptions(test_db);

SQLiteConnection.DropDb(new SQLiteConnectionOptions(test_db));
SQLiteConnection.CreateDb(testOptions, SQLiteEncoding.Utf16);
var c = new SQLiteConnection(testOptions);
c.Using(ctx=>
{
	Console.WriteLine($"Database {c.Handle.FileName()}");
	Console.WriteLine($"created with encoding {ctx.ExecuteScalar<string>("PRAGMA encoding;")} and journal = {ctx.ExecuteScalar<string>("PRAGMA journal_mode;")}");
});
SQLiteConnection.DropDb(new SQLiteConnectionOptions(test_db));
Console.WriteLine("End create database test");

string sql = @"select * from invoices order by RowId desc Limit 2;";
Console.WriteLine("Connection open test");
DateTime d = DateTime.Now;
	new SQLiteConnection(connectionOptionsNoMutex).Using(ctx =>
	{
		for (int i = 0; i < loop_count; i++)
		{
			ctx.Execute(sql);
		}
	});
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
string sql3 = @"SELECT * FROM employees Order By rowid desc limit @limit;";

JsonElement r = default;
var db = new SQLiteConnection(connectionOptions);

db.Using(ctx =>
	{
		using (var cmd = ctx.CreateCommand(sql).Bind(1, 412))
			r = cmd.ExecuteJson();
	});
GC.Collect();
Console.WriteLine($"{nameof(SQLiteCommand)}Reference count: {SQLiteCommand.RefCount}");

Console.WriteLine("Start normal select test");
d = DateTime.Now;
db.Execute("begin transaction;");
for (int i = 0; i < loop_count; i++)
{
	db.Using(ctx =>
	{
		using (var cmd = ctx.CreateCommand(sql).Bind(1, 412))
			r = cmd.ExecuteJson();
	});
}
db.Execute("commit transaction;");
Console.WriteLine($"Finished {loop_count} calls with {(DateTime.Now - d).TotalSeconds} ms");
GC.Collect();
Console.WriteLine($"{nameof(SQLiteCommand)}Reference count: {SQLiteCommand.RefCount}");

Console.WriteLine("Start parallel select test");
d = DateTime.Now;
var db2 = new SQLiteConnection(connectionOptions);
var t = Parallel.For(0, loop_count, i =>
{
	db2.Using(ctx =>
	{
		using (var cmd = ctx.CreateCommand(sql).Bind(1, 412))
			r = cmd.ExecuteJson();
	});
});
Console.WriteLine($"Finished {loop_count} calls with {(DateTime.Now - d).TotalSeconds} ms");
Console.WriteLine(r.GetProperty("BillingCountry").GetString());
GC.Collect();
Console.WriteLine($"{nameof(SQLiteCommand)}Reference count: {SQLiteCommand.RefCount}");

Console.WriteLine("Start select reader test");
d = DateTime.Now;
int count = 0;
db2 = new SQLiteConnection(connectionOptionsNoMutex);
db2.Using(ctx =>
{
	using (SQLiteReader? r1 = ctx.CreateCommand(sql2)
		.Bind("@limit", 20000)
		.ExecuteReader())
		for (; r1?.Read() ?? false;)
		{
			_ = r1.GetInt32(0);
			count++;
		}
});
Console.WriteLine($"Finished {count} calls with {(DateTime.Now - d).TotalSeconds} ms");
GC.Collect();
Console.WriteLine($"{nameof(SQLiteCommand)}Reference count: {SQLiteCommand.RefCount}");

db.Using(ctx =>
{
	using (SQLiteReader? r1 = ctx.ExecuteReader(sql3, "@limit", 5))
	{
		for (; r1?.Read() ?? false;)
			Console.WriteLine($"Id = {r1.GetInt32(0)} LastName = {r1.GetString(1)} FirstName = {r1.GetString(2)} Phone = {r1.GetString(12)}..");
	}
});
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
db.Using(async db =>
{
	db.Execute(sql5);
	db.Execute(sql4);
	d = DateTime.Now;
	db.Execute("begin transaction;");
	using (var stmt = db.CreateCommand(sql7))
		for (int i = 0; i < loop_count * 10; i++)
			//Parallel.For(0, loop_count * 10, i =>
			await stmt.Bind("@ID", i)
				.Bind("@Name", $"This is a name of {i}")
				.Bind("@CreationTime", DateTime.Now)
				.ExecuteAsync(CancellationToken.None);
	db.Execute("commit transaction;");
	var total = (DateTime.Now - d).TotalSeconds;
	count = Convert.ToInt32(db.ExecuteScalar<long>("Select count(*) from Test"));
	Console.WriteLine($"Finished {count} calls with {total} ms");
	Console.WriteLine($"{nameof(SQLiteCommand)}Reference count: {SQLiteCommand.RefCount}");

	using (var r1 = db.CreateCommand("Select Count(value) from json_each(?) where value = 300.14")
		//.Bind(1, new string[] { "five", "six", "seven", "eight" })
		.Bind(1, new double[] { 1.11, 2.12, 300.14, 40.99 })
		.ExecuteReader())
		if (r1.Read())
			Console.WriteLine($"CArray test return {r1.GetInt32(0)} rows count");
		else
			Console.WriteLine($"CArray test return ops(((");
	//db.Execute(sql5);
});

GC.Collect();
Console.WriteLine($"{nameof(SQLiteCommand)}Reference count: {SQLiteCommand.RefCount}");


Console.WriteLine("Start reader next result test");
new SQLiteConnection(connectionOptions).Using(ctx =>
{
	using (SQLiteReader? r1 = ctx
	.CreateCommand(sql2 + sql3 + "PRAGMA encoding;" + sql2)
	.Bind("@limit", 5)
	.ExecuteReader())
	{
		do
		{
			for (; r1?.Read() ?? false;)
			{
				Console.WriteLine($"FirstField = {r1.GetInt32(0)}\tSecondField = {r1.GetInt32(1)}");
			}
		} while (r1?.NextResult() ?? false);
	}
});
GC.Collect();
Console.WriteLine($"{nameof(SQLiteCommand)}Reference count: {SQLiteCommand.RefCount}");
Console.ReadLine();
