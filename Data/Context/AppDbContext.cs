// Data/Context/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using Ideku.Models.Entities;

namespace Ideku.Data.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Existing DbSets
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Divisi> Divisi { get; set; }
        public DbSet<Departement> Departement { get; set; }
        public DbSet<Category> Category { get; set; }
        public DbSet<Event> Event { get; set; }
        public DbSet<Idea> Ideas { get; set; }

        // New DbSets
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<ApprovalHistory> ApprovalHistory { get; set; }
        public DbSet<IdeaMilestone> IdeaMilestones { get; set; }
        public DbSet<SavingMonitoring> SavingMonitoring { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // ===== ROLE PERMISSIONS (Many-to-Many) =====
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== ORGANIZATIONAL STRUCTURE =====
            modelBuilder.Entity<Departement>()
                .HasOne(d => d.Divisi)
                .WithMany(div => div.Departements)
                .HasForeignKey(d => d.DivisiId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Departement)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartementId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Divisi)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DivisiId)
                .OnDelete(DeleteBehavior.SetNull);

            // ===== USER SYSTEM =====
            modelBuilder.Entity<User>()
                .HasOne(u => u.Employee)
                .WithOne(e => e.User)
                .HasForeignKey<User>(u => u.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== IDEAS & WORKFLOW =====
            modelBuilder.Entity<Idea>()
                .HasOne(i => i.Initiator)
                .WithMany(e => e.Ideas)
                .HasForeignKey(i => i.InitiatorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Idea>()
                .HasOne(i => i.TargetDivision)
                .WithMany(d => d.TargetIdeas)
                .HasForeignKey(i => i.TargetDivisionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Idea>()
                .HasOne(i => i.TargetDepartment)
                .WithMany(d => d.TargetIdeas)
                .HasForeignKey(i => i.TargetDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Idea>()
                .HasOne(i => i.Category)
                .WithMany(c => c.Ideas)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Idea>()
                .HasOne(i => i.Event)
                .WithMany(e => e.Ideas)
                .HasForeignKey(i => i.EventId)
                .OnDelete(DeleteBehavior.SetNull);

            // ===== APPROVAL HISTORY =====
            modelBuilder.Entity<ApprovalHistory>()
                .HasOne(ah => ah.Idea)
                .WithMany(i => i.ApprovalHistory)
                .HasForeignKey(ah => ah.IdeaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApprovalHistory>()
                .HasOne(ah => ah.Approver)
                .WithMany(e => e.ApprovalHistory)
                .HasForeignKey(ah => ah.ApproverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApprovalHistory>()
                .HasOne(ah => ah.ApproverRole)
                .WithMany()
                .HasForeignKey(ah => ah.ApproverRoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== MILESTONES =====
            modelBuilder.Entity<IdeaMilestone>()
                .HasOne(im => im.Idea)
                .WithMany(i => i.Milestones)
                .HasForeignKey(im => im.IdeaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<IdeaMilestone>()
                .HasOne(im => im.Creator)
                .WithMany(e => e.CreatedMilestones)
                .HasForeignKey(im => im.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<IdeaMilestone>()
                .HasOne(im => im.Assignee)
                .WithMany(e => e.AssignedMilestones)
                .HasForeignKey(im => im.AssignedTo)
                .OnDelete(DeleteBehavior.SetNull);

            // ===== SAVING MONITORING =====
            modelBuilder.Entity<SavingMonitoring>()
                .HasOne(sm => sm.Idea)
                .WithMany(i => i.SavingMonitoring)
                .HasForeignKey(sm => sm.IdeaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SavingMonitoring>()
                .HasOne(sm => sm.Reporter)
                .WithMany(e => e.ReportedMonitoring)
                .HasForeignKey(sm => sm.ReportedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SavingMonitoring>()
                .HasOne(sm => sm.Reviewer)
                .WithMany(e => e.ReviewedMonitoring)
                .HasForeignKey(sm => sm.ReviewedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // ===== NOTIFICATIONS =====
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Recipient)
                .WithMany(e => e.Notifications)
                .HasForeignKey(n => n.RecipientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Idea)
                .WithMany(i => i.Notifications)
                .HasForeignKey(n => n.IdeaId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== SYSTEM SETTINGS =====
            modelBuilder.Entity<SystemSetting>()
                .HasOne(ss => ss.UpdatedByEmployee)
                .WithMany(e => e.UpdatedSettings)
                .HasForeignKey(ss => ss.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== INDEXES FOR PERFORMANCE =====
            
            // Ideas indexes
            modelBuilder.Entity<Idea>()
                .HasIndex(i => new { i.InitiatorId, i.Status })
                .HasDatabaseName("IX_Ideas_Initiator_Status");

            modelBuilder.Entity<Idea>()
                .HasIndex(i => new { i.Status, i.CurrentStage })
                .HasDatabaseName("IX_Ideas_Status_Stage");


            // Approval History indexes
            modelBuilder.Entity<ApprovalHistory>()
                .HasIndex(ah => new { ah.IdeaId, ah.Stage, ah.ActionDate })
                .HasDatabaseName("IX_ApprovalHistory_Idea_Stage_Date");

            modelBuilder.Entity<ApprovalHistory>()
                .HasIndex(ah => new { ah.ApproverId, ah.ActionDate })
                .HasDatabaseName("IX_ApprovalHistory_Approver_Date");

            // Notifications indexes
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.RecipientId, n.IsRead, n.CreatedDate })
                .HasDatabaseName("IX_Notifications_Recipient_Read_Date");

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.IdeaId, n.NotificationType })
                .HasDatabaseName("IX_Notifications_Idea_Type");

            // Milestones indexes
            modelBuilder.Entity<IdeaMilestone>()
                .HasIndex(im => new { im.IdeaId, im.Stage })
                .HasDatabaseName("IX_Milestones_Idea_Stage");

            modelBuilder.Entity<IdeaMilestone>()
                .HasIndex(im => new { im.AssignedTo, im.Status })
                .HasDatabaseName("IX_Milestones_Assignee_Status");

            // Saving Monitoring indexes
            modelBuilder.Entity<SavingMonitoring>()
                .HasIndex(sm => new { sm.IdeaId, sm.PeriodStartDate })
                .HasDatabaseName("IX_SavingMonitoring_Idea_Period");

            modelBuilder.Entity<SavingMonitoring>()
                .HasIndex(sm => new { sm.ReportedBy, sm.CreatedDate })
                .HasDatabaseName("IX_SavingMonitoring_Reporter_Date");

            // Users indexes
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique()
                .HasDatabaseName("IX_Users_Username");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.EmployeeId)
                .IsUnique()
                .HasDatabaseName("IX_Users_EmployeeId");

            // System Settings indexes
            modelBuilder.Entity<SystemSetting>()
                .HasIndex(ss => ss.SettingKey)
                .IsUnique()
                .HasDatabaseName("IX_SystemSettings_Key");

            // ===== CUSTOM VALUE CONVERSIONS =====
            
            // Convert AttachmentFiles JSON to string
            modelBuilder.Entity<Idea>()
                .Property(i => i.AttachmentFiles)
                .HasConversion(
                    v => v,
                    v => v);

            // Ensure decimal precision for money fields
            modelBuilder.Entity<Idea>()
                .Property(i => i.SavingCost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Idea>()
                .Property(i => i.ValidatedSavingCost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ApprovalHistory>()
                .Property(ah => ah.ValidatedSavingCost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SavingMonitoring>()
                .Property(sm => sm.PlannedSaving)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SavingMonitoring>()
                .Property(sm => sm.ActualSaving)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SavingMonitoring>()
                .Property(sm => sm.Variance)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SavingMonitoring>()
                .Property(sm => sm.VariancePercentage)
                .HasPrecision(5, 2);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Update timestamps
            var entities = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entity in entities)
            {
                if (entity.State == EntityState.Modified)
                {
                    if (entity.Entity is Idea idea)
                    {
                        idea.UpdatedDate = DateTime.UtcNow;
                    }
                    else if (entity.Entity is User user)
                    {
                        user.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (entity.Entity is Role role)
                    {
                        role.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (entity.Entity is IdeaMilestone milestone)
                    {
                        milestone.UpdatedDate = DateTime.UtcNow;
                    }
                    else if (entity.Entity is SavingMonitoring monitoring)
                    {
                        monitoring.UpdatedDate = DateTime.UtcNow;
                    }
                    else if (entity.Entity is SystemSetting setting)
                    {
                        setting.UpdatedDate = DateTime.UtcNow;
                    }
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
