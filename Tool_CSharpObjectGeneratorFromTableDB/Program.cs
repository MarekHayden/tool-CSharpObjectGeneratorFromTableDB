using BtW2Database;

string connectionString = "Data Source=T-114-230-023,60000;Initial Catalog=BTW2;Integrated Security=True; TrustServerCertificate=True;Encrypt=true;";
string tableName = "Business.ActionType";

string csharpCode = DynamicSQLToCSharp.GenerateCSharpCode(connectionString, tableName, typeof(ActionType));
Console.WriteLine(csharpCode);