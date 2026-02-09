using System;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using reportmangerv2.Data;
using reportmangerv2.Domain;

namespace reportmangerv2.Services;

public class ReportService
{
    private readonly ILogger<ReportService> _logger;
    private readonly AppDbContext _Context;
    public ReportService(ILogger<ReportService> logger,AppDbContext context)
    {
        _logger = logger;
        _Context = context;
    }
  public async Task<IEnumerable<Report>> GetReports()
    {
        var reports = await _Context.Reports.ToListAsync();
        _logger.LogInformation("Returning {Count} reports", reports.Count);
        return reports;
    }
    public async Task<Report?> GetReport(string id)
    {
        var report = await _Context.Reports.FirstOrDefaultAsync(r=>r.Id==id );
        if(report is null)
        {
            _logger.LogWarning("No report found for id {Id}", id);
            return null;
        }
        _logger.LogInformation("Returning report {Id}", id);
        return report;
    }
    public async Task<Report> CreateReport(Report report)
    {
        _Context.Reports.Add(report);
        await _Context.SaveChangesAsync();
        _logger.LogInformation("Created report {Id}", report.Id);
        return report;

    }
    private void ValidateReport(Report report)
    {
        // if parameters names  are unique
        if (report.ReportParameters.Select(p => p.Name).Distinct().Count() != report.ReportParameters.Count)
        {
            throw new ArgumentException("Parameter names must be unique");
        }
        // parameter position should be in sequence start from 1
        if (report.ReportParameters.OrderBy(p => p.Position).Select((p, i) => p.Position == i + 1).Any(r => r == false))
        {
            throw new ArgumentException("Parameter position must be in sequence start from 1");
        }
        // if parameter type is valid
        if (report.ReportParameters.Any(p => !Enum.IsDefined(typeof(OracleDbType), p.Type)))
        {
            throw new ArgumentException("Parameter type is not valid");
        }
        // if parameter default value is valid with its type
        if (report.ReportParameters.Any(p => p.DefaultValue != null && p.Type != 0 && IsValidDefaultValue( p.DefaultValue,p.Type)))
        {
            throw new ArgumentException("Parameter default value is not valid with its type");
        }
    }
    private bool IsValidDefaultValue(object? value, OracleDbType type)
    {
        if (value == null) return true;
        return type switch
        {
            OracleDbType.Varchar2 or OracleDbType.Char => value is string,
            OracleDbType.Int32 or OracleDbType.Int64 or OracleDbType.Decimal => value is int or double or decimal,
            OracleDbType.Date => value is DateTime,
            _ => false
        };
    }
}
