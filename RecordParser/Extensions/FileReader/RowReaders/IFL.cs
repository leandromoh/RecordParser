using System;
using System.Collections.Generic;

namespace RecordParser.Extensions.FileReader.RowReaders
{
    internal interface IFL : IDisposable
    {
        /// <summary>
        /// Reads the file and fills the internal buffer.
        /// </summary>
        /// <returns>
        /// The number of characters that have been read. 
        /// This method returns zero when no more characters are left to read.
        /// </returns>
        int FillBuffer();

        /// <summary>
        /// Read and returns the records inside the buffer.
        /// </summary>
        IEnumerable<ReadOnlyMemory<char>> ReadLines();
    }
}
