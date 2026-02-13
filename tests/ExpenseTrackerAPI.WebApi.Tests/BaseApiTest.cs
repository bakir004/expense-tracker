namespace ExpenseTrackerAPI.WebApi.Tests;

public abstract class BaseApiTest : IClassFixture<ExpenseTrackerApiFactory>
{
    protected readonly HttpClient Client;
    protected readonly ExpenseTrackerApiFactory Factory;

    protected BaseApiTest(ExpenseTrackerApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }
}
