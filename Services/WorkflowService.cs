// Services/WorkflowService.cs (Complete rewrite for new schema)
using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Data.Repositories;
using Ideku.Models.Entities;

namespace Ideku.Services
{
    public class WorkflowService
    {
        private readonly AppDbContext _context;
        private readonly WorkflowRepository _workflowRepository;
        private readonly StageRepository _stageRepository;
        private readonly ApprovalHistoryRepository _approvalHistoryRepository;
        private readonly NotificationService _notificationService;
        private readonly EmailService _emailService;
        private readonly ILogger<WorkflowService> _logger;

        public WorkflowService(
            AppDbContext context,
            WorkflowRepository workflowRepository,
            StageRepository stageRepository,
            ApprovalHistoryRepository approvalHistoryRepository,
            NotificationService notificationService,
            EmailService emailService,
            ILogger<WorkflowService> logger)
        {
            _context = context;
            _workflowRepository = workflowRepository;
            _stageRepository = stageRepository;
            _approvalHistoryRepository = approvalHistoryRepository;
            _notificationService = notificationService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task InitiateWorkflowAsync(Idea idea)
        {
            try
            {
                if (idea.WorkflowDefinitionId == null)
                {
                    _logger.LogWarning("Idea {IdeaId} has no workflow definition assigned", idea.Id);
                    return;
                }

                var workflowStages = await _workflowRepository.GetWorkflowStagesAsync(idea.WorkflowDefinitionId);
                if (!workflowStages.Any())
                {
                    _logger.LogWarning("No workflow stages found for workflow {WorkflowId}", idea.WorkflowDefinitionId);
                    return;
                }

                // Find first stage (sequence 1)
                var firstStage = workflowStages.OrderBy(ws => ws.SequenceNumber).First();
                var nextApprovers = await _stageRepository.GetApproversForStageAsync(
                    firstStage.StageId, 
                    idea.TargetDivisionId, 
                    idea.TargetDepartmentId
                );

                if (nextApprovers.Any())
                {
                    foreach (var approver in nextApprovers)
                    {
                        await _notificationService.SendApprovalRequestAsync(idea.Id, approver.Id, 1);
                    }
                    
                    _logger.LogInformation("Workflow initiated for idea {IdeaId}. Approval requests sent to {ApproverCount} approvers", 
                        idea.Id, nextApprovers.Count);
                }
                else
                {
                    _logger.LogWarning("No approvers found for first stage of idea {IdeaId}", idea.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating workflow for idea {IdeaId}", idea.Id);
            }
        }

        public async Task<bool> AdvanceStageAsync(long ideaId, long approverUserId, string comments = null, decimal? validatedSavingCost = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var idea = await _context.Ideas
                    .Include(i => i.WorkflowDefinition)
                        .ThenInclude(wd => wd.WorkflowStages)
                            .ThenInclude(ws => ws.Stage)
                    .Include(i => i.Initiator)
                        .ThenInclude(u => u.Employee)
                    .FirstOrDefaultAsync(i => i.Id == ideaId);
                    
                if (idea == null)
                {
                    _logger.LogWarning("Idea {IdeaId} not found for stage advancement", ideaId);
                    return false;
                }

                var approverUser = await _context.Users
                    .Include(u => u.Employee)
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == approverUserId);
                    
                if (approverUser == null)
                {
                    _logger.LogWarning("Approver {ApproverId} not found", approverUserId);
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
                    ApproverUserId = approverUserId,
                    Comments = comments,
                    ValidatedSavingCost = validatedSavingCost,
                    ActionDate = DateTime.UtcNow
                };

                await _approvalHistoryRepository.CreateAsync(approvalHistory);

                // 2. Update validated saving cost if provided
                if (validatedSavingCost.HasValue)
                {
                    idea.ValidatedSavingCost = validatedSavingCost.Value;
                }

                // 3. Check if this is the final stage
                var workflowStages = idea.WorkflowDefinition?.WorkflowStages?.OrderBy(ws => ws.SequenceNumber).ToList();
                var maxStage = workflowStages?.Count ?? 0;

                if (nextStage > maxStage)
                {
                    // Final approval - mark as completed
                    idea.Status = "Completed";
                    idea.CompletedDate = DateTime.UtcNow;
                    await _notificationService.SendCompletionNotificationAsync(ideaId);
                }
                else
                {
                    // Move to next stage
                    idea.CurrentStage = nextStage;
                    idea.Status = "Under Review";
                    
                    // Find next stage approvers
                    var nextStageWorkflow = workflowStages?.FirstOrDefault(ws => ws.SequenceNumber == nextStage);
                    if (nextStageWorkflow != null)
                    {
                        var nextApprovers = await _stageRepository.GetApproversForStageAsync(
                            nextStageWorkflow.StageId,
                            idea.TargetDivisionId,
                            idea.TargetDepartmentId
                        );

                        foreach (var nextApprover in nextApprovers)
                        {
                            await _notificationService.SendApprovalRequestAsync(ideaId, nextApprover.Id, nextStage);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                _logger.LogInformation("Idea {IdeaId} advanced from stage {CurrentStage} to {NextStage} by user {ApproverId}", 
                    ideaId, currentStage, nextStage, approverUserId);
                
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error advancing stage for idea {IdeaId}", ideaId);
                return false;
            }
        }

        public async Task<bool> RejectIdeaAsync(long ideaId, long approverUserId, string rejectReason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var idea = await _context.Ideas
                    .Include(i => i.Initiator)
                        .ThenInclude(u => u.Employee)
                    .FirstOrDefaultAsync(i => i.Id == ideaId);
                    
                if (idea == null) return false;

                var approverUser = await _context.Users
                    .Include(u => u.Employee)
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == approverUserId);
                    
                if (approverUser == null) return false;

                // 1. Log rejection history
                var approvalHistory = new ApprovalHistory
                {
                    IdeaId = ideaId,
                    Stage = idea.CurrentStage,
                    Action = "REJECT",
                    ApproverUserId = approverUserId,
                    Comments = rejectReason,
                    ActionDate = DateTime.UtcNow
                };

                await _approvalHistoryRepository.CreateAsync(approvalHistory);

                // 2. Update idea status
                idea.Status = "Rejected";
                idea.RejectReason = rejectReason;

                await _context.SaveChangesAsync();

                // 3. Notify initiator about rejection
                await _notificationService.SendRejectionNotificationAsync(ideaId, rejectReason);

                await transaction.CommitAsync();
                
                _logger.LogInformation("Idea {IdeaId} rejected at stage {Stage} by user {ApproverId}", 
                    ideaId, idea.CurrentStage, approverUserId);
                
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error rejecting idea {IdeaId}", ideaId);
                return false;
            }
        }

        public async Task<bool> RequestMoreInfoAsync(long ideaId, long approverUserId, string infoRequest)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var idea = await _context.Ideas
                    .Include(i => i.Initiator)
                        .ThenInclude(u => u.Employee)
                    .FirstOrDefaultAsync(i => i.Id == ideaId);
                if (idea == null) return false;

                var approverUser = await _context.Users
                    .Include(u => u.Employee)
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == approverUserId);
                    
                if (approverUser == null) return false;

                // 1. Log info request
                var approvalHistory = new ApprovalHistory
                {
                    IdeaId = ideaId,
                    Stage = idea.CurrentStage,
                    Action = "REQUEST_INFO",
                    ApproverUserId = approverUserId,
                    Comments = infoRequest,
                    ActionDate = DateTime.UtcNow
                };

                await _approvalHistoryRepository.CreateAsync(approvalHistory);

                // 2. Update idea status (keep same stage, change status)
                idea.Status = "More Info Required";

                await _context.SaveChangesAsync();

                // 3. Notify initiator about info request
                await _notificationService.SendInfoRequestNotificationAsync(ideaId, infoRequest);

                await transaction.CommitAsync();
                
                _logger.LogInformation("More info requested for idea {IdeaId} at stage {Stage} by user {ApproverId}", 
                    ideaId, idea.CurrentStage, approverUserId);
                
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error requesting info for idea {IdeaId}", ideaId);
                return false;
            }
        }

