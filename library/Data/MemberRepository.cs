using library.Models;
using Npgsql;

namespace library.Data;

public class MemberRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public MemberRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    private static Member ReadMember(NpgsqlDataReader reader)
    {
        return new Member
        {
            MemberId = reader.GetInt64(0),
            FullName = reader.GetString(1),
            Email = reader.IsDBNull(2) ? null : reader.GetString(2),
            PhoneNumber = reader.IsDBNull(3) ? null : reader.GetString(3),
            JoinDate = DateOnly.FromDateTime(reader.GetDateTime(4)),
            MemberType = reader.GetString(5)
        };
    }

    public async Task<List<Member>> GetAllAsync()
    {
        const string sql = "SELECT member_id, full_name, email, phone_number, join_date, member_type FROM lending.member ORDER BY full_name;";
        var members = new List<Member>();

        await using var command = _dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            members.Add(ReadMember(reader));
        }

        return members;
    }

    public async Task CreateAsync(Member member)
    {
        const string sql = "INSERT INTO lending.member (full_name, email, phone_number, member_type) " +
                           "VALUES (@fullName, @email, @phoneNumber, @memberType);";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("fullName", member.FullName);

        command.Parameters.AddWithValue("email", string.IsNullOrWhiteSpace(member.Email) ? DBNull.Value : member.Email);
        command.Parameters.AddWithValue("phoneNumber", string.IsNullOrWhiteSpace(member.PhoneNumber) ? DBNull.Value : member.PhoneNumber);
        command.Parameters.AddWithValue("memberType", member.MemberType);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<Member?> GetByIdAsync(long id)
    {
        const string sql = "SELECT member_id, full_name, email, phone_number, join_date, member_type FROM lending.member WHERE member_id = @id;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadMember(reader) : null;
    }

    public async Task UpdateAsync(Member member)
    {
        const string sql = "UPDATE lending.member " +
                           "SET full_name = @fullName, email = @email, phone_number = @phoneNumber, member_type = @memberType " +
                           "WHERE member_id = @id;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("fullName", member.FullName);
        command.Parameters.AddWithValue("email", string.IsNullOrWhiteSpace(member.Email) ? DBNull.Value : member.Email);
        command.Parameters.AddWithValue("phoneNumber", string.IsNullOrWhiteSpace(member.PhoneNumber) ? DBNull.Value : member.PhoneNumber);
        command.Parameters.AddWithValue("memberType", member.MemberType);
        command.Parameters.AddWithValue("id", member.MemberId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(long id)
    {
        const string sql = "DELETE FROM lending.member WHERE member_id = @id;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);

        await command.ExecuteNonQueryAsync();
    }
}