# DataCore
Rappelz Data.XXX Management API for .NET Applications

Change-Log as of 4/22/2016 7:23 AM

- Core.Import/UpdateFileEntry methods switched from OnTotal* events to OnCurrent*
- Core.UpdateFileEntry() overload added with argument (ref List<IndexEntry> blankIndex) to update files while tracking orphaned space
- Restructured code in Import/UpdateFileEntry methods
- Core.Create() method now reports a totalProgress/totalStatus
- Core.Load() method now reports a TotalStatus message and CurrentProgress values
- Core.Load() reportInterval (int) argument added, controls amount of bytes to process before reporting position to caller (progress reporting)
- Core.Load() overloads for data.000 and data.blk merged
- Structures.EventArgs.CurrentEventArgs changed to long from int
- IndexEntry class moved from DataCore namepsace to DataCore.Structures
- BlankIndexEntry and IndexEntry merged
- Core.Save() now reports progress via OnTotal*/OnCurrent* events
- Core.Save() changed from bool to void (bool always returned true)
- Core.Save() (all overloads) code optimized
- Core.Save() overload added to properly save .blk index as a .000 would be
- Added new Core.GetEntriesByDataId() overload to search BlankIndex
- Added Core.GetClosestEntry(ref List<BlankIndexEntry> index, int dataId, int minSize, int maxSize) method to get the closest matching IndexEntry from the data.blk.

