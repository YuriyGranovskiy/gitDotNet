namespace gitDotNet.Core

module Objects =
    open System.IO
    open System.Linq
    open System.Text.RegularExpressions
    open System.Text
    open System

    let private objectIdPattern : Regex =
            new Regex("^(?<objectId>[a-f0-9]{40})\n{0,1}", RegexOptions.Compiled)

    let private headerRegex : Regex =
            new Regex("^(?<type>\\S+)\\ (?<length>\\d+)", RegexOptions.Compiled) 
    
    let private parentRegex : Regex =
            new Regex("^parent\\ (?<id>[a-f0-9]{40})", RegexOptions.Compiled)

    let private treeRegex : Regex =
            new Regex("^tree\\ (?<id>[a-f0-9]{40})", RegexOptions.Compiled)
    
    type Header = {objectType : string; contentLength : int}

    type ObjectId = struct
            val Id : string
            new(id) = {Id = id}
            member this.FilePath : string =
                "objects\\" + this.Id.Substring(0, 2) + "\\" + this.Id.Substring(2, 38)
    end

    type GitObject(header : Header) =
        member __.Header = header
    
    type CommitObject(header : Header, parentId : ObjectId, treeId : ObjectId) =
        inherit GitObject(header)
        member __.ParentId = parentId
        member __.TreeId = treeId

    type BlobObject(header : Header, content : string[]) =
        inherit GitObject(header)
        member __.Content = content

    let readFromStreamTillZero(stream: Stream) = seq<byte> {
        let currentByte = ref 0;
        let moveNext() = 
            currentByte := stream.ReadByte()
            !currentByte > 0
        while moveNext() do
            yield byte currentByte.Value
    }

    let createObjectId(idStream : Stream) : ObjectId =
            use reader = new StreamReader(idStream)            
            let objectIdFileContent = reader.ReadToEnd()
            let matched = objectIdPattern.Match(objectIdFileContent)
            new ObjectId(matched.Groups.["objectId"].Value);

    let extractHeader(stream : Stream) : Header =
            let headerString = Encoding.UTF8.GetString(readFromStreamTillZero(stream).ToArray())
            let headerMatch = headerRegex.Match(headerString)
            let objectType = headerMatch.Groups.["type"].Value;
            let length = headerMatch.Groups.["length"].Value |> int;
            {new Header with objectType = objectType and contentLength = length}

    let getIdByRegex(content : string[], regex : Regex) : ObjectId =
        let element : string =
                content
                |> Array.filter (fun line -> regex.IsMatch(line)) 
                |> Array.head
        new ObjectId(regex.Match(element).Groups.["id"].Value)
     
    let createCommitObject(header : Header, stream : Stream) : GitObject =
        let buffer : byte array = Array.zeroCreate<byte> header.contentLength
        stream.Read(buffer, 0, header.contentLength) |> ignore
        let content = UTF8Encoding.UTF8.GetString(buffer).Split('\n')
        let parentId = getIdByRegex(content, parentRegex)
        let treeId = getIdByRegex(content, treeRegex)
        let commitObject = new CommitObject(header, parentId, treeId)
        commitObject :> GitObject

    let createBlobObject(header : Header, stream : Stream) : GitObject =
        let buffer : byte array = Array.zeroCreate<byte> header.contentLength
        stream.Read(buffer, 0, header.contentLength) |> ignore
        let content = UTF8Encoding.UTF8.GetString(buffer).Split('\n')
        let blobObject = new BlobObject(header, content)
        blobObject :> GitObject

    let createObject(objectStream : Stream) : GitObject =
            let header = extractHeader(objectStream)
            match header.objectType with
            | "commit" -> createCommitObject(header, objectStream)
            | "blob" -> createBlobObject(header, objectStream)
            | _ -> new GitObject(header)

    let readBlob(stream : Stream) : ObjectId =
        readFromStreamTillZero(stream).ToArray() |> ignore
        let buffer : byte array = Array.zeroCreate<byte> 20
        stream.Read(buffer, 0, 20) |> ignore
        let id = BitConverter.ToString(buffer).Replace("-", "").ToLower()
        new ObjectId(id)

        
