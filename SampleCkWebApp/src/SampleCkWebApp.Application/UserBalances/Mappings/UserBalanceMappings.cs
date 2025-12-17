using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.UserBalances;

namespace SampleCkWebApp.Application.UserBalances.Mappings;

public static class UserBalanceMappings
{
    public static UserBalanceResponse ToResponse(this UserBalance userBalance)
    {
        return new UserBalanceResponse
        {
            Id = userBalance.Id,
            UserId = userBalance.UserId,
            CurrentBalance = userBalance.CurrentBalance,
            InitialBalance = userBalance.InitialBalance,
            LastUpdated = userBalance.LastUpdated
        };
    }
}

