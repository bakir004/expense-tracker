using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Domain.Tests.Entities;

public class PaymentMethodTests
{
    [Fact]
    public void PaymentMethod_HasExpectedCount()
    {
        var values = Enum.GetValues<PaymentMethod>();
        Assert.Equal(8, values.Length);
    }

    [Theory]
    [InlineData(PaymentMethod.CASH)]
    [InlineData(PaymentMethod.DEBIT_CARD)]
    [InlineData(PaymentMethod.CREDIT_CARD)]
    [InlineData(PaymentMethod.BANK_TRANSFER)]
    [InlineData(PaymentMethod.MOBILE_PAYMENT)]
    [InlineData(PaymentMethod.PAYPAL)]
    [InlineData(PaymentMethod.CRYPTO)]
    [InlineData(PaymentMethod.OTHER)]
    public void PaymentMethod_AllValues_CanBeUsed(PaymentMethod method)
    {
        var _ = method;
        Assert.True(Enum.IsDefined(typeof(PaymentMethod), method));
    }
}
