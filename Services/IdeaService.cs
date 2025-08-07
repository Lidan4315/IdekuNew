// Services/IdeaService.cs (Complete updated version)
using Ideku.Data.Repositories;
using Ideku.Models.Entities;
using Ideku.Models.ViewModels.Idea;
using Microsoft.Extensions.Options;

namespace Ideku.Services
{
    public class IdeaService
    {
        private readonly IdeaRepository _ideaRepository;
        private readonly UserRepository _userRepository;
        private readonly WorkflowRepository _workflowRepository;
        private readonly FileService _fileService;
        private readonly EmailService _emailService;
        private readonly AuthService _authService;
        private readonly IdeaCodeService _ideaCodeService;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<IdeaService> _logger;

        public IdeaService(
            IdeaRepository ideaRepository,
            UserRepository userRepository,
            WorkflowRepository workflowRepository,
            FileService fileService,
            EmailService emailService,
            AuthService authService,
            IdeaCodeService ideaCodeService,
            IOptions<EmailSettings> emailSettings,
            ILogger<IdeaService> logger)
        {
            _ideaRepository = ideaRepository;
            _userRepository = userRepository;
            _workflowRepository = workflowRepository;
            _fileService = fileService;
            _emailService = emailService;
            _authService = authService;
            _ideaCodeService = ideaCodeService;
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        // ===== BASIC CRUD OPERATIONS =====

        public async Task<List<Idea>> GetAllIdeasAsync()
        {
            return await _ideaRepository.GetAllAsync();
        }

        public async Task<List<Idea>> GetAllIdeasForValidationAsync()
        {
            return await _ideaRepository.GetAllForValidationAsync();
        }

        public async Task<List<Idea>> GetUserIdeasByUsernameAsync(string username)
        {
            return await _ideaRepository.GetByInitiatorUsernameAsync(username);
        }

        public async Task<List<Idea>> GetUserIdeasByUserIdAsync(long userId)
        {
            return await _ideaRepository.GetByInitiatorUserIdAsync(userId);
        }

        public async Task<Idea?> GetIdeaByIdAsync(long id)
        {
            return await _ideaRepository.GetByIdAsync(id);
        }

        public async Task<Idea> CreateIdeaAsync(Idea idea)
        {
            return await _ideaRepository.CreateAsync(idea);
        }

        public async Task UpdateIdeaAsync(Idea idea)
        {
            await _ideaRepository.UpdateAsync(idea);
        }

        public async Task DeleteIdeaAsync(long id)
        {
            await _ideaRepository.DeleteAsync(id);
        }

        // ===== DISPLAY ID OPERATIONS =====

        /// <summary>
        /// Get idea by display ID (e.g., "IMS-0000001")
        /// </summary>
        public async Task<Idea?> GetIdeaByDisplayIdAsync(string displayId)
        {
            var id = _ideaCodeService.ParseIdeaDisplayId(displayId);
            if (!id.HasValue)
                return null;

            return await GetIdeaByIdAsync(id.Value);
        }

        /// <summary>
        /// Search ideas by display ID pattern
        /// </summary>
        public async Task<List<Idea>> SearchIdeasByDisplayIdAsync(string searchTerm)
        {
            return await _ideaCodeService.SearchIdeasByDisplayIdAsync(searchTerm);
        }

        /// <summary>
        /// Get next display ID for preview (useful for forms)
        /// </summary>
        public async Task<string> GetNextDisplayIdPreviewAsync()
        {
            return await _ideaCodeService.GetNextIdeaDisplayIdAsync();
        }

        /// <summary>
        /// Validate display ID format
        /// </summary>
        public bool IsValidDisplayId(string displayId)
        {
            return _ideaCodeService.IsValidIdeaDisplayId(displayId);
        }

        // ===== STATISTICS =====

        public async Task<(int total, int pending, int approved)> GetIdeaStatsAsync(string username)
        {
            var ideas = await GetUserIdeasByUsernameAsync(username);
            var total = ideas.Count;
            var pending = ideas.Count(i => i.Status == "Submitted" || i.Status == "Under Review");
            var approved = ideas.Count(i => i.Status == "Approved" || i.Status == "Completed");

            return (total, pending, approved);
        }

        public async Task<(int total, int pending, int approved)> GetGlobalIdeaStatsAsync()
        {
            var allIdeas = await GetAllIdeasAsync();
            var total = allIdeas.Count;
            var pending = allIdeas.Count(i => i.Status == "Submitted" || i.Status == "Under Review");
            var approved = allIdeas.Count(i => i.Status == "Approved" || i.Status == "Completed");

            return (total, pending, approved);
        }

        // ===== IDEA LIFECYCLE MANAGEMENT =====

        public async Task<(bool Success, Idea? CreatedIdea, List<string> Errors)> CreateIdeaFromViewModelAsync(IdeaCreateViewModel model, string currentUsername)
        {
            var errors = new List<string>();
            
            try
            {
                // Validate employee
                var employee = await _authService.GetEmployeeByBadgeAsync(model.BadgeNumber);
                if (employee == null)
                {
                    errors.Add("Employee with this Badge Number not found.");
                    return (false, null, errors);
                }

                // Get user account
                var initiatorUser = await _authService.GetUserByEmployeeIdAsync(model.BadgeNumber);
                if (initiatorUser == null)
                {
                    errors.Add("No user account found for this employee.");
                    return (false, null, errors);
                }

                // Validate files
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

                // Determine appropriate workflow
                var workflow = await _workflowRepository.GetWorkflowForIdeaAsync(
                    model.SavingCost.Value,
                    model.Category,
                    model.ToDivision,
                    model.ToDepartment,
                    model.Event
                );

                // Create idea entity
                var idea = new Idea
                {
                    InitiatorUserId = initiatorUser.Id,
                    TargetDivisionId = model.ToDivision,
                    TargetDepartmentId = model.ToDepartment,
                    CategoryId = model.Category,
                    EventId = model.Event,
                    WorkflowDefinitionId = workflow?.Id,
                    IdeaName = model.IdeaName,
                    IssueBackground = model.IdeaIssueBackground,
                    Solution = model.IdeaSolution,
                    SavingCost = model.SavingCost.Value,
                    Status = "Submitted",
                    CurrentStage = 0
                };

                var createdIdea = await CreateIdeaAsync(idea);

                // Handle file attachments
                if (model.AttachmentFiles != null && model.AttachmentFiles.Any())
                {
                    var attachmentFileNames = new List<string>();
                    int fileIndex = 1;
                    foreach (var file in model.AttachmentFiles)
                    {
                        var savedFileName = await _fileService.SaveFileAsync(file, createdIdea.Id.ToString(), createdIdea.CurrentStage, fileIndex++);
                        if (savedFileName != null)
                        {
                            attachmentFileNames.Add(savedFileName);
                        }
                    }

                    if (attachmentFileNames.Any())
                    {
                        createdIdea.AttachmentFiles = string.Join(";", attachmentFileNames);
                        await UpdateIdeaAsync(createdIdea);
                    }
                }

                _logger.LogInformation("Idea {DisplayId} (ID: {IdeaId}) created successfully by user {Username}", 
                    createdIdea.DisplayId, createdIdea.Id, currentUsername);

                return (true, createdIdea, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating idea for user {Username}", currentUsername);
                errors.Add("An unexpected error occurred while creating the idea.");
                return (false, null, errors);
            }
        }

        public async Task<(bool Success, List<string> Errors)> UpdateIdeaFromViewModelAsync(long id, IdeaCreateViewModel model, string currentUsername)
        {
            var errors = new List<string>();
            
            try
            {
                var existingIdea = await GetIdeaByIdAsync(id);
                if (existingIdea == null)
                {
                    errors.Add("Idea not found.");
                    return (false, errors);
                }

                var currentUser = await _authService.AuthenticateAsync(currentUsername);
                if (currentUser == null || existingIdea.InitiatorUserId != currentUser.Id)
                {
                    errors.Add("You don't have permission to edit this idea.");
                    return (false, errors);
                }

                // Only allow edit if status is Submitted
                if (existingIdea.Status != "Submitted")
                {
                    errors.Add("You can only edit ideas that haven't been reviewed yet.");
                    return (false, errors);
                }

                var employee = await _authService.GetEmployeeByBadgeAsync(model.BadgeNumber);
                if (employee == null)
                {
                    errors.Add("Employee with this Badge Number not found.");
                    return (false, errors);
                }

                var initiatorUser = await _authService.GetUserByEmployeeIdAsync(model.BadgeNumber);
                if (initiatorUser == null)
                {
                    errors.Add("No user account found for this employee.");
                    return (false, errors);
                }

                if (errors.Any())
                {
                    return (false, errors);
                }

                // Handle file attachments
                if (model.AttachmentFiles != null && model.AttachmentFiles.Any())
                {
                    // Delete old files
                    if (!string.IsNullOrEmpty(existingIdea.AttachmentFiles))
                    {
                        foreach (var oldFile in existingIdea.AttachmentFiles.Split(';', StringSplitOptions.RemoveEmptyEntries))
                        {
                            _fileService.DeleteFile(oldFile);
                        }
                    }

                    // Save new files
                    var attachmentFileNames = new List<string>();
                    int fileIndex = 1;
                    foreach (var file in model.AttachmentFiles)
                    {
                        var savedFileName = await _fileService.SaveFileAsync(file, existingIdea.Id.ToString(), existingIdea.CurrentStage, fileIndex++);
                        if (savedFileName != null)
                        {
                            attachmentFileNames.Add(savedFileName);
                        }
                    }
                    existingIdea.AttachmentFiles = string.Join(";", attachmentFileNames);
                }

                // Update workflow if saving cost changed significantly
                var newWorkflow = await _workflowRepository.GetWorkflowForIdeaAsync(
                    model.SavingCost.Value,
                    model.Category,
                    model.ToDivision,
                    model.ToDepartment,
                    model.Event
                );

                // Update idea properties
                existingIdea.InitiatorUserId = initiatorUser.Id;
                existingIdea.TargetDivisionId = model.ToDivision;
                existingIdea.TargetDepartmentId = model.ToDepartment;
                existingIdea.CategoryId = model.Category;
                existingIdea.EventId = model.Event;
                existingIdea.WorkflowDefinitionId = newWorkflow?.Id;
                existingIdea.IdeaName = model.IdeaName;
                existingIdea.IssueBackground = model.IdeaIssueBackground;
                existingIdea.Solution = model.IdeaSolution;
                existingIdea.SavingCost = model.SavingCost.Value;

                await UpdateIdeaAsync(existingIdea);

                _logger.LogInformation("Idea {DisplayId} (ID: {IdeaId}) updated successfully by user {Username}", 
                    existingIdea.DisplayId, existingIdea.Id, currentUsername);

                return (true, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating idea {IdeaId} for user {Username}", id, currentUsername);
                errors.Add("An unexpected error occurred while updating the idea.");
                return (false, errors);
            }
        }

        // ===== FILTERING AND SEARCH =====

        public async Task<List<Idea>> GetIdeasByStatusAsync(string status)
        {
            return await _ideaRepository.GetIdeasByStatusAsync(status);
        }

        public async Task<List<Idea>> GetIdeasByWorkflowAndStageAsync(string workflowId, int stage)
        {
            return await _ideaRepository.GetIdeasByWorkflowAndStageAsync(workflowId, stage);
        }

        // ===== FILE MANAGEMENT =====

        public async Task<(bool Success, string? FilePath)> GetIdeaAttachmentAsync(long ideaId, string fileName)
        {
            try
            {
                var idea = await GetIdeaByIdAsync(ideaId);
                if (idea?.AttachmentFiles == null)
                {
                    return (false, null);
                }

                var files = idea.AttachmentFiles.Split(';', StringSplitOptions.RemoveEmptyEntries);
                if (!files.Contains(fileName))
                {
                    return (false, null);
                }

                var filePath = Path.Combine("wwwroot", "uploads", fileName);
                if (File.Exists(filePath))
                {
                    return (true, filePath);
                }

                return (false, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attachment {FileName} for idea {IdeaId}", fileName, ideaId);
                return (false, null);
            }
        }
    }
}