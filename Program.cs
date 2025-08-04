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

// 🔥 NEW: Configure Email Settings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Add Repository services
builder.Services.AddScoped<IdeaRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<EmployeeRepository>();
builder.Services.AddScoped<DepartmentRepository>();
builder.Services.AddScoped<DivisionRepository>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<EventRepository>();

// Add Business services
builder.Services.AddScoped<IdeaService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<WorkflowService>();
builder.Services.AddScoped<ApproverService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<IdeaCodeService>();

// 🔥 NEW: Add Email Service
builder.Services.AddScoped<EmailService>();

// Add Authentication
builder.Services.AddAuthentication("MyCookieAuth").AddCookie("MyCookieAuth", options =>
{
    options.Cookie.Name = "MyCookieAuth";
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

// 🔥 NEW: Add Session services for TempData
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
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

app.UseSession(); // 🔥 NEW: Enable session middleware

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

// Database seeding method (existing code remains the same)
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

    // 🔥 NEW: Seed Roles with proper hierarchy and IDs
    var roles = new[]
    {
        new Ideku.Models.Entities.Role { Id = "R01", RoleName = "Superuser", Description = "System Superuser", ApprovalLevel = 99 },
        new Ideku.Models.Entities.Role { Id = "R02", RoleName = "Admin", Description = "System Administrator", ApprovalLevel = 0 },
        new Ideku.Models.Entities.Role { Id = "R03", RoleName = "Initiator", Description = "Idea Initiator", ApprovalLevel = 0 },
        new Ideku.Models.Entities.Role { Id = "R04", RoleName = "Workstream Leader", Description = "Workstream Leader", ApprovalLevel = 1, CanApproveStandard = true, CanApproveHighValue = true },
        new Ideku.Models.Entities.Role { Id = "R05", RoleName = "Implementor", Description = "Idea Implementor", ApprovalLevel = 0 },
        new Ideku.Models.Entities.Role { Id = "R06", RoleName = "Mgr. Dept", Description = "Department Manager", ApprovalLevel = 2, CanApproveStandard = true, CanApproveHighValue = false },
        new Ideku.Models.Entities.Role { Id = "R07", RoleName = "GM Division", Description = "General Manager Division", ApprovalLevel = 2, CanApproveStandard = false, CanApproveHighValue = true },
        new Ideku.Models.Entities.Role { Id = "R08", RoleName = "GM Finance", Description = "General Manager Finance", ApprovalLevel = 3, CanApproveStandard = false, CanApproveHighValue = true },
        new Ideku.Models.Entities.Role { Id = "R09", RoleName = "GM BPID", Description = "General Manager BPID", ApprovalLevel = 4, CanApproveStandard = false, CanApproveHighValue = true },
        new Ideku.Models.Entities.Role { Id = "R10", RoleName = "COO", Description = "Chief Operating Officer", ApprovalLevel = 5, CanApproveStandard = false, CanApproveHighValue = true },
        new Ideku.Models.Entities.Role { Id = "R11", RoleName = "SCFO", Description = "Senior Chief Financial Officer", ApprovalLevel = 6, CanApproveStandard = false, CanApproveHighValue = true },
        new Ideku.Models.Entities.Role { Id = "R12", RoleName = "CEO", Description = "Chief Executive Officer", ApprovalLevel = 99 },
        new Ideku.Models.Entities.Role { Id = "R13", RoleName = "GM Division Act.", Description = "GM Division Acting", ApprovalLevel = 2, CanApproveStandard = false, CanApproveHighValue = true },
        new Ideku.Models.Entities.Role { Id = "R14", RoleName = "GM Finance Act.", Description = "GM Finance Acting", ApprovalLevel = 3, CanApproveStandard = false, CanApproveHighValue = true },
        new Ideku.Models.Entities.Role { Id = "R15", RoleName = "GM BPID Act.", Description = "GM BPID Acting", ApprovalLevel = 4, CanApproveStandard = false, CanApproveHighValue = true },
        new Ideku.Models.Entities.Role { Id = "R16", RoleName = "Mgr. Dept. Act.", Description = "Department Manager Acting", ApprovalLevel = 2, CanApproveStandard = true, CanApproveHighValue = false },
        new Ideku.Models.Entities.Role { Id = "R17", RoleName = "COO Act.", Description = "COO Acting", ApprovalLevel = 5, CanApproveStandard = false, CanApproveHighValue = true },
        new Ideku.Models.Entities.Role { Id = "R18", RoleName = "CEO Act.", Description = "CEO Acting", ApprovalLevel = 99 },
        new Ideku.Models.Entities.Role { Id = "R19", RoleName = "SCFO Act.", Description = "SCFO Acting", ApprovalLevel = 6, CanApproveStandard = false, CanApproveHighValue = true }
    };
    context.Roles.AddRange(roles);
    context.SaveChanges();

    // Seed Categories
    var categories = new[]
    {
        new Ideku.Models.Entities.Category { NamaCategory = "General Transformation" },
        new Ideku.Models.Entities.Category { NamaCategory = "Increase Revenue" },
        new Ideku.Models.Entities.Category { NamaCategory = "Cost Reduction (CR)" },
        new Ideku.Models.Entities.Category { NamaCategory = "Digitalization" },
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
        // Initiators
        new Ideku.Models.Entities.Employee { Id = "EMP001", Name = "Andi Initiator", Email = "andi.initiator@example.com", DepartementId = "P01", DivisiId = "D01", PositionTitle = "Staff BPID" },
        new Ideku.Models.Entities.Employee { Id = "EMP002", Name = "Budi Karyawan", Email = "budi.karyawan@example.com", DepartementId = "P07", DivisiId = "D06", PositionTitle = "Operator CHT" },
        
        // Approvers
        new Ideku.Models.Entities.Employee { Id = "EMP101", Name = "Charlie Workstream", Email = "faiqlidan03@gmail.com", DepartementId = "P01", DivisiId = "D01", PositionTitle = "Workstream Leader" },
        new Ideku.Models.Entities.Employee { Id = "EMP102", Name = "Diana Manager", Email = "faiqlidan15@gmail.com", DepartementId = "P01", DivisiId = "D01", PositionTitle = "Manager BPID" },
        new Ideku.Models.Entities.Employee { Id = "EMP103", Name = "Eko GM Divisi", Email = "bpidstudent@gmail.com", DepartementId = "P01", DivisiId = "D01", PositionTitle = "GM BPID" },
        new Ideku.Models.Entities.Employee { Id = "EMP104", Name = "Fanny GM Finance", Email = "fanny.finance@example.com", DepartementId = "P30", DivisiId = "D10", PositionTitle = "GM Finance" },
        new Ideku.Models.Entities.Employee { Id = "EMP105", Name = "Gilang GM BPID", Email = "gilang.bpid@example.com", DepartementId = "P01", DivisiId = "D01", PositionTitle = "GM BPID" },
        new Ideku.Models.Entities.Employee { Id = "EMP106", Name = "Hana COO", Email = "hana.coo@example.com", DepartementId = "P06", DivisiId = "D05", PositionTitle = "Chief Operating Officer" },
        new Ideku.Models.Entities.Employee { Id = "EMP107", Name = "Indra CFO", Email = "indra.cfo@example.com", DepartementId = "P05", DivisiId = "D04", PositionTitle = "Chief Financial Officer" },
        new Ideku.Models.Entities.Employee { Id = "EMP108", Name = "Joko CEO", Email = "joko.ceo@example.com", DepartementId = "P03", DivisiId = "D03", PositionTitle = "Chief Executive Officer" },
        new Ideku.Models.Entities.Employee { Id = "EMP200", Name = "Super Admin", Email = "super.admin@example.com", DepartementId = "P01", DivisiId = "D01", PositionTitle = "Superuser" },

        // Test Users
        new Ideku.Models.Entities.Employee { Id = "TEST01", Name = "Test Workstream", Email = "faiqlidan03@gmail.com", DepartementId = "P01", DivisiId = "D01", PositionTitle = "Test Workstream Leader" },
        new Ideku.Models.Entities.Employee { Id = "TEST02", Name = "Test Manager", Email = "faiqlidan15@gmail.com", DepartementId = "P01", DivisiId = "D01", PositionTitle = "Test Manager" },
        new Ideku.Models.Entities.Employee { Id = "TEST03", Name = "Test GM", Email = "bpidstudent@gmail.com", DepartementId = "P01", DivisiId = "D01", PositionTitle = "Test GM" }
    };
    context.Employees.AddRange(employees);
    context.SaveChanges();

    // 🔥 UPDATED: Seed Sample Users with new Role IDs
    var users = new[]
    {
        new Ideku.Models.Entities.User { EmployeeId = "EMP001", RoleId = "R03", Username = "andi.initiator", Name = "Andi Initiator", IsActing = false },
        new Ideku.Models.Entities.User { EmployeeId = "EMP002", RoleId = "R03", Username = "budi.karyawan", Name = "Budi Karyawan", IsActing = false },
        new Ideku.Models.Entities.User { EmployeeId = "EMP101", RoleId = "R04", Username = "charlie.ws", Name = "Charlie Workstream", IsActing = false },
        new Ideku.Models.Entities.User { EmployeeId = "EMP102", RoleId = "R06", Username = "diana.manager", Name = "Diana Manager", IsActing = false },
        new Ideku.Models.Entities.User { EmployeeId = "EMP103", RoleId = "R07", Username = "eko.gm", Name = "Eko GM Divisi", IsActing = false },
        new Ideku.Models.Entities.User { EmployeeId = "EMP104", RoleId = "R08", Username = "fanny.finance", Name = "Fanny GM Finance", IsActing = false },
        new Ideku.Models.Entities.User { EmployeeId = "EMP105", RoleId = "R09", Username = "gilang.bpid", Name = "Gilang GM BPID", IsActing = false },
        new Ideku.Models.Entities.User { EmployeeId = "EMP106", RoleId = "R10", Username = "hana.coo", Name = "Hana COO", IsActing = false },
        new Ideku.Models.Entities.User { EmployeeId = "EMP107", RoleId = "R11", Username = "indra.cfo", Name = "Indra CFO", IsActing = false },
        new Ideku.Models.Entities.User { EmployeeId = "EMP108", RoleId = "R12", Username = "joko.ceo", Name = "Joko CEO", IsActing = false },
        new Ideku.Models.Entities.User { EmployeeId = "EMP200", RoleId = "R01", Username = "superuser", Name = "Super Admin", IsActing = false },

        // Test Users
        new Ideku.Models.Entities.User { EmployeeId = "TEST01", RoleId = "R04", Username = "test.workstream", Name = "Test Workstream", IsActing = false },
        new Ideku.Models.Entities.User { EmployeeId = "TEST02", RoleId = "R06", Username = "test.manager", Name = "Test Manager", IsActing = false },
        new Ideku.Models.Entities.User { EmployeeId = "TEST03", RoleId = "R07", Username = "test.gm", Name = "Test GM", IsActing = false }
    };
    context.Users.AddRange(users);
    context.SaveChanges();

    // Seed System Settings
    if (!context.SystemSettings.Any())
    {
        var settings = new[]
        {
            new Ideku.Models.Entities.SystemSetting { SettingKey = "HIGH_VALUE_THRESHOLD", SettingValue = "20000", Description = "Threshold for an idea to be considered high value.", UpdatedBy = "EMP200" }
        };
        context.SystemSettings.AddRange(settings);
        context.SaveChanges();
    }

    // Seed Permissions
    if (!context.Permissions.Any())
    {
        var permissions = new[]
        {
            // Idea Management
            new Ideku.Models.Entities.Permission { Id = 1, PermissionName = "Create Idea", Module = "Idea" },
            new Ideku.Models.Entities.Permission { Id = 2, PermissionName = "Edit Own Idea", Module = "Idea" },
            new Ideku.Models.Entities.Permission { Id = 3, PermissionName = "Delete Own Idea", Module = "Idea" },
            new Ideku.Models.Entities.Permission { Id = 4, PermissionName = "View All Ideas", Module = "Idea" },
            // Approval
            new Ideku.Models.Entities.Permission { Id = 10, PermissionName = "Approve Stage 1", Module = "Approval" },
            new Ideku.Models.Entities.Permission { Id = 11, PermissionName = "Approve Stage 2", Module = "Approval" },
            new Ideku.Models.Entities.Permission { Id = 12, PermissionName = "Approve Stage 3", Module = "Approval" },
            new Ideku.Models.Entities.Permission { Id = 13, PermissionName = "Approve Stage 4", Module = "Approval" },
            new Ideku.Models.Entities.Permission { Id = 14, PermissionName = "Approve Stage 5", Module = "Approval" },
            new Ideku.Models.Entities.Permission { Id = 15, PermissionName = "Approve Stage 6", Module = "Approval" },
            // Admin
            new Ideku.Models.Entities.Permission { Id = 100, PermissionName = "Manage Users", Module = "Admin" },
            new Ideku.Models.Entities.Permission { Id = 101, PermissionName = "Manage Roles", Module = "Admin" },
            new Ideku.Models.Entities.Permission { Id = 102, PermissionName = "Manage System Settings", Module = "Admin" }
        };
        context.Permissions.AddRange(permissions);
        context.SaveChanges();
    }

    // Seed Role-Permission Mapping
    if (!context.RolePermissions.Any())
    {
        var rolePermissions = new[]
        {
            // Initiator
            new Ideku.Models.Entities.RolePermission { RoleId = "R03", PermissionId = 1 }, // Create Idea
            new Ideku.Models.Entities.RolePermission { RoleId = "R03", PermissionId = 2 }, // Edit Own Idea
            new Ideku.Models.Entities.RolePermission { RoleId = "R03", PermissionId = 3 }, // Delete Own Idea
            
            // Workstream Leader
            new Ideku.Models.Entities.RolePermission { RoleId = "R04", PermissionId = 10 }, // Approve S1

            // Manager
            new Ideku.Models.Entities.RolePermission { RoleId = "R06", PermissionId = 11 }, // Approve S2 (Standard)

            // GM Divisi
            new Ideku.Models.Entities.RolePermission { RoleId = "R07", PermissionId = 11 }, // Approve S2 (High Value)
            new Ideku.Models.Entities.RolePermission { RoleId = "R07", PermissionId = 12 }, // Approve S3 (Standard)

            // GM Finance
            new Ideku.Models.Entities.RolePermission { RoleId = "R08", PermissionId = 12 }, // Approve S3 (High Value)

            // GM BPID
            new Ideku.Models.Entities.RolePermission { RoleId = "R09", PermissionId = 13 }, // Approve S4

            // COO
            new Ideku.Models.Entities.RolePermission { RoleId = "R10", PermissionId = 14 }, // Approve S5

            // CFO
            new Ideku.Models.Entities.RolePermission { RoleId = "R11", PermissionId = 15 }, // Approve S6

            // Superuser
            new Ideku.Models.Entities.RolePermission { RoleId = "R01", PermissionId = 1 },
            new Ideku.Models.Entities.RolePermission { RoleId = "R01", PermissionId = 2 },
            new Ideku.Models.Entities.RolePermission { RoleId = "R01", PermissionId = 3 },
            new Ideku.Models.Entities.RolePermission { RoleId = "R01", PermissionId = 4 },
            new Ideku.Models.Entities.RolePermission { RoleId = "R01", PermissionId = 10 },
            new Ideku.Models.Entities.RolePermission { RoleId = "R01", PermissionId = 11 },
            new Ideku.Models.Entities.RolePermission { RoleId = "R01", PermissionId = 12 },
            new Ideku.Models.Entities.RolePermission { RoleId = "R01", PermissionId = 13 },
            new Ideku.Models.Entities.RolePermission { RoleId = "R01", PermissionId = 14 },
            new Ideku.Models.Entities.RolePermission { RoleId = "R01", PermissionId = 15 },
            new Ideku.Models.Entities.RolePermission { RoleId = "R01", PermissionId = 100 },
            new Ideku.Models.Entities.RolePermission { RoleId = "R01", PermissionId = 101 },
            new Ideku.Models.Entities.RolePermission { RoleId = "R01", PermissionId = 102 }
        };
        context.RolePermissions.AddRange(rolePermissions);
        context.SaveChanges();
    }
}
