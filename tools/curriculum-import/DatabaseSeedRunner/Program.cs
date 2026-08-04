using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: DatabaseSeedRunner <connection-string> <seed.sql>");
    return 2;
}

var connectionString = args[0];
var seedPath = Path.GetFullPath(args[1]);
if (!File.Exists(seedPath))
{
    Console.Error.WriteLine($"Seed file not found: {seedPath}");
    return 2;
}

var sql = await File.ReadAllTextAsync(seedPath);
var batches = Regex.Split(
        sql,
        @"^\s*GO\s*(?:--.*)?$",
        RegexOptions.Multiline | RegexOptions.IgnoreCase);

await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();

foreach (var batch in batches.Where(value => !string.IsNullOrWhiteSpace(value)))
{
    await using var command = connection.CreateCommand();
    command.CommandText = batch;
    command.CommandTimeout = 120;
    if (batch.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
    {
        await using var reader = await command.ExecuteReaderAsync();
        Console.WriteLine(string.Join(" | ", Enumerable.Range(0, reader.FieldCount).Select(reader.GetName)));
        while (await reader.ReadAsync())
        {
            Console.WriteLine(string.Join(
                " | ",
                Enumerable.Range(0, reader.FieldCount)
                    .Select(index => reader.IsDBNull(index) ? "NULL" : Convert.ToString(reader.GetValue(index)))));
        }
    }
    else
    {
        await command.ExecuteNonQueryAsync();
    }
}

Console.WriteLine($"Applied {Path.GetFileName(seedPath)} to {connection.Database}.");
return 0;
