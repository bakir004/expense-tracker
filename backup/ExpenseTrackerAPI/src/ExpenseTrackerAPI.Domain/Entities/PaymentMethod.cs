// ============================================================================
// FILE: PaymentMethod.cs
// ============================================================================
// WHAT: Domain enumeration representing payment methods in the expense tracker system.
//
// WHY: This enumeration exists in the Domain layer to represent the valid payment
//      methods that can be used for expenses and income. It matches the database
//      enum type and provides type safety in the application code.
//
// WHAT IT DOES:
//      - Defines all valid payment method values
//      - Used by Expense and Income entities
//      - Provides type safety for payment method values
// ============================================================================

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

