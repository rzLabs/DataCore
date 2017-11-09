using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DataCore.Functions;
using DataCore.Structures;

/// <summary>
/// DataCore provides portable in-depth interactibility with the Rappelz File-Management System
/// based on the works of Glandu2 and xXExiledXx two of my greatest inspirations in Rappelz Developement
/// Please report suggestions and bugs to iSmokeDrow@gmail.com
/// Reminder: This dll uses .NET 4.5.1
/// <version>3.0.0.5</version>
/// </summary>
/// TODO: Add tracking of orphaned files (maybe for a quick versioning system?)
/// TODO: Update method UpdateFileEntry/UpdateFileEntries to store updated file (whose new file gets appended) to data.blk
/// TODO: Add overridable (during construction?) validExt list
/// TODO: Add support for loading / displaying / saving data.blk
/// TODO: Add RebuildDataFile() method (will load file from data.xxx and write it directly into new data.xxx)
namespace DataCore
{
    // TODO: Add 'RemoveDuplicates' (ascii/non-ascii) <reduce client size?>
    // TODO: Add 'RebuildDataFile' function
    // TODO: Add 'CompareFiles' function (to compare external file with data file)
    // TODO: Remove useless ref param from methods

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

        public List<IndexEntry> Index;

        /// <summary>
        /// Count of IndexEntrys listed in the loaded Index
        /// </summary>
        public int RowCount { get { return Index.Count; } }

        LUA luaIO;

        #region Events
        public event EventHandler<ConsoleMessageArgs> MessageOccured;
        public event EventHandler<ErrorArgs> ErrorOccured;
        public event EventHandler<WarningArgs> WarningOccured;
        public event EventHandler<TotalMaxArgs> TotalMaxDetermined;
        public event EventHandler<TotalChangedArgs> TotalProgressChanged;
        public event EventHandler<TotalResetArgs> TotalProgressReset;
        public event EventHandler<CurrentMaxArgs> CurrentMaxDetermined;
        public event EventHandler<CurrentChangedArgs> CurrentProgressChanged;
        public event EventHandler<CurrentResetArgs> CurrentProgressReset;
        #endregion

        #region Event Delegates

        protected void OnMessage(ConsoleMessageArgs c) { MessageOccured?.Invoke(this, c); }

        /// <summary>
        /// Raises an event that informs the caller of an error that has occured
        /// </summary>
        /// <param name="e">Description of the error event ([Method-Name] Error-String)</param>
        protected void OnError(ErrorArgs e) { ErrorOccured?.Invoke(this, e); }

        /// <summary>
        /// Raises an event that informs the caller of a warning that has occured
        /// </summary>
        /// <param name="w">Description of the warning event ([Method-Name] Warning-String)</param>
        protected void OnWarning(WarningArgs w) { WarningOccured?.Invoke(this, w); }

        /// <summary>
        /// Raises an event that informs caller of current TotalProgress operations total
        /// </summary>
        /// <useage>Caller subscribes to event, uses event int as Progressbar.Total</useage>
        /// <param name="t">Total number of processes to be completed</param>
        protected void OnTotalMaxDetermined(TotalMaxArgs t) { TotalMaxDetermined?.Invoke(this, t); }

        /// <summary>
        /// Raises an event that informs the caller of total operations completed.
        /// This event can additionally deliver a string (status update) to the caller
        /// </summary>
        /// <param name="t">Current process of TotalMax</param>
        protected void OnTotalProgressChanged(TotalChangedArgs t) { TotalProgressChanged?.Invoke(this, t); }

        /// <summary>
        /// Raises an event that informs the caller that the TotalProgressbar should be reset to 0
        /// </summary>
        /// <param name="e">Dummy EventArg</param>
        protected void OnTotalProgressReset(TotalResetArgs e) { TotalProgressReset?.Invoke(this, e); }

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

        public Core(bool backup, string configPath)
        {
            makeBackups = backup;
            luaIO = new LUA(IO.LoadConfig(configPath));
        }

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

        // TODO: Rewrite the ConsoleMessageArgs class
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
            OnMessage(new ConsoleMessageArgs("Creating new data.000...", false, 0, true, 1));

            List<IndexEntry> newIndex = new List<IndexEntry>();

