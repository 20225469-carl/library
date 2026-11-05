using library.Models;
using Npgsql;

namespace library.Data;

// Every SQL statement about loans lives in this one class.
public class LoanRepository
{
    public const int MaxOpenLoans = 3;             // the most books one member may have out at once

    private readonly NpgsqlDataSource _dataSource;

    public LoanRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    // Copies the current row into a LoanView. Positions 0 to 7 match every loan SELECT.
    private static LoanView ReadLoan(NpgsqlDataReader reader)
    {
        return new LoanView
        {
            LoanId = reader.GetInt64(0),                                           // 1. the loan's id
            MemberId = reader.GetInt64(1),                                           // 2. the member's id
            MemberName = reader.GetString(2),                                          // 3. the member's name
            BookTitle = reader.GetString(3),                                          // 4. the book's title
            LoanedOn = reader.GetFieldValue<DateOnly>(4),                            // 5. the date it was lent
            ReturnedOn = reader.IsDBNull(5) ? null : reader.GetFieldValue<DateOnly>(5), // 6. the date it came back, which may be NULL
            DaysAllowed = reader.GetInt32(6),                                           // 7. how many days it may be kept
            Status = reader.GetString(7)                                           // 8. Out, Overdue or Returned
        };
    }

    // READ: every loan with its member and book, newest first.
    public async Task<List<LoanView>> GetAllAsync()
    {
        // 1. The SQL: three tables joined, and each loan's status worked out.
        const string sql =
            "SELECT l.loan_id, l.member_id, m.full_name, b.title, l.loaned_on, l.returned_on, l.days_allowed, " +
            "       CASE WHEN l.returned_on IS NOT NULL THEN 'Returned' " +
            "            WHEN l.loaned_on + l.days_allowed < CURRENT_DATE THEN 'Overdue' " +
            "            ELSE 'Out' END " +
            "FROM lending.loan l " +
            "JOIN lending.member m ON l.member_id = m.member_id " +
            "JOIN lending.book b ON l.book_id = b.book_id " +
            "ORDER BY l.loaned_on DESC, l.loan_id DESC;";

        // 2. An empty list, to fill.
        var loans = new List<LoanView>();

        // 3. A command that will run the SQL.
        await using var command = _dataSource.CreateCommand(sql);

        // 4. Run it, and get a reader for the rows.
        await using var reader = await command.ExecuteReaderAsync();

        // 5. there is another row to read
        while (await reader.ReadAsync())
        {
            // 6. Turn the row into a LoanView and add it to the list.
            loans.Add(ReadLoan(reader));
        }

        // 7. Give the list back.
        return loans;
    }
}

