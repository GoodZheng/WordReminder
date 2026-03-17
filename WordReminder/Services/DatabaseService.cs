using Microsoft.Data.Sqlite;
using System.IO;
using WordReminder.Models;

namespace WordReminder.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly string _dbPath;

    public DatabaseService()
    {
        _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "words.db");
        _connectionString = $"Data Source={_dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS Words (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Text TEXT NOT NULL UNIQUE,
                Phonetic TEXT,
                PartOfSpeech TEXT,
                Definition TEXT,
                Example TEXT,
                DisplayOrder INTEGER DEFAULT 0,
                CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP
            )";

        using var cmd = new SqliteCommand(createTableSql, connection);
        cmd.ExecuteNonQuery();

        // 检查是否需要添加 DisplayOrder 列（兼容旧数据库）
        try
        {
            using var checkCmd = new SqliteCommand("SELECT DisplayOrder FROM Words LIMIT 1", connection);
            checkCmd.ExecuteScalar();
        }
        catch
        {
            using var addColumnCmd = new SqliteCommand("ALTER TABLE Words ADD COLUMN DisplayOrder INTEGER DEFAULT 0", connection);
            addColumnCmd.ExecuteNonQuery();
        }
    }

    public void InsertWord(Word word)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"
            INSERT INTO Words (Text, Phonetic, PartOfSpeech, Definition, Example)
            VALUES (@Text, @Phonetic, @PartOfSpeech, @Definition, @Example)
            ON CONFLICT(Text) DO UPDATE SET
                Phonetic = @Phonetic,
                PartOfSpeech = @PartOfSpeech,
                Definition = @Definition,
                Example = @Example";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Text", word.Text);
        cmd.Parameters.AddWithValue("@Phonetic", word.Phonetic ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@PartOfSpeech", word.PartOfSpeech ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Definition", word.Definition ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Example", word.Example ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public List<Word> GetAllWords()
    {
        var words = new List<Word>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = "SELECT Id, Text, Phonetic, PartOfSpeech, Definition, Example, COALESCE(DisplayOrder, 0) FROM Words ORDER BY COALESCE(DisplayOrder, 0), Id";
        using var cmd = new SqliteCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            words.Add(new Word
            {
                Id = reader.GetInt32(0),
                Text = reader.GetString(1),
                Phonetic = reader.IsDBNull(2) ? null : reader.GetString(2),
                PartOfSpeech = reader.IsDBNull(3) ? null : reader.GetString(3),
                Definition = reader.IsDBNull(4) ? null : reader.GetString(4),
                Example = reader.IsDBNull(5) ? null : reader.GetString(5),
                DisplayOrder = reader.GetInt32(6)
            });
        }

        return words;
    }

    public bool WordExists(string text)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = "SELECT COUNT(*) FROM Words WHERE Text = @Text";
        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Text", text);

        var count = Convert.ToInt32(cmd.ExecuteScalar());
        return count > 0;
    }

    public void ClearAllWords()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = new SqliteCommand("DELETE FROM Words", connection);
        cmd.ExecuteNonQuery();
    }

    public void DeleteWord(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = "DELETE FROM Words WHERE Id = @Id";
        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    public void UpdateWord(Word word)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"
            UPDATE Words SET
                Text = @Text,
                Phonetic = @Phonetic,
                PartOfSpeech = @PartOfSpeech,
                Definition = @Definition,
                Example = @Example
            WHERE Id = @Id";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", word.Id);
        cmd.Parameters.AddWithValue("@Text", word.Text);
        cmd.Parameters.AddWithValue("@Phonetic", word.Phonetic ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@PartOfSpeech", word.PartOfSpeech ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Definition", word.Definition ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Example", word.Example ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void UpdateWordOrder(int id, int displayOrder)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = "UPDATE Words SET DisplayOrder = @DisplayOrder WHERE Id = @Id";
        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@DisplayOrder", displayOrder);
        cmd.ExecuteNonQuery();
    }

    public void ReorderWords(List<int> wordIds)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            for (int i = 0; i < wordIds.Count; i++)
            {
                var sql = "UPDATE Words SET DisplayOrder = @DisplayOrder WHERE Id = @Id";
                using var cmd = new SqliteCommand(sql, connection, transaction);
                cmd.Parameters.AddWithValue("@Id", wordIds[i]);
                cmd.Parameters.AddWithValue("@DisplayOrder", i);
                cmd.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public Word? GetWordById(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = "SELECT Id, Text, Phonetic, PartOfSpeech, Definition, Example, COALESCE(DisplayOrder, 0) FROM Words WHERE Id = @Id";
        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", id);
        using var reader = cmd.ExecuteReader();

        if (reader.Read())
        {
            return new Word
            {
                Id = reader.GetInt32(0),
                Text = reader.GetString(1),
                Phonetic = reader.IsDBNull(2) ? null : reader.GetString(2),
                PartOfSpeech = reader.IsDBNull(3) ? null : reader.GetString(3),
                Definition = reader.IsDBNull(4) ? null : reader.GetString(4),
                Example = reader.IsDBNull(5) ? null : reader.GetString(5),
                DisplayOrder = reader.GetInt32(6)
            };
        }

        return null;
    }
}
