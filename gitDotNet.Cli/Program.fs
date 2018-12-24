open System
open System.IO
open gitDotNet.Core
open gitDotNet.Core.Objects
open ICSharpCode.SharpZipLib.Zip.Compression.Streams


let getHeadObjectId(headPath : string) : ObjectId =
    use stream = File.Open(headPath, FileMode.Open)
    CreateObjectId(stream)

let GetObject(objectPath: string) : GitObject =
    use fileStream = File.Open(objectPath, FileMode.Open, FileAccess.Read)
    use inflaterStream = new InflaterInputStream(fileStream)
    CreateObject(inflaterStream);

let GetObject1(objectPath : string) : ObjectId =
    use fileStream = File.Open(objectPath, FileMode.Open, FileAccess.Read)
    use inflaterStream = new InflaterInputStream(fileStream)
    let header = ExtractHeader(inflaterStream)
    let buffer : byte array = Array.zeroCreate<byte> header.contentLength
    inflaterStream.Read(buffer, 0, header.contentLength) |> ignore
    let zeroIndex = 16 // hardcoded
    let idBuffer = Array.sub<byte> buffer (zeroIndex + 1) 20
    let id = BitConverter.ToString(idBuffer).Replace("-", "").ToLower()
    new ObjectId(id)

[<EntryPoint>]
let main argv =
    let repoPath = "D:\\Source\\git-merge"
    let gitPath = Path.Combine(repoPath, ".git")
    let headPath = Structure.GetHead(gitPath)
    let headObjectId = getHeadObjectId(Path.Combine(gitPath, headPath))
    let gitObject = GetObject(Path.Combine(gitPath, headObjectId.FilePath))
    let treeId = GetTree(gitObject)
    let firstFileId = GetObject1(Path.Combine(gitPath, treeId.FilePath))
    let fileObject = GetObject(Path.Combine(gitPath, firstFileId.FilePath))
    0 // return an integer exit code