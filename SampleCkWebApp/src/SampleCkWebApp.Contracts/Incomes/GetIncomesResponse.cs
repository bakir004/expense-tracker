namespace SampleCkWebApp.Incomes;

public class GetIncomesResponse
{
    public List<IncomeResponse> Incomes { get; set; } = new();
    public int TotalCount { get; set; }
}

