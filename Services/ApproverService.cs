// Services/ApproverService.cs (Updated for new schema)
using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Data.Repositories;
using Ideku.Models.Entities;

namespace Ideku.Services
{
    public class ApproverService
    {
        private readonly AppDbContext _context;
        private readonly StageRepository _stageRepository;
        private readonly UserRepository _userRepository;
        private readonly ILogger<ApproverService> _logger;

        public ApproverService(
            AppDbContext context, 
            StageRepository stageRepository,
            UserRepository userRepository,
            ILogger<ApproverService> logger)
        {
            _context = context;
            _stageRepository = stageRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<List<User>> GetNextApproversAsync(long ideaId, int nextStageSequence)
        {
            var idea = await _context.Ideas
                .Include(i => i.WorkflowDefinition)
                    .ThenInclude(wd => wd.WorkflowStages)
                        .ThenInclude(ws => ws.Stage)
                .Include(i => i.TargetDivision)
                .Include(i => i.TargetDepartment)
                .FirstOrDefaultAsync(i => i.Id == ideaId);

            if (idea?.WorkflowDefinition == null)
            {
                _logger.LogWarning("Idea {IdeaId} not found or has no workflow definition", ideaId);
                return new List<User>();
            }

            var workflowStage = idea.WorkflowDefinition.WorkflowStages
                .FirstOrDefault(ws => ws.SequenceNumber == nextStageSequence);

            if (workflowStage == null)
            {
                _logger.LogWarning("No workflow stage found for sequence {Sequence} in idea {IdeaId}", nextStageSequence, ideaId);
                return new List<User>();
            }

            return await _stageRepository.GetApproversForStageAsync(
                workflowStage.StageId,
                idea.TargetDivisionId,
                idea.TargetDepartmentId
            );
        }

        public async Task<User?> GetWorkstreamLeaderAsync(string divisionId, string departmentId)
        {
            // Get users with Workstream Leader role in specific department
            var workstreamLeaders = await _stageRepository.GetApproversForStageAsync(
                "S01", // Assuming S01 is the stage ID for workstream leaders
                divisionId,
                departmentId
            );

            return workstreamLeaders.FirstOrDefault(u => u.Role.RoleName.Contains("Workstream Leader"));
        }

        public async Task<List<User>> GetUsersByRoleAsync(string roleId, string? divisionId = null, string? departmentId = null)
        {
            var users = await _userRepository.GetUsersByRoleAsync(roleId);

            // Apply division/department filtering
            if (!string.IsNullOrEmpty(departmentId))
            {
                users = users.Where(u => u.Employee?.DepartementId == departmentId).ToList();
            }
            else if (!string.IsNullOrEmpty(divisionId))
            {
                users = users.Where(u => u.Employee?.DivisiId == divisionId).ToList();
            }

            return users.Where(u => u.Employee?.EmploymentStatus == "Active").ToList();
        }

        public async Task<bool> IsUserAuthorizedForStageAsync(long userId, long ideaId, int stage)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            var idea = await _context.Ideas
                .Include(i => i.WorkflowDefinition)
                    .ThenInclude(wd => wd.WorkflowStages)
                        .ThenInclude(ws => ws.Stage)
                            .ThenInclude(s => s.StageApprovers)
                .FirstOrDefaultAsync(i => i.Id == ideaId);

            if (idea?.WorkflowDefinition == null) return false;

            var workflowStage = idea.WorkflowDefinition.WorkflowStages
                .FirstOrDefault(ws => ws.SequenceNumber == stage);

            if (workflowStage == null) return false;

            // Check if user's role is authorized for this stage
            var isRoleAuthorized = workflowStage.Stage.StageApprovers
                .Any(sa => sa.RoleId == user.RoleId);

            if (!isRoleAuthorized) return false;

            // Check division/department constraints
            if (user.Employee == null) return false;

            if (IsDepartmentSpecificRole(user.RoleId))
            {
                return user.Employee.DepartementId == idea.TargetDepartmentId &&
                       user.Employee.DivisiId == idea.TargetDivisionId;
            }
            else if (IsDivisionSpecificRole(user.RoleId))
            {
                return user.Employee.DivisiId == idea.TargetDivisionId;
            }

            return true; // Company-wide role
        }

        private bool IsDepartmentSpecificRole(string roleId)
        {
            var departmentRoles = new[] { "R04", "R06", "R16" };
            return departmentRoles.Contains(roleId);
        }

        private bool IsDivisionSpecificRole(string roleId)
        {
            var divisionRoles = new[] { "R07", "R13" };
            return divisionRoles.Contains(roleId);
        }
    }
}