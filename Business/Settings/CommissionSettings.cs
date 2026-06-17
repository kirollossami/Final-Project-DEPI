namespace Business.Settings;

public class CommissionSettings
{
    public const string SectionName = "CommissionSettings";
    public decimal GlobalRate { get; set; } = 0.10m;
}
