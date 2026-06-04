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
    /// <returns>新记录返回其 ID；更新已有记录返回其 ID；无操作返回 -1</returns>
    public int Insert(string inputText, string? translatedText, string? fullJson, string? textType, string? direction)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // 检查是否已存在相同输入文本的记录
        var checkSql = "SELECT Id FROM TranslationHistory WHERE InputText = @InputText ORDER BY CreatedAt DESC LIMIT 1";
        using var checkCmd = new SqliteCommand(checkSql, connection);
        checkCmd.Parameters.AddWithValue("@InputText", inputText);
        var existingId = checkCmd.ExecuteScalar();

        if (existingId != null)
        {
            // 更新已有记录的内容和时间戳，使其排到最前面
            var updateSql = @"
                UPDATE TranslationHistory
                SET TranslatedText = @TranslatedText,
                    FullJson = @FullJson,
                    TextType = @TextType,
                    Direction = @Direction,
                    CreatedAt = CURRENT_TIMESTAMP
                WHERE Id = @Id";

            using var updateCmd = new SqliteCommand(updateSql, connection);
            updateCmd.Parameters.AddWithValue("@TranslatedText", translatedText ?? (object)DBNull.Value);
            updateCmd.Parameters.AddWithValue("@FullJson", fullJson ?? (object)DBNull.Value);
            updateCmd.Parameters.AddWithValue("@TextType", textType ?? (object)DBNull.Value);
            updateCmd.Parameters.AddWithValue("@Direction", direction ?? (object)DBNull.Value);
            updateCmd.Parameters.AddWithValue("@Id", existingId);
            updateCmd.ExecuteNonQuery();

            return Convert.ToInt32(existingId);
        }

        // 不存在，插入新记录
        var insertSql = @"
            INSERT INTO TranslationHistory (InputText, TranslatedText, FullJson, TextType, Direction)
            VALUES (@InputText, @TranslatedText, @FullJson, @TextType, @Direction);
            SELECT last_insert_rowid();";

        using var insertCmd = new SqliteCommand(insertSql, connection);
        insertCmd.Parameters.AddWithValue("@InputText", inputText);
        insertCmd.Parameters.AddWithValue("@TranslatedText", translatedText ?? (object)DBNull.Value);
        insertCmd.Parameters.AddWithValue("@FullJson", fullJson ?? (object)DBNull.Value);
        insertCmd.Parameters.AddWithValue("@TextType", textType ?? (object)DBNull.Value);
        insertCmd.Parameters.AddWithValue("@Direction", direction ?? (object)DBNull.Value);

        return Convert.ToInt32(insertCmd.ExecuteScalar());
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
