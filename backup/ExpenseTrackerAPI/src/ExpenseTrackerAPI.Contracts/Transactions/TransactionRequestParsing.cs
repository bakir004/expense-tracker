using ErrorOr;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;

namespace ExpenseTrackerAPI.Contracts.Transactions;

/// <summary>
/// Parses and structurally validates request DTOs into strongly-typed inputs for the application layer.
/// If <c>.Parse()</c> returns a value, that value is guaranteed valid at the contract level
/// (correct types, non-empty required fields, positive amount, etc.).
/// Business rules (date-in-future, expense-needs-category) are NOT checked here — those belong in the Application layer.
/// </summary>
public static class TransactionRequestParsing
{
    public static ErrorOr<ParsedCreateTransactionInput> Parse(this CreateTransactionRequest request)
    {
        var errors = new List<Error>();

        // --- Parse string → typed values ---

        TransactionType transactionType = default;
        var typeResult = TransactionParsers.ParseTransactionType(request.TransactionType);
        if (typeResult.IsError) errors.AddRange(typeResult.Errors);
        else transactionType = typeResult.Value;

        DateOnly date = default;
        var dateResult = TransactionParsers.ParseDate(request.Date);
        if (dateResult.IsError) errors.AddRange(dateResult.Errors);
        else date = dateResult.Value;

        PaymentMethod paymentMethod = default;
        var paymentResult = TransactionParsers.ParsePaymentMethod(request.PaymentMethod);
        if (paymentResult.IsError) errors.AddRange(paymentResult.Errors);
        else paymentMethod = paymentResult.Value;

        // --- Structural validation (no business context needed) ---

        var subject = request.Subject?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(subject))
            errors.Add(TransactionErrors.InvalidSubject);

        if (request.Amount <= 0)
            errors.Add(TransactionErrors.InvalidAmount);

        if (errors.Count > 0)
            return errors;

        return new ParsedCreateTransactionInput(
            request.UserId,
            transactionType,
            request.Amount,
            date,
            subject,
            string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            paymentMethod,
            request.CategoryId,
            request.TransactionGroupId,
            string.IsNullOrWhiteSpace(request.IncomeSource) ? null : request.IncomeSource.Trim());
    }

    public static ErrorOr<ParsedUpdateTransactionInput> Parse(this UpdateTransactionRequest request)
    {
        var errors = new List<Error>();

        // --- Parse string → typed values ---

        TransactionType transactionType = default;
        var typeResult = TransactionParsers.ParseTransactionType(request.TransactionType);
        if (typeResult.IsError) errors.AddRange(typeResult.Errors);
        else transactionType = typeResult.Value;

        DateOnly date = default;
        var dateResult = TransactionParsers.ParseDate(request.Date);
        if (dateResult.IsError) errors.AddRange(dateResult.Errors);
        else date = dateResult.Value;

        PaymentMethod paymentMethod = default;
        var paymentResult = TransactionParsers.ParsePaymentMethod(request.PaymentMethod);
        if (paymentResult.IsError) errors.AddRange(paymentResult.Errors);
        else paymentMethod = paymentResult.Value;

        // --- Structural validation (no business context needed) ---

        var subject = request.Subject?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(subject))
            errors.Add(TransactionErrors.InvalidSubject);

        if (request.Amount <= 0)
            errors.Add(TransactionErrors.InvalidAmount);

        if (errors.Count > 0)
            return errors;

        return new ParsedUpdateTransactionInput(
            transactionType,
            request.Amount,
            date,
            subject,
            string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            paymentMethod,
            request.CategoryId,
            request.TransactionGroupId,
            string.IsNullOrWhiteSpace(request.IncomeSource) ? null : request.IncomeSource.Trim());
    }
}
