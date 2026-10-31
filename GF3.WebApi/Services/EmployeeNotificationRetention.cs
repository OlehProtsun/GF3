using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Services;

public static class EmployeeNotificationRetention
{
    public static readonly TimeSpan Duration = TimeSpan.FromDays(5);

    public static Task<int> CleanupAsync(AppDbContext db, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var cutoff = now.Subtract(Duration);
        return db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM employee_notification_read WHERE julianday(read_at_utc) <= julianday({cutoff})",
            cancellationToken);
    }
}
