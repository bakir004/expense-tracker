using System.Net;
using System.Net.Http.Json;
using ExpenseTrackerAPI.Contracts.Categories;
using ExpenseTrackerAPI.Contracts.TransactionGroups;
using ExpenseTrackerAPI.Contracts.Transactions;
using ExpenseTrackerAPI.WebApi.Tests.Common;
using ExpenseTrackerAPI.WebApi.Tests.Fixtures;
using FluentAssertions;

namespace ExpenseTrackerAPI.WebApi.Tests.E2E.UserWorkflows;

/// <summary>
/// E2E tests for transaction group workflows and their interactions with transactions.
/// Tests cover CRUD operations, group-transaction relationships, cascading behaviors,
/// and authorization scenarios.
/// </summary>
public class TransactionGroupWorkflowTests : BaseE2ETest
{
    public TransactionGroupWorkflowTests(ExpenseTrackerApiFactory factory) : base(factory) { }

    #region Transaction Group CRUD Tests

    [Fact]
    public async Task TransactionGroupCrudWorkflow_CreateReadUpdateDelete_ShouldCompleteSuccessfully()
    {
        // === CREATE ===
        var createRequest = new CreateTransactionGroupRequest(
            Name: "Vacation Trip 2024",
            Description: "Summer vacation to Europe");

        var createResponse = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, createRequest);

        // Assert Create
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdGroup = await createResponse.Content.ReadFromJsonAsync<TransactionGroupResponse>();
        createdGroup.Should().NotBeNull();
        createdGroup!.Id.Should().BeGreaterThan(0);
        createdGroup.Name.Should().Be("Vacation Trip 2024");
        createdGroup.Description.Should().Be("Summer vacation to Europe");
        createdGroup.UserId.Should().Be(TestAuthHandler.DefaultUserId);

        var groupId = createdGroup.Id;

        // === READ Single ===
        var getResponse = await Client.GetAsync(TestConstants.Routes.TransactionGroup(groupId));

        // Assert Read
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedGroup = await getResponse.Content.ReadFromJsonAsync<TransactionGroupResponse>();
        retrievedGroup.Should().NotBeNull();
        retrievedGroup!.Id.Should().Be(groupId);
        retrievedGroup.Name.Should().Be("Vacation Trip 2024");

        // === UPDATE ===
        var updateRequest = new UpdateTransactionGroupRequest(
            Name: "Updated Vacation Trip",
            Description: "Updated description for trip");

        var updateResponse = await Client.PutAsJsonAsync(
            TestConstants.Routes.TransactionGroup(groupId),
            updateRequest);

        // Assert Update
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedGroup = await updateResponse.Content.ReadFromJsonAsync<TransactionGroupResponse>();
        updatedGroup.Should().NotBeNull();
        updatedGroup!.Name.Should().Be("Updated Vacation Trip");
        updatedGroup.Description.Should().Be("Updated description for trip");

        // Verify update persisted
        var verifyResponse = await Client.GetAsync(TestConstants.Routes.TransactionGroup(groupId));
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifiedGroup = await verifyResponse.Content.ReadFromJsonAsync<TransactionGroupResponse>();
        verifiedGroup!.Name.Should().Be("Updated Vacation Trip");

        // === DELETE ===
        var deleteResponse = await Client.DeleteAsync(TestConstants.Routes.TransactionGroup(groupId));

