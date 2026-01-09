// ============================================================================
// FILE: PaymentMethodHelper.cs
// ============================================================================
// WHAT: Helper class for converting between PaymentMethod enum and database string values.
//
// WHY: The database stores payment methods as UPPER_SNAKE_CASE strings (e.g., 'DEBIT_CARD'),
//      while the C# domain uses PascalCase enum values (e.g., PaymentMethod.DebitCard).
//      This helper provides conversion methods to map between the two formats.
//
// WHAT IT DOES:
//      - Converts PaymentMethod enum to database string format
//      - Converts database string to PaymentMethod enum
//      - Used by ExpenseRepository and IncomeRepository
// ============================================================================

using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Infrastructure.Shared;

public static class PaymentMethodHelper
{
    /// <summary>
    /// Converts PaymentMethod enum to database string format (UPPER_SNAKE_CASE).
    /// </summary>
    public static string ToDatabaseString(PaymentMethod paymentMethod)
    {
        return paymentMethod switch
        {
            PaymentMethod.Cash => "CASH",
            PaymentMethod.DebitCard => "DEBIT_CARD",
            PaymentMethod.CreditCard => "CREDIT_CARD",
            PaymentMethod.BankTransfer => "BANK_TRANSFER",
            PaymentMethod.MobilePayment => "MOBILE_PAYMENT",
            PaymentMethod.PayPal => "PAYPAL",
            PaymentMethod.Crypto => "CRYPTO",
            PaymentMethod.Other => "OTHER",
            _ => throw new ArgumentOutOfRangeException(nameof(paymentMethod), paymentMethod, "Unknown payment method")
        };
    }

    /// <summary>
    /// Converts database string format (UPPER_SNAKE_CASE) to PaymentMethod enum.
    /// </summary>
    public static PaymentMethod FromDatabaseString(string value)
    {
        return value switch
        {
            "CASH" => PaymentMethod.Cash,
            "DEBIT_CARD" => PaymentMethod.DebitCard,
            "CREDIT_CARD" => PaymentMethod.CreditCard,
            "BANK_TRANSFER" => PaymentMethod.BankTransfer,
            "MOBILE_PAYMENT" => PaymentMethod.MobilePayment,
            "PAYPAL" => PaymentMethod.PayPal,
            "CRYPTO" => PaymentMethod.Crypto,
            "OTHER" => PaymentMethod.Other,
            _ => throw new ArgumentException($"Unknown payment method value: {value}", nameof(value))
        };
    }
}

