using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MagicEngine.Engine.Base.Debug;

// This class redirects console output to both the original console and a list of strings.
public class ConsoleInterceptor : TextWriter
{
    private readonly TextWriter _originalOut;
    public readonly List<string> LogMessages = new();

    public ConsoleInterceptor(TextWriter original)
    {
        _originalOut = original;
    }

    public override Encoding Encoding => _originalOut.Encoding;

    public override void WriteLine(string? message)
    {
        // Add to our internal log
        if (message != null)
        {
            LogMessages.Add(message);
        }
        
        // Write to the original console
        _originalOut.WriteLine(message);
    }
}