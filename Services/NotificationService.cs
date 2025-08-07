// Services/NotificationService.cs (Updated for new schema)
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

        public async Task SendApprovalRequestAsync(long ideaId, long recipientUserId, int stage)
        {
            try
            {
                var idea = await _context.Ideas
                    .Include(i => i.Initiator)
                        .ThenInclude(u => u.Employee)
                    .FirstOrDefaultAsync(i => i.Id == ideaId);
                    
                if (idea == null) return;

                var recipient = await _context.Users
                    .Include(u => u.Employee)
                    .FirstOrDefaultAsync(u => u.Id == recipientUserId);
                    
                if (recipient?.Employee == null) return;

                var notification = new Notification
                {
                    IdeaId = ideaId,
                    NotificationType = "APPROVAL_REQUEST",
                    Title = "New Idea Requires Your Approval",
                    Message = $"Idea '{idea.IdeaName}' (ID: {idea.Id}) is waiting for your review at Stage {stage}",
                    Priority = "High",
                    ActionUrl = $"/validation/review/{ideaId}"
                };

                await SendNotificationAndEmailAsync(notification, recipient.Employee.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending approval request for idea {IdeaId} to user {UserId}", ideaId, recipientUserId);
            }
        }

        public async Task SendCompletionNotificationAsync(long ideaId)
        {
            try
            {
                var idea = await _context.Ideas
                    .Include(i => i.Initiator)
                        .ThenInclude(u => u.Employee)
                    .FirstOrDefaultAsync(i => i.Id == ideaId);
                    
                if (idea?.Initiator?.Employee == null) return;

                var notification = new Notification
                {
                    IdeaId = ideaId,
                    NotificationType = "IDEA_APPROVED",
                    Title = "Your Idea Has Been Approved!",
                    Message = $"Congratulations! Your idea '{idea.IdeaName}' (ID: {idea.Id}) has been fully approved and is ready for implementation.",
                    Priority = "High",
                    ActionUrl = $"/idea/details/{ideaId}"
                };

                await SendNotificationAndEmailAsync(notification, idea.Initiator.Employee.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending completion notification for idea {IdeaId}", ideaId);
            }
        }

        public async Task SendRejectionNotificationAsync(long ideaId, string rejectReason)
        {
            try
            {
                var idea = await _context.Ideas
                    .Include(i => i.Initiator)
                        .ThenInclude(u => u.Employee)
                    .FirstOrDefaultAsync(i => i.Id == ideaId);
                    
                if (idea?.Initiator?.Employee == null) return;

                var notification = new Notification
                {
                    IdeaId = ideaId,
                    NotificationType = "IDEA_REJECTED",
                    Title = "Your Idea Requires Revision",
                    Message = $"Your idea '{idea.IdeaName}' (ID: {idea.Id}) needs revision. Reason: {rejectReason}",
                    Priority = "High",
                    ActionUrl = $"/idea/details/{ideaId}"
                };

                await SendNotificationAndEmailAsync(notification, idea.Initiator.Employee.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending rejection notification for idea {IdeaId}", ideaId);
            }
        }

        public async Task SendInfoRequestNotificationAsync(long ideaId, string infoRequest)
        {
            try
            {
                var idea = await _context.Ideas
                    .Include(i => i.Initiator)
                        .ThenInclude(u => u.Employee)
                    .FirstOrDefaultAsync(i => i.Id == ideaId);
                    
                if (idea?.Initiator?.Employee == null) return;

                var notification = new Notification
                {
                    IdeaId = ideaId,
                    NotificationType = "MORE_INFO_REQUIRED",
                    Title = "Additional Information Required",
                    Message = $"More information is needed for your idea '{idea.IdeaName}' (ID: {idea.Id}): {infoRequest}",
                    Priority = "Normal",
                    ActionUrl = $"/idea/edit/{ideaId}"
                };

                await SendNotificationAndEmailAsync(notification, idea.Initiator.Employee.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending info request notification for idea {IdeaId}", ideaId);
            }
        }

        public async Task SendProgressUpdateAsync(long ideaId, long recipientUserId, int stage, string action)
        {
            try
            {
                var idea = await _context.Ideas.FirstOrDefaultAsync(i => i.Id == ideaId);
                if (idea == null) return;

                var recipient = await _context.Users
                    .Include(u => u.Employee)
                    .FirstOrDefaultAsync(u => u.Id == recipientUserId);
                    
                if (recipient?.Employee == null) return;

                var notification = new Notification
                {
                    IdeaId = ideaId,
                    NotificationType = "PROGRESS_UPDATE",
                    Title = "Idea Progress Update",
                    Message = $"Idea '{idea.IdeaName}' (ID: {idea.Id}) has been {action.ToLower()} at Stage {stage}",
                    Priority = "Normal",
                    ActionUrl = $"/idea/details/{ideaId}"
                };

                await SendNotificationAndEmailAsync(notification, recipient.Employee.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending progress update for idea {IdeaId} to user {UserId}", ideaId, recipientUserId);
            }
        }

        public async Task SendMilestoneCreationRequestAsync(long ideaId, long recipientUserId)
        {
            try
            {
                var idea = await _context.Ideas.FirstOrDefaultAsync(i => i.Id == ideaId);
                if (idea == null) return;

                var recipient = await _context.Users
                    .Include(u => u.Employee)
                    .FirstOrDefaultAsync(u => u.Id == recipientUserId);
                    
                if (recipient?.Employee == null) return;

                var notification = new Notification
                {
                    IdeaId = ideaId,
                    NotificationType = "MILESTONE_REQUEST",
                    Title = "Action Required: Create Milestone",
                    Message = $"Please create a milestone for the idea '{idea.IdeaName}' (ID: {idea.Id}).",
                    Priority = "High",
                    ActionUrl = $"/idea/details/{ideaId}#milestones"
                };
                
                await SendNotificationAndEmailAsync(notification, recipient.Employee.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending milestone creation request for idea {IdeaId} to user {UserId}", ideaId, recipientUserId);
            }
        }

        private async Task SendNotificationAndEmailAsync(Notification notification, string recipientEmail)
        {
            try
            {
                bool emailSent = await _emailService.SendGenericEmailAsync(
                    recipientEmail,
                    notification.Title,
                    notification.Message,
                    notification.ActionUrl ?? ""
                );

                notification.IsEmailSent = emailSent;
                if (emailSent)
                {
                    notification.EmailSentDate = DateTime.UtcNow;
                }

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Notification '{NotificationType}' for idea {IdeaId} sent. Email sent: {EmailSent}",
                    notification.NotificationType, notification.IdeaId, emailSent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification {NotificationType} for idea {IdeaId}",
                    notification.NotificationType, notification.IdeaId);
            }
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(long userId, bool unreadOnly = false)
        {
            // Since we removed recipient_id, we'll need to get notifications based on user's ideas
            var userIdeas = await _context.Ideas
                .Where(i => i.InitiatorUserId == userId)
                .Select(i => i.Id)
                .ToListAsync();

            var query = _context.Notifications
                .Include(n => n.Idea)
                .Where(n => n.IdeaId.HasValue && userIdeas.Contains(n.IdeaId.Value));

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query
                .OrderByDescending(n => n.CreatedDate)
                .Take(50)
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
    }
}