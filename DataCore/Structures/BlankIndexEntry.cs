namespace DataCore.Structures
{
    /// <summary>
    /// Stores information regarding an orphaned file
    /// </summary>
    public class BlankIndexEntry
    {
        /// <summary>
        /// The name this space had before being orphaned
        /// </summary>
        public string PreviousName { get; set; }

        /// <summary>
        /// The amount of space where the orphaned file resides
        /// </summary>
        public int AvailableSpace { get; set; }

        /// <summary>
        /// The offset of the orphaned file
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// The dataId of the orphaned file
        /// </summary>
        public int DataID { get; set; }
    }
}
