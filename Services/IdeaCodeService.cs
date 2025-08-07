// Services/IdeaCodeService.cs (Updated for new schema)
using Ideku.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Ideku.Services
{
    public class IdeaCodeService
    {
        private readonly AppDbContext _context;

        public IdeaCodeService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Converts database ID (bigint) to user-friendly display format
        /// Example: 1 -> "IMS-0000001", 25 -> "IMS-0000025"
        /// </summary>
        public string FormatIdeaDisplayId(long id)
        {
            return $"IMS-{id:D7}";
        }

        /// <summary>
        /// Converts display format back to database ID
        /// Example: "IMS-0000001" -> 1, "IMS-0000025" -> 25
        /// </summary>
        public long? ParseIdeaDisplayId(string displayId)
        {
            if (string.IsNullOrEmpty(displayId))
                return null;

            // Remove "IMS-" prefix and parse the number
            if (displayId.StartsWith("IMS-") && displayId.Length >= 11)
            {
                var numberPart = displayId.Substring(4);
                if (long.TryParse(numberPart, out long id))
                {
                    return id;
                }
            }

            // Try to parse as direct number (for backward compatibility)
            if (long.TryParse(displayId, out long directId))
            {
                return directId;
            }

            return null;
        }

        /// <summary>
        /// Gets the next available ID for preview purposes (not used for actual creation since DB auto-increment handles it)
        /// </summary>
        public async Task<string> GetNextIdeaDisplayIdAsync()
        {
            var lastIdea = await _context.Ideas
                .OrderByDescending(i => i.Id)
                .FirstOrDefaultAsync();

            var nextId = (lastIdea?.Id ?? 0) + 1;
            return FormatIdeaDisplayId(nextId);
        }

        /// <summary>
        /// Validates if a display ID format is correct
        /// </summary>
        public bool IsValidIdeaDisplayId(string displayId)
        {
            return ParseIdeaDisplayId(displayId).HasValue;
        }

        /// <summary>
        /// Searches ideas by display ID pattern
        /// Supports partial search like "IMS-0000001" or just "1"
        /// </summary>
        public async Task<List<Models.Entities.Idea>> SearchIdeasByDisplayIdAsync(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return new List<Models.Entities.Idea>();

            var ideas = new List<Models.Entities.Idea>();

            // Try exact match first
            var exactId = ParseIdeaDisplayId(searchTerm);
            if (exactId.HasValue)
            {
                var exactIdea = await _context.Ideas
                    .Include(i => i.Category)
                    .Include(i => i.Event)
                    .Include(i => i.Initiator)
                        .ThenInclude(u => u.Employee)
                    .Include(i => i.TargetDivision)
                    .Include(i => i.TargetDepartment)
                    .FirstOrDefaultAsync(i => i.Id == exactId.Value);

                if (exactIdea != null)
                {
                    ideas.Add(exactIdea);
                    return ideas;
                }
            }

            // If no exact match, try partial search
            // Extract numeric part for range search
            var numericPart = ExtractNumericPart(searchTerm);
            if (numericPart != null)
            {
                // Search for IDs that contain the numeric pattern
                var searchPattern = numericPart.Value.ToString();
                var partialMatches = await _context.Ideas
                    .Include(i => i.Category)
                    .Include(i => i.Event)
                    .Include(i => i.Initiator)
                        .ThenInclude(u => u.Employee)
                    .Include(i => i.TargetDivision)
                    .Include(i => i.TargetDepartment)
                    .Where(i => i.Id.ToString().Contains(searchPattern))
                    .Take(10) // Limit results
                    .ToListAsync();

                ideas.AddRange(partialMatches);
            }

            return ideas;
        }

        private long? ExtractNumericPart(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            // Remove IMS- prefix if present
            var cleanInput = input.Replace("IMS-", "").Replace("-", "");
            
            // Extract only numeric characters
            var numericString = new string(cleanInput.Where(char.IsDigit).ToArray());
            
            if (long.TryParse(numericString, out long result))
            {
                return result;
            }

            return null;
        }
    }
}