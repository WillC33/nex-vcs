namespace Nex.Tests

open Xunit

/// <summary>
/// An attribute to define sequential test executions for those that have shared repository resources
/// </summary>
[<CollectionDefinition("Sequential", DisableParallelization = true)>]
type SequentialCollection() = class end
