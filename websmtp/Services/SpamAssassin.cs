using System.Diagnostics;

namespace websmtp;

public class SpamAssassin
{
    public async Task<string> Run(string message)
    {
        var spamAss = Start();

        try
        {
            spamAss.StandardInput.Write(message);
            spamAss.StandardInput.Close();
            await spamAss.WaitForExitAsync(); 
            var processedMsg = await spamAss.StandardOutput.ReadToEndAsync()
                ?? throw new Exception("Could not read spamAssassin's process output.");
            spamAss.Close();
            return processedMsg;
        }
        finally
        {
            spamAss?.Dispose();
        }

        throw new Exception("Could not run spam assassin.");
    }

    private static Process Start()
    {
        var process = new Process
        {
            StartInfo =
        {
            FileName = "bash",
            ArgumentList = { "-c", "--", "spamassassin" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false
        }
        };
        process.Start();
        return process;
    }
}
