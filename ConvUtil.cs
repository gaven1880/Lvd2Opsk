using NextSekai;
using Sekai;

public class ConvUtil
{
  public static string[] EventDataArchetypes =
  [
    "#BPM_CHANGE",
    "#TIMESCALE_CHANGE",
  ];

  public static string[] NoteArchetypes =
  [
    "NormalTapNote",
    "CriticalTapNote",
    "NormalFlickNote",
    "CriticalFlickNote",
    "NormalTraceNote",
    "CriticalTraceNote",
    "NormalTraceFlickNote",
    "CriticalTraceFlickNote",
  ];

  public static string[] LongHeadArchetypes =
  [
    "NormalHeadTapNote",
    "CriticalHeadTapNote",
    "NormalHeadFlickNote",
    "CriticalHeadFlickNote",
    "NormalHeadTraceNote",
    "CriticalHeadTraceNote",
    "NormalHeadTraceFlickNote",
    "CriticalHeadTraceFlickNote",
    "NormalHeadReleaseNote",
    "CriticalHeadReleaseNote",
  ];

  public static string[] LongTailArchetypes =
  [
    "NormalTailTapNote",
    "CriticalTailTapNote",
    "NormalTailFlickNote",
    "CriticalTailFlickNote",
    "NormalTailTraceNote",
    "CriticalTailTraceNote",
    "NormalTailTraceFlickNote",
    "CriticalTailTraceFlickNote",
    "NormalTailReleaseNote",
    "CriticalTailReleaseNote",
  ];

  public static string[] ConnectionArchetypes =
  [
    "NormalTickNote",
    "CriticalTickNote",
    "TransientHiddenTickNote",
  ];

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

  public static (Entity[], Entity[], Entity[]) FilterEntities(Entity[] entities)
  {
    List<Entity> eventDataEntities = new List<Entity>();
    List<Entity> noteEntities = new List<Entity>();
    List<Entity> longEntities = new List<Entity>();

    foreach (Entity entity in entities)
    {
      if (EventDataArchetypes.Contains(entity.archetype))
      {
        eventDataEntities.Add(entity);
      }
      else if (NoteArchetypes.Contains(entity.archetype))
      {
        noteEntities.Add(entity);
      }
      else if (LongHeadArchetypes.Contains(entity.archetype))
      {
        longEntities.Add(entity);

        Entity[] followedEntities = FollowLongHead(entities, entity.name);
        foreach (Entity followedEntity in followedEntities)
        {
          if (!longEntities.Contains(followedEntity))
          {
            longEntities.Add(followedEntity);
          }
        }
      }
    }

    return (eventDataEntities.ToArray(), noteEntities.ToArray(), longEntities.ToArray());
  }

