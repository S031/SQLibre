## SQLibre - Simple, lightweigt .NET wrapper for sqlite3.dll. 
Contains three main classes:
- [SQLIteConnection](https://github.com/S031/SQLibre/blob/master/SQLibre/Common/SQLiteConnection.cs)
- [SQLIteCommand](https://github.com/S031/SQLibre/blob/master/SQLibre/Common/SQLiteCommand.cs)
- [SQLiteReader](https://github.com/S031/SQLibre/blob/master/SQLibre/Common/SQLiteReader.cs)

with ADO.NET-like functionality

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
c.Using(ctx=>
{
	Console.WriteLine($"Database {c.Handle.FileName()}");
	Console.WriteLine($"created with encoding {ctx.ExecuteScalar<string>("PRAGMA encoding;")} and journal = {ctx.ExecuteScalar<string>("PRAGMA journal_mode;")}");
});
SQLiteConnection.DropDb(new SQLiteConnectionOptions(test_db));
Console.WriteLine("End create database test");
```

**Execute simple select statement.**
```csharp
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
```

**Select json formatted data with query parameters.**

Sql query based on `json_object`,`json_group_array` SQLite json functions
```csharp
const string _connectionString = @"DatabasePath=DATA\chinook.db";
string sql = @"
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

JsonElement r = default;
var db = new SQLiteConnection(connectionOptions);

db.Using(ctx =>
	{
		using (var cmd = ctx.CreateCommand(sql).Bind(1, 412))
			r = cmd.ExecuteJson();
	});
```
**Execute reader.**
```csharp
const string _connectionString = @"DatabasePath=DATA\chinook.db";
string sql2 = @"select * from playlist_track Limit @limit;";
int count = 0;
Console.WriteLine("Start select reader test");
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
```

**Array type parameters binding.**

Now supported `string`, `integer` or `double` array. 
```csharp
db.Using(async ctx =>
{
	using (var r1 = ctx.CreateCommand("Select Count(value) from json_each(?) where value = 300.14")
		.Bind(1, new double[] { 1.11, 2.12, 300.14, 40.99 })
		.ExecuteReader())
		if (r1.Read())
			Console.WriteLine($"Array test return {r1.GetInt32(0)} rows count");
		else
			Console.WriteLine($"Array test return ops(((");
});
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
db.Using(async ctx =>
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
	count = Convert.ToInt32(ctx.ExecuteScalar<long>("Select count(*) from Test"));
	Console.WriteLine($"Finished {count} calls with {total} ms");
	Console.WriteLine($"{nameof(SQLiteCommand)}Reference count: {SQLiteCommand.RefCount}");

	db.Execute(sql5);
});
```
