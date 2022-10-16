# CosmosDb.NGramPartialTextMatch

This is a proof of concept of using [n&#x2011;grams](https://en.wikipedia.org/wiki/N-gram) for partial string match in [Azure Cosmos DB](https://azure.microsoft.com/en-us/products/cosmos-db/).

## Motivation

Since Cosmos DB does not support efficient partial text match out of the box, the idea was to test if n&#x2011;grams can be used for this purpose in a read-heavy application instead of having to connect to another external service like [Azure Cognitive Search](https://azure.microsoft.com/en-us/products/search/).

## Benchmarks

|          Method |         Mean | Avg. RU charge |
|---------------- |-------------:|---------------:|
|   AddItem_Plain |  24,723.0 us |           7.81 |
|   AddItem_NGram |  98,705.9 us |         125.14 |
| QueryItem_Plain | 133,065.5 us |         207.74 |
| QueryItem_NGram |     938.9 us |           2.99 |

| Container  | Document Count | Container Size | Avg. Document Size |
|----------- |---------------:|---------------:|-------------------:|
| UsersPlain |        100,000 |    120,925 KiB |           0.95 KiB |
| UsersNGram |        100,000 |  1,273,532 KiB |          12.47 KiB |

