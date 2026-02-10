using System.Threading.Channels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using ReportManagerv2.Services;
using reportmangerv2.Data;
using reportmangerv2.Domain;
using reportmangerv2.Hubs;
using reportmangerv2.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
//add dbcontext oracle
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("DefaultConnection"), b => b.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19)));
//add identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options=> options.SignIn.RequireConfirmedEmail=false)
.AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddEntityFrameworkStores<AppDbContext>();
    builder.Services.Configure<IdentityOptions>(options =>
    {
        // Password settings
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 3;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        // User settings
        options.User.RequireUniqueEmail = true;
    });
    builder.Services.ConfigureApplicationCookie(options =>
    {
        // Cookie settings
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.SlidingExpiration = true;
    });
    builder.Services.AddScoped<ReportService>();
    builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
    builder.Services.AddTransient<IEmailSender, EmailSender>();
    builder.Services.AddScoped<ICategoryService,CategoryService>();
builder.Services.AddHostedService<ExecutionService>();
builder.Services.AddSingleton(Channel.CreateUnbounded<ExecutionRequest>());
builder.Services.AddSingleton<CurrentActiveExecutionsService>();
builder.Services.AddScoped<IExecutionNotificationService, ExecutionNotificationService>();
 builder.Services.AddHostedService<ScheduledJobsExecuterService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}





app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapHub<ExecutionHub>("/executionHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var roleManager=services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager =services.GetRequiredService<UserManager<ApplicationUser>>();
    //context.Database.EnsureCreated();
    // Seed data if necessary
  await  DbInitializer.Initialize(context,userManager, roleManager);
}

app.Run();