            if (Directory.Exists(dumpDirectory))
            {
                string[] extDirectories = Directory.GetDirectories(dumpDirectory);

                for (int dumpDirIdx = 0; dumpDirIdx < extDirectories.Length; dumpDirIdx++)
                {
                    OnMessage(new ConsoleMessageArgs(string.Format("Indexing files in directory: {0}...", extDirectories[dumpDirIdx]), true, 1));

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
            else { OnError(new ErrorArgs(string.Format("[Create] Cannot locate dump directory at: {0}", dumpDirectory))); }

            return null;
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
        /// <param name="path">Path to the data.xxx files (optional)</param>
        /// <param name="decodeNames">Determines if the file names should be decoded during load</param>
        /// <param name="reportInterval">How many bytes should be processed before reporting</param>
        /// <returns>A populated index or null</returns>
        public void Load(string path, bool decodeNames)
        {
            bool isBlank = !path.Contains(".000");

            Index = new List<IndexEntry>();

            byte b = 0;

            long bytesRead = 0;

            if (File.Exists(path))
            {
                using (var ms = new MemoryStream(File.ReadAllBytes(path)))
                {
                    OnCurrentMaxDetermined(new CurrentMaxArgs(ms.Length));

                    while (ms.Position < ms.Length)
                    {
                        IndexEntry dataIndexEntry = new IndexEntry();

                        byte[] array = new byte[1];
                        ms.Read(array, 0, array.Length);

                        XOR.Cipher(ref array, ref b);
                        byte[] bytes = new byte[array[0]];
                        ms.Read(bytes, 0, bytes.Length);
                        XOR.Cipher(ref bytes, ref b);

                        byte[] value = new byte[8];
                        ms.Read(value, 0, value.Length);
                        XOR.Cipher(ref value, ref b);

                        dataIndexEntry.Name = (decodeNames) ? StringCipher.Decode(bytes) : Encoding.Default.GetString(bytes);
                        dataIndexEntry.Offset = BitConverter.ToInt32(value, 0);
                        dataIndexEntry.Length = BitConverter.ToInt32(value, 4);
                        dataIndexEntry.DataID = StringCipher.GetID(bytes);
                        Index.Add(dataIndexEntry);

                        if ((ms.Position - bytesRead) >= 50000)
                        {
                            OnCurrentProgressChanged(new CurrentChangedArgs(ms.Position, ""));
                            bytesRead = ms.Position;
                        }
                    }
                }
            }
            else { OnError(new ErrorArgs(string.Format("[Load] Cannot find data.000 at path: {0}", path))); }

            OnCurrentProgressReset(new CurrentResetArgs(true));
        }

        /// <summary>
        /// Saves the provided indexList into a ready to use data.000 index
        /// </summary>
        /// <param name="index">Reference to data.000 index</param>
        /// <param name="buildDirectory">Location to build the new data.000 at</param>
        /// <param name="isBlankIndex">Determines if the index is a Blank Space Index</param>
        /// <returns>bool value indicating success or failure</returns>
        /// TODO: UPDATE PATH!
        public void Save(string buildDirectory, bool isBlankIndex)
        {
            string buildPath = string.Format(@"{0}\data.{1}", buildDirectory, (isBlankIndex) ? "blk" : "000");
            bool isBlank = !buildPath.Contains(".000");

            if (makeBackups) { createBackup(buildPath, 64000); }

            if (File.Exists(buildPath)) { File.Delete(buildPath); }

            OnMessage(new ConsoleMessageArgs("Writing new data.000..."));

            using (BinaryWriter bw = new BinaryWriter(File.Create(buildPath), Encoding.Default))
            {
                byte b = 0;

                OnCurrentMaxDetermined(new CurrentMaxArgs(Index.Count));

                for (int idx = 0; idx < Index.Count; idx++)
                {
                    IndexEntry indexEntry = Index[idx];

                    string name = IsEncoded(indexEntry.Name) ? indexEntry.Name : EncodeName(indexEntry.Name);
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
        /// Returns a bool indicating if the matching entry exists in the referenced index
        /// </summary>
        /// <param name="index">Reference to data.000 index</param>
        /// <param name="name">File name being searched for</param>
        /// <returns>true or false</returns>
        public bool EntryExists(string name) { return Index.Find(i => i.Name == name) != null; }

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
        public IndexEntry GetEntry(int index) { return this.Index[index]; }

        /// <summary>
        /// Returns an IndexEntry based on it's [UNHASHED] name
        /// </summary>
        /// <param name="index">Reference to data.000 index</param>
        /// <param name="name">File name being searched for</param>
        /// <returns>IndexEntry of name</returns>
        public IndexEntry GetEntry(string name) { return Index.Find(i => i.Name == name); }

        /// <summary>
        /// Returns an IndexEntry based on it's dataId and offset
        /// </summary>
        /// <param name="index">Reference to data.000 List</param>
        /// <param name="dataId">data.xxx id being searched</param>
        /// <param name="offset">offset of file in dataId being searched</param>
        /// <returns>IndexEntry of dataId and offset</returns>
        public IndexEntry GetEntry(int dataId, int offset) { return Index.Find(i => i.DataID == dataId && i.Offset == offset); }

        /// <summary>
        /// Returns a blankIndex entry with an AvailableSpace betweem the minimum and maximum sizes (if one exists)
        /// </summary>
        /// <param name="blankIndex">Reference to the data.blk index</param>
        /// <param name="dataId">data.xxx Id being requested</param>
        /// <param name="minSize">Minimum size of blank space</param>
        /// <param name="maxSize">Maximum size of blank space</param>
        /// <returns>Populated BlankIndexEntry (if one exists)</returns>
        public IndexEntry GetClosestEntry(List<IndexEntry> blankIndex, int dataId, int minSize, int maxSize)
        {
            // Set lastSmallest to maximum desired size
            int lastSmallest = maxSize;

            // Define a placeholder BlankIndexEntry
            IndexEntry returnEntry = null;

            // Search for all BlankIndexEntries that are in the desired dataId, are above the minSize and below the maxSize
            List<IndexEntry> result = blankIndex.FindAll(i => i.DataID == dataId && i.Length >= minSize && i.Length <= maxSize);

            // Loop through results to find closest match
            foreach (IndexEntry blankIndexEntry in result)
            {
                // If current entry has AvailableSpace smaller than the last accepted entry (or maxSize)
                if (blankIndexEntry.Length <= lastSmallest)
                {
                    // Set the lastSmallest value to current entry AvailableSpace
                    lastSmallest = blankIndexEntry.Length;

                    // Set returnEntry
                    returnEntry = blankIndexEntry;
                }
            }

            return returnEntry;
        }

        /// <summary>
        /// Returns a List of all entries whose name contains partialName
        /// </summary>
        /// <param name="index">Reference to data.000 index</param>
        /// <param name="partialName">Partial fileName (e.g. db_) to be searched for</param>
        /// <returns>Populated List of IndexEntries</returns>
        public List<IndexEntry> GetEntriesByPartialName(string partialName) { return Index.FindAll(i => i.Name.Contains(partialName)); }

        /// <summary>
        /// Returns a List of all entries matching dataId
        /// </summary>
        /// <param name="index">Reference to data.000 index</param>
        /// <param name="dataId">data.xxx Id being requested</param>
        /// <returns>List for data.xx{dataId}</returns>
        public List<IndexEntry> GetEntriesByDataId(int dataId) { return Index.FindAll(i => i.DataID == dataId); }

        /// <summary>
        /// Returns a filtered List of all entries matching dataId
        /// Return is sorted by sortType
        /// </summary>
        /// <param name="index">Reference to data.000 index</param>
        /// <param name="dataId">data.xxx Id being requested</param>
        /// <param name="sortType">Type code for how to sort return</param>
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
        /// <param name="sortType">Type code for how to sort return</param>
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
        /// <param name="index">data.000 index being searched</param>
        /// <param name="extension">extension being searched (e.g. dds)</param>
        /// <returns>Filtered List of extension</returns>
        public List<IndexEntry> GetEntriesByExtension(string extension) { return Index.FindAll(i => i.Name.Contains(string.Format(".{0}", extension.ToLower()))); }

        /// <summary>
        /// Returns a filtered List of all entries matching extension
        /// </summary>
        /// <param name="index">data.000 index being searched</param>
        /// <param name="extension">extension being searched (e.g. dds)</param>
        /// <param name="sortType">Type code for how to sort return</param>
        /// LEGEND:
        /// 0 = Name
        /// 1 = Offset
        /// 2 = Size
        /// <returns>Filtered List of extension</returns>
        public List<IndexEntry> GetEntriesByExtension(string extension, int sortType)
        {
            switch (sortType)
            {
                case 0: // Name
                    return Index.FindAll(i => i.Name.Contains(string.Format(".{0}", extension.ToLower()))).OrderBy(i => i.Name).ToList();

                case 1: // Offset
                    return Index.FindAll(i => i.Name.Contains(string.Format(".{0}", extension.ToLower()))).OrderBy(i => i.Offset).ToList();

                case 2: // Size
                    return Index.FindAll(i => i.Name.Contains(string.Format(".{0}", extension.ToLower()))).OrderBy(i => i.Length).ToList();

                case 3: // dataId
                    return Index.FindAll(i => i.Name.Contains(string.Format(".{0}", extension.ToLower()))).OrderBy(i => i.DataID).ToList();
            }

            return null;
        }

        /// <summary>
        /// Removes a set of entries bearing DataID = dataId from referenced data.000 index
        /// </summary>
        /// <param name="index">Index to be altered</param>
        /// <param name="dataId">Id of file entries to be deleted</param>
        public void DeleteEntriesByDataId(int dataId) { Index.RemoveAll(i => i.DataID == dataId); }

        // TODO: Update this method to not be so stupid!
        /// <summary>
        /// Removes a single entry bearing Name = name from referenced data.000 index
        /// </summary>
        /// <param name="index">Index to be altered</param>
        /// <param name="fileName">Name of the IndexEntry being deleted</param>
        /// <param name="dataDirectory">Directory of the data.xxx files</param>
        /// <param name="dataId">ID of the data.xxx file housing this file</param>
        /// <param name="offset">Offset of the file being deleted</param>
        /// <param name="length">Length of the file being deleted</param>
        /// <param name="chunkSize">Amount of bytes to be processed before reporting</param>
        public void DeleteEntryByName(string fileName, string dataDirectory, int dataId, long offset, long length, int chunkSize)
        {
            DeleteFileEntry(dataDirectory, dataId, offset, length, chunkSize);
            Index.Remove(Index.Find(i => i.Name == fileName));
        }

        /// <summary>
        /// Removes a single entry bearing DataID = id and Offset = offset from referenced data.000 index
        /// </summary>
        /// <param name="index">Index to be altered</param>
        /// <param name="id">DataID of file entry to be deleted</param>
        /// <param name="offset">Offset of file entry to be deleted</param>
        public void DeleteEntryByIdandOffset(int id, int offset) { Index.Remove(Index.Find(i => i.DataID == id && i.Offset == offset)); }

        /// <summary>
        /// Updates the offset for IndexEntry with given fileName in the referenced index
        /// </summary>
        /// <param name="index">Index to be altered</param>
        /// <param name="fileName">Name of the IndexEntry being updated</param>
        /// <param name="offset">New offset for the IndexEntry</param>
        public void UpdateEntryOffset(string fileName, long offset)
        {
            try { Index.Find(i => i.Name == fileName).Offset = offset; }
            catch (Exception ex) { OnError(new ErrorArgs(ex.Message)); }
        }

        #endregion

        #region File Methods

        /// <summary>
        /// Gets the collection of bytes that makes up a given file
        /// </summary>
        /// <param name="dataDirectory">Directory of the Data.XXX files</param>
        /// <param name="dataId">ID of the target data.xxx</param>
        /// <param name="offset">Offset of the target file</param>
        /// <param name="length">Length of the target file</param>
        /// <returns>Bytes of the target file</returns>
        public byte[] GetFileBytes(string dataDirectory, int dataId, long offset, long length)
        {
            byte[] buffer = new byte[length];

            if (dataId > 0 && dataId < 9)
            {
                string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, dataId);

                if (File.Exists(dataPath))
                {
                    using (FileStream fs = File.Open(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        fs.Seek(offset, SeekOrigin.Begin);
                        fs.Read(buffer, 0, buffer.Length);
                    }
                }
                else { OnError(new ErrorArgs(string.Format(@"[GetFileBytes] Cannot locate: {0}", dataPath))); }
            }
            else { OnError(new ErrorArgs("[GetFileBytes] dataId is invalid! Must be between 1-8")); }

            return buffer;
        }

        /// <summary>
        /// Gets the collection of bytes that makes up a given file
        /// </summary>
        /// <param name="dataDirectory">Directory of the Data.XXX files</param>
        /// <param name="entry">(IndexEntry) containing information about the target file</param>
        /// <returns>Bytes of the target file</returns>
        public byte[] GetFileBytes(string dataDirectory, IndexEntry entry)
        {
            int dataId = entry.DataID;

            byte[] buffer = new byte[entry.Length];

            if (dataId > 0 && dataId < 9)
            {
                string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, dataId);

                if (File.Exists(dataPath))
                {
                    using (FileStream fs = File.Open(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        fs.Seek(entry.Offset, SeekOrigin.Begin);
                        fs.Read(buffer, 0, buffer.Length);
                    }
                }
                else { OnError(new ErrorArgs(string.Format(@"[GetFileBytes] Cannot locate: {0}", dataPath))); }
            }
            else { OnError(new ErrorArgs("[GetFileBytes] dataId is invalid! Must be between 1-8")); }

            return buffer;
        }
            
        /// <summary>
        /// Generates an SHA512 hash for the given fileName by locating the bytes in data.XXX storage
        /// </summary>
        /// <param name="index">Reference to loaded data.000 index</param>
        /// <param name="dataDirectory">Location of the data.xxx files</param>
        /// <param name="fileName">Name of the file to generate hash for</param>
        /// <returns>SHA512 Hash String</returns>
        public string GetFileSHA512(string dataDirectory, string fileName)
        {

            int dataId = StringCipher.GetID(fileName);

            if (dataId > 0)
            {
                string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, dataId);

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
                                string ext = IsEncoded(fileName) ? Path.GetExtension(DecodeName(fileName)).Remove(0, 1).ToLower() : Path.GetExtension(fileName).Remove(0, 1);
                                if (XOR.Encrypted(ext)) { byte b = 0; XOR.Cipher(ref buffer, ref b); }

                                string hash = Hash.GetSHA512Hash(buffer, buffer.Length);
                                
                                if (!string.IsNullOrEmpty(hash))
                                {
                                    buffer = null;
                                    return hash;
                                }
                                else { OnError(new ErrorArgs(string.Format(@"Failed to generate hash for: {0}", fileName))); }
                            }
                            else { OnError(new ErrorArgs("[GetFileSHA512] Failed to read file into buffer!")); }
                        }
                    }
                    else { OnError(new ErrorArgs(string.Format(@"[GetFileSHA512] Failed to locate entry for: {0}", fileName))); }
                }
                else { OnError(new ErrorArgs(string.Format(@"[GetFileSHA512] Cannot locate: {0}", dataPath))); }
            }
            else { OnError(new ErrorArgs(string.Format(@"[GetFileSHA512] dataId is 0!\nPerhaps file: {0} doesn't exist!", fileName))); }

            return null;
        }

