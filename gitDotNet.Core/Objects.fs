namespace gitDotNet.Core

module Objects =
    open System.IO
    open System.Linq
    open System.Text.RegularExpressions
    open System.Text
    open System
    
    type Header = {objectType : string; contentLength : int}

    type GitObject = {header : Header; content : string[]}

    type ObjectId = struct
            val Id : string
            new(id) = {Id = id}
            member this.FilePath : string =
                "objects\\" + this.Id.Substring(0, 2) + "\\" + this.Id.Substring(2, 38)
    end

    let private objectIdPattern : Regex =
            new Regex("^(?<objectId>[a-f0-9]{40})\n{0,1}", RegexOptions.Compiled)

    let private headerRegex : Regex =
            new Regex("^(?<type>\\S+)\\ (?<length>\\d+)", RegexOptions.Compiled)   
            
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

    let createObject(objectStream : Stream) : GitObject =
            let header = extractHeader(objectStream)
            let buffer : byte array = Array.zeroCreate<byte> header.contentLength
            objectStream.Read(buffer, 0, header.contentLength) |> ignore
            {new GitObject with header = header and content = UTF8Encoding.UTF8.GetString(buffer).Split('\n') }

    let getTree(gitObject : GitObject) : ObjectId =
            let treeLine : string = 
                gitObject.content 
                |> Array.filter (fun line -> line.StartsWith("tree")) 
                |> Array.head                
            let treeId = treeLine.Substring(5, 40);
            new ObjectId(treeId);

    let readBlob(stream : Stream) : ObjectId =
        readFromStreamTillZero(stream).ToArray() |> ignore
        let buffer : byte array = Array.zeroCreate<byte> 20
        stream.Read(buffer, 0, 20) |> ignore
        let id = BitConverter.ToString(buffer).Replace("-", "").ToLower()
        new ObjectId(id)

        
