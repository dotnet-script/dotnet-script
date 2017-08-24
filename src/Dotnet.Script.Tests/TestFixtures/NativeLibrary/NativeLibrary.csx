#r "nuget:Microsoft.Data.SQLite, 1.1.0"
using Microsoft.Data.Sqlite;


using (var connection = new SqliteConnection("Data Source=:memory:"))
{
    connection.Open();
    Console.WriteLine("Connection successful");
}