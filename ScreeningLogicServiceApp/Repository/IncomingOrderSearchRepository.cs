using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using ScreeningLogicServiceApp.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace ScreeningLogicServiceApp.Repository;

public sealed class IncomingOrderSearchRepository : IIncomingOrderSearchRepository
{
    private static readonly Regex TableNamePattern = new(@"^[A-Za-z0-9_\.\[\]]+$", RegexOptions.Compiled);
    private readonly string _connectionString;
    private readonly string _tableName;

    public IncomingOrderSearchRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("IntegrationDB")
            ?? throw new InvalidOperationException("Missing connection string 'IntegrationDB' in App.config.");

        _tableName = System.Configuration.ConfigurationManager.AppSettings["ScreeningLogicIncomingOrderTable"]?.Trim()
            ?? throw new InvalidOperationException("Missing appSettings key 'ScreeningLogicIncomingOrderTable' in App.config.");

        if (!TableNamePattern.IsMatch(_tableName))
        {
            throw new InvalidOperationException("Configured table name contains invalid characters.");
        }
    }

    public async Task<IReadOnlyList<IncomingOrderSearchResult>> SearchIncomingOrdersAsync(
        string? orderNumber,
        string? lastName,
        string? firstName,
        string? dob)
    {
        var results = new List<IncomingOrderSearchResult>();

        var sql = new StringBuilder($@"
SELECT
    WorkId,
    OrderNumber,
    NameFirst,
    NameLast,
    DOB,
    SSN,
    Status,
    ReceivedAt,
    ErrorMessage,
    FailedSummaryReport
FROM {_tableName}
WHERE 1 = 1");

        var parameters = new List<SqlParameter>();

        if (!string.IsNullOrWhiteSpace(orderNumber))
        {
            sql.Append(" AND OrderNumber LIKE @OrderNumber");
            parameters.Add(new SqlParameter("@OrderNumber", $"%{orderNumber.Trim()}%"));
        }

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            sql.Append(" AND NameLast LIKE @NameLast");
            parameters.Add(new SqlParameter("@NameLast", $"%{lastName.Trim()}%"));
        }

        if (!string.IsNullOrWhiteSpace(firstName))
        {
            sql.Append(" AND NameFirst LIKE @NameFirst");
            parameters.Add(new SqlParameter("@NameFirst", $"%{firstName.Trim()}%"));
        }

        if (!string.IsNullOrWhiteSpace(dob))
        {
            sql.Append(" AND DOB LIKE @DOB");
            parameters.Add(new SqlParameter("@DOB", $"%{dob.Trim()}%"));
        }

        sql.Append(" ORDER BY ReceivedAt DESC");

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql.ToString(), connection);
        command.Parameters.AddRange(parameters.ToArray());

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new IncomingOrderSearchResult
            {
                WorkId = reader.GetInt32(reader.GetOrdinal("WorkId")),
                OrderNumber = GetString(reader, "OrderNumber"),
                NameFirst = GetString(reader, "NameFirst"),
                NameLast = GetString(reader, "NameLast"),
                Dob = GetString(reader, "DOB"),
                Ssn = GetString(reader, "SSN"),
                Status = GetString(reader, "Status"),
                ReceivedAt = GetDateTime(reader, "ReceivedAt"),
                ErrorMessage = GetString(reader, "ErrorMessage"),
                FailedSummaryReport = GetString(reader, "FailedSummaryReport")
            });
        }

        return results;
    }

    private static string? GetString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTime? GetDateTime(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}
