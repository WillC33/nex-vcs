namespace Nex.Core.Types
// Define the configuration record with a public constructor.
[<CLIMutable>]
type NexConfig =
    { WorkingDirectory: string
      Language: string }
