using SampleCkWebApp.Domain.Entities;

namespace SampleCkWebApp.Application.TransactionGroups.Data;

public class GetTransactionGroupsResult
{
    public List<TransactionGroup> TransactionGroups { get; set; } = new();
}

