namespace DataAccessLayer.Models;

/// <summary>
/// Maps a keyboard shortcut to an availability code used by the frontend editor.
/// This entity is intentionally tiny because it acts as configurable reference data rather than a rich aggregate.
/// </summary>
public class BindModel
{
    public int Id { get; set; }

    public int? ManagerAccountId { get; set; }

    public ManagerAccountModel? ManagerAccount { get; set; }

    /// <summary>
    /// Availability code inserted by the shortcut, for example <c>+</c>, <c>-</c>, or <c>HH:mm - HH:mm</c>.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Shortcut key in the format produced by <c>KeysConverter</c>, for example <c>F1</c> or <c>Ctrl+Shift+A</c>.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Disabled bindings are preserved for history/configuration but ignored by the active editor.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
