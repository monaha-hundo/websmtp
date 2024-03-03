namespace Tests;

public class FakeFile : IDisposable
{
    public FakeFile(string filename, Stream content)
    {
        FileName = filename;
        Content = content;
    }

    public string FileName { get; set; }
    public Stream Content { get; set; }

    public void Dispose()
    {
        if(Content != null)
        {
            Content.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
