namespace DataCore
{
    /// <summary>
    /// Defines the type of sort to be performed on a given index
    /// </summary>
    public enum SortType
    {
        /// <summary>
        /// By File Name (unhashed)
        /// </summary>
        Name = 0,
        /// <summary>
        /// By File Offset
        /// </summary>
        Offset = 1,
        /// <summary>
        /// By File Size
        /// </summary>
        Size = 2,
        /// <summary>
        /// By DataID of the File
        /// </summary>
        DataId = 3
    }

    /// <summary>
    /// Defines the extension type of a list of extensions
    /// </summary>
    public enum ExtensionType
    {
        /// <summary>
        /// This type of extension is encrypted in the data.xxx system
        /// </summary>
        Encrypted = 0,
        /// <summary>
        /// This type of extension is unencrypted in the data.xxx system
        /// </summary>
        Unencrypted = 1
    }
}
