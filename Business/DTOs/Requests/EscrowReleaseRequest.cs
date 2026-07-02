namespace Business.DTOs.Requests;

public class EscrowReleaseRequest
{
    public Guid EscrowId { get; set; }
    public string AdminUserId { get; set; } = string.Empty;
    public string? ReleaseNotes { get; set; }
}
