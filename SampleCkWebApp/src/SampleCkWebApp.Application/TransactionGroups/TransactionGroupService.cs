using ErrorOr;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Domain.Errors;
using SampleCkWebApp.Application.TransactionGroups.Data;
using SampleCkWebApp.Application.TransactionGroups.Interfaces.Application;
using SampleCkWebApp.Application.TransactionGroups.Interfaces.Infrastructure;
using SampleCkWebApp.Application.Users.Interfaces.Infrastructure;

namespace SampleCkWebApp.Application.TransactionGroups;

public class TransactionGroupService : ITransactionGroupService
{
    private readonly ITransactionGroupRepository _transactionGroupRepository;
    private readonly IUserRepository _userRepository;

    public TransactionGroupService(
        ITransactionGroupRepository transactionGroupRepository,
        IUserRepository userRepository)
    {
        _transactionGroupRepository = transactionGroupRepository ?? throw new ArgumentNullException(nameof(transactionGroupRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }
    
    public async Task<ErrorOr<GetTransactionGroupsResult>> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = await _transactionGroupRepository.GetAllAsync(cancellationToken);
        if (result.IsError)
        {
            return result.Errors;
        }
        
        return new GetTransactionGroupsResult
        {
            TransactionGroups = result.Value
        };
    }
    
    public async Task<ErrorOr<TransactionGroup>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _transactionGroupRepository.GetByIdAsync(id, cancellationToken);
    }
    
    public async Task<ErrorOr<List<TransactionGroup>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        // Verify the user exists first
        var userResult = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
        if (userResult.IsError)
        {
            return UserErrors.NotFound;
        }
        
        // User exists, get their transaction groups (may be empty)
        return await _transactionGroupRepository.GetByUserIdAsync(userId, cancellationToken);
    }
    
    public async Task<ErrorOr<TransactionGroup>> CreateAsync(string name, string? description, int userId, CancellationToken cancellationToken)
    {
        var validationResult = TransactionGroupValidator.ValidateTransactionGroupRequest(name, description, userId);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }
        
        // Verify the user exists
        var userResult = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
        if (userResult.IsError)
        {
            return TransactionGroupErrors.UserNotFound;
        }
        
        var transactionGroup = new TransactionGroup
        {
            Name = name,
            Description = description,
            UserId = userId
        };
        
        return await _transactionGroupRepository.CreateAsync(transactionGroup, cancellationToken);
    }
    
    public async Task<ErrorOr<TransactionGroup>> UpdateAsync(int id, string name, string? description, CancellationToken cancellationToken)
    {
        var validationResult = TransactionGroupValidator.ValidateTransactionGroupRequest(name, description, 1); // userId not needed for update validation
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }
        
        var existingResult = await _transactionGroupRepository.GetByIdAsync(id, cancellationToken);
        if (existingResult.IsError)
        {
            return existingResult.Errors;
        }
        
        var transactionGroup = new TransactionGroup
        {
            Id = id,
            Name = name,
            Description = description,
            UserId = existingResult.Value.UserId
        };
        
        return await _transactionGroupRepository.UpdateAsync(transactionGroup, cancellationToken);
    }
    
    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        return await _transactionGroupRepository.DeleteAsync(id, cancellationToken);
    }
}

