// Services/NotificationService.cs (Basic implementation)
using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Services
{
    public class NotificationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly EmailService _emailService;

        public NotificationService(AppDbContext context, ILogger<NotificationService> logger, EmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        private async Task SendNotificationAndEmailAsync(Notification notification, string recipientEmail)
        {
            try
            {
                bool emailSent = await _emailService.SendGenericEmailAsync(
                    recipientEmail,
                    notification.Title,
                    notification.Message,
                    notification.ActionUrl);

                notification.IsEmailSent = emailSent;
                if (emailSent)
                {
                    notification.EmailSentDate = DateTime.UtcNow;
                }

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Notification '{NotificationType}' for idea {IdeaId} to {RecipientId}. Email sent: {EmailSent}",
                    notification.NotificationType, notification.IdeaId, notification.RecipientId, emailSent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification {NotificationType} for idea {IdeaId}",
                    notification.NotificationType, notification.IdeaId);
            }
        }

        public async Task SendApprovalRequestAsync(int ideaId, string recipientId, int stage)
        {
            var idea = await _context.Ideas.FirstOrDefaultAsync(i => i.Id == ideaId);
            if (idea == null) return;

            var recipient = await _context.Employees.FirstOrDefaultAsync(e => e.Id == recipientId);
            if (recipient == null) return;

            var notification = new Notification
            {
                RecipientId = recipientId,
                IdeaId = ideaId,
                NotificationType = "APPROVAL_REQUEST",
                Title = "New Idea Requires Your Approval",
                Message = $"Idea '{idea.IdeaName}' (Code: {idea.IdeaCode}) is waiting for your review at Stage {stage}",
                Priority = "High",
                ActionUrl = $"/approval/review/{ideaId}"
            };

            await SendNotificationAndEmailAsync(notification, recipient.Email);
        }

        public async Task SendCompletionNotificationAsync(int ideaId)
        {
            var idea = await _context.Ideas
                .Include(i => i.Initiator)
                .FirstOrDefaultAsync(i => i.Id == ideaId);
            if (idea == null) return;

            var recipient = await _context.Employees.FirstOrDefaultAsync(e => e.Id == idea.InitiatorId);
            if (recipient == null) return;

            var notification = new Notification
            {
                RecipientId = idea.InitiatorId,
                IdeaId = ideaId,
                NotificationType = "IDEA_APPROVED",
                Title = "Your Idea Has Been Approved!",
                Message = $"Congratulations! Your idea '{idea.IdeaName}' (Code: {idea.IdeaCode}) has been fully approved and is ready for implementation.",
                Priority = "High",
                ActionUrl = $"/idea/details/{ideaId}"
            };

            await SendNotificationAndEmailAsync(notification, recipient.Email);
        }

        public async Task SendRejectionNotificationAsync(int ideaId, string rejectReason)
        {
            var idea = await _context.Ideas
                .Include(i => i.Initiator)
                .FirstOrDefaultAsync(i => i.Id == ideaId);
            if (idea == null) return;

            var recipient = await _context.Employees.FirstOrDefaultAsync(e => e.Id == idea.InitiatorId);
            if (recipient == null) return;

            var notification = new Notification
            {
                RecipientId = idea.InitiatorId,
                IdeaId = ideaId,
                NotificationType = "IDEA_REJECTED",
                Title = "Your Idea Requires Revision",
                Message = $"Your idea '{idea.IdeaName}' (Code: {idea.IdeaCode}) needs revision. Reason: {rejectReason}",
                Priority = "High",
                ActionUrl = $"/idea/details/{ideaId}"
            };

            await SendNotificationAndEmailAsync(notification, recipient.Email);
        }

        public async Task SendInfoRequestNotificationAsync(int ideaId, string infoRequest)
        {
            var idea = await _context.Ideas
                .Include(i => i.Initiator)
                .FirstOrDefaultAsync(i => i.Id == ideaId);
            if (idea == null) return;

            var recipient = await _context.Employees.FirstOrDefaultAsync(e => e.Id == idea.InitiatorId);
            if (recipient == null) return;

            var notification = new Notification
            {
                RecipientId = idea.InitiatorId,
                IdeaId = ideaId,
                NotificationType = "MORE_INFO_REQUIRED",
                Title = "Additional Information Required",
                Message = $"More information is needed for your idea '{idea.IdeaName}' (Code: {idea.IdeaCode}): {infoRequest}",
                Priority = "Normal",
                ActionUrl = $"/idea/edit/{ideaId}"
            };

            await SendNotificationAndEmailAsync(notification, recipient.Email);
        }

        public async Task SendProgressUpdateAsync(int ideaId, string recipientId, int stage, string action)
        {
            var idea = await _context.Ideas.FirstOrDefaultAsync(i => i.Id == ideaId);
            if (idea == null) return;

            var recipient = await _context.Employees.FirstOrDefaultAsync(e => e.Id == recipientId);
            if (recipient == null) return;

            var notification = new Notification
            {
                RecipientId = recipientId,
                IdeaId = ideaId,
                NotificationType = "PROGRESS_UPDATE",
                Title = "Idea Progress Update",
                Message = $"Idea '{idea.IdeaName}' (Code: {idea.IdeaCode}) has been {action.ToLower()} at Stage {stage}",
                Priority = "Normal",
                ActionUrl = $"/idea/details/{ideaId}"
            };

            await SendNotificationAndEmailAsync(notification, recipient.Email);
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string employeeId, bool unreadOnly = false)
        {
            var query = _context.Notifications
                .Include(n => n.Idea)
                .Where(n => n.RecipientId == employeeId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query
                .OrderByDescending(n => n.CreatedDate)
                .Take(50) // Limit to latest 50 notifications
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task SendMilestoneCreationRequestAsync(int ideaId, string recipientId)
        {
            var idea = await _context.Ideas.FindAsync(ideaId);
            if (idea == null) return;

            var recipient = await _context.Employees.FirstOrDefaultAsync(e => e.Id == recipientId);
            if (recipient == null) return;

            var notification = new Notification
            {
                RecipientId = recipientId,
                IdeaId = ideaId,
                NotificationType = "MILESTONE_REQUEST",
                Title = "Action Required: Create Milestone",
                Message = $"Please create a milestone for the idea '{idea.IdeaName}' (Code: {idea.IdeaCode}).",
                Priority = "High",
                ActionUrl = $"/idea/details/{ideaId}#milestones"
            };
            
            await SendNotificationAndEmailAsync(notification, recipient.Email);
        }

        public async Task SendMilestoneAndSavingRequestAsync(int ideaId, string recipientId)
        {
            var idea = await _context.Ideas.FindAsync(ideaId);
            if (idea == null) return;

            var recipient = await _context.Employees.FirstOrDefaultAsync(e => e.Id == recipientId);
            if (recipient == null) return;

            var notification = new Notification
            {
                RecipientId = recipientId,
                IdeaId = ideaId,
                NotificationType = "MILESTONE_SAVING_REQUEST",
                Title = "Action Required: Milestone & Saving Plan",
                Message = $"Please create a milestone and input the saving plan for the idea '{idea.IdeaName}' (Code: {idea.IdeaCode}).",
                Priority = "High",
                ActionUrl = $"/idea/details/{ideaId}#monitoring"
            };

            await SendNotificationAndEmailAsync(notification, recipient.Email);
        }

        public async Task SendCompletionRequestAsync(int ideaId, string recipientId)
        {
            var idea = await _context.Ideas.FindAsync(ideaId);
            if (idea == null) return;

            var recipient = await _context.Employees.FirstOrDefaultAsync(e => e.Id == recipientId);
            if (recipient == null) return;

            var notification = new Notification
            {
                RecipientId = recipientId,
                IdeaId = ideaId,
                NotificationType = "COMPLETION_REQUEST",
                Title = "Action Required: Complete Idea",
                Message = $"Please mark the idea '{idea.IdeaName}' (Code: {idea.IdeaCode}) as complete.",
                Priority = "High",
                ActionUrl = $"/idea/details/{ideaId}#complete"
            };
            
            await SendNotificationAndEmailAsync(notification, recipient.Email);
        }
    }
}
