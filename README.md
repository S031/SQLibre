## SQLibre - Simple, lightweigt .NET wrapper for sqlite3.dll. 
Contains three main classes:
- [SQLIteConnection](https://github.com/S031/SQLibre/blob/master/SQLibre/Common/SQLiteConnection.cs)
- [SQLIteCommand](https://github.com/S031/SQLibre/blob/master/SQLibre/Common/SQLIteCommand.cs)
- [SQLiteReader](https://github.com/S031/SQLibre/blob/master/SQLibre/Common/SQLiteReader.cs)

with ADO.NET-like functionality

The full source code of the examples on this page can be viewed 
[at the link](https://github.com/S031/SQLibre/blob/master/SQLibre.TestConsole/Program.cs)

**Connection configuration.**
```csharp
const int loop_count = 10_000;
const string _connectionString = @"DatabasePath=DATA\chinook.db";
SQLiteConnectionOptions connectionOptions = new SQLiteConnectionOptions(_connectionString);
SQLiteConnectionOptions connectionOptionsNoMutex = new SQLiteConnectionOptions(_connectionString)
{
	OpenFlags = (SQLiteConnectionOptions.DefaultOpenFlags & ~SQLiteOpenFlags.SQLITE_OPEN_FULLMUTEX) | SQLiteOpenFlags.SQLITE_OPEN_NOMUTEX, 
	UsingAutoCommit = true
};
```
**Creating a new database.**
```csharp
Console.WriteLine("Start create database test");
const string test_db = @"DatabasePath=DATA\test.db;LifeTime=Scoped";
SQLiteConnectionOptions testOptions = new SQLiteConnectionOptions(test_db);

SQLiteConnection.DropDb(new SQLiteConnectionOptions(test_db));
SQLiteConnection.CreateDb(testOptions, SQLiteEncoding.Utf16);
var c = new SQLiteConnection(testOptions);
using (var c = new SQLiteConnection(testOptions))
{
	Console.WriteLine($"Database {c.Handle.FileName()}");
	Console.WriteLine($"created with encoding {c.ExecuteScalar<string>("PRAGMA encoding;")} and journal = {c.ExecuteScalar<string>("PRAGMA journal_mode;")}");
}
SQLiteConnection.DropDb(new SQLiteConnectionOptions(test_db));
Console.WriteLine("End create database test");
```

**Execute simple select statement.**
```csharp
string sql = @"select * from invoices order by RowId desc Limit 2;";
Console.WriteLine("Connection open test");
DateTime d = DateTime.Now;
for (int i = 0; i < loop_count; i++)
{
	using (var c = new SQLiteConnection(connectionOptionsNoMutex))
	{
		c.Execute(sql);
	}
}
Console.WriteLine($"Finished {loop_count} calls with {(DateTime.Now - d).TotalSeconds} ms");
```

**Select json formatted data with query parameters.**

Sql query based on `json_object`,`json_group_array` SQLite json functions

```csharp
string sql = @"
```
```sql
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
Limit 1;
```
```csharp
";
JsonElement r = default;
using (var db = new SQLiteConnection(connectionOptions))
using (var cmd = db.CreateCommand(sql).Bind(1, 412))
	r = cmd.ExecuteJson();
```
**Execute reader.**
```csharp
string sql2 = @"select * from playlist_track Limit @limit;";
int count = 0;
Console.WriteLine("Start select reader test");
int count = 0;

using (db2 = new SQLiteConnection(connectionOptionsNoMutex))
using (var cmd = db2.CreateCommand(sql2).Bind("@limit", 20000))
using (SQLiteReader? r1 = cmd.ExecuteReader())
{ 
	for (; r1?.Read() ?? false;)
	{
		_ = r1.GetInt32(0);
		count++;
	}
};
```

**Array type parameters binding.**

Now supported `string`, `integer` or `double` array. 
```csharp
using (var db = new SQLiteConnection(connectionOptions))
using (var r1 = db.CreateCommand("Select Count(value) from json_each(?) where value = 300.14")
	//.Bind(1, new string[] { "five", "six", "seven", "eight" })
	.Bind(1, new double[] { 1.11, 2.12, 300.14, 40.99 })
	.ExecuteReader())
	if (r1.Read())
		Console.WriteLine($"CArray test return {r1.GetInt32(0)} rows count");
	else
		Console.WriteLine($"CArray test return ops(((");
```

**Bulk insert and batch of statements execute**

```csharp
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
using (var db = new SQLiteConnection(connectionOptions))
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

	db.Execute(sql5);
}
```
**reader next result**
```csharp
string sql2 = @"select * from playlist_track Limit @limit;";
string sql3 = @"SELECT *, rowid as RowId FROM employees Order By rowid desc limit @limit;";

Console.WriteLine("Start reader next result test");
using (var db = new SQLiteConnection(connectionOptions))
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

```
