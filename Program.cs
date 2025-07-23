using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Data.Repositories;
using Ideku.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Entity Framework
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Repository services
builder.Services.AddScoped<IdeaRepository>();
builder.Services.AddScoped<UserRepository>();

// Add Business services
builder.Services.AddScoped<IdeaService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<FileService>();

// Add Authentication
builder.Services.AddAuthentication("MyCookieAuth").AddCookie("MyCookieAuth", options =>
{
    options.Cookie.Name = "MyCookieAuth";
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        SeedDatabase(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

app.Run();

// Database seeding method
static void SeedDatabase(IServiceProvider services)
{
    using var context = services.GetRequiredService<AppDbContext>();
    
    // Ensure database is created
    context.Database.EnsureCreated();

    // Check if data already exists
    if (context.Roles.Any())
    {
        return; // DB has been seeded
    }

    // Seed Roles
    var roles = new[]
    {
        new Ideku.Models.Entities.Role { RoleName = "SuperAdmin", Description = "Administrator" },
        new Ideku.Models.Entities.Role { RoleName = "User", Description = "Regular User" },
        new Ideku.Models.Entities.Role { RoleName = "Manager", Description = "Manager" }
    };
    context.Roles.AddRange(roles);
    context.SaveChanges();

    // Seed Categories
    var categories = new[]
    {
        new Ideku.Models.Entities.Category { NamaCategory = "General Transformation" },
        new Ideku.Models.Entities.Category { NamaCategory = "Increase Revenue" },
        new Ideku.Models.Entities.Category { NamaCategory = "Cost Reduction (CR)" },
        new Ideku.Models.Entities.Category { NamaCategory = "Digitalisasi" },
    };
    context.Category.AddRange(categories);
    context.SaveChanges();

    // Seed Events
    var events = new[]
    {
        new Ideku.Models.Entities.Event { NamaEvent = "Hackathon" },
        new Ideku.Models.Entities.Event { NamaEvent = "CI Academy" },
    };
    context.Event.AddRange(events);
    context.SaveChanges();

    // Seed Divisions
    var divisions = new[]
    {
        new Ideku.Models.Entities.Divisi { Id = "D01", NamaDivisi = "Business & Performance Improvement" },
        new Ideku.Models.Entities.Divisi { Id = "D02", NamaDivisi = "Business Dev. & Risk Management" },
        new Ideku.Models.Entities.Divisi { Id = "D03", NamaDivisi = "Chief Executive Officer" },
        new Ideku.Models.Entities.Divisi { Id = "D04", NamaDivisi = "Chief Financial Officer" },
        new Ideku.Models.Entities.Divisi { Id = "D05", NamaDivisi = "Chief Operating Officer" },
        new Ideku.Models.Entities.Divisi { Id = "D06", NamaDivisi = "Coal Processing & Handling" },
        new Ideku.Models.Entities.Divisi { Id = "D07", NamaDivisi = "Contract Mining" },
        new Ideku.Models.Entities.Divisi { Id = "D08", NamaDivisi = "Director of Finance" },
        new Ideku.Models.Entities.Divisi { Id = "D09", NamaDivisi = "External Affairs & Sustainable Development" },
        new Ideku.Models.Entities.Divisi { Id = "D10", NamaDivisi = "Finance" }
    };
    context.Divisi.AddRange(divisions);
    context.SaveChanges();

    // Seed Departements
    var departements = new[]
    {
        new Ideku.Models.Entities.Departement { Id = "P01", NamaDepartement = "Business & Performance Improvement", DivisiId = "D01" },
        new Ideku.Models.Entities.Departement { Id = "P02", NamaDepartement = "Business Dev. & Risk Management", DivisiId = "D02" },
        new Ideku.Models.Entities.Departement { Id = "P03", NamaDepartement = "Chief Executive Officer", DivisiId = "D03" },
        new Ideku.Models.Entities.Departement { Id = "P04", NamaDepartement = "Business Analysis", DivisiId = "D04" },
        new Ideku.Models.Entities.Departement { Id = "P05", NamaDepartement = "Chief Financial Officer", DivisiId = "D04" },
        new Ideku.Models.Entities.Departement { Id = "P06", NamaDepartement = "Chief Operating Officer", DivisiId = "D05" },
        new Ideku.Models.Entities.Departement { Id = "P07", NamaDepartement = "CHT Operations", DivisiId = "D06" },
        new Ideku.Models.Entities.Departement { Id = "P08", NamaDepartement = "Coal Processing & Handling", DivisiId = "D06" },
        new Ideku.Models.Entities.Departement { Id = "P09", NamaDepartement = "Coal Technology", DivisiId = "D06" },
        new Ideku.Models.Entities.Departement { Id = "P10", NamaDepartement = "CPP Maintenance", DivisiId = "D06" },
        new Ideku.Models.Entities.Departement { Id = "P11", NamaDepartement = "CPP Operations", DivisiId = "D06" },
        new Ideku.Models.Entities.Departement { Id = "P12", NamaDepartement = "Infrastructure", DivisiId = "D06" },
        new Ideku.Models.Entities.Departement { Id = "P13", NamaDepartement = "Plant Engineering & Project Services", DivisiId = "D06" },
        new Ideku.Models.Entities.Departement { Id = "P14", NamaDepartement = "Power Generation & Transmission", DivisiId = "D06" },
        new Ideku.Models.Entities.Departement { Id = "P15", NamaDepartement = "CHT Maintenance", DivisiId = "D06" },
        new Ideku.Models.Entities.Departement { Id = "P16", NamaDepartement = "Contract Mining", DivisiId = "D07" },
        new Ideku.Models.Entities.Departement { Id = "P17", NamaDepartement = "Contract Mining Issues & Analysis", DivisiId = "D07" },
        new Ideku.Models.Entities.Departement { Id = "P18", NamaDepartement = "Mining Contract Bengalon", DivisiId = "D07" },
        new Ideku.Models.Entities.Departement { Id = "P19", NamaDepartement = "Mining Contract Pama", DivisiId = "D07" },
        new Ideku.Models.Entities.Departement { Id = "P20", NamaDepartement = "Mining Contract Sangatta", DivisiId = "D07" },
        new Ideku.Models.Entities.Departement { Id = "P21", NamaDepartement = "Mining Contract TCI Pits", DivisiId = "D07" },
        new Ideku.Models.Entities.Departement { Id = "P22", NamaDepartement = "Internal Audit", DivisiId = "D08" },
        new Ideku.Models.Entities.Departement { Id = "P23", NamaDepartement = "Bengalon Community Rels & Dev", DivisiId = "D09" },
        new Ideku.Models.Entities.Departement { Id = "P24", NamaDepartement = "Community Empowerment", DivisiId = "D09" },
        new Ideku.Models.Entities.Departement { Id = "P25", NamaDepartement = "Ext. Affairs & Sustainable Dev.", DivisiId = "D09" },
        new Ideku.Models.Entities.Departement { Id = "P26", NamaDepartement = "External Relations", DivisiId = "D09" },
        new Ideku.Models.Entities.Departement { Id = "P27", NamaDepartement = "Land Management", DivisiId = "D09" },
        new Ideku.Models.Entities.Departement { Id = "P28", NamaDepartement = "Project Management & Evaluation", DivisiId = "D09" },
        new Ideku.Models.Entities.Departement { Id = "P29", NamaDepartement = "Accounting and Reporting", DivisiId = "D10" },
        new Ideku.Models.Entities.Departement { Id = "P30", NamaDepartement = "Finance", DivisiId = "D10" },
        new Ideku.Models.Entities.Departement { Id = "P31", NamaDepartement = "Tax & Government Impost", DivisiId = "D10" },
        new Ideku.Models.Entities.Departement { Id = "P32", NamaDepartement = "Treasury", DivisiId = "D10" }
    };
    context.Departement.AddRange(departements);
    context.SaveChanges();

    // Seed Sample Employees
    var employees = new[]
    {
        new Ideku.Models.Entities.Employee
        {
            Id = "EMP001",
            Name = "John Doe",
            Email = "john.doe@company.com",
            DepartementId = "P01",
            DivisiId = "D01",
            PositionTitle = "Software Developer"
        },
        new Ideku.Models.Entities.Employee
        {
            Id = "EMP002",
            Name = "Jane Smith",
            Email = "jane.smith@company.com",
            DepartementId = "P02",
            DivisiId = "D02",
            PositionTitle = "HR Manager"
        },
        new Ideku.Models.Entities.Employee
        {
            Id = "EMP003",
            Name = "Alice Johnson",
            Email = "alice.johnson@company.com",
            DepartementId = "P03",
            DivisiId = "D03",
            PositionTitle = "QA Engineer"
        },
        new Ideku.Models.Entities.Employee
        {
            Id = "EMP004",
            Name = "Bob Wilson",
            Email = "bob.wilson@company.com",
            DepartementId = "P04",
            DivisiId = "D04",
            PositionTitle = "Senior Accountant"
        }
    };
    context.Employees.AddRange(employees);
    context.SaveChanges();

    // Seed Sample Users
    var users = new[]
    {
        new Ideku.Models.Entities.User
        {
            EmployeeId = "EMP001",
            RoleId = 2, // User role
            Username = "john.doe",
            Name = "John Doe",
            FlagActing = false
        },
        new Ideku.Models.Entities.User
        {
            EmployeeId = "EMP002",
            RoleId = 3, // Manager role
            Username = "jane.smith",
            Name = "Jane Smith",
            FlagActing = false
        },
        new Ideku.Models.Entities.User
        {
            EmployeeId = "EMP003",
            RoleId = 2, // User role
            Username = "alice.johnson",
            Name = "Alice Johnson",
            FlagActing = false
        },
        new Ideku.Models.Entities.User
        {
            EmployeeId = "EMP004",
            RoleId = 2, // User role
            Username = "bob.wilson",
            Name = "Bob Wilson",
            FlagActing = false
        }
    };
    context.Users.AddRange(users);
    context.SaveChanges();
}
