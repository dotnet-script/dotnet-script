#! "netcoreapp2.0"
#r "nuget:NetStandard.Library,2.0.0"
#r "nuget:System.Data.SqlClient, 4.4.0"

using System.Data.SqlClient;


using (var connection = new SqlConnection(@"Server=tcp:dotnet-script.database.windows.net,1433;Initial Catalog=Sample;Persist Security Info=False;User ID=readonlylogin;Password=ztygpRhfjgej0we|dxHfxybhmsFT7_&#$!~<!n5,lfvtButr;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"))
{
    connection.Open();
    Console.WriteLine("Connection successful");
}