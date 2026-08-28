/*

# Next SEKAI LevelData => Open Sekai Score JSON

This converter does not support:
- Long notes
- Guide notes
- Dynamic stages
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
List<MusicScoreEventData> eventDataList = new List<MusicScoreEventData>();

// SE Volume
eventDataList.Add(new(
  eventDataList.ToArray().Length + 1,
  MusicScoreEventType.SeVolume,
  0L,
  1.0
));

// Time Signature
eventDataList.Add(new(
  eventDataList.ToArray().Length + 1,
  MusicScoreEventType.TimeSignature,
  0L,
  "4/4"
));

int id = eventDataList.ToArray().Length + 1;

foreach (Entity entity in levelData.entities)
{
  if (ConvUtil.EventDataArchetypes.Contains(entity.archetype))
  {
    eventDataList.Add(ConvUtil.ProcessEventData(entity, id));
    id++;
  }
  else if (ConvUtil.NoteArchetypes.Contains(entity.archetype))
  {
    noteList.Add(ConvUtil.ProcessNote(entity, id));
    id++;
  }
}

MusicScoreEventData[] eventDataArray = [.. eventDataList];
Note[] noteArray = [.. noteList];

MusicScoreMakerData score = new(
  1,
  eventDataArray,
  [],
  noteArray,
  noteArray[^1].ticks,
  -6767,
  null
);

File.WriteAllText(output, JsonSerializer.Serialize(score, options));