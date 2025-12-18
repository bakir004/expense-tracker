namespace SampleCkWebApp.Domain.Entities;

/// <summary>
/// Represents a transaction group for grouping related transactions.
/// Examples: vacation trips, renovation projects, wedding planning.
/// Can group both expenses and income related to a common purpose.
/// </summary>
public class TransactionGroup
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public int UserId { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

