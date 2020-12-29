using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DataCore.Functions;
using DataCore.Structures;

/// <summary>
/// DataCore provides portable in-depth interactibility with the Rappelz File-Management System
/// based on the works of Glandu2 and xXExiledXx two of my greatest inspirations in Rappelz Developement
/// Please report suggestions and bugs to iSmokeDrow@gmail.com
/// Reminder: This dll uses .NET 4.5.1
/// <version>4.2.0</version>
/// </summary>
namespace DataCore
{
    /// <summary>
    /// Provides interactive access to the Rappelz Data.XXX File Management System
    /// </summary>
    public class Core
    {
        bool makeBackups = false;

        /// <summary>
        /// Defines the encoding of files to be the default of the system
        /// unless changed by the caller during construction of Core
        /// </summary>
        internal readonly Encoding encoding = Encoding.Default;

        public bool UseModifiedXOR { get => XOR.UseModifiedKey; set => XOR.UseModifiedKey = value; }
        public void SetXORKey(byte[] key) => XOR.SetKey(key);

        /// <summary>
        /// List storing all IndexEntrys inside of data.000
        /// </summary>
        public List<IndexEntry> Index = new List<IndexEntry>();

        /// <summary>
        /// Count of IndexEntrys listed in the loaded Index
        /// </summary>
        public int RowCount { get { return Index.Count; } }

        /// <summary>
        /// The directory where the Rappelz client data.xxx files are located
        /// </summary>
        public string DataDirectory { get; set; }

        public bool Backups
        {
            get { return makeBackups; }
            set { makeBackups = value; }
        }

        LUA luaIO;

        #region Events

        /// <summary>
        /// Occurs when a message is transmitted to the caller for display
        /// </summary>
        public event EventHandler<MessageArgs> MessageOccured;
        /// <summary>
        /// Occurs when a non-critical issues has been encountered
        /// </summary>
        public event EventHandler<WarningArgs> WarningOccured;
        /// <summary>
        /// Occurs when the maximum progress of an operation has been determined
        /// </summary>
        public event EventHandler<CurrentMaxArgs> CurrentMaxDetermined;
        /// <summary>
        /// Ocurrs when the progress value of the current operation has been changed
        /// </summary>
        public event EventHandler<CurrentChangedArgs> CurrentProgressChanged;
        /// <summary>
        /// Occurs when an operation has completed and the progressbar values of the caller need to be reset
        /// </summary>
        public event EventHandler<CurrentResetArgs> CurrentProgressReset;

        #endregion

        #region Event Delegates

        /// <summary>
        /// Raises an event that informs the caller of a message that has occured
        /// </summary>
        /// <param name="c"></param>
        protected void OnMessage(MessageArgs c) { MessageOccured?.Invoke(this, c); }

        /// <summary>
        /// Raises an event that informs the caller of a warning that has occured
        /// </summary>
        /// <param name="w">Description of the warning event ([Method-Name] Warning-String)</param>
        protected void OnWarning(WarningArgs w) { WarningOccured?.Invoke(this, w); }

        /// <summary>
        /// Raises an event that informs caller of CurrentProgress operations total
        /// </summary>
        /// <param name="c">Total number of processes to be completed</param>
        protected void OnCurrentMaxDetermined(CurrentMaxArgs c) { CurrentMaxDetermined?.Invoke(this, c); }

        /// <summary>
        /// Raises an event that informs the caller of current operations completed.
        /// This event can additionally deliver a string (status update) to the caller
        /// </summary>
        /// <param name="c">CurrentChangedArgs containing event data</param>
        protected void OnCurrentProgressChanged(CurrentChangedArgs c) { CurrentProgressChanged?.Invoke(this, c); }

        /// <summary>
        /// Raises an event that informs the caller that the CurrentProgressbar should be reset to 0
        /// </summary>
        /// <param name="e">Dummy EventArg</param>
        protected void OnCurrentProgressReset(CurrentResetArgs e) { CurrentProgressReset?.Invoke(this, e); }

        #endregion

        #region Contructors

        /// <summary>
        /// Dummy constructor
        /// </summary>
        public Core() { }

        /// <summary>
        /// Instantiates the Core by providing encoding for operations
        /// </summary>
        /// <param name="encoding"></param>
        public Core(Encoding encoding) { this.encoding = encoding; }

        /// <summary>
        /// Instantiates the Core by providing backup and encoding for operations
        /// </summary>
        /// <param name="backup">Determines if this core will use the backup function</param>
        /// <param name="encoding">Encoding to be applied to certain conversions</param>
        public Core(bool backup, Encoding encoding)
        {
            makeBackups = backup;
            this.encoding = encoding;
        }

        /// <summary>
        /// Instantiates the Core by providing backup and configuration file path
        /// </summary>
        /// <param name="backup">Determines if this core will use the backup function</param>
        /// <param name="configPath">Path to the dCore.lua containing overrides</param>
        public Core(bool backup, string configPath)
        {
            makeBackups = backup;
            luaIO = new LUA(IO.LoadConfig(configPath));
        }

        /// <summary>
        /// Instantiates the Core by providing file encoding and backup and configPath
        /// </summary>
        /// <param name="backup">Determines if this core will use the backup function</param>
        /// <param name="encoding">Encoding to be applied to certain conversions</param>
        /// <param name="configPath">Path to the dCore.lua containing overrides</param>
        public Core(Encoding encoding, bool backup, string configPath)
        {
            makeBackups = backup;
            this.encoding = encoding;
            luaIO = new LUA(IO.LoadConfig(configPath));
        }

        #endregion

        #region Get Methods

        /// <summary>
        /// Returns a list of valid extensions that can be exported
        /// </summary>
        public List<ExtensionInfo> ExtensionList
        {
            get
            {
                List<ExtensionInfo> exts = new List<ExtensionInfo>();

                for (int idx = 0; idx < Index.Count; idx++)
                {
                    IndexEntry entry = Index[idx];
                    string ext = entry.Extension;

                    int extIdx = exts.FindIndex(i => i.Type == ext);

                    if (extIdx == -1)
                        exts.Add(new ExtensionInfo() { Count = 1, Type = ext });
                    else
                        exts[extIdx].Count++;
                }

                return exts.OrderBy(i => i.Type).ToList();
            }

        }

