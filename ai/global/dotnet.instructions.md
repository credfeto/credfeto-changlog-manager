# .NET Instructions

[Back to Global Instructions Index](index.md)

These rules apply to all .NET solutions derived from this template.

## Project and Solution Structure

- All projects must be added to the solution file (`.slnx` or `.sln`).
- All projects must pass the latest release of the `FunFair.BuildCheck` dotnet tool before committing.

## Test Assembly Naming

Test projects must follow a consistent naming convention relative to the assembly under test:

| Test type | Assembly name pattern |
|-----------|----------------------|
| Unit tests | `<AssemblyName>.Tests` |
| Integration tests | `<AssemblyName>.Integration.Tests` |
| Benchmarks | `<AssemblyName>.Benchmark.Tests` |

For example, for an assembly `This.Test.Example`:
- Unit tests → `This.Test.Example.Tests`
- Integration tests → `This.Test.Example.Integration.Tests`
- Benchmarks → `This.Test.Example.Benchmark.Tests`

## Test Dependencies

- All test projects must reference the latest release version of `FunFair.Test.Common`.
- All test projects must import the latest release version of the `FunFair.Test.Source.Generator` source generator package.
- Test fixture classes must derive from `FunFair.Test.Common.TestBase`.