  public static Entity[] FollowLongHead(Entity[] entities, string targetName)
  {
    List<Entity> outList = new List<Entity>();

    Entity targetEntity = entities.FirstOrDefault(e => e.name == targetName);

    if (targetEntity == null)
    {
      return outList.ToArray();
    }

    Data nextData = targetEntity.data.FirstOrDefault(data => data.name == "next");

    if (nextData != null)
    {
      Entity nextEntity = entities.FirstOrDefault(e => e.name == nextData._ref);
      if (nextEntity != null)
      {
        outList.Add(nextEntity);

        Entity[] nexterEntities = FollowLongHead(entities, nextEntity.name);

        foreach (Entity nexterEntity in nexterEntities)
        {
          if (!outList.Contains(nexterEntity))
          {
            outList.Add(nexterEntity);
          }
        }
      }
    }

    return outList.ToArray();
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
    if (NoteArchetypes.Contains(entity.archetype) || LongHeadArchetypes.Contains(entity.archetype) ||
        LongTailArchetypes.Contains(entity.archetype) || ConnectionArchetypes.Contains(entity.archetype) ||
        entity.archetype == "AnchorNote")
    {
      long ticks = Beat2Ticks(entity.data.FirstOrDefault(data => data.name == "#BEAT").value);
      (int, int) lanes = UnconvertLane(entity.data.FirstOrDefault(data => data.name == "lane").value,
        entity.data.FirstOrDefault(data => data.name == "size").value);
      NoteCategory category = GetNoteCategory(entity.archetype);
      NoteType type = entity.archetype.Contains("Critical") ? NoteType.Critical : NoteType.Default;
      NoteLineType noteLineType = GetNoteLineType((int)entity.data.FirstOrDefault(data => data.name == "connectorEase").value);
      NoteBaseType noteBaseType = GetNoteBaseType(category, false, false, true);
      NoteDirection direction = GetNoteDirection((int)entity.data.FirstOrDefault(data => data.name == "direction").value);

      return new Note(
        id,
        ticks,
        lanes.Item1,
        lanes.Item2,
        category,
        type,
        1.0,
        noteLineType,
        noteBaseType,
        -1, // long notes will implement manually in Program.cs
        -1, // long notes will implement manually in Program.cs
        direction,
        category == NoteCategory.Skip
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
    else if (archetype.EndsWith("alFlickNote") || archetype.EndsWith("alTailFlickNote"))
    {
      return NoteCategory.Flick;
    }
    else if (archetype.EndsWith("alTraceNote") || archetype.EndsWith("alTailTraceNote"))
    {
      return NoteCategory.Friction;
    }
    else if (archetype.EndsWith("alTraceFlickNote") || archetype.EndsWith("alTailTraceFlickNote"))
    {
      return NoteCategory.FrictionFlick;
    }
    else if (archetype.EndsWith("alHeadTapNote") || archetype.EndsWith("alHeadFlickNote") ||
             archetype.EndsWith("alHeadReleaseNote") || archetype.EndsWith("alTailTapNote") ||
             archetype.EndsWith("alTailReleaseNote"))
    {
      return NoteCategory.Long;
    }
    else if (archetype.EndsWith("alHeadTraceNote") || archetype.EndsWith("alHeadTraceFlickNote"))
    {
      return NoteCategory.FrictionLong;
    }
    else if (archetype.EndsWith("alTickNote"))
    {
      return NoteCategory.Connection;
    }
    else if (archetype == "TransientHiddenTickNote")
    {
      return NoteCategory.Hidden;
    }
    else if (archetype == "AnchorNote")
    {
      return NoteCategory.FrictionHideLong;
    }

    throw new ArgumentException($"Unable to process archetype {archetype} to NoteCategory.");
  }

  public static NoteLineType GetNoteLineType(int connectorEase)
  {
    return connectorEase switch
    {
      2 => NoteLineType.EaseIn,  // IN_QUAD
      3 => NoteLineType.EaseOut, // OUT_QUAD
      _ => NoteLineType.Linear,  // NONE, LINEAR, IN_OUT_QUAD, OUT_IN_QUAD
    };
  }

  // original: https://github.com/UntitledCharts/sonolus-level-converters/blob/main/sonolus_converters/pjsk/exporter.py#L41
  public static NoteBaseType GetNoteBaseType(NoteCategory category, bool isConnectedFirst, bool isConnectedLast, bool isSingle)
  {
    if (isSingle)
    {
      return category switch
      {
        NoteCategory.Normal => NoteBaseType.Normal,
        NoteCategory.Flick => NoteBaseType.Flick,
        NoteCategory.Friction => NoteBaseType.Friction,
        NoteCategory.FrictionHide => NoteBaseType.FrictionHide,
        NoteCategory.FrictionFlick => NoteBaseType.FrictionFlick,
        _ => NoteBaseType.Normal,
      };
    }

    if (isConnectedFirst)
    {
      return category switch
      {
        NoteCategory.Long => NoteBaseType.Long,
        NoteCategory.FrictionLong => NoteBaseType.FrictionLong,
        NoteCategory.FrictionHideLong => NoteBaseType.FrictionHideLong,
        NoteCategory.Guide => NoteBaseType.Guide,
        _ => NoteBaseType.Long,
      };
    }

    if (isConnectedLast)
    {
      return category switch
      {
        NoteCategory.Normal or NoteCategory.Long => NoteBaseType.Normal,
        NoteCategory.Flick => NoteBaseType.Flick,
        NoteCategory.Friction => NoteBaseType.Friction,
        NoteCategory.FrictionHide => NoteBaseType.FrictionHide,
        NoteCategory.FrictionFlick => NoteBaseType.FrictionFlick,
        NoteCategory.GuideEnd => NoteBaseType.GuideEnd,
        _ => NoteBaseType.Normal,
      };
    }

    // mid
    return category switch
    {
      NoteCategory.Connection => NoteBaseType.Connection,
      NoteCategory.Hidden => NoteBaseType.HiddenConnection,
      NoteCategory.GuideHidden => NoteBaseType.GuideHiddenConnection,
      _ => NoteBaseType.Connection,
    };
  }

  public static NoteDirection GetNoteDirection(int direction)
  {
    return direction switch
    {
      1 => NoteDirection.Left,    // UP_LEFT
      2 => NoteDirection.Right,   // UP_RIGHT
      4 => NoteDirection.Left,    // DOWN_LEFT
      5 => NoteDirection.Right,   // DOWN_RIGHT
      _ => NoteDirection.Default  // UP_OMNI, DOWN_OMNI, or no direction
    };
  }
}