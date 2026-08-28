using System.Text.Json.Serialization;

namespace Sekai
{
  public class MusicScoreMakerData
  {
    [JsonPropertyName("$id")]
    public string _id;

    [JsonPropertyName("VersionCode")]
    public int VersionCode;

    [JsonPropertyName("MusicScoreEventDataList")]
    public MusicScoreEventData[] MusicScoreEventDataList;

    [JsonPropertyName("EventArray")]
    public object[] EventArray; // empty until skill/fever

    [JsonPropertyName("NoteList")]
    public Note[] NoteList;

    [JsonPropertyName("MusicScoreTicksMax")]
    public long MusicScoreTicksMax;

    [JsonPropertyName("MusicId")]
    public int MusicId;

    [JsonPropertyName("FullComboDataHash")]
    public object FullComboDataHash; // always null from what i've seen

    public MusicScoreMakerData(int versionCode, MusicScoreEventData[] eventDataList, object[] eventArray, Note[] noteList, long ticksMax, int musicId, object fcDataHash)
    {
      _id = "1";
      VersionCode = versionCode;
      MusicScoreEventDataList = eventDataList;
      EventArray = eventArray;
      NoteList = noteList;
      MusicScoreTicksMax = ticksMax;
      MusicId = musicId;
      FullComboDataHash = fcDataHash;
    }
  }

  public enum MusicScoreEventType
	{
		BPM = 0,
		HighSpeed = 1,
		SeVolume = 2,
		TimeSignature = 3
	}

  public class MusicScoreEventData
	{
    [JsonPropertyName("$id")]
    public string _id;
 
		[JsonPropertyName("id")]
		public int id;

		[JsonPropertyName("eventType")]
		public MusicScoreEventType eventType;

		[JsonPropertyName("ticks")]
		public long ticks;

		[JsonPropertyName("changeValue")]
		public object changeValue;

    public MusicScoreEventData(int id, MusicScoreEventType eventType, long ticks, object changeValue)
    {
      _id = $"{id + 1}";
      this.id = id;
      this.eventType = eventType;
      this.ticks = ticks;
      this.changeValue = changeValue;
    }
	}

  public class Note
  {
    [JsonPropertyName("$id")]
    public string _id;
 
		[JsonPropertyName("id")]
		public int id;

    [JsonPropertyName("ticks")]
		public long ticks;

    [JsonPropertyName("laneStart")]
    public int laneStart;

    [JsonPropertyName("laneEnd")]
    public int laneEnd;

    [JsonPropertyName("category")]
    public NoteCategory category;

    [JsonPropertyName("type")]
    public NoteType type;

    [JsonPropertyName("speedRatio")]
    public double speedRatio;

    [JsonPropertyName("noteLineType")]
    public NoteLineType noteLineType;

    [JsonPropertyName("noteBaseType")]
    public NoteBaseType noteBaseType;

    [JsonPropertyName("previousConnectionId")]
		public int previousConnectionId;

		[JsonPropertyName("nextConnectionId")]
		public int nextConnectionId;

    [JsonPropertyName("direction")]
    public NoteDirection direction;

    [JsonPropertyName("isSkip")]
    public bool isSkip;

    [JsonPropertyName("IsSingle")]
    public bool IsSingle => previousConnectionId == -1 && nextConnectionId == -1;

    [JsonPropertyName("IsConnectedFirst")]
    public bool IsConnectedFirst => previousConnectionId == -1 && nextConnectionId != -1;

    [JsonPropertyName("IsConnectedLast")]
    public bool IsConnectedLast => previousConnectionId != -1 && nextConnectionId == -1;

    public Note(int id, long ticks, int laneStart, int laneEnd, NoteCategory category, NoteType type, double speedRatio, NoteLineType noteLineType, NoteBaseType noteBaseType, int previousConnectionId, int nextConnectionId, NoteDirection direction, bool isSkip)
    {
      _id = $"{id + 1}";
      this.id = id;
      this.ticks = ticks;
      this.laneStart = laneStart;
      this.laneEnd = laneEnd;
      this.category = category;
      this.type = type;
      this.speedRatio = speedRatio;
      this.noteLineType = noteLineType;
      this.noteBaseType = noteBaseType;
      this.previousConnectionId = previousConnectionId;
      this.nextConnectionId = nextConnectionId;
      this.direction = direction;
      this.isSkip = isSkip;
    }
  }

  public enum NoteCategory
	{
		Normal = 0,
		Long = 1,
		Connection = 2,
		Flick = 3,
		Friction = 4,
		FrictionHide = 5,
		FrictionLong = 6,
		FrictionHideLong = 7,
		FrictionFlick = 8,
		Guide = 9,
		GuideEnd = 10,
		GuideHidden = 11,
		Combo = 12,
		Hidden = 13,
		Skip = 14,
		Error = 15
	}

  public enum NoteType
	{
		Default = 0,
		Critical = 1
	}

  public enum NoteLineType
	{
		Linear = 0,
		EaseOut = 1,
		EaseIn = 2
	}

  public enum NoteBaseType
	{
		Base = 0,
		Normal = 1,
		Long = 2,
		Flick = 3,
		FrictionFlick = 4,
		Connection = 5,
		HiddenConnection = 6,
		LongHoldCombo = 7,
		FrictionLong = 8,
		FrictionHideLong = 9,
		Guide = 10,
		Friction = 11,
		FrictionHide = 12,
		GuideEnd = 13,
		GuideHiddenConnection = 14
	}

  public enum NoteDirection
	{
		Default = 0,
		Left = 1,
		Right = 2
	}
}