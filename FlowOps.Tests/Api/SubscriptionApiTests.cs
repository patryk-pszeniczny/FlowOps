using System.Net.Http.Json;
using FlowOps.Tests.Infrastructure.Web;
using FluentAssertions;
using Xunit;

namespace FlowOps.Tests.Api
{
    public class SubscriptionApiTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public SubscriptionApiTests(TestWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Creates_customer_and_subscription_and_reads_back()
        {
            var createCustomerResponse = await _client.PostAsJsonAsync("/api/customer", new
            {
                name = "Test Customer",
                taxId = "TAX-123",
                email = "customer@example.com"
            });

            createCustomerResponse.EnsureSuccessStatusCode();
            var createdCustomer = await createCustomerResponse.Content.ReadFromJsonAsync<CustomerResponse>();
            createdCustomer.Should().NotBeNull();

            var createSubscriptionResponse = await _client.PostAsJsonAsync("/api/subscriptions", new
            {
                customerId = createdCustomer!.CustomerId,
                planCode = "basic"
            });

            createSubscriptionResponse.EnsureSuccessStatusCode();
            var createdSubscription = await createSubscriptionResponse.Content.ReadFromJsonAsync<SubscriptionCreatedResponse>();
            createdSubscription.Should().NotBeNull();

            var getResponse = await _client.GetAsync($"/api/subscriptions/{createdSubscription!.SubscriptionId}");
            getResponse.EnsureSuccessStatusCode();

            var queried = await getResponse.Content.ReadFromJsonAsync<SubscriptionDetailsResponse>();
            queried.Should().NotBeNull();
            queried!.Id.Should().Be(createdSubscription.SubscriptionId);
            queried.CustomerId.Should().Be(createdCustomer.CustomerId);
        }

        private sealed record CustomerResponse(Guid CustomerId, string Name, string TaxId, string Email, DateTime CreatedAt);

        private sealed record SubscriptionCreatedResponse(Guid SubscriptionId, string Message);

        private sealed record SubscriptionDetailsResponse(Guid Id, Guid CustomerId, string PlanCode, string Status);
    }
}
