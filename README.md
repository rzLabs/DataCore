# Description

Provides interactibility with the Rappelz proprietary `Data.xxx` file storage system.

## Namespaces

- `DataCore`
- `DataCore.Functions`
- `DataCore.Structures`

## Important Classes

### IndexEntry

The `IndexEntry` stores information regarding a `file` stored in the `data.xxx`

#### Properties

|Name|Type|Description|
|-|-|-|
|Name|`string`|The file name *(including extension)* of this entry|
|Extension|`string`|The file extension of this entry *(does not include preceeding `.`)*|
|Hash|`byte[]`|The unhashed file name of this entry|
|HashName|`string`|The hashed file name of this entry|
|Length|`int`|The length of this entry|
|Offset|`int64`|The location of this entry in the housing `data.xxx`|
|DataID|`int`|The data file this entry is stored in *(e.g. `data.001`)*|
|DataPath|`string`|The folder this entry would be stored at when using the vanilla `PatchServer`|

## Constructors

|Arguments|Description|
|-|-|
|`none`|Dummy/Generic constructor|
|`bool` backup, `Encoding` encoding|Instantiate the core with only backup toggled and encoding set.|
|`bool` backup, `string` configPath|Not implemented|
|`Encoding` encoding, `bool` backup, `string` configPath|Not Implemented|

## Events

|Name|Arguments|Description|
|-|-|-|
|CurrentMaxDetermined|`int` Maximum|Informs the calling application of the value to be used as `Progress Maximum`|
|CurrentProgressChanged|`int` Value|Informs the calling application that the progress of the current operation has changed.|
|CurrentProgressReset|`bool` WriteOK|Informs the calling application that the current operation has completed and the progressbar should be reset. *(In the case of console apps it also determines if an [OK] and line break should be appended to the current line)*|
|WarningOccured|`string` warning|Informs the calling application of a non-critical error that has occured.|
|MessageOccured|`string` message|Informs the calling application of a message that should be displayed to the user. *(In the case of console apps it also includes formatting such as tabs and line breaks)*

## Properties

|Name|Type|Description|
|-|-|-|
|DataDirectory|`string`|The directory containing `data.xxx` files.|
|Index|`List<IndexEntry>`|Stores all file entries indexed by the `data.000`|
|RowCount|`int`|The count of loaded `IndexEntry` entries.|
|ExtensionsList|`List<ExtensionInfo>`|Collection of extensions present in the loaded `Index`|

## Methods

|Name|Arguments|Return|Description|
|-|-|-|-|
|Load | `string` path (of data.000) | `void` | Loads a `data.000` at the given `path`|
|Save | `string` path | `void` | Saves the contents of `Index` to to the given `path`|
|Sort | `SortType` type | `void` | Sorts a previously loaded `data.000` index|
|EntryExists | `string` name | `bool` | Determines if an `IndexEntry` exists in the loaded `data.000`|
|GetStoredSize | `List<IndexEntry>` filteredList | `int` | Calculates the total size of the files in the filteredList|
|FindAll | `string` fieldName, `string` op, `object` criteria | `List<IndexEntry>` | Locates all `IndexEntry` with matching criteria and returns them as a list.|
|GetEntry | `int` index | `IndexEntry` | Returns the IndexEntry at the given index|
|GetEntry | `string` name | `IndexEntry` | Returns the IndexEntry with matching name|
|GetEntry | `int` dataID, `int` offset | IndexEntry | Returns the IndexEntry with matching dataID and offset|
|GetEntriesByPartialName | `string` partialName | `List<IndexEntry>` | Returns a list of all entries whose names contains `partialName` **(You can use wildcards in the partialName)** *(Update by Gangor)*|
|GetEntriesByDataId | `int` dataId, `SortType` type | `List<IndexEntry>`  | Returns a list of all entries with matching `dataId`, sortable by `type`.|
|GetEntriesByDataId | `List<IndexEntry>` filteredIndex, `int` dataId, `SortType` type | `List<IndexEntry>` | Returns a list of all entries with matching `dataId` and type from the `filteredIndex`|
|GetEntriesByExtension | `string` extension | `List<IndexEntry>` | Returns a list of all entries with matching extension|
|GetEntriesByExtension | `string` extension, `SortType` type | `List<IndexEntry>` | Returns a list of all entries with matching extension sorted by `type`|
|GetEntriesByExtension | `string` extension, `string` term | `List<IndexEntry>` | Returns a list of all entries whose name contains term and extension matches extension *(Overload by Gangor)*|
|GetEntriesByExtension | `string` extension, `string` term, `SortType` type | `List<IndexEntry>` | Returns a list of all entries whose name contains term and extension matches `extension` sorted on `type`|
|GetEntriesByExtension | `string` extension, `int` dataId | `List<IndexEntry>` | Returns a list of all entries whose extension matches extension and `dataId` matches `dataId`|
|GetEntriesByExtensions | `string[]` extensions | `List<IndexEntry>` | Returns a list of all entries whose extension match extensions|
|DeleteEntriesByDataId | `int` dataId | `void` | Removes all entries with matching `dataId` from the loaded index|
|DeleteEntryByName | `string` fileName, `string` dataDirectory | `void` | Deletes an entry and it's stored file entry from the loaded index|
|DeleteEntryByIdAndOffset | `int` id, `int` offset | `void` | Deletes an entry based on a provided id and `offset`|
|GetFileBytes | `string` fileName | `byte[]` | Returns the collection of bytes that represent the given `fileName`|
|GetFileBytes | `string` fileName, int dataId, `long` offset, `long` length | `byte[]` | Returns the collection of bytes that represent the file in the given `data.xx` `{dataId}` at `offset` with `length`|
|GetFileBytes | `IndexEntry` entry | `byte[]` | Returns the collection of bytes that represent the file described by `entry`|
|ExportFileEntry | string `buildPath`, `IndexEntry` entry | `void` | Exports the file associated to the given `entry`|
|ExportExtEntries | `string` buildDirectory, `string` extension | `void` | Exports all files whose extension match the provided extension|
|ExportAllEntries | `string` buildDirectory | `void` | Exports all files |
|ImportFileEntry | `string` filePath | `void` | Adds the file at `filePath` to the Rappelz `data.xxx` file storage system|
|ImportFileEntry | `string` fileName, `byte[]` fileBytes | `void` | Adds the file represented by fileBytes to the Rappelz `data.xxx` file storage system as name fileName *(Suggested by Gangor)*|
|BuildDataFiles | `string` dumpDirectory, `string buildPath` | `List<IndexEntry>` | Builds new `data.001-008` from the provided dump directory and returns the associated `data.000` index|
|RebuildDataFile | `int` dataId, `string` buildDirectory | `void` | Rebuilds the data.00{`dataId`} into the `buildDirectory`|