        // Assert Delete
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getDeletedResponse = await Client.GetAsync(TestConstants.Routes.TransactionGroup(groupId));
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTransactionGroup_WithNullDescription_ShouldSucceed()
    {
        // Arrange
        var request = new CreateTransactionGroupRequest(
            Name: "No Description Group",
            Description: null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var group = await response.Content.ReadFromJsonAsync<TransactionGroupResponse>();
        group.Should().NotBeNull();
        group!.Name.Should().Be("No Description Group");
        group.Description.Should().BeNull();
    }

    [Fact]
    public async Task CreateTransactionGroup_WithEmptyName_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateTransactionGroupRequest(
            Name: "",
            Description: "Some description");

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransactionGroup_WithWhitespaceName_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateTransactionGroupRequest(
            Name: "   ",
            Description: null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAllTransactionGroups_ShouldReturnUserGroups()
    {
        // Arrange - Create a couple of groups first
        var group1 = new CreateTransactionGroupRequest("Test Group Alpha", "Alpha description");
        var group2 = new CreateTransactionGroupRequest("Test Group Beta", "Beta description");

        await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, group1);
        await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, group2);

        // Act
        var response = await Client.GetAsync(TestConstants.Routes.TransactionGroups);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var groups = await response.Content.ReadFromJsonAsync<List<TransactionGroupResponse>>();
        groups.Should().NotBeNull();
        groups!.Count.Should().BeGreaterThanOrEqualTo(2);
        groups.Should().OnlyContain(g => g.UserId == TestAuthHandler.DefaultUserId);
    }

    [Fact]
    public async Task GetTransactionGroup_WithNonExistentId_ShouldReturnNotFound()
    {
        // Act
        var response = await Client.GetAsync(TestConstants.Routes.TransactionGroup(99999));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTransactionGroup_WithInvalidId_ShouldReturnBadRequest()
    {
        // Act
        var response = await Client.GetAsync(TestConstants.Routes.TransactionGroup(-1));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTransactionGroup_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var request = new UpdateTransactionGroupRequest("Updated Name", "Updated description");

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.TransactionGroup(99999), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTransactionGroup_WithNonExistentId_ShouldReturnNotFound()
    {
        // Act
        var response = await Client.DeleteAsync(TestConstants.Routes.TransactionGroup(99999));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTransactionGroup_CanSetDescriptionToNull()
    {
        // Arrange - Create a group with description
        var createRequest = new CreateTransactionGroupRequest(
            Name: "Group With Description",
            Description: "Initial description");

        var createResponse = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, createRequest);
        var createdGroup = await createResponse.Content.ReadFromJsonAsync<TransactionGroupResponse>();

        // Act - Update with null description
        var updateRequest = new UpdateTransactionGroupRequest(
            Name: "Updated Group",
            Description: null);

        var updateResponse = await Client.PutAsJsonAsync(
            TestConstants.Routes.TransactionGroup(createdGroup!.Id),
            updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedGroup = await updateResponse.Content.ReadFromJsonAsync<TransactionGroupResponse>();
        updatedGroup!.Description.Should().BeNull();
    }

    #endregion

    #region Transaction with TransactionGroup Tests

    [Fact]
    public async Task CreateTransaction_WithTransactionGroup_ShouldSucceed()
    {
        // Arrange - Create a transaction group first
        var groupRequest = new CreateTransactionGroupRequest(
            Name: "Project Alpha",
            Description: "Project expenses");

        var groupResponse = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, groupRequest);
        groupResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var group = await groupResponse.Content.ReadFromJsonAsync<TransactionGroupResponse>();

        // Create a transaction with the group
        var transactionRequest = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 150.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Project equipment",
            Notes = "Purchased for Project Alpha",
            PaymentMethod = "CREDIT_CARD",
            CategoryId = null,
            TransactionGroupId = group!.Id
        };

        // Act
        var transactionResponse = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, transactionRequest);

        // Assert
        transactionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var transaction = await transactionResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        transaction.Should().NotBeNull();
        transaction!.TransactionGroupId.Should().Be(group.Id);
        transaction.Subject.Should().Be("Project equipment");
    }

    [Fact]
    public async Task CreateMultipleTransactions_WithSameGroup_ShouldSucceed()
    {
        // Arrange - Create a transaction group
        var groupRequest = new CreateTransactionGroupRequest(
            Name: "Home Renovation Project",
            Description: "Kitchen remodel");

        var groupResponse = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, groupRequest);
        var group = await groupResponse.Content.ReadFromJsonAsync<TransactionGroupResponse>();

