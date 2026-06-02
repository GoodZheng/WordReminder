using System.IO;
using Microsoft.Data.Sqlite;
using WordReminder.Models;

namespace WordReminder.Services;

/// <summary>
/// 对话服务 - 管理对话和消息的 CRUD 操作
/// </summary>
public class ChatService
{
    private readonly string _connectionString;

    public ChatService()
    {
        var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WordReminder");
        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }
        var dbPath = Path.Combine(dataDir, "words.db");
        _connectionString = $"Data Source={dbPath}";
    }

    /// <summary>
    /// 获取某个助手的所有对话（按更新时间倒序）
    /// </summary>
    public List<Conversation> GetConversations(int assistantId)
    {
        var conversations = new List<Conversation>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"
            SELECT * FROM conversations
            WHERE AssistantId = @AssistantId
            ORDER BY UpdatedAt DESC";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@AssistantId", assistantId);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            conversations.Add(ReadConversation(reader));
        }

        return conversations;
    }

    /// <summary>
    /// 创建新对话
    /// </summary>
    public Conversation CreateConversation(int assistantId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"
            INSERT INTO conversations (AssistantId, Title)
            VALUES (@AssistantId, @Title);
            SELECT last_insert_rowid();";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@AssistantId", assistantId);
        cmd.Parameters.AddWithValue("@Title", "新对话");

        var id = Convert.ToInt32(cmd.ExecuteScalar());
        return GetConversation(id) ?? throw new InvalidOperationException("Failed to create conversation");
    }

    /// <summary>
    /// 根据 ID 获取对话
    /// </summary>
    public Conversation? GetConversation(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = "SELECT * FROM conversations WHERE Id = @Id";
        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", id);
        using var reader = cmd.ExecuteReader();

        if (reader.Read())
        {
            return ReadConversation(reader);
        }

        return null;
    }

    /// <summary>
    /// 删除对话（级联删除所有消息）
    /// </summary>
    public bool DeleteConversation(int conversationId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            // 先删除消息
            var deleteMessagesSql = "DELETE FROM chat_messages WHERE ConversationId = @ConversationId";
            using var cmd1 = new SqliteCommand(deleteMessagesSql, connection, transaction);
            cmd1.Parameters.AddWithValue("@ConversationId", conversationId);
            cmd1.ExecuteNonQuery();

            // 再删除对话
            var deleteConversationSql = "DELETE FROM conversations WHERE Id = @Id";
            using var cmd2 = new SqliteCommand(deleteConversationSql, connection, transaction);
            cmd2.Parameters.AddWithValue("@Id", conversationId);

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
    /// 获取对话的所有消息（按创建时间正序）
    /// </summary>
    public List<ChatMessage> GetMessages(int conversationId)
    {
        var messages = new List<ChatMessage>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"
            SELECT * FROM chat_messages
            WHERE ConversationId = @ConversationId
            ORDER BY CreatedAt ASC";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ConversationId", conversationId);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            messages.Add(ReadChatMessage(reader));
        }

        return messages;
    }

    /// <summary>
    /// 保存一条消息
    /// </summary>
    public ChatMessage SaveMessage(int conversationId, string role, string content)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"
            INSERT INTO chat_messages (ConversationId, Role, Content)
            VALUES (@ConversationId, @Role, @Content);
            SELECT last_insert_rowid();";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ConversationId", conversationId);
        cmd.Parameters.AddWithValue("@Role", role);
        cmd.Parameters.AddWithValue("@Content", content);

        var id = Convert.ToInt32(cmd.ExecuteScalar());

        // 更新对话的 UpdatedAt
        TouchConversation(conversationId);

        return new ChatMessage
        {
            Id = id,
            ConversationId = conversationId,
            Role = role,
            Content = content,
            CreatedAt = DateTime.Now
        };
    }

    /// <summary>
    /// 更新对话标题
    /// </summary>
    public bool UpdateConversationTitle(int conversationId, string title)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"
            UPDATE conversations SET
                Title = @Title,
                UpdatedAt = datetime('now','localtime')
            WHERE Id = @Id";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", conversationId);
        cmd.Parameters.AddWithValue("@Title", title);

        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 更新对话的最后活跃时间
    /// </summary>
    public void TouchConversation(int conversationId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = "UPDATE conversations SET UpdatedAt = datetime('now','localtime') WHERE Id = @Id";
        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", conversationId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 获取对话的消息数量
    /// </summary>
    public int GetMessageCount(int conversationId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = "SELECT COUNT(*) FROM chat_messages WHERE ConversationId = @ConversationId";
        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ConversationId", conversationId);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>
    /// 从 SqliteDataReader 读取 Conversation 对象
    /// </summary>
    private Conversation ReadConversation(SqliteDataReader reader)
    {
        return new Conversation
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            AssistantId = reader.GetInt32(reader.GetOrdinal("AssistantId")),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        };
    }

    /// <summary>
    /// 从 SqliteDataReader 读取 ChatMessage 对象
    /// </summary>
    private ChatMessage ReadChatMessage(SqliteDataReader reader)
    {
        return new ChatMessage
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            ConversationId = reader.GetInt32(reader.GetOrdinal("ConversationId")),
            Role = reader.GetString(reader.GetOrdinal("Role")),
            Content = reader.GetString(reader.GetOrdinal("Content")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}
