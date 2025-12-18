using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Application.TransactionGroups.Data;
using SampleCkWebApp.Contracts.TransactionGroups;

namespace SampleCkWebApp.Application.TransactionGroups.Mappings;

public static class TransactionGroupMappings
{
    public static GetTransactionGroupsResponse ToResponse(this GetTransactionGroupsResult result)
    {
        return new GetTransactionGroupsResponse
        {
            TransactionGroups = result.TransactionGroups.Select(tg => tg.ToResponse()).ToList(),
            TotalCount = result.TransactionGroups.Count
        };
    }
    
    public static TransactionGroupResponse ToResponse(this TransactionGroup transactionGroup)
    {
        return new TransactionGroupResponse
        {
            Id = transactionGroup.Id,
            Name = transactionGroup.Name,
            Description = transactionGroup.Description,
            UserId = transactionGroup.UserId,
            CreatedAt = transactionGroup.CreatedAt
        };
    }
}

