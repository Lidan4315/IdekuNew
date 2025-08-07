// Data/Repositories/WorkflowRepository.cs (NEW)
using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public class WorkflowRepository
    {
        private readonly AppDbContext _context;

        public WorkflowRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<WorkflowDefinition>> GetAllActiveAsync()
        {
            return await _context.WorkflowDefinitions
                .Where(w => w.IsActive)
                .Include(w => w.WorkflowStages)
                    .ThenInclude(ws => ws.Stage)
                        .ThenInclude(s => s.StageApprovers)
                            .ThenInclude(sa => sa.Role)
                .Include(w => w.WorkflowConditions)
                .OrderBy(w => w.Name)
                .ToListAsync();
        }

        public async Task<WorkflowDefinition?> GetByIdAsync(string id)
        {
            return await _context.WorkflowDefinitions
                .Include(w => w.WorkflowStages)
                    .ThenInclude(ws => ws.Stage)
                        .ThenInclude(s => s.StageApprovers)
                            .ThenInclude(sa => sa.Role)
                .Include(w => w.WorkflowConditions)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<WorkflowDefinition?> GetWorkflowForIdeaAsync(decimal savingCost, int categoryId, string divisionId, string departmentId, int? eventId = null)
        {
            var workflows = await GetAllActiveAsync();
            
            foreach (var workflow in workflows)
            {
                bool matchesConditions = true;
                
                foreach (var condition in workflow.WorkflowConditions)
                {
                    if (!condition.IsActive) continue;
                    
                    bool conditionMet = condition.ConditionType switch
                    {
                        "SAVING_COST" => EvaluateNumericCondition(savingCost, condition.Operator, condition.ConditionValue),
                        "CATEGORY" => EvaluateListCondition(categoryId.ToString(), condition.Operator, condition.ConditionValue),
                        "DIVISION" => EvaluateListCondition(divisionId, condition.Operator, condition.ConditionValue),
                        "DEPARTMENT" => EvaluateListCondition(departmentId, condition.Operator, condition.ConditionValue),
                        "EVENT" => eventId.HasValue && EvaluateListCondition(eventId.Value.ToString(), condition.Operator, condition.ConditionValue),
                        _ => true
                    };
                    
                    if (!conditionMet)
                    {
                        matchesConditions = false;
                        break;
                    }
                }
                
                if (matchesConditions)
                {
                    return workflow;
                }
            }
            
            return null; // No workflow matches
        }

        private bool EvaluateNumericCondition(decimal value, string operatorType, string conditionValue)
        {
            if (!decimal.TryParse(conditionValue, out decimal targetValue))
                return false;

            return operatorType switch
            {
                ">=" => value >= targetValue,
                "<=" => value <= targetValue,
                "=" => value == targetValue,
                "!=" => value != targetValue,
                ">" => value > targetValue,
                "<" => value < targetValue,
                _ => false
            };
        }

        private bool EvaluateListCondition(string value, string operatorType, string conditionValue)
        {
            var values = conditionValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(v => v.Trim())
                                     .ToList();

            return operatorType switch
            {
                "IN" => values.Contains(value),
                "NOT_IN" => !values.Contains(value),
                "=" => values.Count == 1 && values[0] == value,
                "!=" => values.Count == 1 && values[0] != value,
                _ => false
            };
        }

        public async Task<List<WorkflowStage>> GetWorkflowStagesAsync(string workflowId)
        {
            return await _context.WorkflowStages
                .Where(ws => ws.WorkflowId == workflowId)
                .Include(ws => ws.Stage)
                    .ThenInclude(s => s.StageApprovers)
                        .ThenInclude(sa => sa.Role)
                .OrderBy(ws => ws.SequenceNumber)
                .ToListAsync();
        }
    }
}