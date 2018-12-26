open System.IO
open gitDotNet.Core
open gitDotNet.Core.Objects
open ICSharpCode.SharpZipLib.Zip.Compression.Streams
open System.Globalization

let getHeadObjectId(headPath : string) : ObjectId =
    use stream = File.Open(headPath, FileMode.Open)
    createObjectId(stream)

let getObject(objectId : ObjectId, gitPath : string) : GitObject =
    let objectPath : string = Path.Combine(gitPath, objectId.FilePath)
    use fileStream = File.Open(objectPath, FileMode.Open, FileAccess.Read)
    use inflaterStream = new InflaterInputStream(fileStream)
    createObject(objectId, inflaterStream)

let getBlobObjectId(objectPath : string) : ObjectId =
    use fileStream = File.Open(objectPath, FileMode.Open, FileAccess.Read)
    use inflaterStream = new InflaterInputStream(fileStream)
    let _ = extractHeader(inflaterStream)
    readBlob(inflaterStream)

let printOutCommit(commit : CommitObject) =
    printfn "commit %s" commit.Id.Id
    printfn "Author:\t%s" commit.Author.name
    printfn "Date:\t%s" (commit.Author.dateTime.ToString("ddd MMM dd HH:mm:ss yyyy K", CultureInfo.CreateSpecificCulture("en-US")))
    commit.Comment
    |> Seq.iter (fun line -> printfn "\t%s" line)

[<EntryPoint>]
let main argv =
    let repoPath = "D:\\Source\\git-merge"
    let gitPath = Path.Combine(repoPath, ".git")
    let headPath = Structure.getHead(gitPath)
    let headObjectId = getHeadObjectId(Path.Combine(gitPath, headPath))
    let commitObject = getObject(headObjectId, gitPath) :?> CommitObject
    printOutCommit(commitObject)
    let currentObject = ref commitObject
    while currentObject.Value.ParentId.IsSome do        
        let objectId = (Option.get currentObject.Value.ParentId)
        currentObject := getObject(objectId, gitPath) :?> CommitObject
        printOutCommit(currentObject.Value)
    let firstFileId = getBlobObjectId(Path.Combine(gitPath, commitObject.TreeId.FilePath))
    let myObject = getObject(firstFileId, gitPath)
    0 // return an integer exit code