        /// <summary>
        /// Determines if the given extension is Encrypted
        /// </summary>
        /// <param name="extension">Extension to be determined</param>
        /// <returns>True or False</returns>
        public bool ExtensionEncrypted(string extension)
        {
            return XOR.Encrypted(extension);
        }

        #endregion

        #region Data.000/BLK Methods

        /// <summary>
        /// Generates a new data.000 index based on provided dumpDirectory
        /// Expects: /tga /jpg /wav /dds style dump folder structure
        /// (This function is to be used primarily in saving a newly created/modified 
        /// index.000)
        /// </summary>
        /// <param name="dumpDirectory">Location of dump folders (e.g. client/output/dump/)</param>
        /// <returns>Populated data.000 index</returns>
        public void New(string dumpDirectory)
        {
            OnMessage(new MessageArgs("Creating new data.000...", false, 0, true, 1));

            if (Directory.Exists(dumpDirectory))
            {
                string[] extDirectories = Directory.GetDirectories(dumpDirectory);

                for (int dumpDirIdx = 0; dumpDirIdx < extDirectories.Length; dumpDirIdx++)
                {
                    OnMessage(new MessageArgs(string.Format("Indexing directory: {0}...", extDirectories[dumpDirIdx]), true, 1));

                    string[] directoryFiles = Directory.GetFiles(extDirectories[dumpDirIdx]);

                    OnCurrentMaxDetermined(new CurrentMaxArgs(directoryFiles.Length));

                    for (int directoryFileIdx = 0; directoryFileIdx < directoryFiles.Length; directoryFileIdx++)
                    {
                        OnCurrentProgressChanged(new CurrentChangedArgs(directoryFileIdx, string.Empty));
                        Index.Add(new IndexEntry
                        {
                            Name = Path.GetFileName(directoryFiles[directoryFileIdx]),
                            Length = 0,
                            Offset = 0
                        });
                    }

                    OnCurrentProgressReset(new CurrentResetArgs(true));
                }
            }
            else { throw new FileNotFoundException(string.Format("[Create] Cannot locate dump directory at: {0}", dumpDirectory)); }
        }

        /// <summary>
        /// Reads the data.000 contents into a List of IndexEntries (note toggling on decodeNames will decrease speed)
        /// </summary>
        /// <param name="path">Path to the data.000 index</param>
        public void Load(string path)
        {
            string indexPath = null;

            if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
                indexPath = string.Format(@"{0}\data.000", path);
            else
                indexPath = path;

            byte b = 0;

            long bytesRead = 0;

            if (File.Exists(indexPath))
            {
                DataDirectory = Path.GetDirectoryName(indexPath);

                using (var ms = new MemoryStream(File.ReadAllBytes(indexPath)))
                {
                    OnCurrentMaxDetermined(new CurrentMaxArgs(ms.Length));

                    long len = ms.Length;

                    while (ms.Position < len)
                    {
                        byte[] array = new byte[1];
                        ms.Read(array, 0, array.Length);

                        XOR.Cipher(ref array, ref b);
                        byte[] bytes = new byte[array[0]];
                        ms.Read(bytes, 0, bytes.Length);
                        XOR.Cipher(ref bytes, ref b);

                        byte[] value = new byte[8];
                        ms.Read(value, 0, value.Length);
                        XOR.Cipher(ref value, ref b);

                        Index.Add(new IndexEntry()
                        {
                            Hash = bytes,
                            Offset = BitConverter.ToInt32(value, 0),
                            Length = BitConverter.ToInt32(value, 4)
                        });

                        if ((ms.Position - bytesRead) >= 64000)
                        {
                            OnCurrentProgressChanged(new CurrentChangedArgs(ms.Position, ""));
                            bytesRead = ms.Position;
                        }
                    }
                }
            }
            else { throw new FileNotFoundException(string.Format("[Load] Cannot find data.000 at path: {0}", indexPath)); }

            OnCurrentProgressReset(new CurrentResetArgs(true));
        }

        public void Save() { Save(DataDirectory); }

        /// <summary>
        /// Saves the provided indexList into a ready to use data.000 index
        /// </summary>
        /// <param name="buildDirectory">Location to build the new data.000 at</param>
        /// <returns>bool value indicating success or failure</returns>
        public void Save(string buildDirectory)
        {
            string buildPath = string.Format(@"{0}\data.000", buildDirectory);

            if (makeBackups) { createBackup(buildPath); }

            if (File.Exists(buildPath)) { File.Delete(buildPath); }

            OnMessage(new MessageArgs("Writing new data.000..."));

            using (FileStream fs = File.Create(buildPath))
            {
                byte b = 0;

                OnCurrentMaxDetermined(new CurrentMaxArgs(Index.Count));

                for (int idx = 0; idx < Index.Count; idx++)
                {
                    IndexEntry indexEntry = Index[idx];
                    int hashLen = indexEntry.Hash.Length;

                    byte[] buffer = new byte[hashLen + 9];
                    buffer[0] = Convert.ToByte(indexEntry.HashName.Length);
                    Buffer.BlockCopy(indexEntry.Hash, 0, buffer, 1, hashLen);
                    Buffer.BlockCopy(BitConverter.GetBytes(indexEntry.Offset), 0, buffer, hashLen + 1, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(indexEntry.Length), 0, buffer, hashLen + 5, 4);
                    XOR.Cipher(ref buffer, ref b);
                    fs.Write(buffer, 0, buffer.Length);

                    int lastIdx = 0;
                    if (lastIdx - idx > 64000) { OnCurrentProgressChanged(new CurrentChangedArgs(idx, "")); lastIdx = idx; }
                }

                OnCurrentProgressReset(new CurrentResetArgs(true));
            }
        }

        /// <summary>
        /// Reorders references index by sortType
        /// </summary>
        /// <param name="type">Type of sort to be performed</param>
        public void Sort(SortType type)
        {
            switch (type)
            {
                case SortType.Name:
                    Index = Index.OrderBy(i => i.Name).ToList();
                    break;

                case SortType.Offset:
                    Index = Index.OrderBy(i => i.Offset).ToList();
                    break;

                case SortType.Size:
                    Index = Index.OrderBy(i => i.Length).ToList();
                    break;

                case SortType.DataId:
                    Index = Index.OrderBy(i => i.DataID).ToList();
                    break;
            }
        }

