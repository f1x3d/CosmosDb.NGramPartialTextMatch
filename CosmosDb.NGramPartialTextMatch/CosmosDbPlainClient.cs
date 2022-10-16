using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;

namespace CosmosDb.NGramPartialTextMatch;

public class CosmosDbPlainClient
{
    private readonly Container _cosmosContainer;

    public CosmosDbPlainClient(Container cosmosContainer)
    {
        _cosmosContainer = cosmosContainer;
    }

    public async Task<double> AddUsers(IEnumerable<User> users)
    {
        var bag = new ConcurrentBag<double>();

        await Parallel.ForEachAsync(
            users,
            new ParallelOptions { MaxDegreeOfParallelism = 4 },
            async (user, cancellationToken) => {
                var response = await _cosmosContainer.CreateItemAsync(
                    user, new PartitionKey("PK"), cancellationToken: cancellationToken);

                bag.Add(response.RequestCharge);
            });

        return bag.Average();
    }

    public async Task<(User? User, double RUs)> QueryUser(string query)
    {
        var cosmosQuery = new QueryDefinition(
            "SELECT * " +
            "FROM u " +
            "WHERE CONTAINS(u.FirstName, @query) " +
            "OR CONTAINS(u.LastName, @query) " +
            "OR CONTAINS(u.Email, @query) " +
            "OR CONTAINS(u.Bio, @query) " +
            "OFFSET 0 LIMIT 1")
            .WithParameter("@query", query);

        using var queryIterator = _cosmosContainer.GetItemQueryIterator<User>(cosmosQuery, requestOptions: new()
        {
            PartitionKey = new("PK")
        });

        while (queryIterator.HasMoreResults)
        {
            var response = await queryIterator.ReadNextAsync();

            if (response.Count > 0)
                return (response.First(), response.RequestCharge);
            else
                return (null, response.RequestCharge);
        }

        return (null, 0);
    }
}