## Examples

To add `DataCore` functionality to your `.net` class add the `using` directive.

```csharp
using DataCore;
```

### Loading data.000

```csharp
using System.Text;
using DataCore;

class Program()
{
	static void Main(string[] args)
	{
		//Encoding is optional, but suggested!
		Encoding encoding = Encoding.Default;
		
		//Backup is optional, but suggested!
		bool backups = true;
		
		string path = "C:/Rappelz/Client/data.000";
		
		Core core = new Core(backups, encoding);
		
		core.Load(path);
		
		Console.WriteLine($"{core.RowCount} entries loaded from {path}");
	}	
}
```

### Fetching IndexEntry

```csharp
using System.Text;
using DataCore;
using DataCore.Structures;

class Program()
{
	static void Main(string[] args)
	{
		//Encoding is optional, but suggested!
		Encoding encoding = Encoding.Default;
		
		//Backup is optional, but suggested!
		bool backups = true;
		
		string path = "C:/Rappelz/Client/data.000";
		
		Core core = new Core(backups, encoding);
		
		core.Load(path);
		
		Console.WriteLine($"{core.RowCount} entries loaded from {path}");
		
		IndexEntry entry = core.GetEntry("db_string.rdb");
		
		if (entry != null)
			Console.WriteLine("db_string fetched successfully!");
	}	
}
```

### Exporting File'(s)

#### Single

```csharp
using System.Text;
using DataCore;
using DataCore.Structures;

class Program()
{
	static void Main(string[] args)
	{
		//Encoding is optional, but suggested!
		Encoding encoding = Encoding.Default;
		
		//Backup is optional, but suggested!
		bool backups = true;
		
		string path = "C://Rappelz//Client//data.000";
		
		Core core = new Core(backups, encoding);
		
		core.Load(path);
		
		Console.WriteLine($"{core.RowCount} entries loaded from {path}");
		
		IndexEntry entry = core.GetEntry("db_string.rdb");
		
		if (entry != null)
		{
			string buildDir = "c://Rappelz//Output"
			core.ExportFileEntry(buildDir, entry);
		}
	}	
}
```


#### Multiple

```csharp
using System.Text;
using DataCore;
using DataCore.Structures;

class Program()
{
	static void Main(string[] args)
	{
		//Encoding is optional, but suggested!
		Encoding encoding = Encoding.Default;
		
		//Backup is optional, but suggested!
		bool backups = true;
		
		string path = "C://Rappelz//Client//data.000";
		
		Core core = new Core(backups, encoding);
		
		core.Load(path);
		
		Console.WriteLine($"{core.RowCount} entries loaded from {path}");
		
		string ext = "jpg";
		
		List<IndexEntry> entries = core.GetEntriesByExtension(ext);
				
		if (entries.Count > 0)
		{
			string buildDir = "c://Rappelz//Output"
			core.ExportExtEntries(buildDir, entries);
		}
	}	
}
```

## Special Thanks
---

- xXExiledXx
- Glandu2
- Gangor
