// Services/EmailService.cs
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Ideku.Services
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendIdeaSubmissionNotificationAsync(string ideaTitle, string submitterName, string submitterId, int ideaId, List<string> validatorEmails)
        {
            try
            {
                var subject = $"[Ideku] New Idea Submission Requires Validation - {ideaTitle}";
                var body = GenerateValidationEmailBody(ideaTitle, submitterName, submitterId, ideaId);

                foreach (var email in validatorEmails)
                {
                    await SendEmailAsync(email, subject, body);
                }

                _logger.LogInformation($"Validation emails sent successfully for idea {ideaId} to {validatorEmails.Count} validators");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send validation emails for idea {ideaId}");
                return false;
            }
        }

        public async Task<bool> SendIdeaStatusUpdateAsync(string recipientEmail, string ideaTitle, string newStatus, string? comments = null)
        {
            try
            {
                var subject = $"[Ideku] Idea Status Update - {ideaTitle}";
                var body = GenerateStatusUpdateEmailBody(ideaTitle, newStatus, comments);

                await SendEmailAsync(recipientEmail, subject, body);

                _logger.LogInformation($"Status update email sent to {recipientEmail} for idea: {ideaTitle}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send status update email to {recipientEmail}");
                return false;
            }
        }

        public async Task<bool> SendGenericEmailAsync(string recipientEmail, string subject, string message, string actionUrl)
        {
            try
            {
                var body = GenerateGenericEmailBody(subject, message, actionUrl);
                await SendEmailAsync(recipientEmail, subject, body);
                _logger.LogInformation($"Generic email sent to {recipientEmail} with subject '{subject}'");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send generic email to {recipientEmail}");
                return false;
            }
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            using var client = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort);
            client.EnableSsl = _emailSettings.EnableSsl;
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }

        private string GenerateValidationEmailBody(string ideaTitle, string submitterName, string submitterId, int ideaId)
        {
            var baseUrl = _emailSettings.BaseUrl;
            var validationUrl = $"{baseUrl}/Validation/Review/{ideaId}";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #1b6ec2, #0077cc); color: white; padding: 20px; border-radius: 8px 8px 0 0; margin: -30px -30px 30px -30px; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ line-height: 1.6; color: #333; }}
        .idea-details {{ background-color: #f8f9fa; padding: 20px; border-radius: 6px; margin: 20px 0; }}
        .action-button {{ display: inline-block; background-color: #1b6ec2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 14px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>💡 New Idea Requires Validation</h1>
        </div>
        
        <div class='content'>
            <p>Hello,</p>
            
            <p>A new idea has been submitted in the Ideku system and requires your validation:</p>
            
            <div class='idea-details'>
                <h3>{ideaTitle}</h3>
                <p><strong>Submitted by:</strong> {submitterName} ({submitterId})</p>
                <p><strong>Submission Date:</strong> {DateTime.Now:MMMM dd, yyyy HH:mm}</p>
                <p><strong>Idea ID:</strong> #{ideaId}</p>
            </div>
            
            <p>Please review the idea submission and provide your validation decision:</p>
            
            <a href='{validationUrl}' class='action-button' style='color: white !important;'>Review & Validate Idea</a>
            
            <p>If you cannot click the button above, copy and paste this URL into your browser:</p>
            <p style='word-break: break-all; background-color: #f8f9fa; padding: 10px; border-radius: 4px;'>{validationUrl}</p>
            
            <p>Thank you for your time and contribution to the innovation process.</p>
            
            <p>Best regards,<br>
            The Ideku Team</p>
        </div>
        
        <div class='footer'>
            <p>This is an automated message from the Ideku Idea Management System. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateStatusUpdateEmailBody(string ideaTitle, string newStatus, string? comments)
        {
            var statusColor = newStatus switch
            {
                "Approved" => "#28a745",
                "Rejected" => "#dc3545",
                "Under Review" => "#ffc107",
                _ => "#6c757d"
            };

            var statusIcon = newStatus switch
            {
                "Approved" => "✅",
                "Rejected" => "❌",
                "Under Review" => "🔍",
                _ => "ℹ️"
            };

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #1b6ec2, #0077cc); color: white; padding: 20px; border-radius: 8px 8px 0 0; margin: -30px -30px 30px -30px; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ line-height: 1.6; color: #333; }}
        .status-badge {{ display: inline-block; background-color: {statusColor}; color: white; padding: 8px 16px; border-radius: 20px; font-weight: bold; margin: 10px 0; }}
        .comments {{ background-color: #f8f9fa; padding: 15px; border-left: 4px solid {statusColor}; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 14px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{statusIcon} Idea Status Update</h1>
        </div>
        
        <div class='content'>
            <p>Hello,</p>
            
            <p>The status of your idea submission has been updated:</p>
            
            <h3>{ideaTitle}</h3>
            
            <p><strong>New Status:</strong> <span class='status-badge'>{newStatus}</span></p>
            
            {(string.IsNullOrEmpty(comments) ? "" : $@"
            <div class='comments'>
                <h4>Comments from Validator:</h4>
                <p>{comments}</p>
            </div>")}
            
            <p>You can view your idea details and track its progress by logging into the Ideku system.</p>
            
            <p>Thank you for your contribution to innovation!</p>
            
            <p>Best regards,<br>
            The Ideku Team</p>
        </div>
        
        <div class='footer'>
            <p>This is an automated message from the Ideku Idea Management System. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateGenericEmailBody(string subject, string message, string actionUrl)
        {
            var baseUrl = _emailSettings.BaseUrl;
            var fullActionUrl = $"{baseUrl}{actionUrl}";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #1b6ec2, #0077cc); color: white; padding: 20px; border-radius: 8px 8px 0 0; margin: -30px -30px 30px -30px; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ line-height: 1.6; color: #333; }}
        .action-button {{ display: inline-block; background-color: #1b6ec2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 14px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{subject}</h1>
        </div>
        
        <div class='content'>
            <p>Hello,</p>
            
            <p>{message}</p>
            
            <a href='{fullActionUrl}' class='action-button' style='color: white !important;'>View Details</a>
            
            <p>If you cannot click the button above, copy and paste this URL into your browser:</p>
            <p style='word-break: break-all; background-color: #f8f9fa; padding: 10px; border-radius: 4px;'>{fullActionUrl}</p>
            
            <p>Best regards,<br>
            The Ideku Team</p>
        </div>
        
        <div class='footer'>
            <p>This is an automated message from the Ideku Idea Management System. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }
    }

    public class EmailSettings
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public bool EnableSsl { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public List<string> ValidatorEmails { get; set; } = new();
    }
}
