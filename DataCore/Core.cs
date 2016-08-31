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
namespace DataCore
{
    /// <summary>
    /// Provides interactive access to the Rappelz Data.XXX File Management System
    /// </summary>
    public class Core
    {
        internal bool NamesHashed = false;

        /// <summary>
        /// Defines the encoding of files to be the default of the system
        /// unless changed by the caller during construction of Core
        /// </summary>
        internal readonly Encoding encoding = Encoding.Default;

        #region Events
        public event EventHandler<ErrorArgs> ErrorOccured;
        public event EventHandler<WarningArgs> WarningOccured;
        public event EventHandler<TotalMaxArgs> TotalMaxDetermined;
        public event EventHandler<TotalChangedArgs> TotalProgressChanged;
        public event EventHandler TotalProgressReset;
        public event EventHandler<CurrentMaxArgs> CurrentMaxDetermined;
        public event EventHandler<CurrentChangedArgs> CurrentProgressChanged;
        public event EventHandler CurrentProgressReset;
        #endregion

        #region Event Delegates

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
        protected void OnTotalProgressReset(EventArgs e) { TotalProgressReset?.Invoke(this, e); }

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
        protected void OnCurrentProgressReset(EventArgs e) { CurrentProgressReset?.Invoke(this, e); }

        #endregion

        #region Contructors

        /// <summary>
        /// Dummy constructor
        /// </summary>
        public Core() { }

