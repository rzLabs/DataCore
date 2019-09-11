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
        /// The unhashed file name
        /// </summary>
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                    name = StringCipher.Decode(hash);
                    
                return name;
            }
            set
            {
                name = value;
                Hash = Encoding.ASCII.GetBytes(StringCipher.Encode(value));
            }
        }

        public string Extension
        {
            get
            {
                if (!string.IsNullOrEmpty(Name))
                {
                    string ret = Name.Remove(0, Name.Length - 3);
                    if (ret[0] == '.')
                        ret = ret.Remove(0, 1); // Consider 2 character extensions like 'fx'

                    return ret;
                }                  
                else
                    return null;
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

        string hashName { get; set; }

        public string HashName
        {
            get
            {
                if (hashName == null)
                {
                    hashName = ByteConverterExt.ToString(Hash);
                }

                return hashName;
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

        int dataId { get; set; } = 0;

        /// <summary>
        /// Data.XXX file this entry belongs to
        /// </summary>
        public int DataID
        {
            get
            {
                if (dataId == 0)
                    dataId = StringCipher.GetID(HashName);

                return dataId;
            }
            set { dataId = value; }
        }

        public string DataPath
        {
            get { return StringCipher.GetPath(HashName); }
        }
    }
}