        /// <summary>
        /// Gets the total size of all the files listed in the filteredList
        /// </summary>
        /// <param name="filteredList">List of files to be summed</param>
        /// <returns>(long) File Size of filteredList</returns>
        public long GetStoredSize(List<IndexEntry> filteredList)
        {
            long size = 0;
            foreach (IndexEntry entry in filteredList) { size += entry.Length; }
            return size;
        }

        /// <summary>
        /// Gets the total size of all the files listed for the given dataId
        /// </summary>
        /// <param name="dataId">ID of data unit to be calculated</param>
        /// <returns>(long) File Size of all files in dataId</returns>
        public long GetStoredSize(int dataId)
        {
            long size = 0;

            switch (dataId)
            {
                case 0:
                    foreach (IndexEntry entry in Index) { size += entry.Length; }
                    break;

                case 1:
                    foreach (IndexEntry entry in GetEntriesByDataId(1)) { size += entry.Length; }
                    break;

                case 2:
                    foreach (IndexEntry entry in GetEntriesByDataId(2)) { size += entry.Length; }
                    break;

                case 3:
                    foreach (IndexEntry entry in GetEntriesByDataId(3)) { size += entry.Length; }
                    break;

                case 4:
                    foreach (IndexEntry entry in GetEntriesByDataId(4)) { size += entry.Length; }
                    break;

                case 5:
                    foreach (IndexEntry entry in GetEntriesByDataId(5)) { size += entry.Length; }
                    break;

                case 6:
                    foreach (IndexEntry entry in GetEntriesByDataId(6)) { size += entry.Length; }
                    break;

                case 7:
                    foreach (IndexEntry entry in GetEntriesByDataId(7)) { size += entry.Length; }
                    break;

                case 8:
                    foreach (IndexEntry entry in GetEntriesByDataId(8)) { size += entry.Length; }
                    break;
            }

            return size;
        }

        public long GetPhysicalSize(int dataId)
        {
            string path = null;

            if (dataId == 0)
            {
                long val = 0;

                for (int i = 1; i <= 8; i++)
                {
                    path = string.Format(@"{0}\data.00{1}", DataDirectory, i);
                    val += new FileInfo(path).Length;
                }

                return val;
            }
            else
            {
                path = string.Format(@"{0}\data.00{1}", DataDirectory, dataId);
                return new FileInfo(path).Length;
            }

        }

        /// <summary>
        /// Gets the total size of fragmented space in the entire client
        /// </summary>
        /// <returns>(long) Total fragmented bytes in all data.00*</returns>
        public long GetFragmentedSize()
        {
            long val = 0;

            for (int i = 1; i < 8; i++)
                val += GetFragmentedSize(i);

            return val;
        }

        /// <summary>
        /// Gets the total size of fragmented space in the storage unit
        /// </summary>
        /// <param name="dataId">ID of the data unit to be calculated</param>
        /// <returns>(long) Total fragmented bytes in dataId</returns>
        public long GetFragmentedSize(int dataId)
        {
            long physicalSize = 0;
            long indexedSize = 0;
            long fragmentedSize = 0;

            string dataPath = string.Format(@"{0}\data.00{1}", DataDirectory, dataId);
            if (File.Exists(dataPath))
            {
                physicalSize = new FileInfo(dataPath).Length;
            }
            else
                throw new FileNotFoundException(string.Format("Data Storage Unit #{0} not found at path: {1}", dataId, dataPath));

            indexedSize = GetStoredSize(dataId);
            fragmentedSize = physicalSize - indexedSize;

            return fragmentedSize;
        }

        public int GetFileCount(int dataId)
        {
            if (dataId == 0)
                return Index.Count;
            else
                return GetEntriesByDataId(dataId).Count;
        }

        /// <summary>
        /// Gets the total size of all files in the Index ending with the given extension
        /// </summary>
        /// <param name="extension">Extension of the target files</param>
        /// <returns>(long) Total Size</returns>
        public long GetExtensionSize(string extension)
        {
            return GetStoredSize(GetEntriesByExtension(extension));
        }

        /// <summary>
        /// Locates all IndexEntry with matching criteria and returns them as a list
        /// </summary>
        /// <param name="fieldName">Operand 1 of the search (e.g. name, data_id)</param>
        /// <param name="op">Operator for the search (e.g. ==, >= etc..)</param>
        /// <param name="criteria">Operand 2 of the search (e.g. "db_")</param>
        /// <returns>List of matching IndexEntry</returns>
        public List<IndexEntry> FindAll(string fieldName, string op, object criteria)
        {
            if (fieldName == "name")
            {
                switch (op)
                {
                    case "==": return Index.FindAll(i => i.Name == (string)criteria);
                    case "LIKE": return Index.FindAll(i => i.Name.Contains((string)criteria));
                }
            }
            else if (fieldName == "offset")
            {
                long val = (long)criteria;

                switch (op)
                {                  
                    case "==": return Index.FindAll(i=>i.Offset == val);
                    case ">": return Index.FindAll(i => i.Offset > val);
                    case ">=": return Index.FindAll(i => i.Offset >= val);
                    case "<": return Index.FindAll(i => i.Offset < val);
                    case "<=": return Index.FindAll(i => i.Offset <= val);
                }
            }
            else if (fieldName == "length")
            {
                int val = (int)criteria;

                switch (op)
                {
                    case "==": return Index.FindAll(i => i.Length == val);
                    case ">": return Index.FindAll(i => i.Length > val);
                    case ">=": return Index.FindAll(i => i.Length >= val);
                    case "<": return Index.FindAll(i => i.Length < val);
                    case "<=": return Index.FindAll(i => i.Length <= val);
                }
            }
            else if (fieldName == "data_id")
            {
                int val = (int)criteria;

                switch (op)
                {
                    case "==": return Index.FindAll(i => i.DataID == val);
                    case ">": return Index.FindAll(i => i.DataID > val);
                    case ">=": return Index.FindAll(i => i.DataID >= val);
                    case "<": return Index.FindAll(i => i.DataID < val);
                    case "<=": return Index.FindAll(i => i.DataID <= val);
                }
            }

            return null;
        }

