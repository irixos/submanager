namespace SubManager.ApiClient;

// Workaround for an NSwag v14 file upload bug where generated clients reference
// FileParameter but do not emit the helper type.
//
// This class copies the FileParameter block from:
// NSwag/src/NSwag.CodeGeneration.CSharp/Templates/File.liquid
//
// It uses the GenerateNullableReferenceTypes branch, since this project has
// generateNullableReferenceTypes enabled.

[System.CodeDom.Compiler.GeneratedCode("NSwag", "manual workaround")]
public partial class FileParameter
{
    public FileParameter(System.IO.Stream data)
        : this(data, null, null)
    {
    }

    public FileParameter(System.IO.Stream data, string? fileName)
        : this(data, fileName, null)
    {
    }

    public FileParameter(System.IO.Stream data, string? fileName, string? contentType)
    {
        Data = data;
        FileName = fileName;
        ContentType = contentType;
    }

    public System.IO.Stream Data { get; private set; }

    public string? FileName { get; private set; }

    public string? ContentType { get; private set; }
}