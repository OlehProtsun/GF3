using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("system_news_read")]
public sealed class SystemNewsReadModel
{
    [Key, Column("id")]
    public int Id { get; set; }

    [Column("message_id")]
    public int MessageId { get; set; }

    [MaxLength(16), Column("account_role")]
    public string AccountRole { get; set; } = string.Empty;

    [Column("account_id")]
    public int AccountId { get; set; }

    [Column("read_at_utc")]
    public DateTimeOffset ReadAtUtc { get; set; }

    public SystemNewsMessageModel Message { get; set; } = null!;
}
