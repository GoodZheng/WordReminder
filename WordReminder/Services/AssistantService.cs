using System.IO;
using Microsoft.Data.Sqlite;
using WordReminder.Models;

namespace WordReminder.Services;

/// <summary>
/// AI 助手服务 - 管理助手的 CRUD 操作
/// </summary>
public class AssistantService
{
    private readonly string _connectionString;

    public AssistantService()
    {
        var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WordReminder");
        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }
        var dbPath = Path.Combine(dataDir, "words.db");
        _connectionString = $"Data Source={dbPath}";
        EnsureBuiltinAssistants();
    }

    /// <summary>
    /// 获取所有内置助手的定义
    /// </summary>
    public static List<Assistant> GetBuiltinDefinitions()
    {
        return new List<Assistant>
        {
            new Assistant
            {
                Name = "英语翻译助手",
                Icon = "📖",
                SystemPrompt = "你是一个专业的英语翻译助手。用户会发送中文或英文文本，你需要提供准确、自然的翻译。对于单词，提供音标、词性、释义和例句。对于句子，提供多种翻译方式和适用场景。",
                Temperature = 0.5,
                IsBuiltin = true
            },
            new Assistant
            {
                Name = "口语练习助手",
                Icon = "🗣️",
                SystemPrompt = "你是一个英语口语练习助手。通过对话帮助用户练习日常英语口语。使用简单自然的表达，主动纠正语法错误，提供更地道的说法。每次回复尽量简短，鼓励用户继续对话。",
                Temperature = 0.7,
                IsBuiltin = true
            },
            new Assistant
            {
                Name = "写作批改助手",
                Icon = "✍️",
                SystemPrompt = "你是一个英语写作批改助手。用户会提交英语写作内容，请从语法、用词、结构、逻辑等方面进行批改。先指出问题，再给出修改建议和改进后的版本。保持鼓励性的语气。",
                Temperature = 0.5,
                IsBuiltin = true
            }
        };
    }

    /// <summary>
    /// 确保内置助手存在，不存在则创建
    /// </summary>
    private void EnsureBuiltinAssistants()
    {
        var builtins = GetBuiltinDefinitions();
        var existing = GetAllAssistants().Where(a => a.IsBuiltin).ToList();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        foreach (var builtin in builtins)
        {
            var existingBuiltin = existing.FirstOrDefault(a => a.Name == builtin.Name);
            if (existingBuiltin == null)
            {
                var sql = @"
                    INSERT INTO assistants (Name, Icon, SystemPrompt, Temperature, IsBuiltin)
                    VALUES (@Name, @Icon, @SystemPrompt, @Temperature, @IsBuiltin)";

                using var cmd = new SqliteCommand(sql, connection);
                cmd.Parameters.AddWithValue("@Name", builtin.Name);
                cmd.Parameters.AddWithValue("@Icon", builtin.Icon);
                cmd.Parameters.AddWithValue("@SystemPrompt", builtin.SystemPrompt);
                cmd.Parameters.AddWithValue("@Temperature", builtin.Temperature);
                cmd.Parameters.AddWithValue("@IsBuiltin", 1);
                cmd.ExecuteNonQuery();
            }
        }
    }

    /// <summary>
    /// 获取所有助手
    /// </summary>
    public List<Assistant> GetAllAssistants()
    {
        var assistants = new List<Assistant>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = "SELECT * FROM assistants ORDER BY IsBuiltin DESC, CreatedAt DESC";
        using var cmd = new SqliteCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            assistants.Add(ReadAssistant(reader));
        }

        return assistants;
    }

    /// <summary>
    /// 根据 ID 获取助手
    /// </summary>
    public Assistant? GetAssistant(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = "SELECT * FROM assistants WHERE Id = @Id";
        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", id);
        using var reader = cmd.ExecuteReader();

        if (reader.Read())
        {
            return ReadAssistant(reader);
        }

        return null;
    }

    /// <summary>
    /// 创建新助手
    /// </summary>
    public Assistant CreateAssistant(Assistant assistant)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"
            INSERT INTO assistants (Name, Icon, SystemPrompt, ProviderName, ModelId, Temperature, MaxTokens, IsBuiltin)
            VALUES (@Name, @Icon, @SystemPrompt, @ProviderName, @ModelId, @Temperature, @MaxTokens, @IsBuiltin);
            SELECT last_insert_rowid();";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Name", assistant.Name);
        cmd.Parameters.AddWithValue("@Icon", assistant.Icon);
        cmd.Parameters.AddWithValue("@SystemPrompt", assistant.SystemPrompt);
        cmd.Parameters.AddWithValue("@ProviderName", assistant.ProviderName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ModelId", assistant.ModelId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Temperature", assistant.Temperature);
        cmd.Parameters.AddWithValue("@MaxTokens", assistant.MaxTokens);
        cmd.Parameters.AddWithValue("@IsBuiltin", assistant.IsBuiltin ? 1 : 0);

        var id = Convert.ToInt32(cmd.ExecuteScalar());
        assistant.Id = id;
        return assistant;
    }

    /// <summary>
    /// 更新助手
    /// </summary>
    public bool UpdateAssistant(Assistant assistant)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"
            UPDATE assistants SET
                Name = @Name,
                Icon = @Icon,
                SystemPrompt = @SystemPrompt,
                ProviderName = @ProviderName,
                ModelId = @ModelId,
                Temperature = @Temperature,
                MaxTokens = @MaxTokens,
                UpdatedAt = datetime('now','localtime')
            WHERE Id = @Id";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", assistant.Id);
        cmd.Parameters.AddWithValue("@Name", assistant.Name);
        cmd.Parameters.AddWithValue("@Icon", assistant.Icon);
        cmd.Parameters.AddWithValue("@SystemPrompt", assistant.SystemPrompt);
        cmd.Parameters.AddWithValue("@ProviderName", assistant.ProviderName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ModelId", assistant.ModelId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Temperature", assistant.Temperature);
        cmd.Parameters.AddWithValue("@MaxTokens", assistant.MaxTokens);

        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 删除助手（级联删除其所有对话和消息）
    /// </summary>
    public bool DeleteAssistant(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            // 删除所有对话的消息
            var deleteMessagesSql = @"
                DELETE FROM chat_messages
                WHERE ConversationId IN (
                    SELECT Id FROM conversations WHERE AssistantId = @AssistantId
                )";
            using (var cmd = new SqliteCommand(deleteMessagesSql, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@AssistantId", id);
                cmd.ExecuteNonQuery();
            }

            // 删除所有对话
            var deleteConvsSql = "DELETE FROM conversations WHERE AssistantId = @AssistantId";
            using (var cmd = new SqliteCommand(deleteConvsSql, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@AssistantId", id);
                cmd.ExecuteNonQuery();
            }

            // 删除助手
            var deleteSql = "DELETE FROM assistants WHERE Id = @Id";
            using var cmd2 = new SqliteCommand(deleteSql, connection, transaction);
            cmd2.Parameters.AddWithValue("@Id", id);
            var rowsAffected = cmd2.ExecuteNonQuery();

            transaction.Commit();
            return rowsAffected > 0;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 获取助手的对话数量
    /// </summary>
    public int GetConversationCount(int assistantId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = "SELECT COUNT(*) FROM conversations WHERE AssistantId = @AssistantId";
        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@AssistantId", assistantId);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>
    /// 获取助手最后活跃时间（最后一条对话的 UpdatedAt）
    /// </summary>
    public DateTime? GetLastActiveTime(int assistantId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"
            SELECT MAX(UpdatedAt) FROM conversations
            WHERE AssistantId = @AssistantId";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@AssistantId", assistantId);

        var result = cmd.ExecuteScalar();
        if (result != null && result != DBNull.Value)
        {
            return Convert.ToDateTime(result);
        }

        return null;
    }

    /// <summary>
    /// 从 SqliteDataReader 读取 Assistant 对象
    /// </summary>
    private Assistant ReadAssistant(SqliteDataReader reader)
    {
        return new Assistant
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Icon = reader.GetString(reader.GetOrdinal("Icon")),
            SystemPrompt = reader.GetString(reader.GetOrdinal("SystemPrompt")),
            ProviderName = reader.IsDBNull(reader.GetOrdinal("ProviderName")) ? null! : reader.GetString(reader.GetOrdinal("ProviderName")),
            ModelId = reader.IsDBNull(reader.GetOrdinal("ModelId")) ? null! : reader.GetString(reader.GetOrdinal("ModelId")),
            Temperature = reader.GetDouble(reader.GetOrdinal("Temperature")),
            MaxTokens = reader.GetInt32(reader.GetOrdinal("MaxTokens")),
            IsBuiltin = reader.GetInt32(reader.GetOrdinal("IsBuiltin")) == 1,
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        };
    }
}
