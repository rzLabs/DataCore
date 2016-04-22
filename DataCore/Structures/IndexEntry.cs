namespace DataCore.Structures
{
    /// <summary>
    /// Stores information regarding a data.000 entry
    /// </summary>
    public class IndexEntry
    {
        /// <summary>
        /// The hashed file name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The size of the file
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// The offset the file will begin @ inside it's data.xxx housing
        /// </summary>
        public long Offset { get; set; }

        /// <summary>
        /// Data.XXX file this entry belongs to
        /// </summary>
        public int DataID { get; set;}
    }
}
