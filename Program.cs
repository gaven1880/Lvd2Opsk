/*

# Next SEKAI LevelData => Open Sekai Score JSON

This converter does not support:
- Dynamic stages
- Fake notes
- Damage notes
- Custom SFX

Guide notes are only supported in Green and Yellow; other guide colors are converted to Green.

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

Dictionary<Entity, ConvUtil.HoldChainMember> holdChainMap = ConvUtil.BuildHoldChainMap(levelData.entities);

string primaryTimescaleGroup = levelData.entities
  .Where(e => ConvUtil.NoteArchetypes.Contains(e.archetype) || ConvUtil.IsHoldArchetype(e.archetype))
  .Select(e => e.TryGetRef("#TIMESCALE_GROUP", out string group) ? group : null)
  .Where(g => g != null)
  .GroupBy(g => g)
  .OrderByDescending(g => g.Count())
  .Select(g => g.Key)
  .FirstOrDefault();

string primaryStage = levelData.entities
  .Where(e => ConvUtil.NoteArchetypes.Contains(e.archetype) || ConvUtil.IsHoldArchetype(e.archetype))
  .Select(e => e.TryGetRef("stage", out string stage) ? stage : null)
  .Where(s => s != null)
  .GroupBy(s => s)
  .OrderByDescending(g => g.Count())
  .Select(g => g.Key)
  .FirstOrDefault();

bool IsRelevantEventData(Entity entity)
{
  if (!ConvUtil.EventDataArchetypes.Contains(entity.archetype)) return false;
  if (entity.archetype != "#TIMESCALE_CHANGE") return true;
  return !entity.TryGetRef("#TIMESCALE_GROUP", out string group) || group == primaryTimescaleGroup;
}

bool IsOnPrimaryStage(Entity entity) =>
  !entity.TryGetRef("stage", out string stage) || stage == primaryStage;

Dictionary<Entity, int> idByEntity = new Dictionary<Entity, int>();
int idCursor = id;
foreach (Entity entity in levelData.entities)
{
  bool isHold = ConvUtil.IsHoldArchetype(entity.archetype) && holdChainMap.ContainsKey(entity) && IsOnPrimaryStage(entity);
  bool isNote = ConvUtil.NoteArchetypes.Contains(entity.archetype) && IsOnPrimaryStage(entity);
  if (!IsRelevantEventData(entity) && !isNote && !isHold)
  {
    continue;
  }

  idByEntity[entity] = idCursor;
  idCursor++;
}

foreach (Entity entity in levelData.entities)
{
  if (IsRelevantEventData(entity))
  {
    eventDataList.Add(ConvUtil.ProcessEventData(entity, id));
    id++;
  }
  else if (ConvUtil.NoteArchetypes.Contains(entity.archetype) && IsOnPrimaryStage(entity))
  {
    noteList.Add(ConvUtil.ProcessNote(entity, id));
    id++;
  }
  else if (ConvUtil.IsHoldArchetype(entity.archetype) && IsOnPrimaryStage(entity) &&
    holdChainMap.TryGetValue(entity, out ConvUtil.HoldChainMember member))
  {
    int previousConnectionId = member.Previous != null && idByEntity.TryGetValue(member.Previous, out int prevId) ? prevId : -1;
    int nextConnectionId = member.Next != null && idByEntity.TryGetValue(member.Next, out int nextId) ? nextId : -1;
    noteList.Add(ConvUtil.ProcessHoldNote(entity, id, member, previousConnectionId, nextConnectionId));
    id++;
  }
}

MusicScoreEventData[] eventDataArray = [.. eventDataList];
Note[] noteArray = [.. noteList.OrderBy(n => n.ticks)];

MusicScoreMakerData score = new(
  1,
  eventDataArray,
  [],
  noteArray,
  noteArray.Max(n => n.ticks),
  -6767,
  null
);

File.WriteAllText(output, JsonSerializer.Serialize(score, options));