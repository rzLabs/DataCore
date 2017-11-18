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
/// <version>4.0.0</version>
/// </summary>
/// TODO: Add tracking of orphaned files (maybe for a quick versioning system?)
/// TODO: Update method UpdateFileEntry/UpdateFileEntries to store updated file (whose new file gets appended) to data.blk
/// TODO: Add support for loading / displaying / saving data.blk
namespace DataCore
{
    // TODO: Add 'RemoveDuplicates' (ascii/non-ascii) <reduce client size?>
    // TODO: Add 'RebuildDataFile' function
    // TODO: Add 'CompareFiles' function (to compare external file with data file)

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
        /// Instantiates the Core by providing backup and encoding for operations
        /// </summary>
        /// <param name="backup">Determines if this core will use the backup function</param>
        /// <param name="encoding">Encoding to be applied to certain conversions</param>
        public Core(bool backup, Encoding encoding)
        {
            makeBackups = true;
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
        public List<string> ExtensionList { get { return Extensions.ValidExtensions; } }

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
        public List<IndexEntry> New(string dumpDirectory)
        {
            OnMessage(new MessageArgs("Creating new data.000...", false, 0, true, 1));

            List<IndexEntry> newIndex = new List<IndexEntry>();

            if (Directory.Exists(dumpDirectory))
            {
                string[] extDirectories = Directory.GetDirectories(dumpDirectory);

                for (int dumpDirIdx = 0; dumpDirIdx < extDirectories.Length; dumpDirIdx++)
                {
                    OnMessage(new MessageArgs(string.Format("Indexing files in directory: {0}...", extDirectories[dumpDirIdx]), true, 1));

                    string[] directoryFiles = Directory.GetFiles(extDirectories[dumpDirIdx]);

                    OnCurrentMaxDetermined(new CurrentMaxArgs(directoryFiles.Length));

                    for (int directoryFileIdx = 0; directoryFileIdx < directoryFiles.Length; directoryFileIdx++)
                    {
                        OnCurrentProgressChanged(new CurrentChangedArgs(directoryFileIdx, string.Empty));
                        Index.Add(new IndexEntry
                        {
                            Name = Path.GetFileName(directoryFiles[directoryFileIdx]),
                            Length = (int)new FileInfo(directoryFiles[directoryFileIdx]).Length,
                            DataID = StringCipher.GetID(Path.GetFileName(directoryFiles[directoryFileIdx])),
                            Offset = 0
                        });
                    }

                    OnCurrentProgressReset(new CurrentResetArgs(true));
                }

                return newIndex;
            }
            else { throw new FileNotFoundException(string.Format("[Create] Cannot locate dump directory at: {0}", dumpDirectory)); }
        }

        /// <summary>
        /// Generates a temporary data.000 index consisting of the filePaths array
        /// for further processing (This function is used for creating an index you wish to use for reference only, 
        /// not one you would save as a complete data.000)!
        /// </summary>
        /// <param name="filePaths">Array of file paths to be indexed</param>
        /// <returns>Generated index</returns>
        public List<IndexEntry> New(string[] filePaths)
        {
            List<IndexEntry> newIndex = new List<IndexEntry>(filePaths.Length);

            foreach (string filePath in filePaths)
            {
                FileInfo fileInfo = new FileInfo(filePath);
                IndexEntry indexEntry = new IndexEntry
                {
                    Name = fileInfo.Name,
                    Offset = 0,
                    Length = (int)fileInfo.Length,
                    DataID = StringCipher.GetID(fileInfo.Name)
                };
                Index.Add(indexEntry);
            }

            return newIndex;
        }

        /// <summary>
        /// Reads the data.000 contents into a List of IndexEntries (note toggling on decodeNames will decrease speed)
        /// </summary>
        /// <param name="path">Path to the data.000 index</param>
        public void Load(string path)
        {         
            byte b = 0;

            long bytesRead = 0;

            if (File.Exists(path))
            {
                DataDirectory = Path.GetDirectoryName(path);

                using (var ms = new MemoryStream(File.ReadAllBytes(path)))
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
                            Offset = BitConverter.ToInt32(value,0),
                            Length = BitConverter.ToInt32(value, 4)
                        });

                        if ((ms.Position - bytesRead) >= 50000)
                        {
                            OnCurrentProgressChanged(new CurrentChangedArgs(ms.Position, ""));
                            bytesRead = ms.Position;
                        }
                    }
                }
            }
            else { throw new FileNotFoundException(string.Format("[Load] Cannot find data.000 at path: {0}", path)); }

            OnCurrentProgressReset(new CurrentResetArgs(true));
        }

        /// <summary>
        /// Saves the provided indexList into a ready to use data.000 index
        /// </summary>
        /// <param name="buildDirectory">Location to build the new data.000 at</param>
        /// <param name="isBlankIndex">Determines if the index is a Blank Space Index</param>
        /// <returns>bool value indicating success or failure</returns>
        /// TODO: UPDATE PATH!
        public void Save(string buildDirectory)
        {
            string buildPath = string.Format(@"{0}\data.000", buildDirectory);

            if (makeBackups) { createBackup(buildPath); }

            if (File.Exists(buildPath)) { File.Delete(buildPath); }

            OnMessage(new MessageArgs("Writing new data.000..."));

            using (BinaryWriter bw = new BinaryWriter(File.Create(buildPath), Encoding.Default))
            {
                byte b = 0;

                OnCurrentMaxDetermined(new CurrentMaxArgs(Index.Count));

                for (int idx = 0; idx < Index.Count; idx++)
                {
                    IndexEntry indexEntry = Index[idx];

                    string name = StringCipher.IsEncoded(indexEntry.Name) ? indexEntry.Name : StringCipher.Encode(indexEntry.Name);
                    byte[] buffer = new byte[] { Convert.ToByte(name.Length) };
                    XOR.Cipher(ref buffer, ref b);
                    bw.Write(buffer);
                    byte[] bytes = Encoding.Default.GetBytes(name);
                    XOR.Cipher(ref bytes, ref b);
                    bw.Write(bytes);
                    byte[] array = new byte[8];
                    Buffer.BlockCopy(BitConverter.GetBytes(indexEntry.Offset), 0, array, 0, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(indexEntry.Length), 0, array, 4, 4);
                    XOR.Cipher(ref array, ref b);
                    bw.Write(array);

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
        public long GetStoredSized(List<IndexEntry> filteredList)
        {
            long size = 0;
            foreach (IndexEntry entry in filteredList) { size += entry.Length; }
            return size;
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
        /// <returns>IndexEntry of name</returns>
        public IndexEntry GetEntry(string name)
        {
            int idx = Index.FindIndex(i => i.Name == name);
            return (idx != -1) ? Index[idx] : throw new Exception(string.Format("[GetEntry(string name)] Failed to locate entry with name {0}", name));
        }

        /// <summary>
        /// Returns an IndexEntry based on it's dataId and offset
        /// </summary>
        /// <param name="dataId">data.xxx id being searched</param>
        /// <param name="offset">offset of file in dataId being searched</param>
        /// <returns>IndexEntry of dataId and offset</returns>
        public IndexEntry GetEntry(int dataId, int offset)
        {
            int idx = Index.FindIndex(i => i.DataID == dataId && i.Offset == offset);
            return (idx != -1) ? Index[idx] : throw new Exception("[GetEntry(int dataId, int offset)] Failed to locate the entry");
        }

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
        public List<IndexEntry> GetEntriesByExtension(string extension) { return Index.FindAll(i => i.Name.Contains(string.Format(".{0}", extension.ToLower()))); }

        /// <summary>
        /// Returns a filtered List of all entries matching extension
        /// </summary>
        /// <param name="extension">extension being searched (e.g. dds)</param>
        /// <param name="type">Type code for how to sort return</param>
        /// <returns>Filtered List of extension</returns>
        public List<IndexEntry> GetEntriesByExtension(string extension, SortType type)
        {
            List<IndexEntry> ret = Index.FindAll(i => i.Name.Contains(string.Format(".{0}", extension.ToLower())));

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
        public List<IndexEntry> GetEntriesByExtension(string extension, string term) { return Index.FindAll(i => i.Name.Contains(term) && i.Name.Contains(string.Format(".{0}", extension.ToLower()))); }

        /// <summary>
        /// Returns a filtered List of all entries matching extension
        /// </summary>
        /// <param name="extension">extension being searched (e.g. dds)</param>
        /// <param name="term">Term desired file names must contain</param>
        /// <param name="type">Type code for how to sort return</param>
        /// <returns>Filtered List of extension</returns>
        public List<IndexEntry> GetEntriesByExtension(string extension, string term, SortType type)
        {
            List<IndexEntry> ret = Index.FindAll(i => i.Name.Contains(term) && i.Name.Contains(string.Format(".{0}", extension.ToLower())));

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
            DeleteFileEntry(entry.DataID, entry.Offset, entry.Length);
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

        #endregion

        #region File Methods

        /// <summary>
        /// Gets the collection of bytes that makes up a given file
        /// </summary>
        /// <param name="fileName">Name of the file to generate hash for</param>
        public byte[] GetFileBytes(string fileName)
        {
            var fileEntry = GetEntry(fileName);
            return GetFileBytes(Path.GetExtension(fileName), fileEntry.DataID, fileEntry.Offset, fileEntry.Length);
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

                    // If the file has a valid extension (e.g. .dds)
                    if (Extensions.IsValid(ext))
                    {
                        using (FileStream fs = File.Open(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            fs.Seek(offset, SeekOrigin.Begin);
                            fs.Read(buffer, 0, buffer.Length);
                        }

                        // Check if this particular extension needs to be unencrypted
                        if (XOR.Encrypted(ext)) { byte b = 0; XOR.Cipher(ref buffer, ref b); }
                    }
                    else { OnWarning(new WarningArgs(string.Format("[GetFileBytes] {0} has an invalid extension!", fileName))); }         
                }
                else { throw new FileNotFoundException(string.Format(@"[GetFileBytes] Cannot locate: {0}", dataPath)); }
            }
            else { throw new Exception("[GetFileBytes] dataId is invalid! Must be between 1-8"); }

            return buffer;
        }

        /// <summary>
        /// Gets the collection of bytes that makes up a given file
        /// </summary>
        /// <param name="entry">(IndexEntry) containing information about the target file</param>
        /// <returns>Bytes of the target file</returns>
        public byte[] GetFileBytes(IndexEntry entry)
        {
            int dataId = entry.DataID;

            byte[] buffer = new byte[entry.Length];

            if (dataId > 0 && dataId < 9)
            {
                string dataPath = string.Format(@"{0}\data.00{1}", DataDirectory, dataId);

                if (File.Exists(dataPath))
                {
                    string ext = Path.GetExtension(entry.Name).Remove(0, 1).ToLower();

                    // If the file has a valid extension (e.g. .dds)
                    if (Extensions.IsValid(ext))
                    {
                        using (FileStream fs = File.Open(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            fs.Seek(entry.Offset, SeekOrigin.Begin);
                            fs.Read(buffer, 0, buffer.Length);
                        }

                        // Check if this particular extension needs to be unencrypted
                        if (XOR.Encrypted(ext)) { byte b = 0; XOR.Cipher(ref buffer, ref b); }
                    }
                    else { OnWarning(new WarningArgs(string.Format("[GetFileBytes] {0} has an invalid extension!", entry.Name))); }                 
                }
                else { throw new FileNotFoundException(string.Format(@"[GetFileBytes] Cannot locate: {0}", dataPath)); }
            }
            else { throw new Exception("[GetFileBytes] dataId is invalid! Must be between 1-8"); }

            return buffer;
        }
            
        /// <summary>
        /// Generates an SHA512 hash for the given fileName by locating the bytes in data.XXX storage
        /// </summary>
        /// <param name="fileName">Name of the file to generate hash for</param>
        /// <returns>SHA512 Hash String</returns>
        public string GetFileSHA512(string fileName)
        {
            int dataId = StringCipher.GetID(fileName);

            if (dataId > 0)
            {
                string dataPath = string.Format(@"{0}\data.00{1}", DataDirectory, dataId);

                if (File.Exists(dataPath))
                {
                    byte[] buffer = null;
                    IndexEntry fileEntry = GetEntry(fileName);

                    if (fileEntry != null)
                    {
                        using (FileStream dataFS = new FileStream(dataPath, FileMode.Open, FileAccess.Read))
                        {
                            dataFS.Position = fileEntry.Offset;
                            buffer = new byte[fileEntry.Length];
                            dataFS.Read(buffer, 0, buffer.Length);

                            if (buffer.Length > 0)
                            {
                                string ext = StringCipher.IsEncoded(fileName) ? Path.GetExtension(StringCipher.Decode(fileName)).Remove(0, 1).ToLower() : Path.GetExtension(fileName).Remove(0, 1);
                                if (XOR.Encrypted(ext)) { byte b = 0; XOR.Cipher(ref buffer, ref b); }

                                string hash = Hash.GetSHA512Hash(buffer, buffer.Length);
                                
                                if (!string.IsNullOrEmpty(hash))
                                {
                                    buffer = null;
                                    return hash;
                                }
                                else { throw new Exception(string.Format(@"[GetFileSHA512] Failed to generate hash for: {0}", fileName)); }
                            }
                            else { throw new Exception("[GetFileSHA512] Failed to read file into buffer!"); }
                        }
                    }
                    else { throw new Exception(string.Format(@"[GetFileSHA512] Failed to locate entry for: {0}", fileName)); }
                }
                else { throw new FileNotFoundException(string.Format(@"[GetFileSHA512] Cannot locate: {0}", dataPath)); }
            }
            else { throw new Exception(string.Format(@"[GetFileSHA512] dataId is 0!\nPerhaps file: {0} doesn't exist!", fileName)); }
        }

        /// <summary>
        /// Generates an SHA512 hash for the file at given offset and length
        /// </summary>
        /// <param name="dataId">Data.xxx id (e.g. 1-8)</param>
        /// <param name="offset">Start of the file in dataId</param>
        /// <param name="length">Length of the file in dataId</param>
        /// <param name="fileExt">Extension of the file</param>
        /// <returns>SHA512 Hash String</returns>
        public string GetFileSHA512(int dataId, long offset, int length, string fileExt)
        {
            if (dataId > 0 && dataId < 9)
            {
                string dataPath = string.Format(@"{0}\data.00{1}", DataDirectory, dataId);

                if (File.Exists(dataPath))
                {
                    byte[] buffer = null;

                    using (FileStream dataFS = new FileStream(dataPath, FileMode.Open, FileAccess.Read))
                    {
                        dataFS.Position = offset;
                        buffer = new byte[length];
                        dataFS.Read(buffer, 0, buffer.Length);

                        if (buffer.Length > 0)
                        {
                            if (XOR.Encrypted(fileExt)) { byte b = 0; XOR.Cipher(ref buffer, ref b); }

                            string hash = Hash.GetSHA512Hash(buffer, buffer.Length);

                            if (!string.IsNullOrEmpty(hash))
                            {
                                buffer = null;
                                return hash;
                            }
                            else { throw new Exception(string.Format(@"Failed to generate hash for file @ offset: {0} with length: {1}", offset, length)); }
                        }
                        else { throw new Exception("[GetFileSHA512] Failed to read file into buffer!"); }
                    }
                }
                else { throw new FileNotFoundException(string.Format(@"[GetFileSHA512] Cannot locate: {0}", dataPath)); }
            }
            else { throw new Exception("[GetFileSHA512] dataId is 0!\nPerhaps file doesn't exist!"); }
        }

        /// <summary>
        /// Generates an MD5 hash for the given fileName by locating the bytes in data.XXX storage
        /// </summary>
        /// <param name="fileName">Name of the file to generate hash for</param>
        /// <returns>MD5 Hash String</returns>
        public string GetFileMD5(string fileName)
        {
            int dataId = StringCipher.GetID(fileName);

            if (dataId > 0)
            {
                string dataPath = string.Format(@"{0}\data.00{1}", DataDirectory, dataId);

                if (File.Exists(dataPath))
                {
                    byte[] buffer = null;
                    IndexEntry fileEntry = GetEntry(fileName);

                    if (fileEntry != null)
                    {
                        using (FileStream dataFS = new FileStream(dataPath, FileMode.Open, FileAccess.Read))
                        {
                            dataFS.Position = fileEntry.Offset;
                            buffer = new byte[fileEntry.Length];
                            dataFS.Read(buffer, 0, buffer.Length);

                            if (buffer.Length > 0)
                            {
                                string ext = Path.GetExtension(fileName).Remove(0, 1).ToLower();
                                if (XOR.Encrypted(ext)) { byte b = 0; XOR.Cipher(ref buffer, ref b); }

                                string hash = Hash.GetMD5Hash(buffer, buffer.Length);

                                if (!string.IsNullOrEmpty(hash))
                                {
                                    buffer = null;
                                    return hash;
                                }
                                else { throw new Exception(string.Format(@"Failed to generate hash for: {0}", fileName)); }
                            }
                            else { throw new Exception("[GetFileMD5] Failed to read file into buffer!"); }
                        }
                    }
                    else { throw new Exception(string.Format(@"[GetFileMD5] Failed to locate entry for: {0}", fileName)); }
                }
                else { throw new FileNotFoundException(string.Format(@"[GetFileMD5] Cannot locate: {0}", dataPath)); }
            }
            else { throw new Exception(string.Format(@"[GetFileMD5] dataId is 0!\nPerhaps file: {0} doesn't exist!", fileName)); }
        }

        /// <summary>
        /// Generates an MD5 hash for the file at given offset and length
        /// </summary>
        /// <param name="dataId">Data.xxx id (e.g. 1-8)</param>
        /// <param name="offset">Start of the file in dataId</param>
        /// <param name="length">Length of the file in dataId</param>
        /// <param name="fileExt">Extension of the file</param>
        /// <returns>SHA512 Hash String</returns>
        public string GetFileMD5(int dataId, long offset, int length, string fileExt)
        {
            if (dataId > 0)
            {
                string dataPath = string.Format(@"{0}\data.00{1}", DataDirectory, dataId);

                if (File.Exists(dataPath))
                {
                    byte[] buffer = null;

                    using (FileStream dataFS = new FileStream(dataPath, FileMode.Open, FileAccess.Read))
                    {
                        dataFS.Position = offset;
                        buffer = new byte[length];
                        dataFS.Read(buffer, 0, buffer.Length);

                        if (buffer.Length > 0)
                        {
                            if (XOR.Encrypted(fileExt)) { byte b = 0; XOR.Cipher(ref buffer, ref b); }

                            string hash = Hash.GetMD5Hash(buffer, buffer.Length);

                            if (!string.IsNullOrEmpty(hash))
                            {
                                buffer = null;
                                return hash;
                            }
                            else { throw new Exception(string.Format(@"Failed to generate hash for file @ offset: {0} with length: {1}", offset, length)); }
                        }
                        else { throw new Exception("[GetFileMD5] Failed to read file into buffer!"); }
                    }
                }
                else { throw new FileNotFoundException(string.Format(@"[GetFileMD5] Cannot locate: {0}", dataPath)); }
            }
            else { throw new Exception("[GetFileMD5] dataId is 0!\nPerhaps file doesn't exist!"); }
        }

        /// <summary>
        /// Writes a single files from the data.xxx (specificed by dataXXX_path) to disk
        /// Note: file is written in chunks as to report progress, if chunkSize is not 
        /// defined it would default to 2% of total file size (unless n/a then it will
        /// default to 64k)
        /// </summary>
        /// <param name="dataDirectory">Location of the data.xxx files</param>
        /// <param name="buildPath">Path to create the exported file at</param>
        /// <param name="offset">Offset of the file being exported from dataXXX_path</param>
        /// <param name="length">Length of the file being exported from dataXXX_path</param>
        public void ExportFileEntry(string buildPath, long offset, int length)
        {
            string dataPath = string.Format(@"{0}\data.00{1}", DataDirectory, StringCipher.GetID(Path.GetFileName(buildPath)));

            if (File.Exists(dataPath))
            {
                string fileExt = null;
                byte[] outBuffer = null;

                // Open the housing data.xxx and read the file contents into outBuffer
                using (FileStream dataFS = new FileStream(dataPath, FileMode.Open, FileAccess.Read))
                {
                    dataFS.Position = offset;
                    outBuffer = new byte[length];
                    dataFS.Read(outBuffer, 0, length);
                }

                // IF the buffer actually contains data
                if (outBuffer.Length > 0)
                {
                    string fileName = Path.GetFileName(buildPath);
                    if (StringCipher.IsEncoded(fileName)) { fileName = StringCipher.Decode(fileName); }

                    OnCurrentMaxDetermined(new CurrentMaxArgs(length));

                    // Determine the files extension
                     fileExt = Path.GetExtension(fileName).Remove(0,1).ToLower();

                    // If the file has a valid extension (e.g. .dds)
                    if (Extensions.IsValid(fileExt))
                    {
                        // Check if this particular extension needs to be unencrypted
                        if (XOR.Encrypted(fileExt)) { byte b = 0; XOR.Cipher(ref outBuffer, ref b); }

                        using (FileStream buildFs = new FileStream(buildPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            buildFs.Write(outBuffer, 0, outBuffer.Length);
                            if (((buildFs.Position * 100) / buildFs.Length) != ((buildFs.Position - 1) * 100 / buildFs.Length)) { OnCurrentProgressChanged(new CurrentChangedArgs(buildFs.Position, string.Empty)); }
                        }

                        OnCurrentProgressReset(new CurrentResetArgs(true));
                    } 
                    else { OnWarning(new WarningArgs(string.Format("[ExportFileEntry] Skipping entry {0} with malformed extension {1}", Path.GetFileName(buildPath), fileExt))); }
                }
                else { throw new Exception("[ExportFileEntry] Failed to buffer file for export"); }

                outBuffer = null;
            }
            else { throw new FileNotFoundException(string.Format("[ExportFileEntry] Cannot locate: {0}", dataPath)); }
        }

        /// <summary>
        /// Writes/Appends a file at the filePath in(to) the Rappelz data.xxx storage system
        /// </summary>
        /// <param name="filePath">Location of the file being imported</param>
        public void ImportFileEntry(string filePath)
        {
            // If the file being imported exists
            if (File.Exists(filePath))
            {
                // Define some information about the file
                string fileName = Path.GetFileName(filePath);
                string fileExt = StringCipher.IsEncoded(fileName) ? Path.GetExtension(StringCipher.Decode(fileName)).Remove(0, 1) : Path.GetExtension(fileName).Remove(0, 1);
                long fileLen = new FileInfo(filePath).Length;
                int dataId = StringCipher.GetID(fileName);

                OnCurrentMaxDetermined(new CurrentMaxArgs(fileLen));

                // Load the file being imported into a byte array
                byte[] fileBytes = File.ReadAllBytes(filePath);

                // Determine the path of this particular file's data.xxx exists
                string dataPath = string.Format(@"{0}\data.00{1}", DataDirectory, dataId);

                if (File.Exists(dataPath))
                {
                    // Create backup (if applicable)
                    if (makeBackups) { createBackup(dataPath); }

                    // Open the housing data.xxx file
                    using (FileStream fs = new FileStream(dataPath, FileMode.Open, FileAccess.Write, FileShare.Read))
                    {
                        // Get information on the stored file (if it exists)
                        IndexEntry entry = GetEntry(fileName);

                        // If the fileBytes need to be encrypted do so
                        if (XOR.Encrypted(fileExt)) { byte b = 0; XOR.Cipher(ref fileBytes, ref b); }

                        // Set the filestreams position accordingly
                        fs.Position = (fileLen < entry.Length) ? entry.Offset : fs.Length;

                        // Write the file to the data.xxx file
                        fs.Write(fileBytes, 0, fileBytes.Length);

                        // Report the progress
                        if (((fs.Position * 100) / fs.Length) != ((fs.Position - 1) * 100 / fs.Length)) { OnCurrentProgressChanged(new CurrentChangedArgs(fs.Position, string.Empty)); }
                    }
                }
                else { throw new FileNotFoundException(string.Format("[ImportFileEntry] Cannot locate data file: {0}", dataPath)); }
            }
            else { throw new FileNotFoundException(string.Format("[ImportFileEntry] Cannot locate file: {0}", filePath)); }

            OnCurrentProgressReset(new CurrentResetArgs(true));
        }

        /// <summary>
        /// Writes/Appends a file represented by fileBytes in(to) the Rappelz data.xxx storage system with given fileName
        /// </summary>
        /// <param name="fileName">The name of the file being imported (e.g. db_item.rdb)</param>
        /// <param name="fileBytes">Bytes that represent the file</param>
        public void ImportFileEntry(string fileName, byte[] fileBytes)
        {
            // Define some information about the file
            string fileExt = StringCipher.IsEncoded(fileName) ? Path.GetExtension(StringCipher.Decode(fileName)).Remove(0, 1) : Path.GetExtension(fileName).Remove(0, 1);
            long fileLen = fileBytes.Length;
            int dataId = StringCipher.GetID(fileName);

            OnCurrentMaxDetermined(new CurrentMaxArgs(fileLen));

            // Determine the path of this particular file's data.xxx exists
            string dataPath = string.Format(@"{0}\data.00{1}", DataDirectory, dataId);

            if (File.Exists(dataPath))
            {
                // Create backup (if applicable)
                if (makeBackups) { createBackup(dataPath); }

                // Open the housing data.xxx file
                using (FileStream fs = new FileStream(dataPath, FileMode.Open, FileAccess.Write, FileShare.Read))
                {
                    // Get information on the stored file (if it exists)
                    IndexEntry entry = GetEntry(fileName);

                    // If the fileBytes need to be encrypted do so
                    if (XOR.Encrypted(fileExt)) { byte b = 0; XOR.Cipher(ref fileBytes, ref b); }

                    // Set the filestreams position accordingly
                    fs.Position = (fileLen < entry.Length) ? entry.Offset : fs.Length;

                    // Write the file to the data.xxx file
                    fs.Write(fileBytes, 0, fileBytes.Length);

                    // Report the progress
                    if (((fs.Position * 100) / fs.Length) != ((fs.Position - 1) * 100 / fs.Length)) { OnCurrentProgressChanged(new CurrentChangedArgs(fs.Position, string.Empty)); }
                }
            }
            else { throw new FileNotFoundException(string.Format("[ImportFileEntry(string fileName, byte[] fileBytes)] Cannot locate data file: {0}", dataPath)); }

            OnCurrentProgressReset(new CurrentResetArgs(true));
        }

        /// <summary>
        /// Overwrites a previous files bytes with zeros in effect erasing it
        /// </summary>
        /// <param name="dataId">Id of the data.xxx file to be altered</param>
        /// <param name="offset">Offset to begin writing zeros</param>
        /// <param name="length">How far to write zeros</param>
        public void DeleteFileEntry(int dataId, long offset, long length)
        {
            // TODO: Add proper error catching here

            // Determine the path of this particular file's data.xxx exists
            string dataPath = string.Format(@"{0}\data.00{1}", DataDirectory, dataId);

            if (makeBackups) { createBackup(dataPath); }

            using (FileStream dataFs = File.Open(dataPath, FileMode.Open, FileAccess.ReadWrite))
            {
                dataFs.Position = offset;
                using (BinaryWriter bw = new BinaryWriter(dataFs))
                {
                    bw.Write(new byte[length]);
                }
            }
        }

        /// <summary>
        /// Generates data.xxx file-system from dump structure (client/output/dump/)
        /// </summary>
        /// <param name="dumpDirectory">Location of dump folders (e.g. client/output/dump/)</param>
        /// <param name="buildDirectory">Location of build folder (e.g. client/output/data-files/)</param>
        /// <returns>Newly generated List to be saved</returns>
        /// TODO: Make assurance files don't already exist?
        public List<IndexEntry> BuildDataFiles(string dumpDirectory, string buildDirectory)
        {
            List<IndexEntry> index = null;

            if (Directory.Exists(dumpDirectory))
            {
                // Build new data.000
                string[] extDirectories = Directory.GetDirectories(dumpDirectory);

                // Create a new index of the dumpDirectory
                index = New(dumpDirectory);

                // Issue a reset to the CurrentProgressbar
                OnCurrentProgressReset(new CurrentResetArgs(true));

                // Foreach data.xxx file (1-8)
                for (int dataId = 1; dataId <= 8; dataId++)
                {
                    OnMessage(new MessageArgs(string.Format("Building data.00{0}...", dataId)));

                    // Filter down the new data.000 into the current dataId only
                    List<IndexEntry> filteredIndex = GetEntriesByDataId(index, dataId, SortType.Size);

                    OnCurrentMaxDetermined(new CurrentMaxArgs(filteredIndex.Count));

                    string buildPath = string.Format(@"{0}\data.00{1}", buildDirectory, dataId);

                    // Check if the data.xxx exists if so delete it
                    if (File.Exists(buildPath)) { File.Delete(buildPath); }

                    // Create a new filestream to write current data.xxx with
                    using (FileStream fs = new FileStream(buildPath, FileMode.Create, FileAccess.Write))
                    {
                        // Using a binarywriter write to the filestream
                        using (BinaryWriter bw = new BinaryWriter(fs))
                        {
                            // For each IndexEntry in the filteredIndex
                            for (int curFileIdx = 0; curFileIdx < filteredIndex.Count; curFileIdx++)
                            {
                                IndexEntry fileEntry = filteredIndex[curFileIdx];

                                fileEntry.Offset = fs.Position;

                                OnCurrentProgressChanged(new CurrentChangedArgs(curFileIdx, string.Empty));

                                // Determine the path to the file on the physical disk
                                string filePath = string.Format(@"{0}/{1}/{2}", dumpDirectory, Path.GetExtension(fileEntry.Name).ToUpper().Remove(0, 1), fileEntry.Name);

                                // If the file physically exists
                                if (File.Exists(filePath))
                                {
                                    byte[] fileBytes = File.ReadAllBytes(filePath);
                                    if (XOR.Encrypted(Path.GetExtension(fileEntry.Name).Remove(0, 1))) { byte b = 0; XOR.Cipher(ref fileBytes, ref b); }
                                    ((IndexEntry)index.Find(i => i.Name == fileEntry.Name)).Offset = fs.Position;
                                    bw.Write(fileBytes);
                                }
                                else { throw new FileNotFoundException(string.Format("[BuildDataFiles] Cannot locate: {0}", filePath)); }
                            }
                        }
                    }

                    OnCurrentProgressReset(new CurrentResetArgs(true));
                }
            }
            else { throw new FileNotFoundException(string.Format("[BuildDataFiles] Cannot locate dump directory at: {0}", dumpDirectory)); }

            GC.Collect();
            return index;
        }

        /// <summary>
        /// Rebuilds a data.xxx file potentially removing blank space created by the OEM update method.
        /// Effectiveness increases depending on amount of updates made to desired data.xxx file.
        /// </summary>
        /// <param name="dataId">Id of the data.xxx file to be rebuilt</param>
        /// <param name="buildDirectory">Location of build folder (e.g. client/output/data-files/)</param>
        public void RebuildDataFile(int dataId, string buildDirectory)
        {
            List<IndexEntry> filteredIndex = GetEntriesByDataId(Index, dataId, SortType.Offset);

            string dataPath = string.Format(@"{0}\data.00{1}", DataDirectory, dataId);

            if (File.Exists(dataPath))
            {
                if (makeBackups) { createBackup(dataPath); }

                OnMessage(new MessageArgs(string.Format("Writing new data.00{0}...", dataId), true, 1, false, 0));

                OnCurrentMaxDetermined(new CurrentMaxArgs(filteredIndex.Count));

                // Open the data.xxx file in inFS
                using (FileStream inFS = new FileStream(dataPath, FileMode.Open))
                {
                    // Create the output file
                    using (FileStream outFS = File.Create(string.Format(@"{0}_NEW", dataPath)))
                    {
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
                            else { throw new Exception(string.Format("[RebuildDataFile] failed to buffer file from the original data file!")); }

                            OnCurrentProgressChanged(new CurrentChangedArgs(idx, ""));
                        }
                    }
                }
            }
            else { throw new FileNotFoundException(string.Format("[RebuildDataFile] Cannot locate data file: {0}", dataPath)); }

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

        void createBackup(string dataPath)
        {
            string bakPath = string.Format(@"{0}_NEW", dataPath);
            string altBakPath = string.Format(@"{0}_OLD_{1}", dataPath, DateTime.Now.ToLongDateString());

            if (File.Exists(bakPath))
            {
                File.Move(bakPath, altBakPath);
                OnMessage(new MessageArgs("Previous BAK was detected and renamed.", true, 1, true, 1));
            }

            OnMessage(new MessageArgs("Creating backup...", true));

            using (FileStream inFS = new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int length = (int)inFS.Length;

                OnCurrentMaxDetermined(new CurrentMaxArgs(inFS.Length));

                using (FileStream outFS = File.Create(string.Format(@"{0}_BAK", dataPath)))
                {
                    int chunkSize = Convert.ToInt32(new FileInfo(dataPath).Length * 0.02);

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

        #endregion
    }
}