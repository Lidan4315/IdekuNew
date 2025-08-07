// Data/Repositories/StageRepository.cs (NEW)
using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public class StageRepository
    {
        private readonly AppDbContext _context;

        public StageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Stage>> GetAllActiveAsync()
        {
            return await _context.Stages
                .Where(s => s.IsActive)
                .Include(s => s.StageApprovers)
                    .ThenInclude(sa => sa.Role)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Stage?> GetByIdAsync(string id)
        {
            return await _context.Stages
                .Include(s => s.StageApprovers)
                    .ThenInclude(sa => sa.Role)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<StageApprover>> GetStageApproversAsync(string stageId)
        {
            return await _context.StageApprovers
                .Where(sa => sa.StageId == stageId)
                .Include(sa => sa.Role)
                .Include(sa => sa.Stage)
                .OrderBy(sa => sa.ApprovalOrder)
                .ToListAsync();
        }

        public async Task<List<User>> GetApproversForStageAsync(string stageId, string? divisionId = null, string? departmentId = null)
        {
            var stageApprovers = await GetStageApproversAsync(stageId);
            var approvers = new List<User>();

            foreach (var stageApprover in stageApprovers)
            {
                var roleUsers = await _context.Users
                    .Include(u => u.Employee)
                        .ThenInclude(e => e.Divisi)
                    .Include(u => u.Employee)
                        .ThenInclude(e => e.Departement)
                    .Include(u => u.Role)
                    .Where(u => u.RoleId == stageApprover.RoleId && 
                               u.IsActive && 
                               u.Employee.EmploymentStatus == "Active")
                    .ToListAsync();

                // Apply division/department filtering based on role requirements
                var filteredUsers = roleUsers.Where(u => 
                {
                    // For department-specific roles, match department and division
                    if (IsRoleDepartmentSpecific(stageApprover.RoleId))
                    {
                        return u.Employee.DepartementId == departmentId && 
                               u.Employee.DivisiId == divisionId;
                    }
                    // For division-specific roles, match division only
                    else if (IsRoleDivisionSpecific(stageApprover.RoleId))
                    {
                        return u.Employee.DivisiId == divisionId;
                    }
                    // For company-wide roles, no filtering needed
                    else
                    {
                        return true;
                    }
                }).ToList();

                approvers.AddRange(filteredUsers);
            }

            return approvers.Distinct().OrderBy(u => u.IsActing).ThenBy(u => u.Employee.Name).ToList();
        }

        private bool IsRoleDepartmentSpecific(string roleId)
        {
            // Define which roles are department-specific
            var departmentRoles = new[] { "R04", "R06", "R16" }; // Workstream Leader, Manager, Manager Acting
            return departmentRoles.Contains(roleId);
        }

        private bool IsRoleDivisionSpecific(string roleId)
        {
            // Define which roles are division-specific  
            var divisionRoles = new[] { "R07", "R13" }; // GM Division, GM Division Acting
            return divisionRoles.Contains(roleId);
        }
    }
}