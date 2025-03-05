module Nex.Core.Utils.Serialisation

open System.Collections
open System.IO
open Newtonsoft.Json
open Newtonsoft.Json.Bson


///<summary>
/// Writes a Bson file to disk
///</summary>
///<param name="path">the path to write the file to</param>
///<param name="data">the data to write</param>
let writeBson<'T> (path: string) (data: 'T) =
    use fs = new FileStream(path, FileMode.Create, FileAccess.Write)
    use writer = new BsonDataWriter(fs)
    let serialiser = JsonSerializer()
    serialiser.Serialize(writer, data)

///<summary>
/// Reads a Bson file from disk
///</summary>
///<param name="path">the path to read the file from</param>
///<returns>the deserialised data</returns>
let readBson<'T> (path: string) : 'T =
    use fs = new FileStream(path, FileMode.Open, FileAccess.Read)
    use reader = new BsonDataReader(fs)
    let serialiser = JsonSerializer()
    serialiser.Deserialize<'T>(reader)

let readBsonEnumerable<'T when 'T :> IEnumerable> (path: string) : 'T =
    use fs = new FileStream(path, FileMode.Open, FileAccess.Read)
    use reader = new BsonDataReader(fs)
    reader.ReadRootValueAsArray <- true
    let serialiser = JsonSerializer()
    serialiser.Deserialize<'T>(reader)
