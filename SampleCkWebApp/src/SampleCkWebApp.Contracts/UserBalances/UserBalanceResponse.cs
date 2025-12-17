namespace SampleCkWebApp.UserBalances;

public class UserBalanceResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal InitialBalance { get; set; }
    public DateTime LastUpdated { get; set; }
}