        /// <summary>
        /// Generates an SHA512 hash for the file at given offset and length
        /// </summary>
        /// <param name="dataDirectory">Location of the data.xxx files</param>
        /// <param name="dataId">Data.xxx id (e.g. 1-8)</param>
        /// <param name="offset">Start of the file in dataId</param>
        /// <param name="length">Length of the file in dataId</param>
        /// <param name="fileExt">Extension of the file</param>
        /// <returns>SHA512 Hash String</returns>
        public string GetFileSHA512(string dataDirectory, int dataId, long offset, int length, string fileExt)
        {
            if (dataId > 0 && dataId < 9)
            {
                string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, dataId);

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
                            else { OnError(new ErrorArgs(string.Format(@"Failed to generate hash for file @ offset: {0} with length: {1}", offset, length))); }
                        }
                        else { OnError(new ErrorArgs("[GetFileSHA512] Failed to read file into buffer!")); }
                    }
                }
                else { OnError(new ErrorArgs(string.Format(@"[GetFileSHA512] Cannot locate: {0}", dataPath))); }
            }
            else { OnError(new ErrorArgs("[GetFileSHA512] dataId is 0!\nPerhaps file doesn't exist!")); }

            return null;
        }

        /// <summary>
        /// Generates an MD5 hash for the given fileName by locating the bytes in data.XXX storage
        /// </summary>
        /// <param name="index">Reference to loaded data.000 index</param>
        /// <param name="dataDirectory">Location of the data.xxx files</param>
        /// <param name="fileName">Name of the file to generate hash for</param>
        /// <returns>MD5 Hash String</returns>
        public string GetFileMD5(string dataDirectory, string fileName)
        {
            int dataId = StringCipher.GetID(fileName);

            if (dataId > 0)
            {
                string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, dataId);

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
                                else { OnError(new ErrorArgs(string.Format(@"Failed to generate hash for: {0}", fileName))); }
                            }
                            else { OnError(new ErrorArgs("[GetFileMD5] Failed to read file into buffer!")); }
                        }
                    }
                    else { OnError(new ErrorArgs(string.Format(@"[GetFileMD5] Failed to locate entry for: {0}", fileName))); }
                }
                else { OnError(new ErrorArgs(string.Format(@"[GetFileMD5] Cannot locate: {0}", dataPath))); }
            }
            else { OnError(new ErrorArgs(string.Format(@"[GetFileMD5] dataId is 0!\nPerhaps file: {0} doesn't exist!", fileName))); }

            return null;
        }

        /// <summary>
        /// Generates an MD5 hash for the file at given offset and length
        /// </summary>
        /// <param name="dataDirectory">Location of the data.xxx files</param>
        /// <param name="dataId">Data.xxx id (e.g. 1-8)</param>
        /// <param name="offset">Start of the file in dataId</param>
        /// <param name="length">Length of the file in dataId</param>
        /// <param name="fileExt">Extension of the file</param>
        /// <returns>SHA512 Hash String</returns>
        public string GetFileMD5(string dataDirectory, int dataId, long offset, int length, string fileExt)
        {
            if (dataId > 0)
            {
                string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, dataId);

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
                            else { OnError(new ErrorArgs(string.Format(@"Failed to generate hash for file @ offset: {0} with length: {1}", offset, length))); }
                        }
                        else { OnError(new ErrorArgs("[GetFileMD5] Failed to read file into buffer!")); }
                    }
                }
                else { OnError(new ErrorArgs(string.Format(@"[GetFileMD5] Cannot locate: {0}", dataPath))); }
            }
            else { OnError(new ErrorArgs("[GetFileMD5] dataId is 0!\nPerhaps file doesn't exist!")); }

            return null;
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
        /// <param name="chunkSize">Size (in bytes) to process each iteration of the write loop</param>
        public void ExportFileEntry(string dataDirectory, string buildPath, long offset, int length, int chunkSize)
        {
            string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, StringCipher.GetID(Path.GetFileName(buildPath)));

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
                    if (IsEncoded(fileName)) { fileName = DecodeName(fileName); }

                    OnCurrentMaxDetermined(new CurrentMaxArgs(length));

                    // Determine the files extension
                     fileExt = Path.GetExtension(fileName).Remove(0,1).ToLower();

                    // If the file has a valid extension (e.g. .dds)
                    if (Extensions.IsValid(fileExt))
                    {
                        // Check if this particular extension needs to be unencrypted
                        if (XOR.Encrypted(fileExt)) { byte b = 0; XOR.Cipher(ref outBuffer, ref b); }

                        // If no chunkSize is provided, generate default
                        if (chunkSize == 0) { chunkSize = Math.Max(64000, (int)(outBuffer.Length * .02)); }

                        using (FileStream buildFs = new FileStream(buildPath, FileMode.Create, FileAccess.Write))
                        {
                            using (BinaryWriter bw = new BinaryWriter(buildFs, encoding))
                            {
                                for (int byteCount = 0; byteCount < length; byteCount += Math.Min(length - byteCount, chunkSize))
                                {
                                    bw.Write(outBuffer, byteCount, Math.Min(length - byteCount, chunkSize));
                                    OnCurrentProgressChanged(new CurrentChangedArgs(byteCount, ""));
                                }
                            }
                        }

                        OnCurrentProgressReset(new CurrentResetArgs(true));
                    }
                    else { OnWarning(new WarningArgs(string.Format("[ExportFileEntry] Skipping entry {0} with malformed extension {1}", Path.GetFileName(buildPath), fileExt))); }
                }
                else { OnError(new ErrorArgs("[ExportFileEntry] Failed to buffer file for export!")); }

                outBuffer = null;
            }
            else { OnError(new ErrorArgs(string.Format("[ExportFileEntry] Cannot locate: {0}", dataPath))); }
        }

        /// <summary>
        /// Writes multiple files from the data.xxx identified by the IndexEntry to disk
        /// </summary>
        /// <param name="filteredIndex">data.000 index of entries to export</param>
        /// <param name="dataDirectory">Location of the data.xxx files</param>
        /// <param name="buildDirectory">Location to export files</param>
        /// <param name="chunkSize">Size (in bytes) to process each iteration of the write loop</param>
        public void ExportFileEntries(List<IndexEntry> filteredIndex, string dataDirectory, string buildDirectory, int chunkSize)
        {
            OnTotalMaxDetermined(new TotalMaxArgs(8, true));

            // For each set of dataId files in the filteredIndex
            for (int dataId = 1; dataId <= 8; dataId++)
            {
                // Filter only entries with current dataId into temp index
                List<IndexEntry> tempIndex = GetEntriesByDataId(filteredIndex, dataId, SortType.Offset);

                if (tempIndex.Count > 0)
                {
                    OnTotalProgressChanged(new TotalChangedArgs(dataId, string.Format("Exporting selected files from data.00{0}", dataId)));

                    // Determine the path of the data.xxx file being exported from
                    string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, dataId);

                    if (File.Exists(dataPath))
                    {
                        // Load the data.xxx into filestream
                        using (FileStream dataFs = new FileStream(dataPath, FileMode.Open, FileAccess.Read))
                        {
                            OnCurrentMaxDetermined(new CurrentMaxArgs(tempIndex.Count));

                            // Loop through filex to export
                            for (int entryIdx = 0; entryIdx < tempIndex.Count; entryIdx++)
                            {
                                IndexEntry indexEntry = tempIndex[entryIdx];

                                OnCurrentProgressChanged(new CurrentChangedArgs(entryIdx, string.Format("Exporting file {0}", indexEntry.Name)));

                                int fileLength = indexEntry.Length;

                                // Set the filestreams position to the file entries offset
                                dataFs.Position = indexEntry.Offset;

                                // Read the file into a byte array (buffer)
                                byte[] outBuffer = new byte[indexEntry.Length];
                                dataFs.Read(outBuffer, 0, outBuffer.Length);

                                // Define some information about the file being exported
                                string fileExt = Path.GetExtension(indexEntry.Name).Remove(0, 1).ToLower();
                                string buildPath = string.Format(@"{0}\{1}\{2}", buildDirectory, fileExt.ToUpper(), indexEntry.Name);

                                if (Extensions.IsValid(fileExt))
                                {
                                    // If needed unencrypt the data (fileBytes buffer)
                                    if (XOR.Encrypted(fileExt)) { byte b = 0; XOR.Cipher(ref outBuffer, ref b); }

                                    // If no chunkSize is provided, generate default
                                    if (chunkSize == 0) { chunkSize = Math.Max(64000, (int)(outBuffer.Length * .02)); }

                                    // If the build directory doesn't exist yet, create it.
                                    if (!Directory.Exists(Path.GetDirectoryName(buildPath))) { Directory.CreateDirectory(Path.GetDirectoryName(buildPath)); }

                                    // Using a filestream create the file at the buildPath
                                    using (FileStream buildFs = new FileStream(buildPath, FileMode.Create, FileAccess.Write))
                                    {
                                        // Using a binarywriter write to the previously created filestream
                                        using (BinaryWriter bw = new BinaryWriter(buildFs, encoding))
                                        {
                                            for (int byteCount = 0; byteCount < fileLength; byteCount += Math.Min(fileLength - byteCount, chunkSize))
                                            {
                                                bw.Write(outBuffer, byteCount, Math.Min(fileLength - byteCount, chunkSize));
                                            }
                                        }
                                    }

                                    outBuffer = null;
                                }
                                else { OnWarning(new WarningArgs(string.Format("[ExportFileEntries] Skipping entry {0} with malformed extension {1}", Path.GetFileName(buildPath), fileExt))); }

                                OnCurrentProgressReset(new CurrentResetArgs(true));
                            }
                        }
                    }
                    else { OnError(new ErrorArgs(string.Format("[ExportFileEntries] Cannot locate: {0}", dataPath))); }
                }
            }

            OnTotalProgressReset(new TotalResetArgs(false));

            GC.Collect();
        }

        /// <summary>
        /// Updates the dataDirectory data.xxx stored copy of the physical file at filePath
        /// </summary>
        /// <param name="index">Reference to data.000 index to be updated</param>
        /// <param name="dataDirectory">Location of the data.xxx files</param>
        /// <param name="filePath">Location of the file being imported</param>
        /// <param name="chunkSize">Size (in bytes) to process each iteration of the write loop</param>
        public void UpdateFileEntry(List<IndexEntry> index, string dataDirectory, string filePath, int chunkSize)
        {
            // Define some temporary information about the file being imported
            string fileName = Path.GetFileName(filePath);
            string fileExt;

            // Check if fileName is encoded and decode (if needed) and determine fileName's extension
            bool isEncoded = StringCipher.IsEncoded(index[0].Name);
            if (!isEncoded)
            {
                fileName = StringCipher.Decode(fileName);
                fileExt = Path.GetExtension(fileName.Remove(0, 1));
            }
            else { fileExt = Path.GetExtension(fileName.Remove(0, 1)); }

            //OnWarning(new WarningArgs(string.Format("IsEncoded: {0} | fileName: {1}", isEncoded.ToString(), index[0].Name)));

            // Determine the path to the current files appropriate data.xxx
            string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, StringCipher.GetID(fileName));

            // If the physical data.xxx actually exists
            if (File.Exists(dataPath))
            {
                //OnWarning(new WarningArgs(string.Format("FileName: {0}", fileName)));

                // Find the matching entry for the file (if existing)
                IndexEntry indexEntry = index.Find(i => i.Name == fileName);

                // If the indexEntry exists in the referenced data.000 index
                if (indexEntry != null)
                {
                    // Open it in a file-stream for manipulation
                    using (FileStream fs = new FileStream(dataPath, FileMode.Open, FileAccess.Write))
                    {
                        // Using a binarywriter write to the filestream with appropriate encoding
                        using (BinaryWriter bw = new BinaryWriter(fs, encoding))
                        {
                            // Create a byte[] array of the filePaths length for storing the file
                            byte[] fileBytes = new byte[new FileInfo(filePath).Length];

                            // Inform the caller of progress count for this operation
                            OnCurrentMaxDetermined(new CurrentMaxArgs(fileBytes.Length));
                            
                            // Determine if the file should be added to the end of it's housing .xxx
                            if (fileBytes.Length > indexEntry.Length)
                            {
                                // Set position to eof
                                fs.Position = fs.Length;
                                // Update the original index entry with the new files offset
                                indexEntry.Offset = fs.Position;
                            }
                            else { fs.Position = indexEntry.Offset; }

                            // Update the entries length in the referenced data.000 index
                            indexEntry.Length = fileBytes.Length;

                            // Buffer the file into byte array and write it to fs
                            fileBytes = File.ReadAllBytes(filePath);

                            // If the fileBytes need to be encrypted do so
                            if (XOR.Encrypted(fileExt.ToUpper())) { byte b = 0; XOR.Cipher(ref fileBytes, ref b); }

                            // If no chunkSize is provided, generate default
                            if (chunkSize == 0) { chunkSize = Math.Max(64000, (int)(fileBytes.Length * .02)); }

                            // Iterate through the fileByte array writing the chunkSize to fs and reporting current position in
                            // the array to the caller via CurrentProgress events
                            for (int byteCount = 0; byteCount < fileBytes.Length; byteCount += Math.Min(fileBytes.Length - byteCount, chunkSize))
                            {
                                bw.Write(fileBytes, byteCount, Math.Min(fileBytes.Length - byteCount, chunkSize));
                                OnCurrentProgressChanged(new CurrentChangedArgs(byteCount, ""));
                            }

                            fileBytes = null;

                            // Issue progress reset event
                            OnCurrentProgressReset(new CurrentResetArgs(true));
                        }
                    }
                }
                else { OnError(new ErrorArgs(string.Format("[UpdateFileEntry] Cannot locate entry for {0} in referenced index", fileName))); }
            }
            else { OnError(new ErrorArgs(string.Format("[UpdateFileEntry] Cannot locate: {0}", dataPath))); }
        }

        // Create overload that accepts reference to data.lgy for catalouging the blanked space
        /// <summary>
        /// Updates the dataDirectory data.xxx stored copy of the physical file at filePath
        /// </summary>
        /// <param name="index">Reference to data.000 index to be updated</param>
        /// <param name="blankIndex">Reference to data.lgy index to be updated</param>
        /// <param name="dataDirectory">Location of the data.xxx files</param>
        /// <param name="filePath">Location of the file being imported</param>
        /// <param name="chunkSize">Size (in bytes) to process each iteration of the write loop</param>
        public void UpdateFileEntry(List<IndexEntry> index, ref List<IndexEntry> blankIndex, string dataDirectory, string filePath, int chunkSize)
        {
            // Define some temporary information about the file being imported
            string fileName = Path.GetFileName(filePath);
            string decodedFileName = string.Empty;
            string fileExt;

            // Check if fileName is encoded and decode (if needed) and determine fileName's extension
            bool isEncoded = StringCipher.IsEncoded(fileName);
            if (isEncoded)
            {
                decodedFileName = StringCipher.Decode(fileName);
                fileExt = Path.GetExtension(decodedFileName.Remove(0, 1));
            }
            else { fileExt = Path.GetExtension(fileName.Remove(0, 1)); }

            // Determine the path to the current files appropriate data.xxx
            string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, StringCipher.GetID(fileName));

            // If the physical data.xxx actually exists
            if (File.Exists(dataPath))
            {
                // Find the matching entry for the file (if existing)
                IndexEntry indexEntry = index.Find(i => i.Name == fileName);

                // If the indexEntry exists in the referenced data.000 index
                if (indexEntry != null)
                {
                    // Open it in a file-stream for manipulation
                    using (FileStream fs = new FileStream(dataPath, FileMode.Open, FileAccess.Write))
                    {
                        // Using a binarywriter write to the filestream with appropriate encoding
                        using (BinaryWriter bw = new BinaryWriter(fs, encoding))
                        {
                            // Create a byte[] array of the filePaths length for storing the file
                            byte[] fileBytes = new byte[new FileInfo(filePath).Length];

                            // Inform the caller of progress count for this operation
                            OnCurrentMaxDetermined(new CurrentMaxArgs(fileBytes.Length));

                            // Determine if the file should be added to the end of it's housing .xxx
                            if (fileBytes.Length > indexEntry.Length)
                            {
                                // Record information about the file being orphaned and add it to the blankIndex
                                blankIndex.Add(new IndexEntry
                                {
                                    Name = string.Format("{0}_{1}", indexEntry.Name, DateTime.Now),
                                    Length = indexEntry.Length,
                                    Offset = indexEntry.Offset,
                                    DataID = indexEntry.DataID
                                });

                                // Set position to eof
                                fs.Position = fs.Length;

                                // Update the original index entry with the new files offset
                                indexEntry.Offset = fs.Position;
                            }
                            else { fs.Position = indexEntry.Offset; }

                            // Update the entries length in the referenced data.000 index
                            indexEntry.Length = fileBytes.Length;

                            // Buffer the file into byte array and write it to fs
                            fileBytes = File.ReadAllBytes(filePath);

                            // If the fileBytes need to be encrypted do so
                            if (XOR.Encrypted(fileExt.ToUpper())) { byte b = 0; XOR.Cipher(ref fileBytes, ref b); }

                            // If no chunkSize is provided, generate default
                            if (chunkSize == 0) { chunkSize = Math.Max(64000, (int)(fileBytes.Length * .02)); }

                            // Iterate through the fileByte array writing the chunkSize to fs and reporting current position in
                            // the array to the caller via CurrentProgress events
                            for (int byteCount = 0; byteCount < fileBytes.Length; byteCount += Math.Min(fileBytes.Length - byteCount, chunkSize))
                            {
                                bw.Write(fileBytes, byteCount, Math.Min(fileBytes.Length - byteCount, chunkSize));
                                OnCurrentProgressChanged(new CurrentChangedArgs(byteCount, ""));
                            }

                            fileBytes = null;

                            // Issue progress reset event
                            OnCurrentProgressReset(new CurrentResetArgs(true));
                        }
                    }
                }
                else { OnError(new ErrorArgs(string.Format("[UpdateFileEntry] Cannot locate entry for {0} in referenced index", fileName))); }
            }
            else { OnError(new ErrorArgs(string.Format("[UpdateFileEntry] Cannot locate: {0}", dataPath))); }
        }

        /// <summary>
        /// Creates a file entry that does not exist in the referenced data.000 index
        /// </summary>
        /// <param name="index">Reference to data.000 index to be updated</param>
        /// <param name="dataDirectory">Location of the data.xxx files</param>
        /// <param name="filePath">Location of the file being imported</param>
        /// <param name="chunkSize">Size (in bytes) to process each iteration of the write loop</param>
        public void ImportFileEntry(List<IndexEntry> index, string dataDirectory, string filePath, int chunkSize)
        {
            // If the file being imported exists
            if (File.Exists(filePath))
            {
                // Define some information about the file
                string fileName = Path.GetFileName(filePath);
                string fileExt = IsEncoded(fileName) ? Path.GetExtension(DecodeName(fileName)).Remove(0, 1) : Path.GetExtension(fileName).Remove(0, 1);
                long fileLen = new FileInfo(filePath).Length;
                int dataId = StringCipher.GetID(fileName);

                OnCurrentMaxDetermined(new CurrentMaxArgs(fileLen));

                // Check if the indexEntry already exists
                if (index.Find(i => i.Name == fileName) == null)
                {
                    // Determine the path of this particular file's data.xxx exists
                    string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, dataId);

                    // If the data.xxx file exists
                    if (File.Exists(dataPath))
                    {
                        if (makeBackups) { createBackup(dataPath, chunkSize); }

                        // Using a filestream open the data.xxx file with write access
                        using (FileStream fs = new FileStream(dataPath, FileMode.Open, FileAccess.Write))
                        {
                            // Load the file being imported into a byte array
                            byte[] fileBytes = File.ReadAllBytes(filePath);

                            // Determine the offset of the file
                            long offset = fs.Length;

                            // Set the filestreams position to the end of the data.xxx file
                            fs.Position = offset;

                            // If the fileBytes need to be encrypted do so
                            if (XOR.Encrypted(fileExt)) { byte b = 0; XOR.Cipher(ref fileBytes, ref b); }

                            // If no chunkSize is provided, generate default
                            if (chunkSize == 0) { chunkSize = Math.Min(64000, (int)(fileBytes.Length * .02)); }

                            using (BinaryWriter bw = new BinaryWriter(fs, encoding))
                            {
                                // Iterate through the fileByte array writing the chunkSize to fs and reporting current position in
                                // the array to the caller via CurrentProgress events
                                for (int byteCount = 0; byteCount < fileBytes.Length; byteCount += Math.Min(fileBytes.Length - byteCount, chunkSize))
                                {
                                    bw.Write(fileBytes, byteCount, Math.Min(fileBytes.Length - byteCount, chunkSize));
                                    OnCurrentProgressChanged(new CurrentChangedArgs(byteCount, ""));
                                }
                            }

                            // Add the new indexEntry to the referenced data.000 index
                            index.Add(new IndexEntry
                            {
                                Name = StringCipher.Encode(fileName),
                                Offset = offset,
                                Length = (int)fileLen,
                                DataID = dataId
                            });

                            fileBytes = null;
                        }
                    }
                    else { OnError(new ErrorArgs(string.Format("[ImportFileEntry] Cannot locate data file: {0}", dataPath))); }
                }
                else { OnError(new ErrorArgs(string.Format("[ImportFileEntry] File entry {0} already exists!\n\nTry UpdateFileEntry instead!", fileName))); }
            }
            else { OnError(new ErrorArgs(string.Format("[ImportFileEntry] Cannot locate file: {0}", filePath))); }

            OnCurrentProgressReset(new CurrentResetArgs(true));
        }

        /// <summary>
        /// Overwrites a previous files bytes with zeros in effect erasing it
        /// </summary>
        /// <param name="dataDirectory">Location of the data.xxx files</param>
        /// <param name="dataId">Id of the data.xxx file to be altered</param>
        /// <param name="offset">Offset to begin writing zeros</param>
        /// <param name="length">How far to write zeros</param>
        public void DeleteFileEntry(string dataDirectory, int dataId, long offset, long length, int chunkSize)
        {
            // Determine the path of this particular file's data.xxx exists
            string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, dataId);

            if (makeBackups) { createBackup(dataPath, chunkSize); }

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
                    OnMessage(new ConsoleMessageArgs(string.Format("Building data.00{0}...", dataId)));

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
                                else { OnError(new ErrorArgs(string.Format("[BuildDataFiles] Cannot locate: {0}", filePath))); }
                            }
                        }
                    }

                    OnCurrentProgressReset(new CurrentResetArgs(true));
                }
            }
            else { OnError(new ErrorArgs(string.Format("[BuildDataFiles] Cannot locate dump directory at: {0}", dumpDirectory))); }

            GC.Collect();
            return index;
        }

        /// <summary>
        /// Rebuilds a data.xxx file potentially removing blank space created by the OEM update method.
        /// Effectiveness increases depending on amount of updates made to desired data.xxx file.
        /// </summary>
        /// <param name="index">Reference to data.000</param>
        /// <param name="dataDirectory">Location of the data.xxx files</param>
        /// <param name="dataId">Id of the data.xxx file to be rebuilt</param>
        /// <param name="buildDirectory">Location of build folder (e.g. client/output/data-files/)</param>
        public void RebuildDataFile(string dataDirectory, int dataId, string buildDirectory)
        {
            List<IndexEntry> filteredIndex = GetEntriesByDataId(Index, dataId, SortType.Offset);

            string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, dataId);

            if (File.Exists(dataPath))
            {
                if (makeBackups) { createBackup(dataPath, Convert.ToInt32(new FileInfo(dataPath).Length * 0.02)); }

                OnMessage(new ConsoleMessageArgs(string.Format("Writing new data.00{0}...", dataId), true, 1, false, 0));

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

                            if (EntryExists(entry.Name))
                            {
                                inFS.Seek(entry.Offset, SeekOrigin.Begin);
                                byte[] inFile = new byte[entry.Length];
                                inFS.Read(inFile, 0, entry.Length);

                                if (inFile.Length > 0)
                                {
                                    UpdateEntryOffset(entry.Name, outFS.Position);
                                    outFS.Write(inFile, 0, inFile.Length);
                                }
                                else { OnError(new ErrorArgs(string.Format("[RebuildDataFile] failed to buffer file from the original data file!"))); }
                            }
                            else { OnError(new ErrorArgs(string.Format("[RebuildDataFile] failed to find original entry for {0} in the index!", entry.Name))); }

                            OnCurrentProgressChanged(new CurrentChangedArgs(idx, ""));
                        }
                    }
                }
            }
            else { OnError(new ErrorArgs(string.Format("[RebuildDataFile] Cannot locate data file: {0}", dataPath))); }

            OnCurrentProgressReset(new CurrentResetArgs(true));
        }

        #endregion

        #region Misc Methods

        /// <summary>
        /// Initializes the LUA engine used to load dCore.lua configurations
        /// </summary>
        public void Initialize()
        {
            Extensions.ValidExtensions = luaIO.GetExtensions();
            Extensions.GroupExtensions = luaIO.GetGroupExports();
            XOR.UnencryptedExtensions = luaIO.GetUnencryptedExtensions();
        }

        /// <summary>
        /// Determines if the provided name is currently encoded
        /// </summary>
        /// <param name="name">File name to check</param>
        /// <returns></returns>
        public bool IsEncoded(string name) { return StringCipher.IsEncoded(name); }

        /// <summary>
        /// Encodes provided name [UNHASHED]
        /// </summary>
        /// <param name="name">File name to be hashed</param>
        /// <returns>Hashed name</returns>
        public string EncodeName(string name) {  return StringCipher.Encode(name); }

        /// <summary>
        /// Decodes provided name [HASHED]
        /// </summary>
        /// <param name="name">Name to be unhashed</param>
        /// <returns>Unhashed name</returns>
        public string DecodeName(string name) { return StringCipher.Decode(name); }

        protected void createBackup(string dataPath, int chunkSize)
        {
            string bakPath = string.Format(@"{0}_NEW", dataPath);
            string altBakPath = string.Format(@"{0}_OLD_{1}", dataPath, DateTime.Now.ToLongDateString());

            if (File.Exists(bakPath))
            {
                File.Move(bakPath, altBakPath);
                OnMessage(new ConsoleMessageArgs("Previous BAK was detected and renamed.", true, 1, true, 1));
            }

            OnMessage(new ConsoleMessageArgs("Creating backup...", true));

            using (FileStream inFS = new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int length = (int)inFS.Length;

                OnCurrentMaxDetermined(new CurrentMaxArgs(inFS.Length));

                using (FileStream outFS = File.Create(string.Format(@"{0}_BAK", dataPath)))
                {
                    // If no chunkSize is provided, generate default
                    if (chunkSize == 0) { chunkSize = Math.Min(64000, (int)(length * .02)); }

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