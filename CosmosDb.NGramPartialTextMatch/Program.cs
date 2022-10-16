using BenchmarkDotNet.Running;
using CosmosDb.NGramPartialTextMatch;

BenchmarkRunner.Run<PartialTextMatchBenchmark>();

Console.ReadLine();
