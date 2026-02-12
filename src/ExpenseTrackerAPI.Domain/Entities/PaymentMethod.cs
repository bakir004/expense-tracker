namespace ExpenseTrackerAPI.Domain.Entities;

/// <summary>
/// Represents the payment method used for expenses and income.
/// </summary>
public enum PaymentMethod
{
    CASH,
    DEBIT_CARD,
    CREDIT_CARD,
    BANK_TRANSFER,
    MOBILE_PAYMENT,
    PAYPAL,
    CRYPTO,
    OTHER
}