        /// <summary>
        /// Returns an IndexEntry based on its ordinal position
        /// </summary>
        /// <param name="index">Oridinal position of the desired IndexEntry</param>
        /// <returns>(IndexEntry)</returns>
        public IndexEntry GetEntry(int index) { return Index[index]; }

        /// <summary>
        /// Returns an IndexEntry based on it's [UNHASHED] name
        /// </summary>
        /// <param name="name">File name being searched for</param>
        /// <returns>IndexEntry of name or null</returns>
        public IndexEntry GetEntry(string name) { return Index.Find(i => i.Name == name); }

        /// <summary>
        /// Returns an IndexEntry based on it's dataId and offset
        /// </summary>
        /// <param name="dataId">data.xxx id being searched</param>
        /// <param name="offset">offset of file in dataId being searched</param>
        /// <returns>IndexEntry of dataId and offset or null</returns>
        public IndexEntry GetEntry(int dataId, int offset) { return Index.Find(i => i.DataID == dataId && i.Offset == offset); }

        /// <summary>
        /// Returns a List of all entries whose name contains partialName
        /// </summary>
        /// <param name="partialName">Partial fileName (e.g. db_) to be searched for</param>
        /// <returns>Populated List of IndexEntries</returns>
        public List<IndexEntry> GetEntriesByPartialName(string partialName) { return Index.FindAll(i => Regex.Match(i.Name, partialName.Replace("*", ".")).Success); }

        /// <summary>
        /// Returns a List of all entries matching dataId
        /// </summary>
        /// <param name="dataId">data.xxx Id being requested</param>
        /// <returns>List for data.xx{dataId}</returns>
        public List<IndexEntry> GetEntriesByDataId(int dataId) { return Index.FindAll(i => i.DataID == dataId); }

        /// <summary>
        /// Returns a filtered List of all entries matching dataId
        /// Return is sorted by sortType
        /// </summary>
        /// <param name="dataId">data.xxx Id being requested</param>
        /// <param name="type">Type code for how to sort return</param>
        /// LEGEND:
        /// 0 = Name
        /// 1 = Offset
        /// 2 = Size
        /// <returns>List for data.xx{dataId}</returns>
        public List<IndexEntry> GetEntriesByDataId(int dataId, SortType type)
        {
            switch (type)
            {
                case SortType.Name: // Name
                    return Index.FindAll(i => i.DataID == dataId).OrderBy(i => i.Name).ToList();

                case SortType.Offset: // Offset
                    return Index.FindAll(i => i.DataID == dataId).OrderBy(i => i.Offset).ToList();

                case SortType.Size: // Size
                    return Index.FindAll(i => i.DataID == dataId).OrderBy(i => i.Length).ToList();
            }

            return null;
        }

        /// <summary>
        /// Returns a filtered List of all entries matching dataId
        /// Return is sorted by sortType
        /// </summary>
        /// <param name="filteredIndx">Reference to data.000 index</param>
        /// <param name="dataId">data.xxx Id being requested</param>
        /// <param name="type">Type code for how to sort return</param>
        /// <returns>List for data.xx{dataId}</returns>
        public List<IndexEntry> GetEntriesByDataId(List<IndexEntry> filteredIndx, int dataId, SortType type)
        {
            switch (type)
            {
                case SortType.Name: // Name
                    OnWarning(new WarningArgs("[GetEntriesByDataId] Index cannot be sorted by Name!\nPlease try again."));
                    break;

                case SortType.Offset: // Offset
                    return filteredIndx.FindAll(i => i.DataID == dataId).OrderBy(i => i.Offset).ToList();

                case SortType.Size: // Size
                    return filteredIndx.FindAll(i => i.DataID == dataId).OrderBy(i => i.Length).ToList();
            }

            return null;
        }

        /// <summary>
        /// Returns a filtered List of all entries matching extension
        /// </summary>
        /// <param name="extension">extension being searched (e.g. dds)</param>
        /// <returns>Filtered List of extension</returns>
        public List<IndexEntry> GetEntriesByExtension(string extension)
        {
            return Index.FindAll(i => i.Name.EndsWith(string.Format(".{0}", extension.ToLower())));
        }

        /// <summary>
        /// Returns a filtered List of all entries matching extension
        /// </summary>
        /// <param name="extension">extension being searched (e.g. dds)</param>
        /// <param name="type">Type code for how to sort return</param>
        /// <returns>Filtered List of extension</returns>
        public List<IndexEntry> GetEntriesByExtension(string extension, SortType type)
        {
            List<IndexEntry> ret = Index.FindAll(i => i.Name.EndsWith(string.Format(".{0}", extension.ToLower())));

            switch (type)
            {
                case SortType.Name:
                    return ret.OrderBy(i => i.Name).ToList();

                case SortType.Offset:
                    return ret.OrderBy(i => i.Offset).ToList();

                case SortType.Size:
                    return ret.OrderBy(i => i.Length).ToList();

                case SortType.DataId:
                    return ret.OrderBy(i => i.DataID).ToList();

                default:
                    return null;
            }
        }

        /// <summary>
        /// Returns a filtered List of all entries matching both extension and term
        /// </summary>
        /// <param name="extension">Extension of desired files</param>
        /// <param name="term">Term desired file names must contain</param>
        /// <returns>Filtered List of files with extension whose names contain term</returns>
        public List<IndexEntry> GetEntriesByExtension(string extension, string term)
        {
            return Index.FindAll(i => i.Name.Contains(term) && i.Name.EndsWith(string.Format(".{0}", extension.ToLower())));
        }

