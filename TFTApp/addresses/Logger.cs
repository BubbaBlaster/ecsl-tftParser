using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using System.IO;

namespace CDC.Logging
{
    /// <summary>
    /// Wrapper class for logging framework or libraries.
    /// 
    /// Current implementation uses NLog. Please see NLog documentation for proper configuration and use.
    /// 
    /// </summary>
    public class Logger : ILogger
    {
        StreamWriter logFile = new StreamWriter("system.log");

        enum LogLevel
        {
            Debug,
            Trace,
            Info,
            Warn,
            Fatal
        };

        private void Write(LogLevel level, string Message,
            string memberName_DoNotUse = "",
            string sourceFilePath_DoNotUse = "",
             int sourceLineNumber_DoNotUse = 0)
        {
            if (level != LogLevel.Info && level != LogLevel.Trace)
                logFile.WriteLine("[" + level.ToString() + "] " + Message + " (" + sourceFilePath_DoNotUse + ":" + sourceLineNumber_DoNotUse + " - " + memberName_DoNotUse + ")");
            else
                logFile.WriteLine("[" + level.ToString() + "] " + Message);
            logFile.Flush();
        }

        private void WriteHeading(string Message)
        {
            logFile.WriteLine("--------------------------------------------------------------------------------");
            StringBuilder str = new StringBuilder(Message.Length < 80 ? 80 : Message.Length);
            for (int i = 0; i < 79 - Message.Length; i++)
                str.Append(" ");
            str.Append(Message);
            logFile.WriteLine(Message);
            logFile.WriteLine("--------------------------------------------------------------------------------");
            logFile.Flush();
        }

        private void WriteException(LogLevel level, Exception ex, string Message,
            string memberName_DoNotUse = "",
            string sourceFilePath_DoNotUse = "",
            int sourceLineNumber_DoNotUse = 0)
        {
            if (level != LogLevel.Info && level != LogLevel.Trace)
                logFile.WriteLine("[" + level.ToString() + "] " + Message + " (" + sourceFilePath_DoNotUse + ":" + sourceLineNumber_DoNotUse + " - " + memberName_DoNotUse + ")");
            else
                logFile.WriteLine("[" + level.ToString() + "] " + Message);

            _WriteException(level, ex);
            logFile.Flush();
        }

        private void _WriteException(LogLevel level, Exception ex)
        {
            if (ex == null) return;
            logFile.WriteLine("[" + level.ToString() + "] " + ex.Message);
            logFile.WriteLine("[" + level.ToString() + "] " + ex.StackTrace);
            _WriteException(level, ex.InnerException);
        }

        public void Trace(string Message, [CallerMemberName] string memberName_DoNotUse = "", [CallerFilePath] string sourceFilePath_DoNotUse = "", [CallerLineNumber] int sourceLineNumber_DoNotUse = 0)
        {
            Write(LogLevel.Trace, Message, memberName_DoNotUse, sourceFilePath_DoNotUse, sourceLineNumber_DoNotUse);
        }

        public void Trace(Exception ex, string Message, [CallerMemberName] string memberName_DoNotUse = "", [CallerFilePath] string sourceFilePath_DoNotUse = "", [CallerLineNumber] int sourceLineNumber_DoNotUse = 0)
        {
            WriteException(LogLevel.Trace, ex, Message, memberName_DoNotUse, sourceFilePath_DoNotUse, sourceLineNumber_DoNotUse);
        }

        public void Info(string Message, [CallerMemberName] string memberName_DoNotUse = "", [CallerFilePath] string sourceFilePath_DoNotUse = "", [CallerLineNumber] int sourceLineNumber_DoNotUse = 0)
        {
            Write(LogLevel.Info, Message, memberName_DoNotUse, sourceFilePath_DoNotUse, sourceLineNumber_DoNotUse);
        }

        public void Info(Exception ex, string Message, [CallerMemberName] string memberName_DoNotUse = "", [CallerFilePath] string sourceFilePath_DoNotUse = "", [CallerLineNumber] int sourceLineNumber_DoNotUse = 0)
        {
            WriteException(LogLevel.Info, ex, Message, memberName_DoNotUse, sourceFilePath_DoNotUse, sourceLineNumber_DoNotUse);
        }

        public void Debug(string Message, [CallerMemberName] string memberName_DoNotUse = "", [CallerFilePath] string sourceFilePath_DoNotUse = "", [CallerLineNumber] int sourceLineNumber_DoNotUse = 0)
        {
            Write(LogLevel.Debug, Message, memberName_DoNotUse, sourceFilePath_DoNotUse, sourceLineNumber_DoNotUse);
        }

        public void Debug(Exception ex, string Message, [CallerMemberName] string memberName_DoNotUse = "", [CallerFilePath] string sourceFilePath_DoNotUse = "", [CallerLineNumber] int sourceLineNumber_DoNotUse = 0)
        {
            WriteException(LogLevel.Debug, ex, Message, memberName_DoNotUse, sourceFilePath_DoNotUse, sourceLineNumber_DoNotUse);
        }

        public void Warning(string Message, [CallerMemberName] string memberName_DoNotUse = "", [CallerFilePath] string sourceFilePath_DoNotUse = "", [CallerLineNumber] int sourceLineNumber_DoNotUse = 0)
        {
            Write(LogLevel.Warn, Message, memberName_DoNotUse, sourceFilePath_DoNotUse, sourceLineNumber_DoNotUse);
        }

        public void Warning(Exception ex, string Message, [CallerMemberName] string memberName_DoNotUse = "", [CallerFilePath] string sourceFilePath_DoNotUse = "", [CallerLineNumber] int sourceLineNumber_DoNotUse = 0)
        {
            WriteException(LogLevel.Warn, ex, Message, memberName_DoNotUse, sourceFilePath_DoNotUse, sourceLineNumber_DoNotUse);
        }

        public void Fatal(string Message, [CallerMemberName] string memberName_DoNotUse = "", [CallerFilePath] string sourceFilePath_DoNotUse = "", [CallerLineNumber] int sourceLineNumber_DoNotUse = 0)
        {
            Write(LogLevel.Fatal, Message, memberName_DoNotUse, sourceFilePath_DoNotUse, sourceLineNumber_DoNotUse);
        }

        public void Fatal(Exception ex, string Message, [CallerMemberName] string memberName_DoNotUse = "", [CallerFilePath] string sourceFilePath_DoNotUse = "", [CallerLineNumber] int sourceLineNumber_DoNotUse = 0)
        {
            WriteException(LogLevel.Fatal, ex, Message, memberName_DoNotUse, sourceFilePath_DoNotUse, sourceLineNumber_DoNotUse);
        }
    }
}

