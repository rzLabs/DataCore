using System.Text;
using DataCore.Functions;

namespace DataCore.Structures
{
    /// <summary>
    /// Stores information regarding a data.000 entry
    /// </summary>
    public class IndexEntry
    {       
        string name { get; set; }
        /// <summary>
        /// The hashed file name
        /// </summary>
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(name)) { name = StringCipher.Decode(hash); }
                return name;
            }
            set
            {
                name = value;
                Hash = Encoding.ASCII.GetBytes(StringCipher.Encode(value));
            }
        }
      
        byte[] hash { get; set; }
        /// <summary>
        /// The unhashed file name byte collection
        /// </summary>
        public byte[] Hash
        {
            get { return hash; }
            set
            {
                name = null;
                hash = value;
            }
        }

        /// <summary>
        /// The size of the file
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// The offset the file will begin @ inside it's data.xxx housing
        /// </summary>
        public long Offset { get; set; }

        
        int dataid { get; set; }
        /// <summary>
        /// Data.XXX file this entry belongs to
        /// </summary>
        public int DataID
        {
            get
            {
                if (dataid == 0) { dataid = StringCipher.GetID(hash); }
                return dataid;
            }
            set { dataid = value; }
        }
    }
}
