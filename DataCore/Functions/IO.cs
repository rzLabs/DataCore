using System;
using System.IO;

namespace DataCore.Functions
{
    public class IO
    {
        public static string LoadConfig(string path)
        {
            if (File.Exists(path)) { return File.ReadAllText(path); }
            else { throw new FileNotFoundException("Specified file cannot be found.", path); }
        }

    }
}
