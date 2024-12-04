using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

public class DynamicSQLToCSharp
{
    public static string GenerateCSharpCode(string connectionString, string tableFullName, Type modelType)
    {
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            // Extract schema and table name
            var schemaAndTable = tableFullName.Split('.');
            string schema = schemaAndTable.Length == 2 ? schemaAndTable[0] : null;
            string tableName = schemaAndTable.Length == 2 ? schemaAndTable[1] : tableFullName;

            // Get identity columns
            var identityColumns = GetIdentityColumns(connection, schema, tableName);

            // Query the table data
            string query = $"SELECT * FROM {tableFullName}";
            SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
            DataTable table = new DataTable();
            adapter.Fill(table);

            // Get property names from the model
            var modelProperties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                           .ToDictionary(p => p.Name.ToLower(), p => p.Name);

            StringBuilder result = new StringBuilder();

            foreach (DataRow row in table.Rows)
            {
                result.AppendLine($"new {modelType.Name}");
                result.AppendLine("{");

                foreach (DataColumn column in table.Columns)
                {
                    string columnName = column.ColumnName.ToLower();

                    // Skip identity columns
                    if (identityColumns.Contains(columnName))
                        continue;

                    // Match column name to model property (case-insensitive)
                    if (modelProperties.TryGetValue(columnName, out string propertyName))
                    {
                        string propertyValue = FormatValue(row[column], column.DataType);
                        result.AppendLine($"    {propertyName} = {propertyValue},");
                    }
                }

                result.AppendLine("},");
            }

            return result.ToString();
        }
    }

    private static HashSet<string> GetIdentityColumns(SqlConnection connection, string schema, string tableName)
    {
        HashSet<string> identityColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string query = $@"
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @TableName 
              AND COLUMNPROPERTY(OBJECT_ID(QUOTENAME(@Schema) + '.' + QUOTENAME(@TableName)), COLUMN_NAME, 'IsIdentity') = 1";

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Schema", schema ?? "dbo");  // Default schema is 'dbo' if not provided
            command.Parameters.AddWithValue("@TableName", tableName);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    identityColumns.Add(reader.GetString(0)); // Add the identity column names
                }
            }
        }

        return identityColumns;
    }

    private static string FormatValue(object value, Type type)
    {
        if (value == DBNull.Value)
            return "null";

        if (type == typeof(string) || type == typeof(Guid))
            return $"\"{value}\"";

        if (type == typeof(DateTime))
        {
            DateTime date = (DateTime)value;
            return $"new DateTime({date.Year}, {date.Month}, {date.Day}, {date.Hour}, {date.Minute}, {date.Second})";
        }

        if (type == typeof(bool))
            return value.ToString().ToLower();

        if (type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long) ||
            type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return Convert.ToString(value, CultureInfo.InvariantCulture);

        return value.ToString();
    }
}
