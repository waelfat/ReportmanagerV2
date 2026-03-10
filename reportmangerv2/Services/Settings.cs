using System;

namespace reportmangerv2.Services;

public class ReportManagerSettings
{
   public string OutputDirectory     { get; set; } = "Reports";
    public int    MaxConcurrentReports { get; set; } = 5;
    public int    MaxAttachmentSizeMb  { get; set; } = 25;
    public int    FileRetentionDays    { get; set; } = 30;
}
