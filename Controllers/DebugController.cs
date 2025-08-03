// 🔍 Debug Controller untuk troubleshoot My Ideas
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;

namespace Ideku.Controllers
{
    public class DebugController : Controller
    {
        private readonly AppDbContext _context;

        public DebugController(AppDbContext context)
        {
            _context = context;
        }

        // Debug endpoint: /Debug/CheckIdeas
        public async Task<IActionResult> CheckIdeas()
        {
            var debugInfo = new
            {
                // Check current user
                CurrentUser = User.Identity?.Name ?? "NOT_AUTHENTICATED",
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList(),

                // Check total ideas in database
                TotalIdeas = await _context.Ideas.CountAsync(),
                
                // Check all ideas with their initiators
                AllIdeas = await _context.Ideas
                    .Select(i => new { 
                        i.Id, 
                        i.IdeaName, 
                        i.InitiatorId, 
                        i.SubmittedDate,
                        i.Status 
                    })
                    .OrderByDescending(i => i.SubmittedDate)
                    .ToListAsync(),

                // Check users table
                AllUsers = await _context.Users
                    .Select(u => new { 
                        u.Id, 
                        u.Username, 
                        u.Name, 
                        u.EmployeeId 
                    })
                    .ToListAsync(),

                // Check employees table
                AllEmployees = await _context.Employees
                    .Select(e => new { 
                        e.Id, 
                        e.Name, 
                        e.Email 
                    })
                    .ToListAsync()
            };

            return Json(debugInfo);
        }

        // Debug endpoint: /Debug/TestUserIdeas
        public async Task<IActionResult> TestUserIdeas(string username = "")
        {
            var currentUser = string.IsNullOrEmpty(username) 
                ? User.Identity?.Name ?? "" 
                : username;

            var result = new
            {
                SearchingFor = currentUser,
                
                // Method 1: Direct username match
                DirectMatch = await _context.Ideas
                    .Where(i => i.InitiatorId == currentUser)
                    .Select(i => new { i.Id, i.IdeaName, i.InitiatorId })
                    .ToListAsync(),

                // Method 2: Case insensitive match
                CaseInsensitiveMatch = await _context.Ideas
                    .Where(i => i.InitiatorId.ToLower() == currentUser.ToLower())
                    .Select(i => new { i.Id, i.IdeaName, i.InitiatorId })
                    .ToListAsync(),

                // Method 3: Contains match
                ContainsMatch = await _context.Ideas
                    .Where(i => i.InitiatorId.Contains(currentUser))
                    .Select(i => new { i.Id, i.IdeaName, i.InitiatorId })
                    .ToListAsync(),

                // Method 4: Check if user exists and get their employee ID
                UserInfo = await _context.Users
                    .Where(u => u.Username == currentUser)
                    .Select(u => new { u.Username, u.EmployeeId, u.Name })
                    .FirstOrDefaultAsync(),

                // Method 5: Try matching with employee ID
                EmployeeIdMatch = await (from idea in _context.Ideas
                                       join user in _context.Users on idea.InitiatorId equals user.EmployeeId
                                       where user.Username == currentUser
                                       select new { idea.Id, idea.IdeaName, idea.InitiatorId, user.Username })
                                       .ToListAsync()
            };

            return Json(result);
        }

        // Debug endpoint: /Debug/FixUserIdeas
        public async Task<IActionResult> FixUserIdeas()
        {
            try
            {
                // Check if ideas are using employee ID instead of username
                var ideasWithEmployeeId = await _context.Ideas
                    .Where(i => _context.Users.Any(u => u.EmployeeId == i.InitiatorId))
                    .ToListAsync();

                var fixResults = new List<object>();

                foreach (var idea in ideasWithEmployeeId)
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.EmployeeId == idea.InitiatorId);
                    
                    if (user != null)
                    {
                        var oldInitiator = idea.InitiatorId;
                        idea.InitiatorId = user.Username; // Change to username
                        
                        fixResults.Add(new
                        {
                            IdeaId = idea.Id,
                            IdeaName = idea.IdeaName,
                            OldInitiator = oldInitiator,
                            NewInitiator = idea.InitiatorId,
                            UserName = user.Name
                        });
                    }
                }

                if (fixResults.Any())
                {
                    await _context.SaveChangesAsync();
                }

                return Json(new
                {
                    Success = true,
                    Message = $"Fixed {fixResults.Count} ideas",
                    FixedIdeas = fixResults
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        // Debug endpoint: /Debug/SqlQuery
        public async Task<IActionResult> SqlQuery()
        {
            var currentUser = User.Identity?.Name ?? "";
            
            // Raw SQL query untuk debugging
            var sql = @"
                SELECT 
                    i.id,
                    i.cIdea_name,
                    i.cInitiator,
                    i.dSubmitted_date,
                    u.Username,
                    u.Name as UserName,
                    u.employee_id
                FROM ideas i
                LEFT JOIN users u ON (i.cInitiator = u.Username OR i.cInitiator = u.employee_id)
                ORDER BY i.dSubmitted_date DESC
            ";

            var results = await _context.Database
                .SqlQueryRaw<dynamic>(sql)
                .ToListAsync();

            return Json(new
            {
                CurrentUser = currentUser,
                SqlResults = results
            });
        }

        // 🔥 NEW: Debug endpoint untuk memeriksa data user, role, dan assignment berdasarkan email
        [HttpGet]
        public async Task<IActionResult> CheckUserByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email parameter is required.");
            }

            var employee = await _context.Employees
                .Include(e => e.User)
                    .ThenInclude(u => u.Role)
                .Include(e => e.Divisi)
                .Include(e => e.Departement)
                .FirstOrDefaultAsync(e => e.Email == email);

            if (employee == null)
            {
                return NotFound($"No employee found with email: {email}");
            }

            var result = new
            {
                EmployeeInfo = new
                {
                    employee.Id,
                    employee.Name,
                    employee.Email,
                    employee.PositionTitle,
                    employee.EmploymentStatus
                },
                AssignedDivision = employee.Divisi != null ? new { employee.Divisi.Id, employee.Divisi.NamaDivisi } : null,
                AssignedDepartment = employee.Departement != null ? new { employee.Departement.Id, employee.Departement.NamaDepartement } : null,
                UserInfo = employee.User != null ? (object)new
                {
                    employee.User.Username,
                    employee.User.IsActive,
                    Role = employee.User.Role != null ? new { employee.User.Role.Id, employee.User.Role.RoleName, employee.User.Role.ApprovalLevel } : null
                } : "No associated user account."
            };

            return Json(result);
        }
    }
}
