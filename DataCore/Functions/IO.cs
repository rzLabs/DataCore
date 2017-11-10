using System;
using System.IO;

namespace DataCore.Functions
{
    /// <summary>
    /// Provides the ability to load all text lines from a .lua
    /// </summary>
    public class IO
    {
        /// <summary>
        /// Loads the .lua in path and returns the contents
        /// </summary>
        /// <param name="path">Path of the .lua to be loaded</param>
        /// <returns>string contents of .lua</returns>
        public static string LoadConfig(string path)
        {
            if (File.Exists(path)) { return File.ReadAllText(path); }
            else { throw new FileNotFoundException("Specified file cannot be found.", path); }
        }

    }
}
