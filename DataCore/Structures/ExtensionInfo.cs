namespace DataCore.Structures
{
    /// <summary>
    /// Information regarding a stored extension
    /// </summary>
    public class ExtensionInfo
    {
        /// <summary>
        /// Amount of the given extension type
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Type of the extension in regards to this info
        /// </summary>
        public string Type { get; set; }
    }
}
