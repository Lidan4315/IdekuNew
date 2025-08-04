// Services/WorkflowService.cs (Updated for new schema)
using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Services
{
    public class WorkflowService
    {
        private readonly AppDbContext _context;
        private readonly ApproverService _approverService;
        private readonly NotificationService _notificationService;
        private readonly EmailService _emailService;
        private readonly ILogger<WorkflowService> _logger;

        public WorkflowService(
            AppDbContext context,
            ApproverService approverService,
            NotificationService notificationService,
            EmailService emailService,
            ILogger<WorkflowService> logger)
        {
            _context = context;
            _approverService = approverService;
            _notificationService = notificationService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<(string WorkflowType, int MaxStage)> DetermineWorkflowTypeAsync(decimal savingCost)
        {
            var threshold = await GetHighValueThresholdAsync();
            
            if (savingCost >= threshold)
            {
                return ("HIGH_VALUE", 6);
            }
            else
            {
                return ("STANDARD", 3);
            }
        }

        public async Task InitiateWorkflowAsync(Idea idea)
        {
            // Stage 0 is submission, so we need to find the approver for Stage 1
            var nextApprover = await _approverService.GetNextApproverAsync(idea.Id, 1);
            if (nextApprover != null)
            {
                await _notificationService.SendApprovalRequestAsync(idea.Id, nextApprover.Id, 1);
                _logger.LogInformation("Workflow initiated for idea {IdeaId}. First approval request sent to {ApproverId}", idea.Id, nextApprover.Id);
            }
            else
            {
                _logger.LogWarning("Workflow initiation failed for idea {IdeaId}: No approver found for stage 1.", idea.Id);
            }
        }

        public async Task<bool> AdvanceStageAsync(string ideaId, string approverId, string comments = null, decimal? validatedSavingCost = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var idea = await _context.Ideas
                    .Include(i => i.Initiator)
                    .FirstOrDefaultAsync(i => i.Id == ideaId);
                    
                if (idea == null)
                {
                    _logger.LogWarning("Idea {IdeaId} not found for stage advancement", ideaId);
                    return false;
                }

                var approver = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.EmployeeId == approverId);
                    
                if (approver == null)
                {
                    _logger.LogWarning("Approver {ApproverId} not found", approverId);
                    return false;
                }

                var currentStage = idea.CurrentStage;
                var nextStage = currentStage + 1;

                // 1. Log approval history
                var approvalHistory = new ApprovalHistory
                {
                    IdeaId = ideaId,
                    Stage = currentStage,
                    Action = "APPROVE",
                    ApproverId = approverId,
                    ApproverRoleId = approver.RoleId,
                    Comments = comments,
                    ValidatedSavingCost = validatedSavingCost,
                    ActionDate = DateTime.UtcNow
                };

                _context.ApprovalHistory.Add(approvalHistory);

                // 2. Update validated saving cost if provided
                if (validatedSavingCost.HasValue)
                {
                    idea.ValidatedSavingCost = validatedSavingCost.Value;
                }

                // 3. Update idea status
                idea.CurrentStage = nextStage;
                idea.UpdatedDate = DateTime.UtcNow;

                if (nextStage >= idea.MaxStage)
                {
                    idea.Status = "Completed";
                    idea.CompletedDate = DateTime.UtcNow;
                }
                else
                {
                    idea.Status = "Under Review";
                }

                await _context.SaveChangesAsync();

                // 4. Handle next stage or completion
                await HandleStageTransitionAsync(idea, currentStage, nextStage);

                // 5. Notify stakeholders about progress (handled within HandleStageTransitionAsync)

                await transaction.CommitAsync();
                
                _logger.LogInformation("Idea {IdeaId} advanced from stage {CurrentStage} to {NextStage} by {ApproverId}", 
                    ideaId, currentStage, nextStage, approverId);
                
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error advancing stage for idea {IdeaId}", ideaId);
                return false;
            }
        }

        public async Task<bool> RejectIdeaAsync(string ideaId, string approverId, string rejectReason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var idea = await _context.Ideas
                    .Include(i => i.Initiator)
                    .FirstOrDefaultAsync(i => i.Id == ideaId);
                    
                if (idea == null) return false;

                var approver = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.EmployeeId == approverId);
                    
                if (approver == null) return false;

                // 1. Log rejection history
                var approvalHistory = new ApprovalHistory
                {
                    IdeaId = ideaId,
                    Stage = idea.CurrentStage,
                    Action = "REJECT",
                    ApproverId = approverId,
                    ApproverRoleId = approver.RoleId,
                    Comments = rejectReason,
                    ActionDate = DateTime.UtcNow
                };

                _context.ApprovalHistory.Add(approvalHistory);

                // 2. Update idea status
                idea.Status = "Rejected";
                idea.RejectReason = rejectReason;
                idea.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // 3. Notify initiator about rejection
                await _notificationService.SendRejectionNotificationAsync(ideaId, rejectReason);

                await transaction.CommitAsync();
                
                _logger.LogInformation("Idea {IdeaId} rejected at stage {Stage} by {ApproverId}", 
                    ideaId, idea.CurrentStage, approverId);
                
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error rejecting idea {IdeaId}", ideaId);
                return false;
            }
        }

        public async Task<bool> RequestMoreInfoAsync(string ideaId, string approverId, string infoRequest)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var idea = await _context.Ideas.FirstOrDefaultAsync(i => i.Id == ideaId);
                if (idea == null) return false;

                var approver = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.EmployeeId == approverId);
                    
                if (approver == null) return false;

                // 1. Log info request
                var approvalHistory = new ApprovalHistory
                {
                    IdeaId = ideaId,
                    Stage = idea.CurrentStage,
                    Action = "REQUEST_INFO",
                    ApproverId = approverId,
                    ApproverRoleId = approver.RoleId,
                    Comments = infoRequest,
                    ActionDate = DateTime.UtcNow
                };

                _context.ApprovalHistory.Add(approvalHistory);

                // 2. Update idea status (keep same stage, change status)
                idea.Status = "More Info Required";
                idea.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // 3. Notify initiator about info request
                await _notificationService.SendInfoRequestNotificationAsync(ideaId, infoRequest);

                await transaction.CommitAsync();
                
                _logger.LogInformation("More info requested for idea {IdeaId} at stage {Stage} by {ApproverId}", 
                    ideaId, idea.CurrentStage, approverId);
                
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error requesting info for idea {IdeaId}", ideaId);
                return false;
            }
        }


        public async Task<List<Idea>> GetPendingApprovalsForUserAsync(string employeeId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
                
            if (user == null) return new List<Idea>();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == employeeId);
                
            if (employee == null) return new List<Idea>();

            var query = _context.Ideas
                .Include(i => i.Initiator)
                .Include(i => i.Category)
                .Include(i => i.TargetDivision)
                .Include(i => i.TargetDepartment)
                .Where(i => i.Status == "Under Review");

            // Filter based on user's role and current stage
            var userDivision = employee.DivisiId;
            var userDepartment = employee.DepartementId;

            // Filter based on user's role and the stage they are allowed to approve
            if (user.Role.RoleName == "Superuser")
            {
                // Superuser sees everything, no extra filters needed.
            }
            else
            {
                // FIX: An approver acts on the current stage to move it to the next.
                // So, an approver with ApprovalLevel 'L' should see ideas at CurrentStage 'L-1'.
                query = query.Where(i => (i.CurrentStage + 1) == user.Role.ApprovalLevel &&
                                   (i.WorkflowType == "STANDARD" ? user.Role.CanApproveStandard : user.Role.CanApproveHighValue));

                // Add division/department filters for specific roles
                if (new[] { "R04", "R06" }.Contains(user.Role.Id)) // Department/Workstream specific roles
                {
                    query = query.Where(e => e.TargetDepartmentId == userDepartment && e.TargetDivisionId == userDivision);
                }
                else if (user.Role.Id == "R07") // Division specific role
                {
                    query = query.Where(e => e.TargetDivisionId == userDivision);
                }
                // Company-wide roles (R08, R09, R10, R11) don't need division/department filter
            }

            return await query
                .OrderBy(i => i.SubmittedDate)
                .ToListAsync();
        }

        private async Task<decimal> GetHighValueThresholdAsync()
        {
            var setting = await _context.SystemSettings
                .Where(s => s.SettingKey == "HIGH_VALUE_THRESHOLD")
                .FirstOrDefaultAsync();

            if (setting != null && decimal.TryParse(setting.SettingValue, out decimal threshold))
            {
                return threshold;
            }

            return 20000; // Default threshold
        }

        private async Task HandleStageTransitionAsync(Idea idea, int currentStage, int nextStage)
        {
            // After moving to nextStage, we need to find the approver for the stage *after* that.
            var approverForNextAction = await _approverService.GetNextApproverAsync(idea.Id, nextStage + 1);
            var initiator = await _context.Employees.FindAsync(idea.InitiatorId);
            var workstreamLeader = await _approverService.GetWorkstreamLeaderAsync(idea.TargetDivisionId, idea.TargetDepartmentId);

            // Standard Workflow Notifications
            if (idea.WorkflowType == "STANDARD")
            {
                if (nextStage == 1) // Just finished stage 0, now at stage 1. Notify approver for stage 2.
                {
                    // Email to Manager (approver for stage 2) & Initiator
                    if (approverForNextAction != null) await _notificationService.SendApprovalRequestAsync(idea.Id, approverForNextAction.Id, nextStage + 1);
                    if (initiator != null) await _notificationService.SendProgressUpdateAsync(idea.Id, initiator.Id, currentStage, "APPROVED");
                }
                else if (nextStage == 2) // Just finished stage 1, now at stage 2. Notify approver for stage 3.
                {
                    // Email to Initiator & Workstream Leader & send request to next approver
                    if (approverForNextAction != null) await _notificationService.SendApprovalRequestAsync(idea.Id, approverForNextAction.Id, nextStage + 1);
                    if (initiator != null) await _notificationService.SendProgressUpdateAsync(idea.Id, initiator.Id, currentStage, "APPROVED");
                    if (workstreamLeader != null) await _notificationService.SendProgressUpdateAsync(idea.Id, workstreamLeader.Id, currentStage, "APPROVED");
                    // Prompt Workstream Leader to create milestone
                    if (workstreamLeader != null) await _notificationService.SendMilestoneCreationRequestAsync(idea.Id, workstreamLeader.Id);
                }
                else if (nextStage == 3) // Approved by GM Divisi
                {
                     // Prompt Workstream Leader to create milestone and input saving monitoring
                    if (workstreamLeader != null) await _notificationService.SendMilestoneAndSavingRequestAsync(idea.Id, workstreamLeader.Id);
                }
            }
            // High Value Workflow Notifications
            else if (idea.WorkflowType == "HIGH_VALUE")
            {
                if (nextStage == 1) // Just finished stage 0, now at stage 1. Notify approver for stage 2.
                {
                    // Email to Initiator & GM Divisi (approver for stage 2)
                    if (initiator != null) await _notificationService.SendProgressUpdateAsync(idea.Id, initiator.Id, currentStage, "APPROVED");
                    if (approverForNextAction != null) await _notificationService.SendApprovalRequestAsync(idea.Id, approverForNextAction.Id, nextStage + 1);
                }
                else if (nextStage == 2) // Just finished stage 1, now at stage 2. Notify approver for stage 3.
                {
                    // Email to Initiator, Workstream Leader & send request to next approver
                    if (approverForNextAction != null) await _notificationService.SendApprovalRequestAsync(idea.Id, approverForNextAction.Id, nextStage + 1);
                    if (initiator != null) await _notificationService.SendProgressUpdateAsync(idea.Id, initiator.Id, currentStage, "APPROVED");
                    if (workstreamLeader != null) await _notificationService.SendProgressUpdateAsync(idea.Id, workstreamLeader.Id, currentStage, "APPROVED");
                    if (workstreamLeader != null) await _notificationService.SendMilestoneCreationRequestAsync(idea.Id, workstreamLeader.Id);
                }
                else if (nextStage == 3) // Just finished stage 2, now at stage 3. Notify approver for stage 4.
                {
                    if (approverForNextAction != null) await _notificationService.SendApprovalRequestAsync(idea.Id, approverForNextAction.Id, nextStage + 1);
                    if (initiator != null) await _notificationService.SendProgressUpdateAsync(idea.Id, initiator.Id, currentStage, "APPROVED");
                    if (workstreamLeader != null) await _notificationService.SendProgressUpdateAsync(idea.Id, workstreamLeader.Id, currentStage, "APPROVED");
                    if (workstreamLeader != null) await _notificationService.SendMilestoneAndSavingRequestAsync(idea.Id, workstreamLeader.Id);
                }
                else if (nextStage == 4) // Just finished stage 3, now at stage 4. Notify approver for stage 5.
                {
                    if (approverForNextAction != null) await _notificationService.SendApprovalRequestAsync(idea.Id, approverForNextAction.Id, nextStage + 1);
                    if (initiator != null) await _notificationService.SendProgressUpdateAsync(idea.Id, initiator.Id, currentStage, "APPROVED");
                    if (workstreamLeader != null) await _notificationService.SendProgressUpdateAsync(idea.Id, workstreamLeader.Id, currentStage, "APPROVED");
                    if (workstreamLeader != null) await _notificationService.SendMilestoneCreationRequestAsync(idea.Id, workstreamLeader.Id);
                }
                 else if (nextStage == 5) // Just finished stage 4, now at stage 5. Notify approver for stage 6.
                {
                    if (approverForNextAction != null) await _notificationService.SendApprovalRequestAsync(idea.Id, approverForNextAction.Id, nextStage + 1);
                    if (initiator != null) await _notificationService.SendProgressUpdateAsync(idea.Id, initiator.Id, currentStage, "APPROVED");
                    if (workstreamLeader != null) await _notificationService.SendProgressUpdateAsync(idea.Id, workstreamLeader.Id, currentStage, "APPROVED");
                    if (workstreamLeader != null) await _notificationService.SendCompletionRequestAsync(idea.Id, workstreamLeader.Id);
                }
                else if (nextStage == 6) // Approved by CFO
                {
                    // Final approval, notify Initiator and Workstream Leader
                    if (initiator != null) await _notificationService.SendProgressUpdateAsync(idea.Id, initiator.Id, currentStage, "COMPLETED");
                    if (workstreamLeader != null) await _notificationService.SendProgressUpdateAsync(idea.Id, workstreamLeader.Id, currentStage, "COMPLETED");
                }
            }

            // Handle final completion
            if (nextStage >= idea.MaxStage)
            {
                await _notificationService.SendCompletionNotificationAsync(idea.Id);
            }
        }
    }
}
