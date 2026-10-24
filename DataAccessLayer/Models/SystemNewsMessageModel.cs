using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("system_news_message")]
public sealed class SystemNewsMessageModel
{
    [Key, Column("id")]
    public int Id { get; set; }

    [MaxLength(160), Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("body")]
    public string Body { get; set; } = string.Empty;

    [MaxLength(16), Column("audience")]
    public string Audience { get; set; } = "all";

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [MaxLength(500), Column("video_url")]
    public string? VideoUrl { get; set; }

    [Column("created_at_utc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    [Column("updated_at_utc")]
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<SystemNewsReadModel> Reads { get; set; } = [];
}
