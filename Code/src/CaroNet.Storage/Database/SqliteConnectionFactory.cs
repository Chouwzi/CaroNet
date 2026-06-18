using Microsoft.Data.Sqlite;

namespace CaroNet.Storage.Database;

internal static class SqliteConnectionFactory
{
    public static string CreateConnectionString(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Đường dẫn database không được để trống.", nameof(databasePath));
        }

        string fullPath = Path.GetFullPath(databasePath);
        string? directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath
        };

        return builder.ToString();
    }
}