        /// <summary>
        /// Constructor allowing encoding to be passed in by caller
        /// </summary>
        /// <param name="overrideEncoding">Encoding to be used during operation</param>
        public Core(Encoding overrideEncoding) { encoding = overrideEncoding; }

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
        public List<IndexEntry> Create(string dumpDirectory)
        {
            OnTotalMaxDetermined(new TotalMaxArgs(1));
            OnTotalProgressChanged(new TotalChangedArgs(0, "Creating new data.000..."));

            List<IndexEntry> index = new List<IndexEntry>();

            if (Directory.Exists(dumpDirectory))
            {
                string[] extDirectories = Directory.GetDirectories(dumpDirectory);

                OnCurrentMaxDetermined(new CurrentMaxArgs(extDirectories.Length));

                for (int dumpDirIdx = 0; dumpDirIdx < extDirectories.Length; dumpDirIdx++)
                {
                    OnCurrentProgressChanged(new CurrentChangedArgs(dumpDirIdx, string.Format("Indexing files in: {0}", extDirectories[dumpDirIdx])));
                    string[] directoryFiles = Directory.GetFiles(extDirectories[dumpDirIdx]);
                    for (int directoryFileIdx = 0; directoryFileIdx < directoryFiles.Length; directoryFileIdx++)
                    {
                        index.Add(new IndexEntry
                        {
                            Name = Path.GetFileName(directoryFiles[directoryFileIdx]),
                            Length = (int)new FileInfo(directoryFiles[directoryFileIdx]).Length,
                            DataID = GetID(Path.GetFileName(directoryFiles[directoryFileIdx])),
                            Offset = 0
                        });
                    }
                }

                OnTotalProgressChanged(new TotalChangedArgs(1, ""));

                OnCurrentProgressReset(EventArgs.Empty);

                return index;
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
        public List<IndexEntry> Create(string[] filePaths)
        {
            List<IndexEntry> tempIndex = new List<IndexEntry>(filePaths.Length);

            foreach (string filePath in filePaths)
            {
                FileInfo fileInfo = new FileInfo(filePath);
                tempIndex.Add(new IndexEntry
                {
                    Name = fileInfo.Name,
                    Offset = 0,
                    Length = (int)fileInfo.Length,
                    DataID = GetID(fileInfo.Name)
                });
            }

            return tempIndex;
        }

        /// <summary>
        /// Reads the data.000 contents into a List of IndexEntries (note toggling on decodeNames will decrease speed)
        /// </summary>
        /// <param name="path">Path to the data.xxx files (optional)</param>
        /// <param name="decodeNames">Determines if the file names should be decoded during load</param>
        /// <param name="reportInterval">How many bytes should be processed before reporting</param>
        /// <returns>A populated index or null</returns>
        public List<IndexEntry> Load(string path, bool decodeNames, int reportInterval)
        {
            bool isBlank = !path.Contains(".000");

            List<IndexEntry> index = new List<IndexEntry>();

            byte b = 0;

            long lastCount = 0;

            if (File.Exists(path))
            {
                using (var ms = new MemoryStream(File.ReadAllBytes(path)))
                {
                    OnTotalMaxDetermined(new TotalMaxArgs(1));
                    OnTotalProgressChanged(new TotalChangedArgs(1, string.Format("Indexing data.{0}...", (isBlank) ? "blk" : "000")));

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

                        dataIndexEntry.Name = (decodeNames) ? StringCipher.Decode(Encoding.Default.GetString(bytes)) : Encoding.Default.GetString(bytes);
                        dataIndexEntry.Offset = BitConverter.ToInt32(value, 0);
                        dataIndexEntry.Length = BitConverter.ToInt32(value, 4);
                        dataIndexEntry.DataID = GetID(Encoding.Default.GetString(bytes));
                        index.Add(dataIndexEntry);

                        if ((ms.Position - lastCount) >= reportInterval)
                        {
                            OnCurrentProgressChanged(new CurrentChangedArgs(ms.Position, ""));
                            lastCount = ms.Position;
                        }
                    }

                    OnCurrentProgressReset(EventArgs.Empty);
                }

                OnTotalProgressReset(EventArgs.Empty);

                NamesHashed = !decodeNames;
            }
            else { OnError(new ErrorArgs(string.Format("[Load] Cannot find data.000 at path: {0}", path))); }

            return (index.Count > 0) ? index : null;
        }

        /// <summary>
        /// Saves the provided indexList into a ready to use data.000 index
        /// </summary>
        /// <param name="index">Reference to data.000 index</param>
        /// <param name="buildDirectory">Location to build the new data.000 at</param>
        /// <param name="encodeNames">Determines if the fileNames in index should be encoded</param>
        /// <param name="isBlankIndex">Determines if the index is a Blank Space Index</param>
        /// <returns>bool value indicating success or failure</returns>
        /// TODO: UPDATE PATH!
        public void Save(ref List<IndexEntry> index, string buildDirectory, bool isBlankIndex, bool encodeNames)
        {
            string buildPath = string.Format(@"{0}\data.{1}", buildDirectory, (isBlankIndex) ? ".blk" : ".000");
            bool isBlank = !buildPath.Contains(".000");

            OnTotalMaxDetermined(new TotalMaxArgs(1));
            OnTotalProgressChanged(new TotalChangedArgs(0, string.Format("Saving index.{0}...", (isBlank) ? "blk" : "000")));

            if (File.Exists(buildPath)) { File.Delete(buildPath); }

            using (BinaryWriter bw = new BinaryWriter(File.Create(buildPath), Encoding.Default))
            {
                byte b = 0;

                OnCurrentMaxDetermined(new CurrentMaxArgs(index.Count));

                for (int idx = 0; idx < index.Count; idx++)
                {
                    IndexEntry indexEntry = index[idx];

                    int lastIdx = 0;
                    if (lastIdx - idx > 64000) { OnCurrentProgressChanged(new CurrentChangedArgs(idx, "")); }

                    string name = (encodeNames) ? StringCipher.Encode(indexEntry.Name) : indexEntry.Name;
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
                }

                OnCurrentProgressReset(EventArgs.Empty);
            }

            OnTotalProgressReset(EventArgs.Empty);
        }

        /// <summary>
        /// Reorders references index by sortType
        /// </summary>
        /// <param name="index">Reference to data.000 index</param>
        /// <param name="sortType">Type of sort to be performed</param>
        public void Sort(ref List<IndexEntry> index, int sortType)
        {
            switch (sortType)
            {
                case SortType.Name:
                    index.OrderBy(i => i.Name);
                    break;

                case SortType.Offset:
                    index.OrderBy(i => i.Offset);
                    break;

                case SortType.Size:
                    index.OrderBy(i => i.Length);
                    break;

                case SortType.DataId:
                    index.OrderBy(i => i.DataID);
                    break;
            }
        }

        /// <summary>
        /// Returns an IndexEntry based on it's [UNHASHED] name
        /// </summary>
        /// <param name="index">Reference to data.000 index</param>
        /// <param name="name">File name being searched for</param>
        /// <returns>IndexEntry of name</returns>
        public IndexEntry GetEntry(ref List<IndexEntry> index, string name) { return index.Find(i => i.Name == name); }

        /// <summary>
        /// Returns an IndexEntry based on it's dataId and offset
        /// </summary>
        /// <param name="index">Reference to data.000 List</param>
        /// <param name="dataId">data.xxx id being searched</param>
        /// <param name="offset">offset of file in dataId being searched</param>
        /// <returns>IndexEntry of dataId and offset</returns>
        public IndexEntry GetEntry(ref List<IndexEntry> index, int dataId, int offset) { return index.Find(i => i.DataID == dataId && i.Offset == offset); }

        /// <summary>
        /// Returns a blankIndex entry with an AvailableSpace betweem the minimum and maximum sizes (if one exists)
        /// </summary>
        /// <param name="blankIndex">Reference to the data.blk index</param>
        /// <param name="dataId">data.xxx Id being requested</param>
        /// <param name="minSize">Minimum size of blank space</param>
        /// <param name="maxSize">Maximum size of blank space</param>
        /// <returns>Populated BlankIndexEntry (if one exists)</returns>
        public IndexEntry GetClosestEntry(ref List<IndexEntry> blankIndex, int dataId, int minSize, int maxSize)
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
        public List<IndexEntry> GetEntriesByPartialName(ref List<IndexEntry> index, string partialName) { return index.FindAll(i => i.Name.Contains(partialName)); }

        /// <summary>
        /// Returns a List of all entries matching dataId
        /// </summary>
        /// <param name="index">Reference to data.000 index</param>
        /// <param name="dataId">data.xxx Id being requested</param>
        /// <returns>List for data.xx{dataId}</returns>
        public List<IndexEntry> GetEntriesByDataId(ref List<IndexEntry> index, int dataId) { return index.FindAll(i => i.DataID == dataId); }

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
        public List<IndexEntry> GetEntriesByDataId(ref List<IndexEntry> index, int dataId, int sortType)
        {
            switch (sortType)
            {
                case SortType.Name: // Name
                    return index.FindAll(i => i.DataID == dataId).OrderBy(i => i.Name).ToList();

                case SortType.Offset: // Offset
                    return index.FindAll(i => i.DataID == dataId).OrderBy(i => i.Offset).ToList();

                case SortType.Size: // Size
                    return index.FindAll(i => i.DataID == dataId).OrderBy(i => i.Length).ToList();
            }

            return null;
        }

        /// <summary>
        /// Returns a filtered List of all entries matching dataId
        /// Return is sorted by sortType
        /// </summary>
        /// <param name="blankIndex">Reference to data.blk index</param>
        /// <param name="dataId">data.xxx Id being requested</param>
        /// <param name="sortType">Type code for how to sort return</param>
        /// <returns>List for data.xx{dataId}</returns>
        public List<BlankIndexEntry> GetEntriesByDataId(ref List<BlankIndexEntry> blankIndex, int dataId, int sortType)
        {
            switch (sortType)
            {
                case SortType.Name: // Name
                    OnWarning(new WarningArgs("[GetEntriesByDataId] BlankIndex cannot be sorted by Name!\nPlease try again."));
                    break;

                case SortType.Offset: // Offset
                    return blankIndex.FindAll(i => i.DataID == dataId).OrderBy(i => i.Offset).ToList();

                case SortType.Size: // Size
                    return blankIndex.FindAll(i => i.DataID == dataId).OrderBy(i => i.AvailableSpace).ToList();
            }

            return null;
        }

        /// <summary>
        /// Returns a filtered List of all entries matching extension
        /// </summary>
        /// <param name="index">data.000 index being searched</param>
        /// <param name="extension">extension being searched (e.g. dds)</param>
        /// <returns>Filtered List of extension</returns>
        public List<IndexEntry> GetEntriesByExtension(ref List<IndexEntry> index, string extension) { return index.FindAll(i => i.Name.Contains(string.Format(".{0}", extension.ToLower()))); }

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
        public List<IndexEntry> GetEntriesByExtension(ref List<IndexEntry> index, string extension, int sortType)
        {
            switch (sortType)
            {
                case 0: // Name
                    return index.FindAll(i => i.Name.Contains(string.Format(".{0}", extension.ToLower()))).OrderBy(i => i.Name).ToList();

                case 1: // Offset
                    return index.FindAll(i => i.Name.Contains(string.Format(".{0}", extension.ToLower()))).OrderBy(i => i.Offset).ToList();

                case 2: // Size
                    return index.FindAll(i => i.Name.Contains(string.Format(".{0}", extension.ToLower()))).OrderBy(i => i.Length).ToList();

                case 3: // dataId
                    return index.FindAll(i => i.Name.Contains(string.Format(".{0}", extension.ToLower()))).OrderBy(i => i.DataID).ToList();
            }

            return null;
        }

        /// <summary>
        /// Removes a set of entries bearing DataID = dataId from referenced data.000 index
        /// </summary>
        /// <param name="index">Index to be altered</param>
        /// <param name="dataId">Id of file entries to be deleted</param>
        public void DeleteEntriesByDataId(ref List<IndexEntry> index, int dataId) { index.RemoveAll(i => i.DataID == dataId); }

        /// <summary>
        /// Removes a single entry bearing Name = name from referenced data.000 index
        /// </summary>
        /// <param name="index">Index to be altered</param>
        /// <param name="name">Name of file entry to be deleted</param>
        public void DeleteEntryByName(ref List<IndexEntry> index, string name) { index.Remove(index.Find(i => i.Name == name)); }

        /// <summary>
        /// Removes a single entry bearing DataID = id and Offset = offset from referenced data.000 index
        /// </summary>
        /// <param name="index">Index to be altered</param>
        /// <param name="id">DataID of file entry to be deleted</param>
        /// <param name="offset">Offset of file entry to be deleted</param>
        public void DeleteEntryByIdandOffset(ref List<IndexEntry> index, int id, int offset) { index.Remove(index.Find(i => i.DataID == id && i.Offset == offset)); }

        #endregion

        #region File Methods

        /// <summary>
        /// Generates an SHA512 hash for the given fileName by locating the bytes in data.XXX storage
        /// </summary>
        /// <param name="index">Reference to loaded data.000 index</param>
        /// <param name="dataDirectory">Location of the data.xxx files</param>
        /// <param name="fileName">Name of the file to generate hash for</param>
        /// <returns>SHA512 Hash String</returns>
        public string GetFileSHA512(ref List<IndexEntry> index, string dataDirectory, string fileName)
        {
            int dataId = GetID(fileName);

            if (dataId > 0)
            {
                string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, dataId);

                if (File.Exists(dataPath))
                {
                    byte[] buffer = null;
                    IndexEntry fileEntry = GetEntry(ref index, fileName);

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
        public string GetFileMD5(ref List<IndexEntry> index, string dataDirectory, string fileName)
        {
            int dataId = GetID(fileName);

            if (dataId > 0)
            {
                string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, dataId);

                if (File.Exists(dataPath))
                {
                    byte[] buffer = null;
                    IndexEntry fileEntry = GetEntry(ref index, fileName);

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
            string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, GetID(Path.GetFileName(buildPath)));

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
                    OnTotalMaxDetermined(new TotalMaxArgs(length));

                    // Determine the files extension
                    fileExt = Path.GetExtension(buildPath).Remove(0,1).ToLower();

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
                                    OnTotalProgressChanged(new TotalChangedArgs(byteCount, ""));
                                }
                            }
                        }
                    }
                    else { OnWarning(new WarningArgs(string.Format("[ExportFileEntry] Skipping entry {0} with malformed extension {1}", Path.GetFileName(buildPath), fileExt))); }
                }
                else { OnError(new ErrorArgs("[ExportFileEntry] Failed to buffer file for export!")); }

