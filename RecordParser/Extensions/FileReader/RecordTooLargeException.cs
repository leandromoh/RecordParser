using System;

namespace RecordParser.Extensions
{
    /// <summary>
    /// The exception that is thrown when a single record is too large to fit in the read buffer.
    /// </summary>
    /// <remarks>
    /// A possible cause is incorrectly formatted file. 
    /// At the moment, the library does not support to customize the buffer size.
    /// </remarks>
    public class RecordTooLargeException : Exception
    {
        public RecordTooLargeException(string Message) : base(Message) { }
    }
}
