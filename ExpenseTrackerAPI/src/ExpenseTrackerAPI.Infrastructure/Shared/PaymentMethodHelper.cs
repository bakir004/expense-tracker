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
            PaymentMethod.CASH => "CASH",
            PaymentMethod.DEBIT_CARD => "DEBIT_CARD",
            PaymentMethod.CREDIT_CARD => "CREDIT_CARD",
            PaymentMethod.BANK_TRANSFER => "BANK_TRANSFER",
            PaymentMethod.MOBILE_PAYMENT => "MOBILE_PAYMENT",
            PaymentMethod.PAYPAL => "PAYPAL",
            PaymentMethod.CRYPTO => "CRYPTO",
            PaymentMethod.OTHER => "OTHER",
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
            "CASH" => PaymentMethod.CASH,
            "DEBIT_CARD" => PaymentMethod.DEBIT_CARD,
            "CREDIT_CARD" => PaymentMethod.CREDIT_CARD,
            "BANK_TRANSFER" => PaymentMethod.BANK_TRANSFER,
            "MOBILE_PAYMENT" => PaymentMethod.MOBILE_PAYMENT,
            "PAYPAL" => PaymentMethod.PAYPAL,
            "CRYPTO" => PaymentMethod.CRYPTO,
            "OTHER" => PaymentMethod.OTHER,
            _ => throw new ArgumentException($"Unknown payment method value: {value}", nameof(value))
        };
    }
}

