using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Bogus;
using Microsoft.Azure.Cosmos;

namespace CosmosDb.NGramPartialTextMatch;

[Config(typeof(Config))]
public class PartialTextMatchBenchmark
{
    private const string EndpointUrl = "https://localhost:8081/";
    private const string AuthorizationKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
    private const string DatabaseName = "PartialTextMatchDemo";
    private const string ContainerNamePlain = "UsersPlain";
    private const string ContainerNameNGram = "UsersNGram";
    private const int AmountToInsert = 100_000;

    private static readonly ConcurrentBag<double> _RUs = new();
    private Microsoft.Azure.Cosmos.Database? _cosmosDatabase;

    private CosmosDbPlainClient? _cosmosDbPlainClient;
    private CosmosDbPlainClient? _cosmosDbPlainTempClient;
    private readonly Randomizer _randomizerPlain = new(12345);

    private CosmosDbNGramClient? _cosmosDbNGramClient;
    private CosmosDbNGramClient? _cosmosDbNGramTempClient;
    private readonly Randomizer _randomizerNGram = new(12345);

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        var cosmosClient = new CosmosClient(
            EndpointUrl,
            AuthorizationKey,
            new CosmosClientOptions()
            {
                AllowBulkExecution = true
            });

        _cosmosDatabase = (await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName, 10_000)).Database;

        var users = GenerateUsers(AmountToInsert);

        _cosmosDbPlainClient = new(await CreateNewContainer(_cosmosDatabase!, ContainerNamePlain));
        await _cosmosDbPlainClient.AddUsers(users);

        _cosmosDbNGramClient = new(await CreateNewContainer(_cosmosDatabase!, ContainerNameNGram));
        await _cosmosDbNGramClient.AddUsers(users);
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        await _cosmosDatabase!.DeleteAsync();
    }

    [IterationSetup(Target = nameof(AddItem_Plain))]
    public void AddItem_Plain_InvocationSetup()
    {
        _cosmosDbPlainTempClient = new(CreateNewContainer(_cosmosDatabase!, ContainerNamePlain + "Temp", true).GetAwaiter().GetResult());
    }

    [Benchmark(OperationsPerInvoke = 100)]
    public async Task AddItem_Plain()
    {
        var result = await _cosmosDbPlainTempClient!.AddUsers(GenerateUsers(100));
        _RUs.Add(result);
    }

    [IterationCleanup(Target = nameof(AddItem_Plain))]
    public void AddItem_Plain_InvocationCleanup()
    {
        Environment.SetEnvironmentVariable(
            $"request-unit-charge-{nameof(AddItem_Plain).ToLower()}", _RUs.Average().ToString("0.00"),
            EnvironmentVariableTarget.User);

        _RUs.Clear();
    }

    [IterationSetup(Target = nameof(AddItem_NGram))]
    public void AddItem_NGram_InvocationSetup()
    {
        _cosmosDbNGramTempClient = new(CreateNewContainer(_cosmosDatabase!, ContainerNameNGram + "Temp", true).GetAwaiter().GetResult());
    }

    [Benchmark(OperationsPerInvoke = 100)]
    public async Task AddItem_NGram()
    {
        var result = await _cosmosDbNGramTempClient!.AddUsers(GenerateUsers(100));
        _RUs.Add(result);
    }

    [IterationCleanup(Target = nameof(AddItem_NGram))]
    public void AddItem_NGram_InvocationCleanup()
    {
        Environment.SetEnvironmentVariable(
            $"request-unit-charge-{nameof(AddItem_NGram).ToLower()}", _RUs.Average().ToString("0.00"),
            EnvironmentVariableTarget.User);

        _RUs.Clear();
    }

    [Benchmark(OperationsPerInvoke = 100)]
    public async Task QueryItem_Plain()
    {
        for (int i = 0; i < 100; ++i)
        {
            var result = await _cosmosDbPlainClient!.QueryUser(_randomizerPlain.String(1, 5, 'a', 'z'));
            _RUs.Add(result.RUs);
        }
    }

    [IterationCleanup(Target = nameof(QueryItem_Plain))]
    public void QueryItem_Plain_InvocationCleanup()
    {
        Environment.SetEnvironmentVariable(
            $"request-unit-charge-{nameof(QueryItem_Plain).ToLower()}", _RUs.Average().ToString("0.00"),
            EnvironmentVariableTarget.User);

        _RUs.Clear();
    }

    [Benchmark(OperationsPerInvoke = 100)]
    public async Task QueryItem_NGram()
    {
        for (int i = 0; i < 100; ++i)
        {
            var result = await _cosmosDbNGramClient!.QueryUser(_randomizerNGram.String(1, 5, 'a', 'z'));
            _RUs.Add(result.RUs);
        }
    }

    [IterationCleanup(Target = nameof(QueryItem_NGram))]
    public void QueryItem_NGram_InvocationCleanup()
    {
        Environment.SetEnvironmentVariable(
            $"request-unit-charge-{nameof(QueryItem_NGram).ToLower()}", _RUs.Average().ToString("0.00"),
            EnvironmentVariableTarget.User);

        _RUs.Clear();
    }

    private static async Task<Container> CreateNewContainer(
        Microsoft.Azure.Cosmos.Database database,
        string containerName,
        bool deleteIfExists = false)
    {
        try
        {
            if (deleteIfExists)
                await database.GetContainer(containerName).DeleteContainerAsync();
        }
        catch
        {
            // Ignore if doesn't exist
        }

        return (await database
            .DefineContainer(containerName, "/pk")
            .WithIndexingPolicy()
                .WithIndexingMode(IndexingMode.Consistent)
                .WithIncludedPaths()
                    .Path("/*")
                    .Attach()
                .Attach()
            .CreateIfNotExistsAsync(1_000))
            .Container;
    }

    private static User[] GenerateUsers(int amountToInsert)
        => new Faker<User>()
            .StrictMode(true)
            .UseSeed(12345)
            .RuleFor(u => u.Id, f => f.Random.Uuid())
            .RuleFor(u => u.FirstName, f => f.Person.FirstName.ToLower())
            .RuleFor(u => u.LastName, f => f.Person.LastName.ToLower())
            .RuleFor(u => u.Email, f => f.Internet.Email().ToLower())
            .RuleFor(u => u.Bio, f => f.Lorem.Sentences(3, " "))
            .RuleFor(u => u.NGrams, Array.Empty<string>())
            .RuleFor(u => u.Pk, "PK")
            .Generate(amountToInsert)
            .ToArray();

    private sealed class Config : ManualConfig
    {
        public Config() => AddColumn(new RUsColumn());
    }
}
