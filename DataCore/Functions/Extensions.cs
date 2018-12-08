using System.Collections.Generic;
using DataCore.Structures;

namespace DataCore.Functions
{
    /// <summary>
    /// Provides interactibility with file extensions
    /// </summary>
    public class Extensions
    {
        /// <summary>
        /// List containing all valid extensions in the Rappelz File-System
        /// </summary>
        internal static List<string> ValidExtensions { get; set; } = new List<string>()
        {
            "bmp", "cfg", "cob", "db", "dds", "dmp", "fx", "gc2", "gci", "ini", "jpg", "jtv", "lua", "lst",
            "m4v", "max", "naf", "nfa", "nfc", "nfe", "nfk", "nfl", "nfm", "nfp", "nfs", "nfw", "nui", "nx3",
            "otf", "obj", "ogg", "png", "pvs", "qpf", "rdb", "ref", "rdu", "sdb", "spr", "spt", "scn", "tif",
            "tga", "tml", "ttf", "txt", "wav", "xml"
        };

        internal static Dictionary<string, List<string>> GroupExtensions { get; set; }

        /// <summary>
        /// Determines if the provided ext exists in the validExts list
        /// </summary>
        /// <param name="ext">[LOWERCASE] extension (e.g. .dds)</param>
        /// <returns>Bool value indicating existance</returns>
        public static bool IsValid(string ext) { return (ValidExtensions.Contains(ext)) ? true : false; }
    }
}