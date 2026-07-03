using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Splitly.Api.Contracts.Common;
using Splitly.Api.Contracts.Expenses;
using Splitly.Api.Contracts.Groups;
using Splitly.Api.Contracts.Participants;
using Splitly.Api.Contracts.Payments;
using Splitly.Api.Contracts.Settlement;

namespace Splitly.Tests.Integration;

public sealed class SplitlyApiTests(SplitlyApiFactory factory) : IClassFixture<SplitlyApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task FullFlow_FromGroupCreationToSettlement()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/groups", new CreateGroupRequest("Trip", "EUR"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);

        var group = await createResponse.Content.ReadFromJsonAsync<GroupResponse>();
        Assert.NotNull(group);

        var alice = await AddParticipantAsync(group.Id, "Alice");
        var bob = await AddParticipantAsync(group.Id, "Bob");
        var carol = await AddParticipantAsync(group.Id, "Carol");

        await AddExpenseAsync(group.Id, new AddExpenseRequest(alice, 90m, "Dinner", Today, [alice, bob, carol]));
        await AddExpenseAsync(group.Id, new AddExpenseRequest(bob, 30m, "Taxi", Today, [alice, bob, carol]));

        var fetched = await _client.GetFromJsonAsync<GroupResponse>($"/groups/{group.Id}");
        Assert.NotNull(fetched);
        Assert.Equal(3, fetched.Participants.Count);

        var expenses = await _client.GetFromJsonAsync<PaginatedResponse<ExpenseResponse>>(
            $"/groups/{group.Id}/expenses?page=1&pageSize=10");
        Assert.NotNull(expenses);
        Assert.Equal(2, expenses.TotalCount);

        var settlement = await _client.GetFromJsonAsync<SettlementResponse>(
            $"/groups/{group.Id}/settlement");
        Assert.NotNull(settlement);

        Assert.Equal(2, settlement.Transfers.Count);
        Assert.Contains(settlement.Transfers,
            t => t.FromParticipantId == carol && t.ToParticipantId == alice && t.Amount == 40m);
        Assert.Contains(settlement.Transfers,
            t => t.FromParticipantId == bob && t.ToParticipantId == alice && t.Amount == 10m);
    }

    [Fact]
    public async Task DuplicateParticipant_ReturnsConflictProblemDetails()
    {
        var groupId = await CreateGroupAsync("Flat");
        await AddParticipantAsync(groupId, "Alice");

        var response = await _client.PostAsJsonAsync(
            $"/groups/{groupId}/participants", new AddParticipantRequest("alice"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("ExpenseGroup.DuplicateParticipant", problem.Title);
    }

    [Fact]
    public async Task UnknownGroup_ReturnsNotFoundProblemDetails()
    {
        var response = await _client.GetAsync($"/groups/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task InvalidAmount_ReturnsValidationProblemDetails()
    {
        var groupId = await CreateGroupAsync("Picnic");
        var alice = await AddParticipantAsync(groupId, "Alice");

        var response = await _client.PostAsJsonAsync(
            $"/groups/{groupId}/expenses",
            new AddExpenseRequest(alice, -5m, "Snacks", Today, [alice]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("ExpenseGroup.InvalidAmount", problem.Errors.Keys);
    }

    [Fact]
    public async Task RemovingParticipantInvolvedInExpense_ReturnsConflict()
    {
        var groupId = await CreateGroupAsync("Roadtrip");
        var alice = await AddParticipantAsync(groupId, "Alice");
        var bob = await AddParticipantAsync(groupId, "Bob");
        await AddExpenseAsync(groupId, new AddExpenseRequest(alice, 20m, "Fuel", Today, [alice, bob]));

        var response = await _client.DeleteAsync($"/groups/{groupId}/participants/{bob}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RecordedPayment_SettlesTheGroup_AndCanBeUndone()
    {
        var groupId = await CreateGroupAsync("Weekend");
        var alice = await AddParticipantAsync(groupId, "Alice");
        var bob = await AddParticipantAsync(groupId, "Bob");
        await AddExpenseAsync(groupId, new AddExpenseRequest(alice, 100m, "Hotel", Today, [alice, bob]));

        var recordResponse = await _client.PostAsJsonAsync(
            $"/groups/{groupId}/payments",
            new RecordPaymentRequest(bob, alice, 50m, Today));

        Assert.Equal(HttpStatusCode.Created, recordResponse.StatusCode);
        var payment = await recordResponse.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(payment);

        var settled = await _client.GetFromJsonAsync<SettlementResponse>(
            $"/groups/{groupId}/settlement");
        Assert.NotNull(settled);
        Assert.Empty(settled.Transfers);

        var payments = await _client.GetFromJsonAsync<PaginatedResponse<PaymentResponse>>(
            $"/groups/{groupId}/payments");
        Assert.NotNull(payments);
        Assert.Equal(1, payments.TotalCount);

        var deleteResponse = await _client.DeleteAsync($"/groups/{groupId}/payments/{payment.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var unsettled = await _client.GetFromJsonAsync<SettlementResponse>(
            $"/groups/{groupId}/settlement");
        Assert.NotNull(unsettled);
        var transfer = Assert.Single(unsettled.Transfers);
        Assert.Equal(bob, transfer.FromParticipantId);
        Assert.Equal(alice, transfer.ToParticipantId);
        Assert.Equal(50m, transfer.Amount);
    }

    [Fact]
    public async Task SettlementStrategies_AreSelectableViaQueryParams()
    {
        var groupId = await CreateGroupAsync("Ski trip");
        var alice = await AddParticipantAsync(groupId, "Alice");
        var bob = await AddParticipantAsync(groupId, "Bob");
        var carol = await AddParticipantAsync(groupId, "Carol");
        await AddExpenseAsync(groupId, new AddExpenseRequest(alice, 90m, "Dinner", Today, [alice, bob, carol]));
        await AddExpenseAsync(groupId, new AddExpenseRequest(bob, 30m, "Taxi", Today, [alice, bob, carol]));

        var direct = await _client.GetFromJsonAsync<SettlementResponse>(
            $"/groups/{groupId}/settlement?strategy=direct-payback");
        Assert.NotNull(direct);
        Assert.Equal(3, direct.Transfers.Count);

        var exact = await _client.GetFromJsonAsync<SettlementResponse>(
            $"/groups/{groupId}/settlement?strategy=exact-minimum");
        Assert.NotNull(exact);
        Assert.Equal(2, exact.Transfers.Count);

        var viaBanker = await _client.GetFromJsonAsync<SettlementResponse>(
            $"/groups/{groupId}/settlement?strategy=via-banker&hub={carol}");
        Assert.NotNull(viaBanker);
        Assert.All(viaBanker.Transfers, t =>
            Assert.True(t.FromParticipantId == carol || t.ToParticipantId == carol));

        var unknown = await _client.GetAsync($"/groups/{groupId}/settlement?strategy=zigzag");
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);

        var hubMissing = await _client.GetAsync($"/groups/{groupId}/settlement?strategy=via-banker");
        Assert.Equal(HttpStatusCode.BadRequest, hubMissing.StatusCode);

        var hubUnknown = await _client.GetAsync(
            $"/groups/{groupId}/settlement?strategy=via-banker&hub={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, hubUnknown.StatusCode);
    }

    [Fact]
    public async Task SelfPayment_ReturnsValidationProblemDetails()
    {
        var groupId = await CreateGroupAsync("Solo");
        var alice = await AddParticipantAsync(groupId, "Alice");

        var response = await _client.PostAsJsonAsync(
            $"/groups/{groupId}/payments",
            new RecordPaymentRequest(alice, alice, 10m, Today));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("ExpenseGroup.PaymentToSelf", problem.Errors.Keys);
    }

    [Fact]
    public async Task DeletedGroup_IsGone()
    {
        var groupId = await CreateGroupAsync("Ephemeral");

        var deleteResponse = await _client.DeleteAsync($"/groups/{groupId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/groups/{groupId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<Guid> CreateGroupAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/groups", new CreateGroupRequest(name, "EUR"));
        response.EnsureSuccessStatusCode();

        var group = await response.Content.ReadFromJsonAsync<GroupResponse>();
        return group!.Id;
    }

    private async Task<Guid> AddParticipantAsync(Guid groupId, string name)
    {
        var response = await _client.PostAsJsonAsync(
            $"/groups/{groupId}/participants", new AddParticipantRequest(name));
        response.EnsureSuccessStatusCode();

        var participant = await response.Content.ReadFromJsonAsync<ParticipantResponse>();
        return participant!.Id;
    }

    private async Task AddExpenseAsync(Guid groupId, AddExpenseRequest request)
    {
        var response = await _client.PostAsJsonAsync($"/groups/{groupId}/expenses", request);
        response.EnsureSuccessStatusCode();
    }
}
