using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("regulation_document")]
public sealed class RegulationDocumentModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(160)]
    [Column("title")]
    public string Title { get; set; } = null!;

    [Required]
    [MaxLength(80)]
    [Column("version")]
    public string Version { get; set; } = null!;

    [Required]
    [MaxLength(4000)]
    [Column("message")]
    public string Message { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    [Column("pdf_file_name")]
    public string PdfFileName { get; set; } = null!;

    [Required]
    [Column("pdf_content")]
    public byte[] PdfContent { get; set; } = null!;

    [Required]
    [MaxLength(64)]
    [Column("pdf_sha256")]
    public string PdfSha256 { get; set; } = null!;

    [Column("is_published")]
    public bool IsPublished { get; set; }

    [Column("published_at_utc")]
    public DateTimeOffset? PublishedAtUtc { get; set; }

    [Column("created_by_manager_id")]
    public int? CreatedByManagerId { get; set; }

    [Required]
    [MaxLength(160)]
    [Column("created_by_manager_name")]
    public string CreatedByManagerName { get; set; } = null!;

    [Column("created_at_utc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    [Column("updated_at_utc")]
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<RegulationAcceptanceModel> Acceptances { get; set; } = new List<RegulationAcceptanceModel>();
}
