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
    public static string GenerateCSharpCode(string connectionString, string tableName, Type modelType)
    {
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            // Fetch table data
            string query = $"SELECT * FROM {tableName}";
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
                    // Match column name to model property (case-insensitive)
                    string columnName = column.ColumnName.ToLower();
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
