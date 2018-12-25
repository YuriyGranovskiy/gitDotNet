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

let getBlobObject(objectPath : string) : ObjectId =
    use fileStream = File.Open(objectPath, FileMode.Open, FileAccess.Read)
    use inflaterStream = new InflaterInputStream(fileStream)
    extractHeader(inflaterStream) |> ignore
    readBlob(inflaterStream)

[<EntryPoint>]
let main argv =
    let repoPath = "D:\\Source\\git-merge"
    let gitPath = Path.Combine(repoPath, ".git")
    let headPath = Structure.getHead(gitPath)
    let headObjectId = getHeadObjectId(Path.Combine(gitPath, headPath))
    let gitObject = getObject(Path.Combine(gitPath, headObjectId.FilePath))
    let treeId = getTree(gitObject)
    let firstFileId = getBlobObject(Path.Combine(gitPath, treeId.FilePath))
    let fileObject = getObject(Path.Combine(gitPath, firstFileId.FilePath))
    0 // return an integer exit code