        /// <summary>
        /// Returns a filtered List of all entries matching extension
        /// </summary>
        /// <param name="extension">extension being searched (e.g. dds)</param>
        /// <param name="term">Term desired file names must contain</param>
        /// <param name="type">Type code for how to sort return</param>
        /// <returns>Filtered List of extension</returns>
        public List<IndexEntry> GetEntriesByExtension(string extension, string term, SortType type)
        {
            List<IndexEntry> ret = Index.FindAll(i => i.Name.Contains(term) && i.Name.EndsWith(string.Format(".{0}", extension.ToLower())));

            switch (type)
            {
                case SortType.Name:
                    return ret.OrderBy(i => i.Name).ToList();

                case SortType.Offset:
                    return ret.OrderBy(i => i.Offset).ToList();

                case SortType.Size:
                    return ret.OrderBy(i => i.Length).ToList();

                case SortType.DataId:
                    return ret.OrderBy(i => i.DataID).ToList();

                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets all entries in the Index ending with the given extension with the given dataId
        /// </summary>
        /// <param name="extension">Extension of the target files</param>
        /// <param name="dataID">Data ID of the target files</param>
        /// <returns>Collection of entries matching the given extension and dataID</returns>
        public List<IndexEntry> GetEntriesByExtension(string extension, int dataID)
        {
            return Index.FindAll(i => i.Extension == extension && i.DataID == dataID);
        }

        /// <summary>
        /// Gets all entries in the Index ending with the given extension with the given dataId sorted on the given type
        /// </summary>
        /// <param name="extension">Extension of the target files</param>
        /// <param name="dataID">Data ID of the target files</param>
        /// <param name="type">Desired SortType for the returned collection</param>
        /// <returns>Collection of entries matching the given extension and dataID</returns>
        public List<IndexEntry> GetEntriesByExtension(string extension, int dataID, SortType type)
        {
            List<IndexEntry> ret = GetEntriesByExtension(extension, dataID);

            switch (type)
            {
                case SortType.Name:
                    return ret.OrderBy(i => i.Name).ToList();

                case SortType.Offset:
                    return ret.OrderBy(i => i.Offset).ToList();

                case SortType.Size:
                    return ret.OrderBy(i => i.Length).ToList();

                case SortType.DataId:
                    return ret.OrderBy(i => i.DataID).ToList();

                default:
                    return null;
            }
        }

        /// <summary>
        /// Returns a filtered List of all entries with matching provided extensions
        /// </summary>
        /// <param name="extensions"></param>
        /// <returns></returns>
        public List<IndexEntry> GetEntriesByExtensions(params string[] extensions)
        {
            List<IndexEntry> ret = new List<IndexEntry>();

            foreach (string ext in extensions)
                ret.AddRange(Index.FindAll(i => i.Name.EndsWith(string.Format(".{0}", ext.ToLower()))));           

            return ret.OrderBy(i => i.Name).ToList();
        }

        /// <summary>
        /// Removes a set of entries bearing DataID = dataId from referenced data.000 index
        /// </summary>
        /// <param name="dataId">Id of file entries to be deleted</param>
        public void DeleteEntriesByDataId(int dataId) { Index.RemoveAll(i => i.DataID == dataId); }

        /// <summary>
        /// Removes a single entry bearing Name = name from referenced data.000 index
        /// </summary>
        /// <param name="fileName">Name of the IndexEntry being deleted</param>
        /// <param name="dataDirectory">Directory of the data.xxx files</param>
        public void DeleteEntryByName(string fileName, string dataDirectory)
        {
            IndexEntry entry = GetEntry(fileName);
            Index.Remove(entry);
        }

        /// <summary>
        /// Removes a single entry bearing DataID = id and Offset = offset from referenced data.000 index
        /// </summary>
        /// <param name="id">DataID of file entry to be deleted</param>
        /// <param name="offset">Offset of file entry to be deleted</param>
        public void DeleteEntryByIdandOffset(int id, int offset) { Index.Remove(Index.Find(i => i.DataID == id && i.Offset == offset)); }

        /// <summary>
        /// Updates the offset for IndexEntry with given fileName in the referenced index
        /// </summary>
        /// <param name="fileName">Name of the IndexEntry being updated</param>
        /// <param name="offset">New offset for the IndexEntry</param>
        public void UpdateEntryOffset(string fileName, long offset)
        {
            int idx = Index.FindIndex(i => i.Name == fileName);
            if (idx != -1) { Index[idx].Offset = offset; }
            else { throw new Exception(string.Format("[UpdateEntryOffset] IndexEntry for {0} not found!", fileName)); }
        }


        /// <summary>
        /// Gets the IndexEntry with given fileName regardless of locale
        /// </summary>
        /// <param name="fileName">Name o the IndexEntry being saught</param>
        /// <returns>Matching IndexEntry of fileName</returns>
        public IndexEntry GetEntryNoLocale(string fileName)
        {
            var fileEntry = GetEntry(fileName);

            if (fileEntry == null)
                fileEntry = GetEntry(fileName.Replace(".", "(ascii)."));

            return fileEntry;
        }

        #endregion

        #region File Methods


        /// <summary>
        /// Gets the collection of bytes that makes up a given file
        /// </summary>
        /// <param name="fileName">Name of the file to generate hash for</param>
        public byte[] GetFileBytes(string fileName)
        {
            var fileEntry = GetEntry(fileName.ToLower());

            return GetFileBytes(fileEntry);
        }

        /// <summary>
        /// Gets the collection of bytes that makes up a given file
        /// </summary>
        /// <param name="fileName">Name of the target file</param>
        /// <param name="dataId">ID of the target data.xxx</param>
        /// <param name="offset">Offset of the target file</param>
        /// <param name="length">Length of the target file</param>
        /// <returns>Bytes of the target file</returns>
        public byte[] GetFileBytes(string fileName, int dataId, long offset, long length)
        {
            byte[] buffer = new byte[length];

            if (dataId > 0 && dataId < 9)
            {
                string dataPath = string.Format(@"{0}\data.00{1}", DataDirectory, dataId);

                if (File.Exists(dataPath))
                {
                    string ext = Path.GetExtension(fileName).Remove(0, 1).ToLower();

                    using (FileStream fs = File.Open(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        fs.Seek(offset, SeekOrigin.Begin);
                        fs.Read(buffer, 0, buffer.Length);
                    }

                    // Check if this particular extension needs to be unencrypted
                    if (XOR.Encrypted(ext)) { byte b = 0; XOR.Cipher(ref buffer, ref b); }
                }
                else
                    throw new FileNotFoundException(string.Format(@"[GetFileBytes] Cannot locate: {0}", dataPath));
            }
            else
                throw new Exception("[GetFileBytes] dataId is invalid! Must be between 1-8");

            return buffer;
        }

        /// <summary>
        /// Gets the collection of bytes that makes up a given file
        /// </summary>
        /// <param name="entry">(IndexEntry) containing information about the target file</param>
        /// <returns>Bytes of the target file</returns>
        public byte[] GetFileBytes(IndexEntry entry) =>
            GetFileBytes(entry.Name, entry.DataID, entry.Offset, entry.Length);

        /// <summary>
        /// Writes a single files from the data.xxx (specificed by dataXXX_path) to disk
        /// Note: file is written in chunks as to report progress, if chunkSize is not 
        /// defined it would default to 2% of total file size (unless n/a then it will
        /// default to 64k)
        /// </summary>
        /// <param name="buildDir">Directory to create the exported file at</param>
        /// <param name="entry">Entry information for file being exported</param>
        public void ExportFileEntry(string buildDir, IndexEntry entry)
        {
            string buildPath = string.Format(@"{0}\{1}", buildDir, entry.Name);
            string dataPath = string.Format(@"{0}\data.00{1}", DataDirectory, entry.DataID);          

            if (File.Exists(dataPath))
            {
                byte[] outBuffer = null;

                // Open the housing data.xxx and read the file contents into outBuffer
                using (FileStream dataFS = new FileStream(dataPath, FileMode.Open, FileAccess.Read))
                {
                    dataFS.Position = entry.Offset;
                    outBuffer = new byte[entry.Length];
                    dataFS.Read(outBuffer, 0, outBuffer.Length);
                }

                // IF the buffer actually contains data
                if (outBuffer.Length > 0)
                {
                    // Check if this particular extension needs to be unencrypted
                    if (XOR.Encrypted(entry.Extension)) { byte b = 0; XOR.Cipher(ref outBuffer, ref b); }

                    using (FileStream buildFs = new FileStream(buildPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                        buildFs.Write(outBuffer, 0, outBuffer.Length);

                }
                else { throw new Exception("[ExportFileEntry] Failed to buffer file for export"); }

                outBuffer = null;
            }
            else { throw new FileNotFoundException(string.Format("[ExportFileEntry] Cannot locate: {0}", dataPath)); }
        }

        /// <summary>
        /// Exports all files whose name ends with the given extension to the build directory
        /// </summary>
        /// <param name="buildDirectory">Directory to which exported files will be written</param>
        /// <param name="ext">Extension of the target files</param>
        public void ExportExtEntries(string buildDirectory, string ext)
        {
            int count = GetEntriesByExtension(ext).Count;
            int exported = 0;

            OnCurrentMaxDetermined(new CurrentMaxArgs(count));
            OnMessage(new MessageArgs("{0} entries found for extension: {1}", count, ext));

            for (int dataId = 1; dataId <= 8; dataId++)
            {
                string dataPath = string.Format(@"{0}\data.00{1}", DataDirectory, dataId);
                List<IndexEntry> entriesByExtID = GetEntriesByExtension(ext, dataId, SortType.Offset);

                if (entriesByExtID.Count > 0)
                {
                    OnMessage(new MessageArgs("Exporting {0} files from data.00{1}", entriesByExtID.Count, dataId));

                    if (!File.Exists(dataPath))
                        throw new Exception(string.Format("Data unit not found at path: {0}", dataPath));

                    using (FileStream ds = File.Open(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        for (int entryIdx = 0; entryIdx < entriesByExtID.Count; entryIdx++)
                        {
                            IndexEntry entry = entriesByExtID[entryIdx];
                            string buildPath = string.Format(@"{0}\{1}", buildDirectory, entry.Name);

                            //OnMessage(new MessageArgs("Exporting: {0}", entry.Name));

                            byte[] buffer = new byte[entry.Length];

                            ds.Seek(entry.Offset, SeekOrigin.Begin);
                            ds.Read(buffer, 0, buffer.Length);

                            if (XOR.Encrypted(ext))
                            {
                                byte b = 0;
                                XOR.Cipher(ref buffer, ref b);
                            }

                            using (FileStream fs = File.Create(buildPath))
                                fs.Write(buffer, 0, buffer.Length);

                            if (((exported * 100) / RowCount) != ((exported - 1) * 100 / RowCount))
                                OnCurrentProgressChanged(new CurrentChangedArgs(exported, null));

                            exported++;
                        }
                    }
                }
            }

            OnCurrentProgressReset(new CurrentResetArgs(true));
        }

        /// <summary>
        /// Exports all entries indexed by the data.000 into the build directory
        /// </summary>
        /// <param name="buildDirectory">Directory in which exported files will be written</param>
        public void ExportAllEntries(string buildDirectory)
        {
            OnCurrentMaxDetermined(new CurrentMaxArgs(Index.Count));

            int exported = 0;

            for (int dataId = 1; dataId <= 8; dataId++)
            {
                string dataPath = string.Format(@"{0}\data.00{1}", DataDirectory, dataId);
                List<IndexEntry> entriesById = GetEntriesByDataId(dataId, SortType.Offset);

                if (entriesById.Count == 0)
                    throw new Exception(string.Format("No results for data id: {0}", dataId));

                if (!File.Exists(dataPath))
                    throw new Exception(string.Format("Data unit not found at path: {0}", dataPath));

                using (FileStream ds = File.Open(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    for (int entryIdx = 0; entryIdx < entriesById.Count; entryIdx++)
                    {
                        IndexEntry entry = entriesById[entryIdx];
                        string extDirectory = string.Format(@"{0}\{1}", buildDirectory, entry.Extension);
                        string buildPath = string.Format(@"{0}\{1}", extDirectory, entry.Name);

                        if (!Directory.Exists(extDirectory))
                            Directory.CreateDirectory(extDirectory);

                        byte[] buffer = new byte[entry.Length];

                        ds.Seek(entry.Offset, SeekOrigin.Begin);
                        ds.Read(buffer, 0, buffer.Length);

                        if (XOR.Encrypted(entry.Extension))
                        {
                            byte b = 0;
                            XOR.Cipher(ref buffer, ref b);
                        }

                        using (FileStream fs = File.Create(buildPath))
                            fs.Write(buffer, 0, buffer.Length);

                        if (((exported * 100) / RowCount) != ((exported - 1) * 100 / RowCount))
                            OnCurrentProgressChanged(new CurrentChangedArgs(exported, null));

                        exported++;
                    }
                }
            }

            OnCurrentProgressReset(new CurrentResetArgs(true));
        }

        /// <summary>
        /// Writes/Appends a file at the filePath in(to) the Rappelz data.xxx storage system
        /// </summary>
        /// <param name="filePath">Location of the file being imported</param>
        public void ImportFileEntry(string filePath)
        {
            ImportFileEntry(Path.GetFileName(filePath), File.ReadAllBytes(filePath));
        }

        /// <summary>
        /// Writes/Appends a file represented by fileBytes in(to) the Rappelz data.xxx storage system with given fileName
        /// </summary>
        /// <param name="fileName">The name of the file being imported (e.g. db_item.rdb) [UNHASHED]</param>
        /// <param name="fileBytes">Bytes that represent the file</param>
        public void ImportFileEntry(string fileName, byte[] fileBytes)
        {
            // Define some information about the file
            string fileExt = Path.GetExtension(fileName).Remove(0, 1);
            long fileLen = fileBytes.Length;
            int dataId = StringCipher.GetID(fileName);

            // Determine the path of this particular file's data.xxx exists
            string dataPath = string.Format(@"{0}\data.00{1}", DataDirectory, dataId);

            if (File.Exists(dataPath))
            {
                // Create backup (if applicable)
                if (makeBackups) { createBackup(dataPath); }

                OnCurrentMaxDetermined(new CurrentMaxArgs(fileLen));

                // Open the housing data.xxx file
                using (FileStream fs = new FileStream(dataPath, FileMode.Open, FileAccess.Write, FileShare.Read))
                {
                    // Get information on the stored file, otherwise create it.
                    IndexEntry entry = GetEntry(fileName);
                    if (entry == null)
                    {
                        entry = new IndexEntry() { Hash = Encoding.ASCII.GetBytes(StringCipher.Encode(fileName)) };
                        Index.Add(entry);
                    }

                    // If the fileBytes need to be encrypted do so
                    if (XOR.Encrypted(fileExt)) { byte b = 0; XOR.Cipher(ref fileBytes, ref b); }

                    // Set the filestreams position accordingly
                    fs.Position = (fileLen < entry.Length) ? entry.Offset : fs.Length;

                    // Update the entry accordingly
                    entry.Offset = fs.Position;
                    if (entry.Length != fileBytes.Length)
                        entry.Length = fileBytes.Length;

                    int chunkSize = Convert.ToInt32(fileLen * 0.02);

                    for (int byteCount = 0; byteCount < fileLen; byteCount += Math.Min((int)fileLen - byteCount, chunkSize))
                    {
                        long nextChunk = Math.Min(fileLen - byteCount, chunkSize);
                        byte[] inChunks = new byte[nextChunk];
                        Buffer.BlockCopy(fileBytes, byteCount, inChunks, 0, inChunks.Length);
                        fs.Write(inChunks, 0, inChunks.Length);
                    }
                }
            }
            else { throw new FileNotFoundException(string.Format("[ImportFileEntry(string fileName, byte[] fileBytes)] Cannot locate data file: {0}", dataPath)); }

            OnCurrentProgressReset(new CurrentResetArgs(true));
        }

        /// <summary>
        /// Overwrites a previous files bytes with zeros in effect erasing it
        /// <param name="dataId">Id of the data.xxx file to be altered</param>
        /// <param name="offset">Offset to begin writing zeros</param>
        /// <param name="length">How far to write zeros</param>
        public void DeleteFileEntry(int dataId, int offset, int length)
        {
            // Determine the path of this particular file's data.xxx exists
            string dataPath = string.Format(@"{0}\data.00{1}", DataDirectory, dataId);

            if (!File.Exists(dataPath))
                throw new FileNotFoundException(string.Format("data.00{0} unit not found at: {1}", dataId, dataPath));

            OnMessage(new MessageArgs("Attempting to delete file content from data.00{0}", dataId));

            if (makeBackups) { createBackup(dataPath); }

            byte[] inBytes = File.ReadAllBytes(dataPath);
            byte[] outBytes = new byte[inBytes.Length - length];

            Buffer.BlockCopy(inBytes, 0, outBytes, 0, offset);

            int so2 = offset + length;
            int remainder = inBytes.Length - (offset + length);

            Buffer.BlockCopy(inBytes, so2, outBytes, offset, remainder);

            try
            {
                using (FileStream fs = new FileStream(dataPath, FileMode.Append, FileAccess.ReadWrite, FileShare.None))
                    fs.Write(outBytes, 0, outBytes.Length);
            }
            catch (Exception ex) { throw ex; }
            finally
            {
                DeleteEntryByIdandOffset(dataId, offset);
                OnMessage(new MessageArgs("The file at offset: {0} in data.00{1} deleted", offset, dataId));

                Save();
            }
        }

        /// <summary>
        /// Generates data.xxx file-system from dump structure (client/output/dump/)
        /// </summary>
        /// <param name="dumpDirectory">Location of dump folders (e.g. client/output/dump/)</param>
        /// <param name="buildDirectory">Location of build folder (e.g. client/output/data-files/)</param>
        /// <returns>Newly generated List to be saved</returns>
        public void BuildDataFiles(string dumpDirectory, string buildDirectory)
        {
            int written = 0;

            if (Directory.Exists(dumpDirectory))
            {
                New(dumpDirectory);

                OnCurrentMaxDetermined(new CurrentMaxArgs(RowCount));

                for (int dataId = 1; dataId <= 8; dataId++)
                {
                    OnMessage(new MessageArgs(string.Format("Building new data.00{0}", dataId)));

                    List<IndexEntry> entriesByID = GetEntriesByDataId(dataId);

                    string buildPath = string.Format(@"{0}\data.00{1}", buildDirectory, dataId);

                    if (File.Exists(buildPath))
                        File.Delete(buildPath);

                    using (FileStream fs = new FileStream(buildPath, FileMode.Create, FileAccess.Write))
                    {
                        for (int curFileIdx = 0; curFileIdx < entriesByID.Count; curFileIdx++)
                        {
                            IndexEntry entry = entriesByID[curFileIdx];
                            string filePath = string.Format(@"{0}\{1}\{2}", dumpDirectory, entry.Extension, entry.Name);
                            byte[] buffer = null;

                            if (File.Exists(filePath))
                            {
                                buffer = File.ReadAllBytes(filePath);

                                if (XOR.Encrypted(entry.Extension))
                                {
                                    byte b = 0;
                                    XOR.Cipher(ref buffer, ref b);
                                }

                                entry.Offset = fs.Position;
                                entry.Length = buffer.Length;

                                fs.Write(buffer, 0, buffer.Length);

                                if ((written * 100 / RowCount) != ((written - 1) * 100 / RowCount))
                                    OnCurrentProgressChanged(new CurrentChangedArgs(written, null));

                                written++;
                            }
                            else
                                throw new Exception(string.Format("Could not find file: {0}", filePath));
                        }
                    }
                }
            }
            else
                throw new FileNotFoundException(string.Format("[BuildDataFiles] Cannot locate dump directory at: {0}", dumpDirectory));

            GC.Collect();
            OnCurrentProgressReset(new CurrentResetArgs(false));
            Save(buildDirectory);
        }

        /// <summary>
        /// Rebuilds a data.xxx file potentially removing blank space created by the OEM update method.
        /// Effectiveness increases depending on amount of updates made to desired data.xxx file.
        /// </summary>
        /// <param name="dataId">Id of the data.xxx file to be rebuilt</param>
        /// <param name="buildDirectory">Location of build folder (e.g. client/output/data-files/)</param>
        public void RebuildDataFile(int dataId, string buildDirectory)
        {
            List<IndexEntry> filteredIndex = GetEntriesByDataId(dataId, SortType.Offset);

            string dataPath = string.Format(@"{0}\data.00{1}", DataDirectory, dataId);

            if (File.Exists(dataPath))
            {
                if (makeBackups)
                    createBackup(dataPath);

                OnMessage(new MessageArgs(string.Format("Writing new data.00{0}...", dataId), true, 1, false, 0));

                OnCurrentMaxDetermined(new CurrentMaxArgs(filteredIndex.Count));

                // Open the data.xxx file in inFS
                using (FileStream inFS = new FileStream(dataPath, FileMode.Open))
                    // Create the output file
                    using (FileStream outFS = File.Create(string.Format(@"{0}_NEW", dataPath)))
                        // Foreach file in data.xxx
                        for (int idx = 0; idx < filteredIndex.Count; idx++)
                        {
                            IndexEntry entry = filteredIndex[idx];

                            inFS.Seek(entry.Offset, SeekOrigin.Begin);
                            byte[] inFile = new byte[entry.Length];
                            inFS.Read(inFile, 0, entry.Length);

                            if (inFile.Length > 0)
                            {
                                UpdateEntryOffset(entry.Name, outFS.Position);
                                outFS.Write(inFile, 0, inFile.Length);
                            }
                            else
                                throw new Exception(string.Format("[RebuildDataFile] failed to buffer file from the original data file!"));

                            OnCurrentProgressChanged(new CurrentChangedArgs(idx, ""));
                        }
            }
            else
                throw new FileNotFoundException(string.Format("[RebuildDataFile] Cannot locate data file: {0}", dataPath));

            OnCurrentProgressReset(new CurrentResetArgs(true));
        }

        #endregion

        #region Misc Methods

        /// <summary>
        /// Initializes the LUA engine used to load dCore.lua configurations
        /// </summary>
        public void LoadConfig()
        {
            Extensions.ValidExtensions = luaIO.GetExtensions();
            Extensions.GroupExtensions = luaIO.GetGroupExports();
            XOR.UnencryptedExtensions = luaIO.GetUnencryptedExtensions();
        }

        /// <summary>
        /// Clears the loaded index
        /// </summary>
        public void Clear()
        {
            Index.Clear();
        }

        /// <summary>
        /// Creates a backup of the target at filepath if it exists
        /// </summary>
        /// <param name="filepath">Target file to be backed up</param>
        void createBackup(string filepath)
        {
            string bakPath = string.Format(@"{0}_NEW", filepath);
            string altBakPath = string.Format(@"{0}_OLD_{1}{2}{3}", filepath, DateTime.Now.Year,
                                                                              DateTime.Now.Month.ToString("D2"),
                                                                              DateTime.Now.Day.ToString("D2"));

            if (File.Exists(bakPath))
            {
                File.Move(bakPath, altBakPath);
                OnMessage(new MessageArgs("Previous BAK was detected and renamed.", true, 1, true, 1));
            }

            if (File.Exists(filepath))
            {
                OnMessage(new MessageArgs("Creating backup of file: {0}", Path.GetFileName(filepath)));

                using (FileStream inFS = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    int length = (int)inFS.Length;

                    OnCurrentMaxDetermined(new CurrentMaxArgs(inFS.Length));

                    using (FileStream outFS = File.Create(string.Format(@"{0}_BAK", filepath)))
                    {
                        int chunkSize = Convert.ToInt32(new FileInfo(filepath).Length * 0.02);

                        for (int byteCount = 0; byteCount < length; byteCount += Math.Min(length - byteCount, chunkSize))
                        {
                            long nextChunk = Math.Min(length - byteCount, chunkSize);
                            byte[] inChunks = new byte[nextChunk];
                            inFS.Read(inChunks, 0, inChunks.Length);
                            outFS.Write(inChunks, 0, inChunks.Length);
                            OnCurrentProgressChanged(new CurrentChangedArgs(byteCount, ""));
                        }
                    }
                }

                OnCurrentProgressReset(new CurrentResetArgs(true));
            }
            else
                throw new FileNotFoundException("Cannot backup file, original file not found!", string.Format("Original File Location: {0}", filepath));
        }

        #endregion
    }
}