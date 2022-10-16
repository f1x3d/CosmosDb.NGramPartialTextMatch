using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;

namespace CosmosDb.NGramPartialTextMatch;

public class CosmosDbNGramClient
{
    private readonly Container _cosmosContainer;

    public CosmosDbNGramClient(Container cosmosContainer)
    {
        _cosmosContainer = cosmosContainer;
    }

    public async Task<double> AddUsers(IEnumerable<User> users)
    {
        var bag = new ConcurrentBag<double>();

        await Parallel.ForEachAsync(
            users.Select(PopulateNGrams),
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
            "WHERE ARRAY_CONTAINS(u.NGrams, @query) " +
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

    private static User PopulateNGrams(User user)
    {
        user.NGrams = GenerateNGrams(user.FirstName)
            .Concat(GenerateNGrams(user.LastName))
            .Concat(GenerateNGrams(user.Email))
            .Concat(GenerateNGrams(user.Bio))
            .ToList();

        return user;
    }

    private static IEnumerable<string> GenerateNGrams(string input)
    {
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var nGrams = new HashSet<string>();

        foreach (var word in words)
            for (int start = 0; start < word.Length; ++start)
                for (int length = 1; start + length <= word.Length; ++length)
                    nGrams.Add(word.Substring(start, length));

        return nGrams;
    }
}
