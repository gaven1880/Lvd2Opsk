/*

# Next SEKAI LevelData => Open Sekai Score JSON

This converter does not support:
- Long notes with flick heads
- Long notes with hidden heads
- Guide notes
- Ease In-Out
- Ease Out-In
- Dynamic Stages
- Layers
- Timescale skips
- Timescale eases
- Fake notes
- Damage notes
- Custom SFX

Made by Gaven. ( @gaven1880 on most platforms )

*/

using System.IO.Compression;
using System.Text.Json;
using NextSekai;
using Sekai;

string input = args[0];
string output = Path.Combine(Path.GetDirectoryName(input), Path.GetFileNameWithoutExtension(input)) + "_opsk.json";

LevelData levelData;

JsonSerializerOptions options = new JsonSerializerOptions
{
  IncludeFields = true,
};

if (File.ReadAllBytes(input) is [0x1F, 0x8B, ..])
{
  using FileStream stream = File.Open(input, FileMode.Open);
  using var decompressor = new GZipStream(stream, CompressionMode.Decompress);
  levelData = JsonSerializer.Deserialize<LevelData>(decompressor, options);
}
else
{
  levelData = JsonSerializer.Deserialize<LevelData>(File.ReadAllText(input), options);
}

List<Note> noteList = new List<Note>();
List<Note> longList = new List<Note>();
List<MusicScoreEventData> eventDataList = new List<MusicScoreEventData>();

// SE Volume
eventDataList.Add(new MusicScoreEventData(
  eventDataList.ToArray().Length + 1,
  MusicScoreEventType.SeVolume,
  0L,
  1.0
));

// Time Signature
eventDataList.Add(new MusicScoreEventData(
  eventDataList.ToArray().Length + 1,
  MusicScoreEventType.TimeSignature,
  0L,
  "4/4"
));

(Entity[], Entity[], Entity[]) filteredEntities = ConvUtil.FilterEntities(levelData.entities);
Entity[] eventDataEntities = filteredEntities.Item1;
Entity[] noteEntities = filteredEntities.Item2;
Entity[] longEntities = filteredEntities.Item3;

int id = eventDataList.ToArray().Length + 1;

// event data
foreach (Entity entity in eventDataEntities)
{
  eventDataList.Add(ConvUtil.ProcessEventData(entity, id));
  id++;
}

// notes
foreach (Entity entity in noteEntities)
{
  noteList.Add(ConvUtil.ProcessNote(entity, id));
  id++;
}

// long notes
foreach (Entity entity in longEntities)
{
  Note note = ConvUtil.ProcessNote(entity, id);
  note.NSName = entity.name;
  longList.Add(note);
  id++;
}

foreach (Note note in longList)
{
  Entity entity = longEntities.FirstOrDefault(e => e.name == note.NSName);
  
  Data nextData = entity.data.FirstOrDefault(d => d.name == "next");
  if (nextData != null)
  {
    Note nextNote = longList.FirstOrDefault(n => n.NSName == nextData._ref);

    note.nextConnectionId = nextNote.id;
    nextNote.previousConnectionId = note.id;
  }
}

foreach (Note note in longList)
{
  note.noteBaseType = ConvUtil.GetNoteBaseType(note.category, note.IsConnectedFirst, note.IsConnectedLast, note.IsSingle);
}

MusicScoreEventData[] eventDataArray = eventDataList.ToArray();
Note[] noteArray = noteList.Concat(longList).ToArray();

MusicScoreMakerData score = new MusicScoreMakerData(
  1,
  eventDataArray,
  Array.Empty<object>(),
  noteArray,
  noteArray.OrderBy(note => note.ticks).ToArray()[^1].ticks,
  -6767,
  null
);

File.WriteAllText(output, JsonSerializer.Serialize(score, options));