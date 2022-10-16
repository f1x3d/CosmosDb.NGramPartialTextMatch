using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace CosmosDb.NGramPartialTextMatch;

public class RUsColumn : IColumn
{
    public string Id => nameof(RUsColumn);

    public string ColumnName => "Avg. RUs";

    public string Legend => "Average RUs charge per query";

    public UnitType UnitType => UnitType.Size;

    public bool AlwaysShow => true;

    public ColumnCategory Category => ColumnCategory.Metric;

    public int PriorityInCategory => 0;

    public bool IsNumeric => true;

    public bool IsAvailable(Summary summary) => true;

    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        => GetValue(summary, benchmarkCase, SummaryStyle.Default);

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        var benchmarkName = benchmarkCase.Descriptor.WorkloadMethod.Name.ToLower();
        var envVarName = $"request-unit-charge-{benchmarkName}";

        return Environment.GetEnvironmentVariable(envVarName, EnvironmentVariableTarget.User)
            ?? "n/a";
    }

    public override string ToString() => ColumnName;
}
