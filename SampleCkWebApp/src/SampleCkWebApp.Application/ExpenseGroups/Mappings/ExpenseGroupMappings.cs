using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Application.ExpenseGroups.Data;
using SampleCkWebApp.ExpenseGroups;

namespace SampleCkWebApp.Application.ExpenseGroups.Mappings;

public static class ExpenseGroupMappings
{
    public static GetExpenseGroupsResponse ToResponse(this GetExpenseGroupsResult result)
    {
        return new GetExpenseGroupsResponse
        {
            ExpenseGroups = result.ExpenseGroups.Select(eg => eg.ToResponse()).ToList(),
            TotalCount = result.ExpenseGroups.Count
        };
    }
    
    public static ExpenseGroupResponse ToResponse(this ExpenseGroup expenseGroup)
    {
        return new ExpenseGroupResponse
        {
            Id = expenseGroup.Id,
            Name = expenseGroup.Name,
            Description = expenseGroup.Description,
            UserId = expenseGroup.UserId
        };
    }
}

