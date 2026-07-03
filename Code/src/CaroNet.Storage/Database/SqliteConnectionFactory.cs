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
            DataSource = fullPath,
            DefaultTimeout = 5 // Giữ lại dòng này để cấu hình Busy Timeout thành công
        };

        return builder.ToString();
    }
}