using NextSekai;
using Sekai;

public class ConvUtil
{
  public static string[] EventDataArchetypes =
  {
    "#BPM_CHANGE",
    "#TIMESCALE_CHANGE",
  };

  public static string[] NoteArchetypes =
  {
    "NormalTapNote",
    "CriticalTapNote",
    "NormalFlickNote",
    "CriticalFlickNote",
    "NormalTraceNote",
    "CriticalTraceNote",
    "NormalTraceFlickNote",
    "CriticalTraceFlickNote",
  };

  public static long Beat2Ticks(double beat)
  {
    return (long)(480L * beat);
  }

  public static (int, int) UnconvertLane(double lane, double size)
  {
    double laneStart = Math.Clamp(lane - size + 5.5 + 0.5, 0, 11);
    double laneEnd = Math.Clamp(lane + size + 5.5 - 1 + 0.5, 0, 11);
    return ((int)laneStart, (int)laneEnd);
  }

  public static MusicScoreEventData ProcessEventData(Entity entity, int id)
  {
    if (EventDataArchetypes.Contains(entity.archetype))
    {
      MusicScoreEventType eventType = GetEventType(entity.archetype);
      long ticks = Beat2Ticks(entity.data.FirstOrDefault(data => data.name == "#BEAT").value);
      object changeValue = eventType switch
      {
        MusicScoreEventType.BPM => entity.data.FirstOrDefault(data => data.name == "#BPM").value,
        MusicScoreEventType.HighSpeed => entity.data.First(data => data.name == "#TIMESCALE").value,
        _ => null,
      };

      return new MusicScoreEventData(
        id,
        eventType,
        ticks,
        changeValue
      );
    }

    throw new InvalidOperationException($"Attempted to run ProcessEventData on a non-event-data entity. Archetype: {entity.archetype}.");
  }

  public static MusicScoreEventType GetEventType(string archetype)
  {
    if (archetype == "#BPM_CHANGE")
    {
      return MusicScoreEventType.BPM;
    }
    else if (archetype == "#TIMESCALE_CHANGE")
    {
      return MusicScoreEventType.HighSpeed;
    }

    throw new ArgumentException($"Unable to process archetype {archetype} to MusicScoreEventType.");
  }

  public static Note ProcessNote(Entity entity, int id)
  {
    if (NoteArchetypes.Contains(entity.archetype))
    {
      long ticks = Beat2Ticks(entity.data.FirstOrDefault(data => data.name == "#BEAT").value);
      (int, int) lanes = UnconvertLane(entity.data.FirstOrDefault(data => data.name == "lane").value,
        entity.data.FirstOrDefault(data => data.name == "size").value);
      NoteCategory category = GetNoteCategory(entity.archetype);
      NoteType type = entity.archetype.Contains("Critical") ? NoteType.Critical : NoteType.Default;
      NoteBaseType noteBaseType = GetNoteBaseType(category);
      NoteDirection direction = entity.data.FirstOrDefault(data => data.name == "direction").value switch
      {
        0 => NoteDirection.Default, // UP_OMNI
        1 => NoteDirection.Left,    // UP_LEFT
        2 => NoteDirection.Right,   // UP_RIGHT
        3 => NoteDirection.Default, // DOWN_OMNI
        4 => NoteDirection.Left,    // DOWN_LEFT
        5 => NoteDirection.Right,   // DOWN_RIGHT
        _ => NoteDirection.Default
      };

      return new Note(
        id,
        ticks,
        lanes.Item1,
        lanes.Item2,
        category,
        type,
        1.0,
        NoteLineType.Linear, // unused till holds
        noteBaseType,
        -1, // unused till holds
        -1, // unused till holds
        direction,
        false // ???
      );
    }

    throw new InvalidOperationException($"Attempted to run ProcessNote on a non-note entity. Archetype: {entity.archetype}.");
  }

  public static NoteCategory GetNoteCategory(string archetype)
  {
    if (archetype.EndsWith("alTapNote"))
    {
      return NoteCategory.Normal;
    }
    else if (archetype.EndsWith("alFlickNote"))
    {
      return NoteCategory.Flick;
    }
    else if (archetype.EndsWith("alTraceNote"))
    {
      return NoteCategory.Friction;
    }
    else if (archetype.EndsWith("alTraceFlickNote"))
    {
      return NoteCategory.FrictionFlick;
    }

    throw new ArgumentException($"Unable to process archetype {archetype} to NoteCategory.");
  }

  public static NoteBaseType GetNoteBaseType(NoteCategory category)
  {
    return category switch
    {
      NoteCategory.Normal => NoteBaseType.Normal,
      NoteCategory.Long => NoteBaseType.Long,
      NoteCategory.Connection => NoteBaseType.Connection,
      NoteCategory.Flick => NoteBaseType.Flick,
      NoteCategory.Friction => NoteBaseType.Friction,
      NoteCategory.FrictionHide => NoteBaseType.FrictionHide,
      NoteCategory.FrictionLong => NoteBaseType.FrictionLong,
      NoteCategory.FrictionHideLong => NoteBaseType.FrictionHideLong,
      NoteCategory.FrictionFlick => NoteBaseType.FrictionFlick,
      NoteCategory.Guide => NoteBaseType.Guide,
      NoteCategory.GuideEnd => NoteBaseType.GuideEnd,
      NoteCategory.GuideHidden => NoteBaseType.GuideHiddenConnection,
      NoteCategory.Combo => NoteBaseType.LongHoldCombo,
      _ => NoteBaseType.Base,
    };
  }
}