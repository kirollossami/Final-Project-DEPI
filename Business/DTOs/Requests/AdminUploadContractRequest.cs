namespace Business.DTOs.Requests;

/// <summary>
/// Request sent by the admin to upload a contract PDF file for a paid booking.
/// The file is provided via IFormFile in the controller, which passes the stream here.
/// </summary>
public class AdminUploadContractRequest
{
    /// <summary>The booking that has completed payment and is awaiting a contract.</summary>
    public Guid BookingId { get; set; }

    /// <summary>The admin's user ID (injected from JWT claim).</summary>
    public string AdminUserId { get; set; } = string.Empty;

    /// <summary>The uploaded PDF file stream.</summary>
    public Stream PdfStream { get; set; } = Stream.Null;

    /// <summary>The original file name of the uploaded PDF.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Optional notes from the admin.</summary>
    public string? Notes { get; set; }
}
