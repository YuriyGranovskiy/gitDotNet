open System.IO
open gitDotNet.Core
open gitDotNet.Core.Objects
open ICSharpCode.SharpZipLib.Zip.Compression.Streams

let getHeadObjectId(headPath : string) : ObjectId =
    use stream = File.Open(headPath, FileMode.Open)
    createObjectId(stream)

let getObject(objectPath: string) : GitObject =
    use fileStream = File.Open(objectPath, FileMode.Open, FileAccess.Read)
    use inflaterStream = new InflaterInputStream(fileStream)
    createObject(inflaterStream);

let getBlobObjectId(objectPath : string) : ObjectId =
    use fileStream = File.Open(objectPath, FileMode.Open, FileAccess.Read)
    use inflaterStream = new InflaterInputStream(fileStream)
    let _ = extractHeader(inflaterStream)
    readBlob(inflaterStream)

[<EntryPoint>]
let main argv =
    let repoPath = "D:\\Source\\git-merge"
    let gitPath = Path.Combine(repoPath, ".git")
    let headPath = Structure.getHead(gitPath)
    let headObjectId = getHeadObjectId(Path.Combine(gitPath, headPath))
    let commitObject = getObject(Path.Combine(gitPath, headObjectId.FilePath)) :?> CommitObject
    let firstFileId = getBlobObjectId(Path.Combine(gitPath, commitObject.TreeId.FilePath))
    let myObject = getObject(Path.Combine(gitPath, firstFileId.FilePath))
    0 // return an integer exit code