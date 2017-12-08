using System;
using System.Runtime.CompilerServices;

namespace Asteria.Logging
{
    public interface ILogger
    {
        void Trace(string Message,
            [CallerMemberName] string memberName_DoNotUse = "",
            [CallerFilePath] string sourceFilePath_DoNotUse = "",
            [CallerLineNumber] int sourceLineNumber_DoNotUse = 0);
        void Trace(Exception ex, string Message,
            [CallerMemberName] string memberName_DoNotUse = "",
            [CallerFilePath] string sourceFilePath_DoNotUse = "",
            [CallerLineNumber] int sourceLineNumber_DoNotUse = 0);

        void Info(string Message,
            [CallerMemberName] string memberName_DoNotUse = "",
            [CallerFilePath] string sourceFilePath_DoNotUse = "",
            [CallerLineNumber] int sourceLineNumber_DoNotUse = 0);
        void Info(Exception ex, string Message,
            [CallerMemberName] string memberName_DoNotUse = "",
            [CallerFilePath] string sourceFilePath_DoNotUse = "",
            [CallerLineNumber] int sourceLineNumber_DoNotUse = 0);

        void Debug( string Message,
            [CallerMemberName] string memberName_DoNotUse = "",
            [CallerFilePath] string sourceFilePath_DoNotUse = "",
            [CallerLineNumber] int sourceLineNumber_DoNotUse = 0);
        void Debug(Exception ex, string Message,
            [CallerMemberName] string memberName_DoNotUse = "",
            [CallerFilePath] string sourceFilePath_DoNotUse = "",
            [CallerLineNumber] int sourceLineNumber_DoNotUse = 0);

        void Warning(string Message,
            [CallerMemberName] string memberName_DoNotUse = "",
            [CallerFilePath] string sourceFilePath_DoNotUse = "",
            [CallerLineNumber] int sourceLineNumber_DoNotUse = 0);
        void Warning(Exception ex, string Message,
            [CallerMemberName] string memberName_DoNotUse = "",
            [CallerFilePath] string sourceFilePath_DoNotUse = "",
            [CallerLineNumber] int sourceLineNumber_DoNotUse = 0);

        void Fatal(string Message,
            [CallerMemberName] string memberName_DoNotUse = "",
            [CallerFilePath] string sourceFilePath_DoNotUse = "",
            [CallerLineNumber] int sourceLineNumber_DoNotUse = 0);
        void Fatal(Exception ex, string Message,
            [CallerMemberName] string memberName_DoNotUse = "",
            [CallerFilePath] string sourceFilePath_DoNotUse = "",
            [CallerLineNumber] int sourceLineNumber_DoNotUse = 0);
    }

    /// <summary>
    /// Use this 'using static Asteria.Logging.Log;'
    /// </summary>
    public static class AsteriaLogger
    {
        public static ILogger Log { get; set; }
    }
}