        public async Task<List<Idea>> GetPendingApprovalsForUserAsync(long userId)
        {
            var user = await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);
                
            if (user == null) return new List<Idea>();

            // Get all ideas that are under review
            var query = _context.Ideas
                .Include(i => i.Initiator)
                    .ThenInclude(u => u.Employee)
                .Include(i => i.Category)
                .Include(i => i.TargetDivision)
                .Include(i => i.TargetDepartment)
                .Include(i => i.WorkflowDefinition)
                    .ThenInclude(wd => wd.WorkflowStages)
                        .ThenInclude(ws => ws.Stage)
                            .ThenInclude(s => s.StageApprovers)
                .Where(i => i.Status == "Under Review");

            var ideas = await query.ToListAsync();
            var pendingIdeas = new List<Idea>();

            foreach (var idea in ideas)
            {
                if (await CanUserApproveIdeaAsync(user, idea))
                {
                    pendingIdeas.Add(idea);
                }
            }

            return pendingIdeas.OrderBy(i => i.SubmittedDate).ToList();
        }

        private async Task<bool> CanUserApproveIdeaAsync(User user, Idea idea)
        {
            if (user.Role.RoleName == "Superuser") return true;

            // Get current stage workflow
            var currentStageSequence = idea.CurrentStage + 1; // Next stage to approve
            var workflowStage = idea.WorkflowDefinition?.WorkflowStages?
                .FirstOrDefault(ws => ws.SequenceNumber == currentStageSequence);

            if (workflowStage == null) return false;

            // Check if user's role can approve this stage
            var stageApprovers = await _stageRepository.GetStageApproversAsync(workflowStage.StageId);
            var canApprove = stageApprovers.Any(sa => sa.RoleId == user.RoleId);

            if (!canApprove) return false;

            // Check division/department constraints
            var userEmployee = user.Employee;
            if (userEmployee == null) return false;

            // Department-specific roles
            if (IsDepartmentSpecificRole(user.RoleId))
            {
                return userEmployee.DepartementId == idea.TargetDepartmentId &&
                       userEmployee.DivisiId == idea.TargetDivisionId;
            }
            // Division-specific roles
            else if (IsDivisionSpecificRole(user.RoleId))
            {
                return userEmployee.DivisiId == idea.TargetDivisionId;
            }
            // Company-wide roles
            else
            {
                return true;
            }
        }

        private bool IsDepartmentSpecificRole(string roleId)
        {
            var departmentRoles = new[] { "R04", "R06", "R16" }; // Workstream Leader, Manager, Manager Acting
            return departmentRoles.Contains(roleId);
        }

        private bool IsDivisionSpecificRole(string roleId)
        {
            var divisionRoles = new[] { "R07", "R13" }; // GM Division, GM Division Acting
            return divisionRoles.Contains(roleId);
        }
    }
}