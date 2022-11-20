using Agora.Logging;
using System.ComponentModel;

namespace Agora.Forms
{
    public class FileLoggerTarget : ILoggerTarget
    {
        string Filename;
        StreamWriter FS;
        string Dashes = "--------------------------------------------------------------------------------";
        string Spaces = "                                                                                ";

        public FileLoggerTarget(string directory, bool bAppend = false)
        {
            directory = directory.Replace("\\", "/");
            if (!directory.EndsWith("/")) 
                directory += "/";
            Filename = directory + System.IO.Path.GetFileNameWithoutExtension(System.AppDomain.CurrentDomain.FriendlyName) + ".log";
            FS = new(Filename, bAppend);
            FS.WriteLine("Hello");
            FS.Flush();
        }

        char[] Level = { 'T', 'D', 'I', 'W', 'E', 'F', 'O' };

        public void Write(long ticks, LogLevel level, string message, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            FS.WriteLine($"{Level[(int)level]}({ticks}) - {message}");
            FS.Flush();
        }

        public void WriteHeading(long ticks, string message)
        {
            FS.WriteLine(Dashes);
            FS.WriteLine($"{Spaces.Substring(0, 80-message.Length)}{message}");
            FS.WriteLine(Dashes);
            FS.Flush();
        }

        public void WriteException(long ticks, LogLevel level, Exception ex, string message, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            FS.WriteLine($"{Level[(int)level]}({ticks}) - {message}");

            FS.WriteLine("---");
            while(ex != null) 
            {
                FS.WriteLine("\t" + ex.GetType().Name);
                if (string.IsNullOrEmpty(ex.Message))
                    FS.WriteLine("\t" + ex.Message);
            }
            FS.WriteLine("---");
            FS.Flush();
        }
    }
}
