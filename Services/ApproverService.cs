// Services/ApproverService.cs (Updated for new schema)
using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Services
{
    public class ApproverService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ApproverService> _logger;

        public ApproverService(AppDbContext context, ILogger<ApproverService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Employee?> GetNextApproverAsync(string ideaId, int nextStage)
        {
            var idea = await _context.Ideas
                .Include(i => i.TargetDivision)
                .Include(i => i.TargetDepartment)
                .FirstOrDefaultAsync(i => i.Id == ideaId);

            if (idea == null)
            {
                _logger.LogWarning("Idea {IdeaId} not found for approver detection", ideaId);
                return null;
            }

            var requiredRole = await GetRequiredRoleForStageAsync(nextStage, idea.WorkflowType);
            if (requiredRole == null)
            {
                _logger.LogWarning("No role found for stage {NextStage} and workflow {WorkflowType}", nextStage, idea.WorkflowType);
                return null;
            }

            return await GetEmployeeByRoleAsync(requiredRole.Id, idea.TargetDivisionId, idea.TargetDepartmentId);
        }

        private async Task<Role?> GetRequiredRoleForStageAsync(int stage, string workflowType)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.ApprovalLevel == stage &&
                                          (workflowType == "STANDARD" ? r.CanApproveStandard : r.CanApproveHighValue));
        }

        public async Task<Employee?> GetWorkstreamLeaderAsync(string divisionId, string departmentId)
        {
            return await GetEmployeeByRoleAsync("R04", divisionId, departmentId);
        }

        public async Task<Employee?> GetEmployeeByRoleAsync(string roleId, string? divisionId = null, string? departmentId = null)
        {
            var query = _context.Employees
                .Include(e => e.User)
                .ThenInclude(u => u!.Role)
                .Where(e => e.User != null && 
                           e.User.RoleId == roleId && 
                           e.User.IsActive && 
                           e.EmploymentStatus == "Active");

            // Add division/department filters for specific roles
            if (new[] { "R04", "R06" }.Contains(roleId)) // Department/Workstream specific roles
            {
                if (!string.IsNullOrEmpty(departmentId))
                {
                    query = query.Where(e => e.DepartementId == departmentId);
                }
                if (!string.IsNullOrEmpty(divisionId))
                {
                    query = query.Where(e => e.DivisiId == divisionId);
                }
            }
            else if (roleId == "R07") // Division specific role
            {
                if (!string.IsNullOrEmpty(divisionId))
                {
                    query = query.Where(e => e.DivisiId == divisionId);
                }
            }
            // Company-wide roles (R08, R09, R10, R11) don't need division/department filter

            // --- REVISED LOGIC: Sequential Search (Department -> Division -> Company) ---

            Employee? approver = null;

            // 1. Try to find at the most specific level (Department, if applicable)
            if (new[] { "R04", "R06" }.Contains(roleId) && !string.IsNullOrEmpty(departmentId))
            {
                approver = await query.FirstOrDefaultAsync();
            }

            // 2. If not found, try at the Division level (if applicable)
            if (approver == null && new[] { "R04", "R06", "R07" }.Contains(roleId) && !string.IsNullOrEmpty(divisionId))
            {
                // Re-build query for division level
                var divisionQuery = _context.Employees
                    .Include(e => e.User).ThenInclude(u => u!.Role)
                    .Where(e => e.User != null && e.User.RoleId == roleId && e.User.IsActive && e.EmploymentStatus == "Active" && e.DivisiId == divisionId);
                
                approver = await divisionQuery
                    .OrderBy(e => e.User!.IsActing)
                    .ThenByDescending(e => e.User!.CreatedAt)
                    .FirstOrDefaultAsync();
            }

            // 3. If still not found, try at the company-wide level
            if (approver == null)
            {
                // Re-build query for company-wide
                 var companyQuery = _context.Employees
                    .Include(e => e.User).ThenInclude(u => u!.Role)
                    .Where(e => e.User != null && e.User.RoleId == roleId && e.User.IsActive && e.EmploymentStatus == "Active");

                approver = await companyQuery
                    .OrderBy(e => e.User!.IsActing)
                    .ThenByDescending(e => e.User!.CreatedAt)
                    .FirstOrDefaultAsync();
            }
            
            if (approver == null)
            {
                 _logger.LogWarning("No active approver found for role {RoleId} at any level (Dept: {DepartmentId}, Div: {DivisionId}, Company-wide)", 
                    roleId, departmentId, divisionId);
            }

            return approver;
        }

    }
}
