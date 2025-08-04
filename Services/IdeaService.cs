using Ideku.Data.Context;
using Ideku.Data.Repositories;
using Ideku.Models.Entities;
using Ideku.Models.ViewModels.Idea;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ideku.Services
{
    public class IdeaService
    {
        private readonly IdeaRepository _ideaRepository;
        private readonly FileService _fileService;
        private readonly EmailService _emailService;
        private readonly AuthService _authService;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<IdeaService> _logger;
        private readonly IdeaCodeService _ideaCodeService;
        private readonly AppDbContext _context;
        private readonly WorkflowService _workflowService;

        public IdeaService(
            IdeaRepository ideaRepository,
            FileService fileService,
            EmailService emailService,
            AuthService authService,
            IOptions<EmailSettings> emailSettings,
            ILogger<IdeaService> logger,
            IdeaCodeService ideaCodeService,
            AppDbContext context,
            WorkflowService workflowService)
        {
            _ideaRepository = ideaRepository;
            _fileService = fileService;
            _emailService = emailService;
            _authService = authService;
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _ideaCodeService = ideaCodeService;
            _context = context;
            _workflowService = workflowService;
        }

        public async Task<List<Idea>> GetAllIdeasAsync()
        {
            return await _ideaRepository.GetAllAsync();
        }

        public async Task<List<Idea>> GetAllIdeasForValidationAsync()
        {
            return await _ideaRepository.GetAllForValidationAsync();
        }

        public async Task<List<Idea>> GetUserIdeasAsync(string initiator)
        {
            return await _ideaRepository.GetByInitiatorAsync(initiator);
        }

        public async Task<Idea?> GetIdeaByIdAsync(int id)
        {
            return await _ideaRepository.GetByIdAsync(id);
        }

        public async Task<Idea> CreateIdeaAsync(Idea idea)
        {
            // Values are now set in AppDbContext SaveChangesAsync
            return await _ideaRepository.CreateAsync(idea);
        }

        public async Task UpdateIdeaAsync(Idea idea)
        {
            idea.UpdatedDate = DateTime.UtcNow;
            await _ideaRepository.UpdateAsync(idea);
        }

        public async Task DeleteIdeaAsync(int id)
        {
            await _ideaRepository.DeleteAsync(id);
        }

        public async Task<(int total, int pending, int approved)> GetIdeaStatsAsync(string initiator)
        {
            var ideas = await GetUserIdeasAsync(initiator);
            var total = ideas.Count;
            var pending = ideas.Count(i => i.Status == "Submitted" || i.Status == "Under Review");
            var approved = ideas.Count(i => i.Status == "Approved");

            return (total, pending, approved);
        }

        // 🔥 NEW: Method untuk mendapatkan statistik semua ide
        public async Task<(int total, int pending, int approved)> GetGlobalIdeaStatsAsync()
        {
            var allIdeas = await GetAllIdeasAsync();
            var total = allIdeas.Count;
            var pending = allIdeas.Count(i => i.Status == "Submitted" || i.Status == "Under Review");
            var approved = allIdeas.Count(i => i.Status == "Approved");

            return (total, pending, approved);
        }

        public async Task<(bool Success, Idea? CreatedIdea, List<string> Errors)> CreateIdeaFromViewModelAsync(IdeaCreateViewModel model)
        {
            var errors = new List<string>();
            var employee = await _authService.GetEmployeeByBadgeAsync(model.BadgeNumber);
            if (employee == null)
            {
                errors.Add("Employee with this Badge Number not found.");
            }

            if (model.AttachmentFiles != null)
            {
                foreach (var file in model.AttachmentFiles)
                {
                    if (file.Length > 5 * 1024 * 1024) // 5MB
                    {
                        errors.Add($"File '{file.FileName}' exceeds the 5MB size limit.");
                    }
                }
            }

            if (errors.Any())
            {
                return (false, null, errors);
            }

            var attachmentFileNames = new List<string>();
            if (model.AttachmentFiles != null)
            {
                int fileIndex = 1;
                foreach (var file in model.AttachmentFiles)
                {
                    var savedFileName = await _fileService.SaveFileAsync(file, model.BadgeNumber, 0, fileIndex++);
                    if (savedFileName != null)
                    {
                        attachmentFileNames.Add(savedFileName);
                    }
                }
            }

            var idea = new Idea
            {
                AttachmentFiles = attachmentFileNames.Any() ? string.Join(";", attachmentFileNames) : null,
                InitiatorId = model.BadgeNumber,
                TargetDivisionId = model.ToDivision,
                TargetDepartmentId = model.ToDepartment,
                CategoryId = model.Category,
                EventId = model.Event,
                IdeaName = model.IdeaName,
                IssueBackground = model.IdeaIssueBackground,
                Solution = model.IdeaSolution,
                SavingCost = model.SavingCost.Value,
                Status = "Under Review",
                CurrentStage = 0,
                IdeaCode = await _ideaCodeService.GenerateNextIdeaCodeAsync()
            };

            var threshold = await GetHighValueThresholdAsync();
            if (idea.SavingCost >= threshold)
            {
                idea.WorkflowType = "HIGH_VALUE";
                idea.MaxStage = 6;
            }
            else
            {
                idea.WorkflowType = "STANDARD";
                idea.MaxStage = 3;
            }

            var createdIdea = await CreateIdeaAsync(idea);

            // Initiate the workflow to send the first notification
            await _workflowService.InitiateWorkflowAsync(createdIdea);

            return (true, createdIdea, errors);
        }

        public async Task<(bool Success, List<string> Errors)> UpdateIdeaFromViewModelAsync(int id, IdeaCreateViewModel model, string currentUser)
        {
            var errors = new List<string>();
            var existingIdea = await GetIdeaByIdAsync(id);
            if (existingIdea == null)
            {
                errors.Add("Idea not found.");
                return (false, errors);
            }

            if (existingIdea.InitiatorId != currentUser)
            {
                errors.Add("You don't have permission to edit this idea.");
                return (false, errors);
            }

            var employee = await _authService.GetEmployeeByBadgeAsync(model.BadgeNumber);
            if (employee == null)
            {
                errors.Add("Employee with this Badge Number not found.");
            }

            if (errors.Any())
            {
                return (false, errors);
            }

            if (model.AttachmentFiles != null && model.AttachmentFiles.Any())
            {
                if (!string.IsNullOrEmpty(existingIdea.AttachmentFiles))
                {
                    foreach (var oldFile in existingIdea.AttachmentFiles.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        _fileService.DeleteFile(oldFile);
                    }
                }

                var attachmentFileNames = new List<string>();
                int fileIndex = 1;
                foreach (var file in model.AttachmentFiles)
                {
                    var savedFileName = await _fileService.SaveFileAsync(file, model.BadgeNumber, 0, fileIndex++);
                    if (savedFileName != null)
                    {
                        attachmentFileNames.Add(savedFileName);
                    }
                }
            existingIdea.AttachmentFiles = string.Join(";", attachmentFileNames);
            }

            existingIdea.InitiatorId = model.BadgeNumber;
            existingIdea.TargetDivisionId = model.ToDivision;
            existingIdea.TargetDepartmentId = model.ToDepartment;
            existingIdea.CategoryId = model.Category;
            existingIdea.EventId = model.Event;
            existingIdea.IdeaName = model.IdeaName;
            existingIdea.IssueBackground = model.IdeaIssueBackground;
            existingIdea.Solution = model.IdeaSolution;
            existingIdea.SavingCost = model.SavingCost.Value;

            await UpdateIdeaAsync(existingIdea);

            return (true, errors);
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
    }
}
