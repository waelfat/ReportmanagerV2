namespace reportmangerv2.ViewModels;

public class ScheduleReportViewModel
{
    public string ReportId { get; set; }
    public string CronExpression { get; set; }
    public  DateTime? SelectedDate { get; set; }
    public List<ParameterViewModel> Parameters { get; set; } = new();
}
