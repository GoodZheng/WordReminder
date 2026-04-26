using System.IO;
using Microsoft.Data.Sqlite;
using WordReminder.Models;

namespace WordReminder.Services;

/// <summary>
/// 翻译历史记录服务 - 持久化翻译历史到 SQLite
/// </summary>
public class TranslationHistoryService
{
    private readonly string _connectionString;

    public TranslationHistoryService()
    {
        var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WordReminder");
        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }
        var dbPath = Path.Combine(dataDir, "words.db");
        _connectionString = $"Data Source={dbPath}";
        InitializeTable();
    }

    private void InitializeTable()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"
            CREATE TABLE IF NOT EXISTS TranslationHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                InputText TEXT NOT NULL,
                TranslatedText TEXT,
                FullJson TEXT,
                TextType TEXT,
                Direction TEXT,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            )";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 插入一条翻译历史
    /// </summary>
    public int Insert(string inputText, string? translatedText, string? fullJson, string? textType, string? direction)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"
            INSERT INTO TranslationHistory (InputText, TranslatedText, FullJson, TextType, Direction)
            VALUES (@InputText, @TranslatedText, @FullJson, @TextType, @Direction);
            SELECT last_insert_rowid();";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@InputText", inputText);
        cmd.Parameters.AddWithValue("@TranslatedText", translatedText ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@FullJson", fullJson ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@TextType", textType ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Direction", direction ?? (object)DBNull.Value);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>
    /// 分页查询历史记录（按时间倒序）
    /// </summary>
    public (List<TranslationHistoryEntry> Items, int Total) GetPaged(int page, int pageSize)
    {
        var offset = (page - 1) * pageSize;

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // 查询总数
        using var countCmd = new SqliteCommand("SELECT COUNT(*) FROM TranslationHistory", connection);
        var total = Convert.ToInt32(countCmd.ExecuteScalar());

        // 查询分页数据
        var sql = @"
            SELECT Id, InputText, TranslatedText, FullJson, TextType, Direction, CreatedAt
            FROM TranslationHistory
            ORDER BY CreatedAt DESC
            LIMIT @PageSize OFFSET @Offset";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);
        cmd.Parameters.AddWithValue("@Offset", offset);

        var items = new List<TranslationHistoryEntry>();
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            items.Add(new TranslationHistoryEntry
            {
                Id = reader.GetInt32(0),
                InputText = reader.GetString(1),
                TranslatedText = reader.IsDBNull(2) ? null : reader.GetString(2),
                FullJson = reader.IsDBNull(3) ? null : reader.GetString(3),
                TextType = reader.IsDBNull(4) ? null : reader.GetString(4),
                Direction = reader.IsDBNull(5) ? null : reader.GetString(5),
                CreatedAt = reader.GetDateTime(6)
            });
        }

        return (items, total);
    }

    /// <summary>
    /// 获取历史记录总数
    /// </summary>
    public int GetTotalCount()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = new SqliteCommand("SELECT COUNT(*) FROM TranslationHistory", connection);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>
    /// 删除一条历史记录
    /// </summary>
    public bool Delete(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = "DELETE FROM TranslationHistory WHERE Id = @Id";
        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", id);

        return cmd.ExecuteNonQuery() > 0;
    }
}
