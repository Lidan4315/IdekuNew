// Data/Context/AppDbContext.cs (Updated for new schema)
using Microsoft.EntityFrameworkCore;
using Ideku.Models.Entities;

namespace Ideku.Data.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Core Organizational Structure
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Divisi> Divisi { get; set; }
        public DbSet<Departement> Departement { get; set; }

        // User & Role System
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RoleFeaturePermission> RoleFeaturePermissions { get; set; }

        // Workflow System (NEW)
        public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; }
        public DbSet<Stage> Stages { get; set; }
        public DbSet<StageApprover> StageApprovers { get; set; }
        public DbSet<WorkflowStage> WorkflowStages { get; set; }
        public DbSet<WorkflowCondition> WorkflowConditions { get; set; }

        // Idea Management
        public DbSet<Category> Category { get; set; }
        public DbSet<Event> Event { get; set; }
        public DbSet<Idea> Ideas { get; set; }

        // Process Tracking
        public DbSet<ApprovalHistory> ApprovalHistory { get; set; }
        public DbSet<IdeaMilestone> IdeaMilestones { get; set; }
        public DbSet<SavingMonitoring> SavingMonitoring { get; set; }

        // System Management
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
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

            // ===== USER & ROLE SYSTEM =====
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

            // ===== ROLE FEATURE PERMISSIONS =====
            modelBuilder.Entity<RoleFeaturePermission>()
                .HasOne(rfp => rfp.Role)
                .WithMany(r => r.RoleFeaturePermissions)
                .HasForeignKey(rfp => rfp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RoleFeaturePermission>()
                .HasOne(rfp => rfp.Permission)
                .WithMany(p => p.RoleFeaturePermissions)
                .HasForeignKey(rfp => rfp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== WORKFLOW SYSTEM =====
            modelBuilder.Entity<WorkflowStage>()
                .HasOne(ws => ws.WorkflowDefinition)
                .WithMany(wd => wd.WorkflowStages)
                .HasForeignKey(ws => ws.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkflowStage>()
                .HasOne(ws => ws.Stage)
                .WithMany(s => s.WorkflowStages)
                .HasForeignKey(ws => ws.StageId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkflowCondition>()
                .HasOne(wc => wc.WorkflowDefinition)
                .WithMany(wd => wd.WorkflowConditions)
                .HasForeignKey(wc => wc.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StageApprover>()
                .HasOne(sa => sa.Stage)
                .WithMany(s => s.StageApprovers)
                .HasForeignKey(sa => sa.StageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StageApprover>()
                .HasOne(sa => sa.Role)
                .WithMany(r => r.StageApprovers)
                .HasForeignKey(sa => sa.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== IDEAS & WORKFLOW =====
            modelBuilder.Entity<Idea>()
                .HasOne(i => i.Initiator)
                .WithMany(u => u.InitiatedIdeas)
                .HasForeignKey(i => i.InitiatorUserId)
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

            modelBuilder.Entity<Idea>()
                .HasOne(i => i.WorkflowDefinition)
                .WithMany(wd => wd.Ideas)
                .HasForeignKey(i => i.WorkflowDefinitionId)
                .OnDelete(DeleteBehavior.SetNull);

            // ===== APPROVAL HISTORY =====
            modelBuilder.Entity<ApprovalHistory>()
                .HasOne(ah => ah.Idea)
                .WithMany(i => i.ApprovalHistory)
                .HasForeignKey(ah => ah.IdeaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApprovalHistory>()
                .HasOne(ah => ah.ApproverUser)
                .WithMany(u => u.ApprovalHistory)
                .HasForeignKey(ah => ah.ApproverUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== MILESTONES =====
            modelBuilder.Entity<IdeaMilestone>()
                .HasOne(im => im.Idea)
                .WithMany(i => i.Milestones)
                .HasForeignKey(im => im.IdeaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<IdeaMilestone>()
                .HasOne(im => im.CreatedByUser)
                .WithMany(u => u.CreatedMilestones)
                .HasForeignKey(im => im.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<IdeaMilestone>()
                .HasOne(im => im.AssignedToUser)
                .WithMany(u => u.AssignedMilestones)
                .HasForeignKey(im => im.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // ===== SAVING MONITORING =====
            modelBuilder.Entity<SavingMonitoring>()
                .HasOne(sm => sm.Idea)
                .WithMany(i => i.SavingMonitoring)
                .HasForeignKey(sm => sm.IdeaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SavingMonitoring>()
                .HasOne(sm => sm.ReportedByUser)
                .WithMany(u => u.ReportedMonitoring)
                .HasForeignKey(sm => sm.ReportedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SavingMonitoring>()
                .HasOne(sm => sm.ReviewedByUser)
                .WithMany(u => u.ReviewedMonitoring)
                .HasForeignKey(sm => sm.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // ===== NOTIFICATIONS =====
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Idea)
                .WithMany(i => i.Notifications)
                .HasForeignKey(n => n.IdeaId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== SYSTEM SETTINGS =====
            modelBuilder.Entity<SystemSetting>()
                .HasOne(ss => ss.UpdatedByUser)
                .WithMany(u => u.UpdatedSettings)
                .HasForeignKey(ss => ss.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== INDEXES FOR PERFORMANCE =====
            
            // Ideas indexes
            modelBuilder.Entity<Idea>()
                .HasIndex(i => new { i.InitiatorUserId, i.Status })
                .HasDatabaseName("IX_Ideas_Initiator_Status");

            modelBuilder.Entity<Idea>()
                .HasIndex(i => new { i.Status, i.CurrentStage })
                .HasDatabaseName("IX_Ideas_Status_Stage");

            modelBuilder.Entity<Idea>()
                .HasIndex(i => i.WorkflowDefinitionId)
                .HasDatabaseName("IX_Ideas_WorkflowDefinition");

            // Approval History indexes
            modelBuilder.Entity<ApprovalHistory>()
                .HasIndex(ah => new { ah.IdeaId, ah.Stage, ah.ActionDate })
                .HasDatabaseName("IX_ApprovalHistory_Idea_Stage_Date");

            modelBuilder.Entity<ApprovalHistory>()
                .HasIndex(ah => new { ah.ApproverUserId, ah.ActionDate })
                .HasDatabaseName("IX_ApprovalHistory_Approver_Date");

            // Notifications indexes
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.IsRead, n.CreatedDate })
                .HasDatabaseName("IX_Notifications_Read_Date");

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.IdeaId, n.NotificationType })
                .HasDatabaseName("IX_Notifications_Idea_Type");

            // Milestones indexes
            modelBuilder.Entity<IdeaMilestone>()
                .HasIndex(im => new { im.IdeaId, im.Stage })
                .HasDatabaseName("IX_Milestones_Idea_Stage");

            modelBuilder.Entity<IdeaMilestone>()
                .HasIndex(im => new { im.AssignedToUserId, im.Status })
                .HasDatabaseName("IX_Milestones_Assignee_Status");

            // Saving Monitoring indexes
            modelBuilder.Entity<SavingMonitoring>()
                .HasIndex(sm => new { sm.IdeaId, sm.PeriodStartDate })
                .HasDatabaseName("IX_SavingMonitoring_Idea_Period");

            modelBuilder.Entity<SavingMonitoring>()
                .HasIndex(sm => new { sm.ReportedByUserId, sm.CreatedDate })
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

            // Workflow indexes
            modelBuilder.Entity<WorkflowStage>()
                .HasIndex(ws => new { ws.WorkflowId, ws.SequenceNumber })
                .HasDatabaseName("IX_WorkflowStages_Workflow_Sequence");

            modelBuilder.Entity<WorkflowCondition>()
                .HasIndex(wc => new { wc.WorkflowId, wc.ConditionType })
                .HasDatabaseName("IX_WorkflowConditions_Workflow_Type");

            modelBuilder.Entity<StageApprover>()
                .HasIndex(sa => new { sa.StageId, sa.ApprovalOrder })
                .HasDatabaseName("IX_StageApprovers_Stage_Order");

            // System Settings indexes
            modelBuilder.Entity<SystemSetting>()
                .HasIndex(ss => ss.SettingKey)
                .IsUnique()
                .HasDatabaseName("IX_SystemSettings_Key");

            // ===== DECIMAL PRECISION =====
            
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
                    else if (entity.Entity is WorkflowDefinition workflow)
                    {
                        workflow.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}