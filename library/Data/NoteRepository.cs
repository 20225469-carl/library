using library.Models;
using Npgsql;

namespace library.Data;

public class NoteRepository
{
    private readonly NpgsqlDataSource _dataSource;

    // ASP.NET Core injects the shared data source registered in Program.cs
    public NoteRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    // Copies the current row of the reader into a Note object.
    // Columns: 0: note_id, 1: title, 2: body, 3: created_on
    private static Note ReadNote(NpgsqlDataReader reader)
    {
        return new Note
        {
            NoteId = reader.GetInt64(0),
            Title = reader.GetString(1),
            Body = reader.IsDBNull(2) ? null : reader.GetString(2), // Handles nullable body
            CreatedOn = reader.GetFieldValue<DateOnly>(3)
        };
    }

    // READ: Get every note, sorted by creation date descending
    public async Task<List<Note>> GetAllAsync()
    {
        const string sql = "SELECT note_id, title, body, created_on FROM lending.note ORDER BY created_on DESC;";
        var notes = new List<Note>();

        await using var command = _dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            notes.Add(ReadNote(reader));
        }

        return notes;
    }

    // CREATE: Insert a new note (note_id and created_on are handled by the database)
    public async Task AddAsync(Note note)
    {
        const string sql = "INSERT INTO lending.note (title, body) VALUES (@title, @body);";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("title", note.Title);
        command.Parameters.AddWithValue("body", (object?)note.Body ?? DBNull.Value); // Sends C# null as DBNull.Value

        await command.ExecuteNonQueryAsync();
    }

    // READ ONE: Get a single note by its ID, or return null if not found
    public async Task<Note?> GetByIdAsync(long id)
    {
        const string sql = "SELECT note_id, title, body, created_on FROM lending.note WHERE note_id = @id;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadNote(reader) : null;
    }

    // UPDATE: Modify the title and body of an existing note
    public async Task UpdateAsync(Note note)
    {
        const string sql = "UPDATE lending.note SET title = @title, body = @body WHERE note_id = @id;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("title", note.Title);
        command.Parameters.AddWithValue("body", (object?)note.Body ?? DBNull.Value);
        command.Parameters.AddWithValue("id", note.NoteId);

        await command.ExecuteNonQueryAsync();
    }

    // DELETE: Remove a note by its ID
    public async Task DeleteAsync(long id)
    {
        const string sql = "DELETE FROM lending.note WHERE note_id = @id;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);

        await command.ExecuteNonQueryAsync();
    }
}