using SampleCkWebApp.Domain.Entities;

namespace SampleCkWebApp.Application.ExpenseGroups.Data;

public class GetExpenseGroupsResult
{
    public List<ExpenseGroup> ExpenseGroups { get; set; } = new();
}