                outBuffer = null;
                OnTotalProgressReset(EventArgs.Empty);
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
        public void ExportFileEntries(ref List<IndexEntry> filteredIndex, string dataDirectory, string buildDirectory, int chunkSize)
        {
            OnTotalMaxDetermined(new TotalMaxArgs(8));

            // For each set of dataId files in the filteredIndex
            for (int dataId = 1; dataId <= 8; dataId++)
            {
                // Filter only entries with current dataId into temp index
                List<IndexEntry> tempIndex = GetEntriesByDataId(ref filteredIndex, dataId, SortType.Offset);

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
                            }

                            OnCurrentProgressReset(EventArgs.Empty);
                        }
                    }
                    else { OnError(new ErrorArgs(string.Format("[ExportFileEntries] Cannot locate: {0}", dataPath))); }
                }
            }

            OnTotalProgressReset(EventArgs.Empty);

            GC.Collect();
        }

        /// <summary>
        /// Updates the dataDirectory data.xxx stored copy of the physical file at filePath
        /// </summary>
        /// <param name="index">Reference to data.000 index to be updated</param>
        /// <param name="dataDirectory">Location of the data.xxx files</param>
        /// <param name="filePath">Location of the file being imported</param>
        /// <param name="chunkSize">Size (in bytes) to process each iteration of the write loop</param>
        public void UpdateFileEntry(ref List<IndexEntry> index, string dataDirectory, string filePath, int chunkSize)
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
            string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, GetID(fileName));

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
                            OnCurrentProgressReset(EventArgs.Empty);
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
        public void UpdateFileEntry(ref List<IndexEntry> index, ref List<IndexEntry> blankIndex, string dataDirectory, string filePath, int chunkSize)
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
            string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, GetID(fileName));

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
                            OnCurrentProgressReset(EventArgs.Empty);
                        }
                    }
                }
                else { OnError(new ErrorArgs(string.Format("[UpdateFileEntry] Cannot locate entry for {0} in referenced index", fileName))); }
            }
            else { OnError(new ErrorArgs(string.Format("[UpdateFileEntry] Cannot locate: {0}", dataPath))); }
        }

        /// <summary>
        /// Updates the dataDirectory data.xxx stored copies of physical files in filePaths
        /// </summary>
        /// <param name="index">Reference to data.000 index to be updated</param>
        /// <param name="dataDirectory">Location of the data.xxx files</param>
        /// <param name="filePaths">Array of file paths for physical files</param>
        /// <param name="chunkSize">Size (in bytes) to process each iteration of the write loop</param>
        public void UpdateFileEntries(ref List<IndexEntry> index, string dataDirectory, string[] filePaths, int chunkSize)
        {
            List<IndexEntry> tempIndex = Create(filePaths);

            OnTotalMaxDetermined(new TotalMaxArgs(8));

            // Foreach dataId filter the referenced index
            for (int dataId = 1; dataId <= 8; dataId++)
            {
                OnTotalProgressChanged(new TotalChangedArgs(dataId, string.Format("Updating Files in data.00{0}", dataId)));

                List<IndexEntry> filteredIndex = GetEntriesByDataId(ref tempIndex, dataId, SortType.Size);

                for (int entryIdx = 0; entryIdx < filteredIndex.Count; entryIdx++)
                {
                    IndexEntry currentEntry = filteredIndex[entryIdx];

                    // Grab the original copy from referenced index
                    IndexEntry originalEntry = index.Find(i => i.Name == currentEntry.Name);

                    if (originalEntry != null)
                    {
                        // Determine the path to the current files appropriate data.xxx
                        string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, GetID(currentEntry.Name));

                        if (File.Exists(dataPath))
                        {
                            using (FileStream fs = new FileStream(dataPath, FileMode.Open, FileAccess.Write))
                            {
                                string fileName = currentEntry.Name;
                                string fileExt = Path.GetExtension(fileName);

                                // Define the file path to the file being processed
                                string filePath = filePaths.ToList().Find(f => f.Contains(fileName));

                                // If the physical file exists
                                if (File.Exists(filePath))
                                {
                                    bool append = false;

                                    // Load it into a byte array
                                    byte[] fileBytes = File.ReadAllBytes(filePath);

                                    OnCurrentMaxDetermined(new CurrentMaxArgs(fileBytes.Length));

                                    // Check if the new file is bigger than the old file
                                    if (fileBytes.Length > originalEntry.Length) { append = true; }

                                    // Based on append bool set the position of the filestream (data.xxx file)
                                    // Also update the originalEntry offset
                                    if (append)
                                    {
                                        fs.Position = fs.Length;
                                        originalEntry.Offset = fs.Position;
                                    }
                                    else { fs.Position = originalEntry.Offset; }

                                    // Update originalEntry length
                                    originalEntry.Length = fileBytes.Length;

                                    // If the fileBytes need to be encrypted do so
                                    if (XOR.Encrypted(fileExt.ToUpper())) { byte b = 0; XOR.Cipher(ref fileBytes, ref b); }

                                    // If no chunkSize is provided, generate default
                                    if (chunkSize == 0) { chunkSize = Math.Max(64000, (int)(fileBytes.Length * .02)); }

                                    using (BinaryWriter bw = new BinaryWriter(fs, encoding, true))
                                    {
                                        // Iterate through the fileByte array writing the chunkSize to fs and reporting current position in
                                        // the array to the caller via CurrentProgress events
                                        for (int byteCount = 0; byteCount < fileBytes.Length; byteCount += Math.Min(fileBytes.Length - byteCount, chunkSize))
                                        {
                                            bw.Write(fileBytes, byteCount, Math.Min(fileBytes.Length - byteCount, chunkSize));
                                            OnCurrentProgressChanged(new CurrentChangedArgs(byteCount, ""));
                                        }
                                    }

                                    fileBytes = null;
                                }
                                else { OnError(new ErrorArgs(string.Format("[UpdateFileEntries] Cannot locate update file: {0}", filePath))); }
                            }
                        }
                        else { OnError(new ErrorArgs(string.Format("[UpdateFileEntries] Cannot locate data file: {0}", dataPath))); }
                    }
                    else { OnError(new ErrorArgs(string.Format("[UpdateFileEntries] Cannot locate entry for {0} in referenced index", currentEntry.Name))); }

                    OnCurrentProgressReset(EventArgs.Empty);
                }
            }

            OnTotalProgressReset(EventArgs.Empty);
            GC.Collect();
        }

        /// <summary>
        /// Creates a file entry that does not exist in the referenced data.000 index
        /// </summary>
        /// <param name="index">Reference to data.000 index to be updated</param>
        /// <param name="dataDirectory">Location of the data.xxx files</param>
        /// <param name="filePath">Location of the file being imported</param>
        /// <param name="chunkSize">Size (in bytes) to process each iteration of the write loop</param>
        public void ImportFileEntry(ref List<IndexEntry> index, string dataDirectory, string filePath, int chunkSize)
        {
            // If the file being imported exists
            if (File.Exists(filePath))
            {
                // Define some information about the file
                string fileName = Path.GetFileName(filePath);
                string fileExt = Path.GetExtension(fileName).Remove(0,1);
                long fileLen = new FileInfo(filePath).Length;
                int dataId = GetID(fileName);

                OnCurrentMaxDetermined(new CurrentMaxArgs(fileLen));

                // Check if the indexEntry already exists
                if (index.Find(i => i.Name == fileName) == null)
                {
                    // Determine the path of this particular file's data.xxx exists
                    string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, dataId);

                    // If the data.xxx file exists
                    if (File.Exists(dataPath))
                    {
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
                            if (chunkSize == 0) { chunkSize = Math.Max(64000, (int)(fileBytes.Length * .02)); }

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
                                Name = fileName,
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

            OnCurrentProgressReset(EventArgs.Empty);
        }

        /// <summary>
        /// Creates file entries that do not exist in the referenced data.000 index
        /// </summary>
        /// <param name="index">Reference to data.000 index to be updated</param>
        /// <param name="dataDirectory">Location of the data.xxx files</param>
        /// <param name="filePaths">Array of file paths of files to import</param>
        /// <param name="chunkSize">Size (in bytes) to process each iteration of the write loop</param>
        public void ImportFileEntries(ref List<IndexEntry> index, string dataDirectory, string[] filePaths, int chunkSize)
        {
            OnTotalMaxDetermined(new TotalMaxArgs(8));

            // Create a temporary data.xxx from the filePaths
            List<IndexEntry> tempIndex = Create(filePaths);

            // Foreach dataId filter the referenced index
            for (int dataId = 1; dataId <= 8; dataId++)
            {
                OnTotalProgressChanged(new TotalChangedArgs(dataId, string.Format("Importing files to data.00{0}", dataId)));

                // Filter the temporary index by current dataId
                List<IndexEntry> filteredIndex = GetEntriesByDataId(ref tempIndex, dataId, SortType.Size);

                // Determine the path to the current files appropriate data.xxx
                string dataPath = string.Format(@"{0}\data.00{1}", dataDirectory, dataId);

                // If the data.xxx exists
                if (File.Exists(dataPath))
                {
                    // Open the data.xxx in a filestream with write access
                    using (FileStream fs = new FileStream(dataPath, FileMode.Open, FileAccess.Write))
                    {
                        OnCurrentMaxDetermined(new CurrentMaxArgs(filteredIndex.Count));

                        // Foreach indexEntry in the filteredIndex
                        for (int entryIdx = 0; entryIdx < filteredIndex.Count; entryIdx++)
                        {
                            IndexEntry currentEntry = filteredIndex[entryIdx];

                            // Define file name and extension
                            string fileName = currentEntry.Name;
                            string fileExt = Path.GetExtension(fileName).Remove(0,1);

                            OnCurrentProgressChanged(new CurrentChangedArgs(entryIdx, string.Format("Importing file {0}", fileName)));

                            // Define the file path to the file being processed
                            string filePath = filePaths.ToList().Find(f => f.Contains(fileName));

                            // If the physical file exists
                            if (File.Exists(filePath))
                            {
                                // Load it into a byte array
                                byte[] fileBytes = File.ReadAllBytes(filePath);

                                fs.Position = fs.Length;

                                currentEntry.Offset = fs.Position;

                                // If the fileBytes need to be encrypted do so
                                if (XOR.Encrypted(fileExt.ToUpper())) { byte b = 0; XOR.Cipher(ref fileBytes, ref b); }

                                // If no chunkSize is provided, generate default
                                if (chunkSize == 0) { chunkSize = Math.Max(64000, (int)(fileBytes.Length * .02)); }

                                using (BinaryWriter bw = new BinaryWriter(fs, encoding, true))
                                {
                                    // Iterate through the fileByte array writing the chunkSize to fs and reporting current position in
                                    // the array to the caller via CurrentProgress events
                                    for (int byteCount = 0; byteCount < fileBytes.Length; byteCount += Math.Min(fileBytes.Length - byteCount, chunkSize))
                                    {
                                        bw.Write(fileBytes, byteCount, Math.Min(fileBytes.Length - byteCount, chunkSize));
                                    }
                                }

                                // Add the current indexEntry to the referenced data.000 index
                                index.Add(currentEntry);
                            }
                            else { OnError(new ErrorArgs(string.Format("[ImportFileEntries] Cannot locate update file: {0}", filePath))); }
                        }

                        OnCurrentProgressReset(EventArgs.Empty);
                    }
                }
                else { OnError(new ErrorArgs(string.Format("[ImportFileEntries] Cannot locate data file: {0}", dataPath))); }
            }

            OnTotalProgressReset(EventArgs.Empty);
            GC.Collect();
        }

        /// <summary>
        /// Builds the data.xx[dataId] file from provided dumpDirectory into provided buildDirectory
        /// while updating referenced data.000 index
        /// </summary>
        /// <param name="dataId">data.xxx id (e.g. 1-8)</param>
        /// <param name="index">reference to data.000 (target will be updated)</param>
        /// <param name="dumpDirectory">Directory containing dumped extension folders (e.g. client/output/dump/)</param>
        /// <param name="buildDirectory">Directory where the data.xxx file will be built</param>
        public void BuildDataFile(int dataId, ref List<IndexEntry> index, string dumpDirectory, string buildDirectory)
        {
            // If the dumpDirectory exists
            if (Directory.Exists(dumpDirectory))
            {
                OnTotalMaxDetermined(new TotalMaxArgs(1));

                // Build directory list of the dump/ext/ folders
                string[] extDirectories = Directory.GetDirectories(dumpDirectory);

                // Create a temporary index
                List<IndexEntry> tempIndex = Create(dumpDirectory);

                // Sort the new data index
                List<IndexEntry> filteredIndex = GetEntriesByDataId(ref tempIndex, dataId, SortType.Size);

                // Nullify the tempIndex
                tempIndex = null;

                OnCurrentMaxDetermined(new CurrentMaxArgs(filteredIndex.Count));

                // Define where the newly generated data.xxx file will be built
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
                            // Define a handle to access the current indexEntry
                            IndexEntry fileEntry = filteredIndex[curFileIdx];

                            // Update the entries offset
                            fileEntry.Offset = fs.Position;

                            OnCurrentProgressChanged(new CurrentChangedArgs(curFileIdx, string.Format("Packing file {0}", fileEntry.Name)));

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
                            else { OnError(new ErrorArgs(string.Format("[BuildDataFile] Cannot locate file {0} at: {1}", Path.GetFileName(filePath), filePath))); }
                        }
                    }
                }

                // Delete all entries in the index matching the dataId
                DeleteEntriesByDataId(ref index, dataId);

                // Add the newly generate data.xxx index section to the referenced index
                index.AddRange(filteredIndex);

                OnTotalProgressChanged(new TotalChangedArgs(1, ""));
                OnTotalProgressReset(EventArgs.Empty);
                OnCurrentProgressReset(EventArgs.Empty);
            }
            else { OnError(new ErrorArgs(string.Format("[BuildDataFile] Cannot locate dump directory at: {0}", dumpDirectory))); }
        }

        /// <summary>
        /// Generates data.xxx file-system from dump structure (client/output/dump/)
        /// </summary>
        /// <param name="dumpDirectory">Location of dump folders (e.g. client/output/dump/)</param>
        /// <param name="buildDirectory">Location of build folder (e.g. client/output/data-files/)</param>
        /// <returns>Newly generated List to be saved</returns>
        public List<IndexEntry> BuildDataFiles(string dumpDirectory, string buildDirectory)
        {
            List<IndexEntry> index = null;

            if (Directory.Exists(dumpDirectory))
            {
                OnTotalMaxDetermined(new TotalMaxArgs(9));

                // Build new data.000
                string[] extDirectories = Directory.GetDirectories(dumpDirectory);

                OnCurrentMaxDetermined(new CurrentMaxArgs(extDirectories.Length));

                // Create a new index of the dumpDirectory
                index = Create(dumpDirectory);

                // Issue a reset to the CurrentProgressbar
                OnCurrentProgressReset(EventArgs.Empty);

                // Foreach data.xxx file (1-8)
                for (int dataId = 1; dataId <= 8; dataId++)
                {
                    OnTotalProgressChanged(new TotalChangedArgs(dataId, string.Format("Building data.00{0}", dataId)));

                    // Filter down the new data.000 into the current dataId only
                    List<IndexEntry> filteredIndex = GetEntriesByDataId(ref index, dataId, SortType.Size);

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

                                OnCurrentProgressChanged(new CurrentChangedArgs(curFileIdx, string.Format("Packing file {0}", fileEntry.Name)));

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

                    OnCurrentProgressReset(EventArgs.Empty);
                }

                OnTotalProgressReset(EventArgs.Empty);
            }
            else { OnError(new ErrorArgs(string.Format("[BuildDataFiles] Cannot locate dump directory at: {0}", dumpDirectory))); }

            GC.Collect();
            return index;
        }

        #endregion

        #region Misc Methods

        /// <summary>
        /// Calculates the id of the data.xxx file a particular hash name belongs to
        /// </summary>
        /// <param name="name">Hashed/Unhashed name being searched</param>
        /// <param name="isHashed">Determines if name is hashed or not</param>
        /// <returns>Data.00x id</returns>
        public int GetID(string name)
        {
            byte[] bytes;
            if (StringCipher.IsEncoded(name)) { bytes = encoding.GetBytes(name.ToLower()); }
            else { bytes = encoding.GetBytes(StringCipher.Encode(name).ToLower()); }
            int num = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                num = (num << 5) - num + (int)bytes[i];
            }
            if (num < 0)
            {
                num *= -1;
            }
            return num % 8 + 1;
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

        #endregion
    }
}