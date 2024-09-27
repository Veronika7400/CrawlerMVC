using WebApiCrawler.SearchModels;
using Mapster;
using CrawlerMVC.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CrawlerMVC.Areas.Identity.Data;
using CrawlerMVC.Services;
using CrawlerMVC.Areas.Email.Data;
using Hangfire;
using Hangfire.Dashboard;


var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("CrawlerDbConnection") ?? throw new InvalidOperationException("Connection string 'IdentityContextConnection' not found.");

builder.Services.AddHangfire(configuration =>configuration.UseSqlServerStorage(connectionString)); 

builder.Services.AddHangfireServer(); 

builder.Services.AddDbContext<IdentityContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDbContext<EmailDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false).AddRoles<IdentityRole>().AddEntityFrameworkStores<IdentityContext>();
builder.Services.AddTransient<INotificationSender, EmailSender>();
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<NLog.ILogger>(_ => NLog.LogManager.GetCurrentClassLogger());

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("HangfireAdminOnly", policy =>
    {
        policy.RequireRole("admin");
    });
});

var app = builder.Build();

// Apply migrations if needed
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<EmailDbContext>();
    if (app.Environment.IsDevelopment())
    {
        dbContext.Database.Migrate();
    }

    var dbContext2 = scope.ServiceProvider.GetRequiredService<IdentityContext>();
    if (app.Environment.IsDevelopment())
    {
        dbContext2.Database.Migrate();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});
/*
RecurringJob.AddOrUpdate<INotificationSender>(
    "notify-subscribers-every-3-minutes",
    sender => sender.NotifySubscribers("daily"),*/
  // "*/3 * * * *"); //Svake 3 minute 
   
RecurringJob.AddOrUpdate<INotificationSender>(
    "notify-subscribers-daily-at-6am",
    sender => sender.NotifySubscribers("daily"),
    "0 6 * * *"); // Svaki dan u 6 ujutro

RecurringJob.AddOrUpdate<INotificationSender>(
    "notify-subscribers-every-monday-at-6am",
    sender => sender.NotifySubscribers("weekly"),
    "0 6 * * 1"); // Svaki ponedjeljak u 6 ujutro

RecurringJob.AddOrUpdate<INotificationSender>(
    "notify-subscribers-first-day-of-month",
    sender => sender.NotifySubscribers("monthly"),
    "0 6 1 * *"); // Svaki prvi dan u mjesecu u 6 ujutro

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "price",
    pattern: "Price/{action=AllStoresSearchPage}/{id?}",
    defaults: new { controller = "Price" });

app.MapControllerRoute(
    name: "users",
    pattern: "Users/{action=GetAllUsers}/{id?}",
    defaults: new { controller = "Users" });

app.MapControllerRoute(
    name: "Subscribers",
    pattern: "Subscribers/{action=AddSubscriber}/{frequency?}/{searchWord?}/{notificationTypeName?}/{subscriptionTargetName?}/{storeId?}",
    defaults: new { controller = "Subscriber" });

app.MapRazorPages();

app.Run();
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity.IsAuthenticated &&
               httpContext.User.IsInRole("admin");
    }
}