internal class DbInitializer
{
    internal static async Task Initialize(AppDbContext context,UserManager<ApplicationUser> userManager,RoleManager<IdentityRole> roleManager)
    {
        // find admin user
        ApplicationUser? adminUser =await userManager.FindByEmailAsync("admin@example.com");
        ApplicationUser? user=await userManager.FindByEmailAsync("user@example.com");
       
        
        context.Database.EnsureCreated();
        if (!roleManager.Roles.Any())
        {
            // Create roles
            IdentityRole role1 = new IdentityRole("Admin");
            IdentityRole role2 = new IdentityRole("User");
            await roleManager.CreateAsync(role1);
           await roleManager.CreateAsync(role2);
        }
        // Check if there are any users in the database
        if (!userManager.Users.Any())
        {
           // add admin user
           adminUser = new ApplicationUser
            {
                UserName = "admin@example.com",
                Email = "admin@example.com",
              
                FullName="wael",
                IsActive=true
                
            };
            string password = "wael";
           
            var result =await userManager.CreateAsync(adminUser, password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    System.Console.WriteLine(error.Description);
                }
            }
            if (!result.Succeeded) throw new InvalidOperationException("Failed to create admin user.");
            adminUser =await userManager.FindByEmailAsync("admin@example.com");
            // add FullName to User Claims
            await userManager.AddClaimAsync(adminUser, new System.Security.Claims.Claim("FullName", adminUser.FullName));


            await userManager.AddToRoleAsync(adminUser, "Admin");
            //add user 
             user= new ApplicationUser
            {
                UserName = "user@example.com",
                Email = "user@example.com",
       
                FullName = "Ahmed",
                IsActive=true
            };
            string password2 = "wael";
            await userManager.CreateAsync(user, password2);
            user = await userManager.FindByEmailAsync("user@example.com");
            await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("FullName", user.FullName));
            await userManager.AddToRoleAsync(user, "User");
        }
         if (!context.Categories.Any())
         {   
            // add categories
                Category categoryFinance=  new Category
                {
                    CreatedById=adminUser.Id,
                    Name="Finance",
                };
                //add subcategories
                Category subcategory1=new Category
                {
                    Name="Budgeting",
                    ParentCategory=categoryFinance,
                    CreatedById=adminUser.Id
                };
                Category subcategory2 = new Category
                {
                    Name = "Accounting",
                    ParentCategory = categoryFinance,
                    CreatedById = adminUser.Id
                };
                categoryFinance.SubCategories.AddRange(subcategory1,subcategory2);
                Category categoryHR = new Category
                {
                    CreatedById = adminUser.Id,
                    Name = "Human Resources",
                };
                // hr subcategories
                Category subcategory3 = new Category
                {
                    Name = "Recruitment",
                    ParentCategory = categoryHR,
                    CreatedById = adminUser.Id
                };
                Category subcategory4 = new Category
                {
                    Name = "Training",
                    ParentCategory = categoryHR,
                    CreatedById = adminUser.Id
                };
                categoryHR.SubCategories.AddRange(subcategory3, subcategory4);
                Category categoryIT = new Category
                {
                    CreatedById = adminUser.Id,
                    Name = "Information Technology",
                };
                // IT subcategories
                Category subcategory5 = new Category
                {
                    Name = "Support",
                    ParentCategory = categoryIT,
                    CreatedById = adminUser.Id
                };
                Category subcategory6 = new Category
                {
                    Name = "Development",
                    ParentCategory = categoryIT,
                    CreatedById = adminUser.Id
                };
                Category subCategory7 = new Category
                {
                    Name = "Maintenance",
                    ParentCategory = categoryIT,
                    CreatedById = adminUser.Id
                };
                categoryIT.SubCategories.AddRange(subcategory5, subcategory6,subCategory7);
                // add heirarchy to db
                context.Categories.AddRange(categoryFinance, categoryHR, categoryIT);
                context.SaveChanges();
                
            
                //add subcategories
           
                // add schemas
                var schema1 = new Schema
                {
                    //"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.1.20)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XEPDB1)));User Id=gravit;Password=wael;"
                    Host="192.168.1.20",
                    ServiceName="XEPDB1",
                    UserId="gravit",
                    Password="wael",
                    Port="1521",
                    Name="Financial Schema",
                    Description="Schema for financial data"
                    
                 
                };
                var schema2 =new Schema
                {
                     Host="192.168.1.20",
                    ServiceName="XEPDB1",
                    UserId="reportmanagerv2",
                    Password="wael",
                    Port="1521" ,
                    Name="Reporting Schema",
                    Description="Schema for reporting and analytics"
                };
                context.Schemas.Add(schema1);
                context.Schemas.Add(schema2);
                context.SaveChanges();
                // add reports 
                Report report1 = new Report
                {
                    Name = "Monthly Financial Report",
                    Description = "A report showing the monthly financial summary.",
                    ReportQuery = "SELECT * FROM MAIL_ITEM_2 WHERE total_amount > :TotlAmount",
                    CreatedBy = adminUser,
                  
                    CategoryId = categoryFinance.Id,
                  
                    SchemaId = schema1.Id,

                };
                Report report2 = new Report
                {
                    Name = "Employee Attendance Report",
                    Description = "A report showing employee attendance records.",
                    ReportQuery = "SELECT * FROM MAIL_ITEM_2 WHERE created_date BETWEEN :startDate AND :endDate",
                 CreatedById = adminUser.Id,
                   
                    CategoryId = categoryHR.Id,
                   
                    SchemaId = schema1.Id
                };
                ReportParameter param1 = new ReportParameter
                {
                    Name = "TotlAmount",
                    Type = OracleDbType.Decimal,
                    Description = "Total Amount Threshold",
                    DefaultValue = "1000",
                    Report = report1,
                    Position=1
                };
                ReportParameter param2 = new ReportParameter
                {
                    Name = "startDate",
                    Type = OracleDbType.Date,
                    Description = "Start Date for Attendance",
                    DefaultValue = "2024-01-01",
                    Report = report2
                };
                ReportParameter param3 = new ReportParameter
                {
                    Name = "endDate",
                    Type = OracleDbType.Date,
                    Description = "End Date for Attendance",
                    DefaultValue = "2024-01-31",
                    Report = report2
                };
                report1.ReportParameters.Add(param1);
                report2.ReportParameters.Add(param2);
                report2.ReportParameters.Add(param3);
                context.Reports.AddRange(report1, report2);
              //  context.ReportParameters.AddRange(param1, param2, param3);

            context.SaveChanges();
        }
        // if(!context.ScheduledJobs.Any())
        // {
        //     ScheduledJob job1 = new ScheduledJob
        //     {
        //         Name = "Monthly Financial Report",
        //         Description = "A report showing the monthly financial summary.",
        //         SQLStatement = "SELECT * FROM MAIL_ITEM_2 WHERE total_amount > :TotlAmount",
        //         CreatedBy = 1,
        //         UpdatedBy = 1,
        //         CategoryId = 1,
        //         ReportType = "SQL",
        //         SchemaId = 1,
        //         CronExpression = "0 0 * * *",
        //         IsActive = true
        //     };
        //     context.ScheduledJobs.Add(job1);
        //     context.SaveChanges();
        // }
    }
}