using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("regulation_acceptance")]
public sealed class RegulationAcceptanceModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("regulation_document_id")]
    public int RegulationDocumentId { get; set; }

    [Required]
    [MaxLength(16)]
    [Column("account_role")]
    public string AccountRole { get; set; } = null!;

    [Column("account_id")]
    public int AccountId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("username_snapshot")]
    public string UsernameSnapshot { get; set; } = null!;

    [Required]
    [MaxLength(160)]
    [Column("display_name_snapshot")]
    public string DisplayNameSnapshot { get; set; } = null!;

    [Column("accepted_at_utc")]
    public DateTimeOffset AcceptedAtUtc { get; set; }

    public RegulationDocumentModel RegulationDocument { get; set; } = null!;
}
