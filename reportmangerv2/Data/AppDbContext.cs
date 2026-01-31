using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentFormat.OpenXml.Drawing;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using reportmangerv2.Domain;

namespace reportmangerv2.Data;

public class AppDbContext: IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Model.SetMaxIdentifierLength(30);
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
       {

        var tableName = entity.GetTableName();
    //    if (tableName != null && tableName.StartsWith("AspNet"))
    //    {
    //        // Skip this entity
    //        continue;
    //    }
       if(tableName !=null){

         entity.SetTableName(tableName.ToUpper());
           foreach (var property in entity.GetProperties())
           {
               property.SetColumnName(property.GetColumnName().ToUpper());
           }
           }
       }
       var jsonOptions= new JsonSerializerOptions
       {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
           
       };
       jsonOptions.Converters.Add(new JsonStringEnumConverter());
         ValueComparer execComparer= new ValueComparer<List<ExecutionParameter>>(
        (r, l)=> JsonSerializer.Serialize(r, jsonOptions) == JsonSerializer.Serialize(l, jsonOptions),
        v=> v==null ? 0: JsonSerializer.Serialize(v, jsonOptions).GetHashCode() ,
        v=>JsonSerializer.Deserialize<List<ExecutionParameter>>(JsonSerializer.Serialize(v, jsonOptions), jsonOptions)?? new List<ExecutionParameter>()
       );
        modelBuilder.Entity<Execution>().Property(r=>r.ExecutionParameters).HasConversion(
        v=>JsonSerializer.Serialize(v, jsonOptions),
        v=>JsonSerializer.Deserialize<List<ExecutionParameter>>(v, jsonOptions)?? new List<ExecutionParameter>()
       )
       .Metadata.SetValueComparer(execComparer);
       ValueComparer scheduledJobComparer=new ValueComparer<List<ScheduledJobParameter>>(
        
        (r,l)=>JsonSerializer.Serialize(r, jsonOptions)==JsonSerializer.Serialize(l, jsonOptions),
        v=> v==null ? 0: JsonSerializer.Serialize(v, jsonOptions).GetHashCode(),
        v=>JsonSerializer.Deserialize<List<ScheduledJobParameter>>(JsonSerializer.Serialize(v, jsonOptions), jsonOptions)?? new List<ScheduledJobParameter>()
       );
       modelBuilder.Entity<Report>(entity =>
       {
           entity.HasOne(r => r.Category)
                 .WithMany(c => c.Reports)
                 .HasForeignKey(r => r.CategoryId)
                 .OnDelete(DeleteBehavior.Cascade);

           entity.HasOne(r => r.Schema)
                 .WithMany(s => s.Reports)
                 .HasForeignKey(r => r.SchemaId)
                 .OnDelete(DeleteBehavior.Cascade);
       });
       modelBuilder.Entity<ReportParameter>(entity =>
       {
           entity.HasKey(rp => new { rp.ReportId, rp.Name });

           entity.HasOne(rp => rp.Report)
                 .WithMany(r => r.ReportParameters)
                 .HasForeignKey(rp => rp.ReportId)
                 .OnDelete(DeleteBehavior.Cascade);
                 entity.Property(rp=>rp.Type).HasConversion<string>();
                 entity.Property(e=>e.ViewControl).HasConversion<string>();
           
       });
       modelBuilder.Entity<Execution>(entity =>
       {
           entity.HasOne(e => e.Report)
                 .WithMany(r => r.Executions)
                 .HasForeignKey(e => e.ReportId)
                 .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e=>e.ExecutionType).HasConversion<string>();
            entity.Property(e=>e.ExecutionStatus).HasConversion<string>();

                 
               
       });
       modelBuilder.Entity<Execution>(
        entity =>
       {
          entity.HasOne(e=>e.ScheduledJob)
          .WithMany(j=>j.Executions)
          .HasForeignKey(e=>e.ScheduledJobId)
          .OnDelete(DeleteBehavior.Cascade);
       }
       );
       modelBuilder.Entity<ScheduledJob>(entity =>
       {
        
        entity.Property(j=>j.Parameters).HasConversion(
        v=>JsonSerializer.Serialize(v, jsonOptions),
        v=>JsonSerializer.Deserialize<List<ScheduledJobParameter>>(v, jsonOptions)?? new List<ScheduledJobParameter>()
       )
       .Metadata.SetValueComparer(scheduledJobComparer);
       });
       //build hierarchy for Category
       modelBuilder.Entity<Category>(entity =>
       {
           entity.HasOne(c => c.ParentCategory)
                 .WithMany(c => c.SubCategories)
                 .HasForeignKey(c => c.ParentCategoryId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.Cascade);
       });
       modelBuilder.Entity<Category>(entity =>
       {
           entity.HasIndex(c => c.Name)
                 .IsUnique(false);
           entity.HasIndex(c => new { c.Name, c.ParentCategoryId })
                 .IsUnique(true);
       });
       modelBuilder.Entity<Schema>(entity =>
       {
           entity.HasIndex(s => s.Name)
                 .IsUnique();
       });
    
    }
    
    public DbSet<Report> Reports { get; set; }
    public DbSet<ReportParameter> ReportParameters { get; set; }
    public DbSet<Execution> Executions { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Schema> Schemas { get; set; }
    public DbSet<ScheduledJob> ScheduledJobs { get; set; }



}
