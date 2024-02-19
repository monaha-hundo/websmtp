namespace ManualTester;

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
        Content?.Dispose();
    }
}
