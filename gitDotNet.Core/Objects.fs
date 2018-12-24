namespace gitDotNet.Core

module Objects =
    open System
    open System.IO
    open System.Linq
    open System.Text.RegularExpressions
    open System.Text
    
    type Header = {objectType : string; contentLength : int}

    type GitObject = {header : Header; content : string[]}

    type ObjectId = struct
            val Id : string
            new(id) = {Id = id}
            member this.FilePath : string =
                "objects\\" + this.Id.Substring(0, 2) + "\\" + this.Id.Substring(2, 38)
    end

    let private ObjectIdPattern : Regex =
            new Regex("^(?<objectId>[a-f0-9]{40})\n{0,1}", RegexOptions.Compiled)

    let private HeaderRegex : Regex =
            new Regex("^(?<type>\\S+)\\ (?<length>\\d+)", RegexOptions.Compiled)      

    let CreateObjectId (idStream : Stream) : ObjectId =
            use reader = new StreamReader(idStream)            
            let objectIdFileContent = reader.ReadToEnd()
            let matched = ObjectIdPattern.Match(objectIdFileContent)
            new ObjectId(matched.Groups.["objectId"].Value);

    let ExtractHeader(stream : Stream) : Header = 
            let headerBytes = new ResizeArray<Byte>()
            let currentByte = ref 0;
            let moveNext() = 
                currentByte := stream.ReadByte()
                !currentByte > 0
            while moveNext() do
                headerBytes.Add(byte currentByte.Value)
            let headerString = Encoding.UTF8.GetString(headerBytes.ToArray())
            let headerMatch = HeaderRegex.Match(headerString)
            let objectType = headerMatch.Groups.["type"].Value;
            let length = headerMatch.Groups.["length"].Value |> int;
            {new Header with objectType = objectType and contentLength = length}

    let CreateObject (objectStream : Stream) : GitObject =
            let header = ExtractHeader(objectStream)
            let buffer : byte array = Array.zeroCreate<byte> header.contentLength
            objectStream.Read(buffer, 0, header.contentLength) |> ignore            
            {new GitObject with header = header and content = UTF8Encoding.UTF8.GetString(buffer).Split('\n') }
     
     
    let GetTree(gitObject : GitObject) : ObjectId =
            let treeLine : string = 
                gitObject.content 
                |> Array.filter (fun line -> line.StartsWith("tree")) 
                |> Array.head                
            let treeId = treeLine.Substring(5, 40);
            new ObjectId(treeId);
        
