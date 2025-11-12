#r "nuget:Microsoft.Data.SQLite, 9.0.10"
using Microsoft.Data.Sqlite;


using (var connection = new SqliteConnection("Data Source=:memory:"))
{
    connection.Open();
    Console.WriteLine("Connection successful");
}