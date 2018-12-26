namespace gitDotNet.Core

module Objects =
    open System.IO
    open System.Linq
    open System.Text.RegularExpressions
    open System.Text
    open System
    open System
    open System.Globalization

    let private objectIdPattern : Regex =
            new Regex("^(?<objectId>[a-f0-9]{40})\n{0,1}", RegexOptions.Compiled)

    let private headerRegex : Regex =
            new Regex("^(?<type>\\S+)\\ (?<length>\\d+)", RegexOptions.Compiled) 
    
    let private parentRegex : Regex =
            new Regex("^parent\\ (?<id>[a-f0-9]{40})", RegexOptions.Compiled)

    let private treeRegex : Regex =
            new Regex("^tree\\ (?<id>[a-f0-9]{40})", RegexOptions.Compiled)
    
    let private authorRegex : Regex =
            new Regex("^author\\ (?<name>.+)\\ (?<dateTime>\\d{10})\\ (?<timeZone>[+-]\\d{4})$", RegexOptions.Compiled)

    type Header = {objectType : string; contentLength : int}

    type Author = {name : string; dateTime : DateTime}

    type ObjectId = struct
            val Id : string
            new(id) = {Id = id}
            member this.FilePath : string =
                "objects\\" + this.Id.Substring(0, 2) + "\\" + this.Id.Substring(2, 38)
    end

    type GitObject(id : ObjectId, header : Header) =
        member __.Id = id
        member __.Header = header
    
    type CommitObject(id : ObjectId, header : Header, parentId : ObjectId option, treeId : ObjectId, author : Author, comment : string[]) =
        inherit GitObject(id, header)
        member __.ParentId = parentId
        member __.TreeId = treeId
        member __.Author = author
        member __.Comment = comment

    type BlobObject(id : ObjectId, header : Header, content : string[]) =
        inherit GitObject(id, header)
        member __.Content = content

    let readFromStreamTillZero(stream: Stream) = seq<byte> {
        let currentByte = ref 0
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

    let getIdByRegex(content : string[], regex : Regex) : ObjectId option =
        let element : string option =
                content
                |> Seq.filter (fun line -> regex.IsMatch(line)) 
                |> Seq.tryHead
        match element with
            | Some(element) -> Some(new ObjectId(regex.Match(element).Groups.["id"].Value))
            | None -> None

    let readStringContentFormStream(stream : Stream, contentLength : int) : string[] = 
        let buffer : byte array = Array.zeroCreate<byte> contentLength
        let _ = stream.Read(buffer, 0, contentLength)
        UTF8Encoding.UTF8.GetString(buffer).Split('\n')
    
    let getCommmitComment(content : string[]) : seq<string> =
        content
        |> Seq.skipWhile (fun line -> line <> "")

    let getDateTime(dateTimeInSeconds : int64, timeZone : string) : DateTime =
        let dateTime = DateTimeOffset.FromUnixTimeSeconds(dateTimeInSeconds).LocalDateTime
        let datetimeString = dateTime.ToString("dd-MM-yy HH:mm:ss ") + timeZone;
        DateTime.ParseExact(datetimeString, "dd-MM-yy HH:mm:ss zzz", CultureInfo.CreateSpecificCulture("en-US"))        

    let getAuthor(content : seq<string>) : Author =
        let authorLine : string = content
                                    |> Seq.find (fun line -> authorRegex.IsMatch(line))
        let authorMatch = authorRegex.Match(authorLine)
        let dateTime = getDateTime((authorMatch.Groups.["dateTime"].Value |> int64), authorMatch.Groups.["timeZone"].Value)
        {new Author with name = authorMatch.Groups.["name"].Value and dateTime = dateTime}
     
    let createCommitObject(commitObjectId : ObjectId, header : Header, stream : Stream) : GitObject =
        let content = readStringContentFormStream(stream, header.contentLength)
        let parentId = getIdByRegex(content, parentRegex)
        let treeId = getIdByRegex(content, treeRegex).Value
        let comment = getCommmitComment(content).ToArray()
        let auhtor = getAuthor(content)
        let commitObject = new CommitObject(commitObjectId, header, parentId, treeId, auhtor, comment)
        commitObject :> GitObject

    let createBlobObject(blobObjectId : ObjectId, header : Header, stream : Stream) : GitObject =
        let content = readStringContentFormStream(stream, header.contentLength)
        let blobObject = new BlobObject(blobObjectId, header, content)
        blobObject :> GitObject

    let createObject(objectId : ObjectId, objectStream : Stream) : GitObject =
            let header = extractHeader(objectStream)
            match header.objectType with
            | "commit" -> createCommitObject(objectId, header, objectStream)
            | "blob" -> createBlobObject(objectId, header, objectStream)
            | _ -> new GitObject(objectId, header)

    let readBlob(stream : Stream) : ObjectId =
        let _ = readFromStreamTillZero(stream).ToArray()
        let buffer : byte array = Array.zeroCreate<byte> 20
        let _ = stream.Read(buffer, 0, 20)
        let id = BitConverter.ToString(buffer).Replace("-", "").ToLower()
        new ObjectId(id)

        
