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

    // BORROW: lend one book, unless the member already has MaxOpenLoans books out.
    // The check and the INSERT run inside one transaction: both happen, or neither does.
    public async Task<bool> BorrowAsync(long memberId, long bookId, int daysAllowed)
    {
        // 1. Open one connection to the database.
        await using var connection = await _dataSource.OpenConnectionAsync();

        // 2. Start a transaction on that connection.
        await using var transaction = await connection.BeginTransactionAsync();

        // 3. Lock this member's row: a second borrow for the same member waits until this one ends.
        const string lockSql = "SELECT member_id FROM lending.member WHERE member_id = @memberId FOR UPDATE;";
        await using var lockCmd = new NpgsqlCommand(lockSql, connection, transaction);
        lockCmd.Parameters.AddWithValue("memberId", memberId);
        await lockCmd.ExecuteNonQueryAsync();

        // 4. Count the books this member has out.
        const string countSql = "SELECT COUNT(*) FROM lending.loan WHERE member_id = @memberId AND returned_on IS NULL;";
        await using var countCmd = new NpgsqlCommand(countSql, connection, transaction);
        countCmd.Parameters.AddWithValue("memberId", memberId);
        var openLoans = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        if (openLoans >= MaxOpenLoans) // 5. the member is already at the limit
        {
            // 6. Undo the transaction: nothing is saved.
            await transaction.RollbackAsync();

            // 7. Report that nothing was borrowed.
            return false;
        }

        // 8. The member is below the limit: insert the new loan.
        const string insertSql = "INSERT INTO lending.loan (member_id, book_id, loaned_on, days_allowed) VALUES (@memberId, @bookId, CURRENT_DATE, @daysAllowed);";
        await using var insertCmd = new NpgsqlCommand(insertSql, connection, transaction);
        insertCmd.Parameters.AddWithValue("memberId", memberId);
        insertCmd.Parameters.AddWithValue("bookId", bookId);
        insertCmd.Parameters.AddWithValue("daysAllowed", daysAllowed);
        await insertCmd.ExecuteNonQueryAsync();

        // 9. Keep the changes: the loan is saved.
        await transaction.CommitAsync();

        // 10. Report that the book was borrowed.
        return true;
    }

    // RETURN: mark a loan as returned today. A loan already returned is left alone.[cite: 6]
    public async Task ReturnAsync(long loanId)
    {
        // 1. The SQL: today's date, set only on a loan not returned yet.[cite: 6]
        const string sql = "UPDATE lending.loan SET returned_on = CURRENT_DATE WHERE loan_id = @loanId AND returned_on IS NULL;";

        // 2. A command that will run the SQL.[cite: 6]
        await using var command = _dataSource.CreateCommand(sql);

        // 3. Give the command the loan's id.[cite: 6]
        command.Parameters.AddWithValue("loanId", loanId);

        // 4. Run it.[cite: 6]
        await command.ExecuteNonQueryAsync();
    }
}

