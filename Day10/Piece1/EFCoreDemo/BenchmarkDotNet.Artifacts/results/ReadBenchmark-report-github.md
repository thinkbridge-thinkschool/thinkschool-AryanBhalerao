```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8457/25H2/2025Update/HudsonValley2)
Intel Core i5-9300H CPU 2.40GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]     : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  Job-AMZPBM : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3

IterationCount=5  WarmupCount=2  

```
| Method          | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0      | Gen1     | Gen2     | Allocated | Alloc Ratio |
|---------------- |----------:|---------:|----------:|------:|--------:|----------:|---------:|---------:|----------:|------------:|
| WithTracking    | 22.066 ms | 2.090 ms | 0.5428 ms |  1.00 |    0.03 | 1531.2500 | 937.5000 | 437.5000 |   8.47 MB |        1.00 |
| WithoutTracking |  9.663 ms | 1.203 ms | 0.3125 ms |  0.44 |    0.02 |  531.2500 | 218.7500 |  78.1250 |   3.09 MB |        0.36 |