        // Create multiple transactions for the same group
        var transaction1 = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 500.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            Subject = "Paint supplies",
            PaymentMethod = "DEBIT_CARD",
            TransactionGroupId = group!.Id
        };

        var transaction2 = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 1500.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
            Subject = "Kitchen cabinets",
            PaymentMethod = "CREDIT_CARD",
            TransactionGroupId = group.Id
        };

        var transaction3 = new CreateTransactionRequest
        {
            TransactionType = "INCOME",
            Amount = 200.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Refund for returned items",
            PaymentMethod = "BANK_TRANSFER",
            TransactionGroupId = group.Id
        };

        // Act
        var response1 = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, transaction1);
        var response2 = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, transaction2);
        var response3 = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, transaction3);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);
        response3.StatusCode.Should().Be(HttpStatusCode.Created);

        var t1 = await response1.Content.ReadFromJsonAsync<TransactionResponse>();
        var t2 = await response2.Content.ReadFromJsonAsync<TransactionResponse>();
        var t3 = await response3.Content.ReadFromJsonAsync<TransactionResponse>();

        t1!.TransactionGroupId.Should().Be(group.Id);
        t2!.TransactionGroupId.Should().Be(group.Id);
        t3!.TransactionGroupId.Should().Be(group.Id);
    }

    [Fact]
    public async Task CreateTransaction_WithNonExistentGroup_ShouldReturnBadRequest()
    {
        // Arrange
        var transactionRequest = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 100.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Test transaction",
            PaymentMethod = "CASH",
            TransactionGroupId = 99999 // Non-existent group
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, transactionRequest);

        // Assert - Should fail because the group doesn't exist
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTransaction_AddGroupToExistingTransaction_ShouldSucceed()
    {
        // Arrange - Create a transaction without a group
        var transactionRequest = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 75.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Unassigned expense",
            PaymentMethod = "CASH",
            TransactionGroupId = null
        };

        var createTransactionResponse = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, transactionRequest);
        var transaction = await createTransactionResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        transaction!.TransactionGroupId.Should().BeNull();

        // Create a transaction group
        var groupRequest = new CreateTransactionGroupRequest("New Group", "For testing");
        var groupResponse = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, groupRequest);
        var group = await groupResponse.Content.ReadFromJsonAsync<TransactionGroupResponse>();

        // Act - Update the transaction to add the group
        var updateRequest = new UpdateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 75.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Now assigned to group",
            PaymentMethod = "CASH",
            TransactionGroupId = group!.Id
        };

        var updateResponse = await Client.PutAsJsonAsync(
            TestConstants.Routes.Transaction(transaction.Id),
            updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedTransaction = await updateResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        updatedTransaction!.TransactionGroupId.Should().Be(group.Id);
    }

    [Fact]
    public async Task UpdateTransaction_RemoveGroupFromTransaction_ShouldSucceed()
    {
        // Arrange - Create a group and transaction with the group
        var groupRequest = new CreateTransactionGroupRequest("Temporary Group", null);
        var groupResponse = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, groupRequest);
        var group = await groupResponse.Content.ReadFromJsonAsync<TransactionGroupResponse>();

        var transactionRequest = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 50.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Grouped expense",
            PaymentMethod = "DEBIT_CARD",
            TransactionGroupId = group!.Id
        };

        var createResponse = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, transactionRequest);
        var transaction = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        transaction!.TransactionGroupId.Should().Be(group.Id);

        // Act - Remove the group
        var updateRequest = new UpdateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 50.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "No longer grouped",
            PaymentMethod = "DEBIT_CARD",
            TransactionGroupId = null
        };

        var updateResponse = await Client.PutAsJsonAsync(
            TestConstants.Routes.Transaction(transaction.Id),
            updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedTransaction = await updateResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        updatedTransaction!.TransactionGroupId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTransaction_ChangeGroupToAnotherGroup_ShouldSucceed()
    {
        // Arrange - Create two groups
        var group1Request = new CreateTransactionGroupRequest("Group One", "First group");
        var group2Request = new CreateTransactionGroupRequest("Group Two", "Second group");

        var group1Response = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, group1Request);
        var group2Response = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, group2Request);

        var group1 = await group1Response.Content.ReadFromJsonAsync<TransactionGroupResponse>();
        var group2 = await group2Response.Content.ReadFromJsonAsync<TransactionGroupResponse>();

        // Create transaction with group 1
        var transactionRequest = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 100.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Moving between groups",
            PaymentMethod = "CREDIT_CARD",
            TransactionGroupId = group1!.Id
        };

        var createResponse = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, transactionRequest);
        var transaction = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>();

        // Act - Move to group 2
        var updateRequest = new UpdateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 100.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Moved to group two",
            PaymentMethod = "CREDIT_CARD",
            TransactionGroupId = group2!.Id
        };

        var updateResponse = await Client.PutAsJsonAsync(
            TestConstants.Routes.Transaction(transaction!.Id),
            updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedTransaction = await updateResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        updatedTransaction!.TransactionGroupId.Should().Be(group2.Id);
    }

    #endregion

    #region Delete Group Cascade Behavior Tests (SetNull)

    [Fact]
    public async Task DeleteTransactionGroup_WithAssociatedTransactions_ShouldSetTransactionGroupIdToNull()
    {
        // Arrange - Create a group
        var groupRequest = new CreateTransactionGroupRequest(
            Name: "Group To Delete",
            Description: "Will be deleted");

        var groupResponse = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, groupRequest);
        groupResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var group = await groupResponse.Content.ReadFromJsonAsync<TransactionGroupResponse>();

        // Create transactions associated with the group
        var transaction1Request = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 100.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            Subject = "First linked transaction",
            PaymentMethod = "CASH",
            TransactionGroupId = group!.Id
        };

        var transaction2Request = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 200.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            Subject = "Second linked transaction",
            PaymentMethod = "DEBIT_CARD",
            TransactionGroupId = group.Id
        };

        var t1Response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, transaction1Request);
        var t2Response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, transaction2Request);

        var transaction1 = await t1Response.Content.ReadFromJsonAsync<TransactionResponse>();
        var transaction2 = await t2Response.Content.ReadFromJsonAsync<TransactionResponse>();

        // Verify transactions are linked to the group
        transaction1!.TransactionGroupId.Should().Be(group.Id);
        transaction2!.TransactionGroupId.Should().Be(group.Id);

        // Act - Delete the group
        var deleteResponse = await Client.DeleteAsync(TestConstants.Routes.TransactionGroup(group.Id));
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert - Transactions should still exist but with null TransactionGroupId
        var t1VerifyResponse = await Client.GetAsync(TestConstants.Routes.Transaction(transaction1.Id));
        var t2VerifyResponse = await Client.GetAsync(TestConstants.Routes.Transaction(transaction2.Id));

        t1VerifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        t2VerifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var verifiedT1 = await t1VerifyResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        var verifiedT2 = await t2VerifyResponse.Content.ReadFromJsonAsync<TransactionResponse>();

        verifiedT1!.TransactionGroupId.Should().BeNull();
        verifiedT2!.TransactionGroupId.Should().BeNull();

        // Transaction data should remain unchanged except for the group ID
        verifiedT1.Subject.Should().Be("First linked transaction");
        verifiedT1.Amount.Should().Be(100.00m);
        verifiedT2.Subject.Should().Be("Second linked transaction");
        verifiedT2.Amount.Should().Be(200.00m);
    }

    [Fact]
    public async Task DeleteTransactionGroup_OnlyAffectsLinkedTransactions_OtherTransactionsUnchanged()
    {
        // Arrange - Create a group
        var groupRequest = new CreateTransactionGroupRequest("Group To Delete", "Test");
        var groupResponse = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, groupRequest);
        var group = await groupResponse.Content.ReadFromJsonAsync<TransactionGroupResponse>();

        // Create a linked transaction
        var linkedTransactionRequest = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 50.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Linked to group",
            PaymentMethod = "CASH",
            TransactionGroupId = group!.Id
        };

        // Create an unlinked transaction
        var unlinkedTransactionRequest = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 75.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Not linked to any group",
            PaymentMethod = "CASH",
            TransactionGroupId = null
        };

        var linkedResponse = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, linkedTransactionRequest);
        var unlinkedResponse = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, unlinkedTransactionRequest);

        var linkedTransaction = await linkedResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        var unlinkedTransaction = await unlinkedResponse.Content.ReadFromJsonAsync<TransactionResponse>();

        // Act - Delete the group
        await Client.DeleteAsync(TestConstants.Routes.TransactionGroup(group.Id));

        // Assert - Both transactions should still exist
        var linkedVerify = await Client.GetAsync(TestConstants.Routes.Transaction(linkedTransaction!.Id));
        var unlinkedVerify = await Client.GetAsync(TestConstants.Routes.Transaction(unlinkedTransaction!.Id));

        linkedVerify.StatusCode.Should().Be(HttpStatusCode.OK);
        unlinkedVerify.StatusCode.Should().Be(HttpStatusCode.OK);

        var verifiedLinked = await linkedVerify.Content.ReadFromJsonAsync<TransactionResponse>();
        var verifiedUnlinked = await unlinkedVerify.Content.ReadFromJsonAsync<TransactionResponse>();

        // Linked transaction should have null group ID now
        verifiedLinked!.TransactionGroupId.Should().BeNull();

        // Unlinked transaction should still have null group ID (unchanged)
        verifiedUnlinked!.TransactionGroupId.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTransactionGroup_WithNoTransactions_ShouldSucceed()
    {
        // Arrange - Create a group with no transactions
        var groupRequest = new CreateTransactionGroupRequest("Empty Group", "No transactions");
        var groupResponse = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, groupRequest);
        var group = await groupResponse.Content.ReadFromJsonAsync<TransactionGroupResponse>();

        // Act
        var deleteResponse = await Client.DeleteAsync(TestConstants.Routes.TransactionGroup(group!.Id));

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's actually deleted
        var getResponse = await Client.GetAsync(TestConstants.Routes.TransactionGroup(group.Id));
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Combined Transaction and Category Tests with Groups

    [Fact]
    public async Task CreateTransaction_WithCategoryAndGroup_ShouldSucceed()
    {
        // Arrange - Create a transaction group
        var groupRequest = new CreateTransactionGroupRequest("Business Trip", "Work travel expenses");
        var groupResponse = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, groupRequest);
        var group = await groupResponse.Content.ReadFromJsonAsync<TransactionGroupResponse>();

        // Get available categories
        var categoriesResponse = await Client.GetAsync(TestConstants.Routes.Categories);
        categoriesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await categoriesResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        categories.Should().NotBeEmpty();

        var transportationCategory = categories!.FirstOrDefault(c => c.Name.Contains("Transportation"));
        transportationCategory.Should().NotBeNull();

        // Create transaction with both category and group
        var transactionRequest = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 350.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Flight tickets for business trip",
            Notes = "Round trip to conference",
            PaymentMethod = "CREDIT_CARD",
            CategoryId = transportationCategory!.Id,
            TransactionGroupId = group!.Id
        };

        // Act
        var transactionResponse = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, transactionRequest);

        // Assert
        transactionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var transaction = await transactionResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        transaction.Should().NotBeNull();
        transaction!.CategoryId.Should().Be(transportationCategory.Id);
        transaction.TransactionGroupId.Should().Be(group.Id);
    }

    #endregion

    #region Transaction Group List with Transactions Tests

    [Fact]
    public async Task GetAllGroups_AfterMultipleCreations_ShouldReturnAllUserGroups()
    {
        // Arrange - Create multiple groups
        var groupNames = new[] { "Group A Test", "Group B Test", "Group C Test" };

        foreach (var name in groupNames)
        {
            var request = new CreateTransactionGroupRequest(name, $"Description for {name}");
            var response = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, request);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        // Act
        var getResponse = await Client.GetAsync(TestConstants.Routes.TransactionGroups);

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var groups = await getResponse.Content.ReadFromJsonAsync<List<TransactionGroupResponse>>();
        groups.Should().NotBeNull();

        // Should contain all the groups we created
        foreach (var name in groupNames)
        {
            groups!.Should().Contain(g => g.Name == name);
        }
    }

    #endregion

    #region Edge Cases and Validation Tests

    [Fact]
    public async Task CreateTransactionGroup_WithVeryLongName_ShouldHandleCorrectly()
    {
        // Arrange - Name at max length (255 characters)
        var maxLengthName = new string('A', 255);
        var request = new CreateTransactionGroupRequest(maxLengthName, null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, request);

        // Assert - Should succeed
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var group = await response.Content.ReadFromJsonAsync<TransactionGroupResponse>();
        group!.Name.Should().Be(maxLengthName);
    }

    [Fact]
    public async Task CreateTransactionGroup_WithNameTooLong_ShouldReturnBadRequest()
    {
        // Arrange - Name exceeds max length (256 characters)
        var tooLongName = new string('A', 256);
        var request = new CreateTransactionGroupRequest(tooLongName, null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTransactionGroup_TrimsWhitespaceFromName()
    {
        // Arrange - Create a group
        var createRequest = new CreateTransactionGroupRequest("Original Name", null);
        var createResponse = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, createRequest);
        var group = await createResponse.Content.ReadFromJsonAsync<TransactionGroupResponse>();

        // Act - Update with whitespace-padded name
        var updateRequest = new UpdateTransactionGroupRequest("  Trimmed Name  ", null);
        var updateResponse = await Client.PutAsJsonAsync(
            TestConstants.Routes.TransactionGroup(group!.Id),
            updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedGroup = await updateResponse.Content.ReadFromJsonAsync<TransactionGroupResponse>();
        updatedGroup!.Name.Should().Be("Trimmed Name");
    }

    [Fact]
    public async Task TransactionGroup_CreatedAtTimestamp_ShouldBeSet()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);
        var request = new CreateTransactionGroupRequest("Timestamp Test", null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, request);
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var group = await response.Content.ReadFromJsonAsync<TransactionGroupResponse>();
        group!.CreatedAt.Should().BeAfter(beforeCreation);
        group.CreatedAt.Should().BeBefore(afterCreation);
    }

    #endregion

    #region Mixed Income and Expense with Same Group Tests

    [Fact]
    public async Task TransactionGroup_CanContainBothExpensesAndIncome()
    {
        // Arrange - Create a group for a project that has both expenses and income
        var groupRequest = new CreateTransactionGroupRequest(
            "Freelance Project",
            "Client project with expenses and payments");
        var groupResponse = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, groupRequest);
        var group = await groupResponse.Content.ReadFromJsonAsync<TransactionGroupResponse>();

        // Create expense
        var expenseRequest = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 50.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
            Subject = "Software license for project",
            PaymentMethod = "CREDIT_CARD",
            TransactionGroupId = group!.Id
        };

        // Create income
        var incomeRequest = new CreateTransactionRequest
        {
            TransactionType = "INCOME",
            Amount = 500.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Client payment milestone 1",
            PaymentMethod = "BANK_TRANSFER",
            TransactionGroupId = group.Id
        };

        // Act
        var expenseResponse = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, expenseRequest);
        var incomeResponse = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, incomeRequest);

        // Assert
        expenseResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        incomeResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var expense = await expenseResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        var income = await incomeResponse.Content.ReadFromJsonAsync<TransactionResponse>();

        expense!.TransactionGroupId.Should().Be(group.Id);
        expense.TransactionType.Should().Be("EXPENSE");

        income!.TransactionGroupId.Should().Be(group.Id);
        income.TransactionType.Should().Be("INCOME");
    }

    #endregion

    #region Complete Workflow Tests

    [Fact]
    public async Task CompleteProjectWorkflow_CreateGroupAddTransactionsDeleteGroup()
    {
        // This test simulates a complete project lifecycle:
        // 1. Create a transaction group for a project
        // 2. Add multiple transactions (expenses and income)
        // 3. Update some transactions
        // 4. Delete the group
        // 5. Verify transactions still exist with null group ID

        // === STEP 1: Create project group ===
        var groupRequest = new CreateTransactionGroupRequest(
            "Kitchen Renovation 2024",
            "Complete kitchen remodel project");
        var groupResponse = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, groupRequest);
        groupResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var group = await groupResponse.Content.ReadFromJsonAsync<TransactionGroupResponse>();

        // === STEP 2: Add transactions ===
        var expenses = new[]
        {
            new CreateTransactionRequest
            {
                TransactionType = "EXPENSE",
                Amount = 2000.00m,
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
                Subject = "Kitchen cabinets",
                PaymentMethod = "CREDIT_CARD",
                TransactionGroupId = group!.Id
            },
            new CreateTransactionRequest
            {
                TransactionType = "EXPENSE",
                Amount = 800.00m,
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20)),
                Subject = "Countertops",
                PaymentMethod = "DEBIT_CARD",
                TransactionGroupId = group.Id
            },
            new CreateTransactionRequest
            {
                TransactionType = "EXPENSE",
                Amount = 500.00m,
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                Subject = "Plumbing work",
                PaymentMethod = "CASH",
                TransactionGroupId = group.Id
            }
        };

        var income = new CreateTransactionRequest
        {
            TransactionType = "INCOME",
            Amount = 200.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            Subject = "Refund for damaged cabinet",
            PaymentMethod = "BANK_TRANSFER",
            TransactionGroupId = group.Id
        };

        var transactionIds = new List<int>();

        foreach (var expense in expenses)
        {
            var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, expense);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>();
            transactionIds.Add(transaction!.Id);
        }

        var incomeResponse = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, income);
        incomeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var incomeTransaction = await incomeResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        transactionIds.Add(incomeTransaction!.Id);

        // Verify all transactions are linked to the group
        foreach (var id in transactionIds)
        {
            var getResponse = await Client.GetAsync(TestConstants.Routes.Transaction(id));
            var transaction = await getResponse.Content.ReadFromJsonAsync<TransactionResponse>();
            transaction!.TransactionGroupId.Should().Be(group.Id);
        }

        // === STEP 3: Update a transaction ===
        var updateRequest = new UpdateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 850.00m, // Price adjustment
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20)),
            Subject = "Countertops (upgraded)",
            PaymentMethod = "DEBIT_CARD",
            TransactionGroupId = group.Id
        };

        var updateResponse = await Client.PutAsJsonAsync(
            TestConstants.Routes.Transaction(transactionIds[1]),
            updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // === STEP 4: Delete the group ===
        var deleteResponse = await Client.DeleteAsync(TestConstants.Routes.TransactionGroup(group.Id));
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // === STEP 5: Verify transactions still exist with null group ID ===
        foreach (var id in transactionIds)
        {
            var getResponse = await Client.GetAsync(TestConstants.Routes.Transaction(id));
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var transaction = await getResponse.Content.ReadFromJsonAsync<TransactionResponse>();
            transaction!.TransactionGroupId.Should().BeNull(
                $"Transaction {id} should have null TransactionGroupId after group deletion");
        }

        // Verify the updated transaction still has the updated values
        var updatedTransactionResponse = await Client.GetAsync(TestConstants.Routes.Transaction(transactionIds[1]));
        var updatedTransaction = await updatedTransactionResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        updatedTransaction!.Amount.Should().Be(850.00m);
        updatedTransaction.Subject.Should().Be("Countertops (upgraded)");
    }

    #endregion
}
