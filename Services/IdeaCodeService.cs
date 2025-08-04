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

        public async Task<string> GenerateNextIdeaCodeAsync()
        {
            var latestCode = await _context.Ideas
                .Where(i => i.Id.StartsWith("IMS-"))
                .OrderByDescending(i => i.Id)
                .Select(i => i.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            
            if (latestCode != null)
            {
                var numberPart = latestCode.Substring(4);
                if (int.TryParse(numberPart, out int currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }

            return $"IMS-{nextNumber:D5}";
        }
    }
}
