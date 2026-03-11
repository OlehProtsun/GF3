using Microsoft.Data.Sqlite;
using SQLitePCL;

Batteries_V2.Init();

var dbPath = @"C:\Users\Oleg\AppData\Local\GF3\SQLite.db";

if (!File.Exists(dbPath))
{
    Console.WriteLine($"DB not found: {dbPath}");
    return;
}

using var connection = new SqliteConnection($"Data Source={dbPath}");
connection.Open();

DumpTableNames(connection);
Console.WriteLine();
DumpAvailabilityGroups(connection);
Console.WriteLine();
DumpAvailabilityOrphans(connection);

static void DumpTableNames(SqliteConnection connection)
{
    using var command = connection.CreateCommand();
    command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name;";
    using var reader = command.ExecuteReader();

    Console.WriteLine("Tables:");
    while (reader.Read())
    {
        Console.WriteLine($"- {reader.GetString(0)}");
    }
}

static void DumpAvailabilityGroups(SqliteConnection connection)
{
    using var command = connection.CreateCommand();
    command.CommandText = @"
SELECT
    g.id,
    g.name,
    g.month,
    g.year,
    COUNT(DISTINCT m.id) AS member_count,
    COUNT(d.id) AS day_count
FROM availability_groups g
LEFT JOIN availability_group_members m ON m.availability_group_id = g.id
LEFT JOIN availability_group_days d ON d.availability_group_member_id = m.id
GROUP BY g.id, g.name, g.month, g.year
ORDER BY g.id;";

    using var reader = command.ExecuteReader();

    Console.WriteLine("Availability groups:");
    while (reader.Read())
    {
        Console.WriteLine($"- id={reader.GetInt64(0)}, name={reader.GetString(1)}, month={reader.GetInt64(2)}, year={reader.GetInt64(3)}, members={reader.GetInt64(4)}, days={reader.GetInt64(5)}");
    }
}

static void DumpAvailabilityOrphans(SqliteConnection connection)
{
    Console.WriteLine("Availability integrity checks:");
    DumpScalar(connection, "Orphan members (missing group)", "SELECT COUNT(*) FROM availability_group_members m LEFT JOIN availability_groups g ON g.id = m.availability_group_id WHERE g.id IS NULL;");
    DumpScalar(connection, "Orphan members (missing employee)", "SELECT COUNT(*) FROM availability_group_members m LEFT JOIN employees e ON e.id = m.employee_id WHERE e.id IS NULL;");
    DumpScalar(connection, "Orphan days (missing member)", "SELECT COUNT(*) FROM availability_group_days d LEFT JOIN availability_group_members m ON m.id = d.availability_group_member_id WHERE m.id IS NULL;");
}

static void DumpScalar(SqliteConnection connection, string label, string sql)
{
    using var command = connection.CreateCommand();
    command.CommandText = sql;
    var value = command.ExecuteScalar();
    Console.WriteLine($"- {label}: {value}");
}
