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

  public static string[] HoldHeadArchetypes =
  {
    "NormalHeadTapNote",
    "CriticalHeadTapNote",
    "NormalHeadFlickNote",
    "CriticalHeadFlickNote",
    "NormalHeadTraceNote",
    "CriticalHeadTraceNote",
    "NormalHeadTraceFlickNote",
    "CriticalHeadTraceFlickNote",
  };

  public static string[] HoldTailArchetypes =
  {
    "NormalTailReleaseNote",
    "CriticalTailReleaseNote",
    "NormalTailFlickNote",
    "CriticalTailFlickNote",
    "NormalTailTraceNote",
    "CriticalTailTraceNote",
    "NormalTailTraceFlickNote",
    "CriticalTailTraceFlickNote",
  };

  public static string[] HoldTickArchetypes =
  {
    "NormalTickNote",
    "CriticalTickNote",
    "NormalTraceTickNote",
    "CriticalTraceTickNote",
  };

  public const string AnchorArchetype = "AnchorNote";

  public const int SegmentKindGreen = 103;
  public const int SegmentKindYellow = 105;

  public static bool IsHoldArchetype(string archetype) =>
    !archetype.Contains("Transient") &&
    (HoldHeadArchetypes.Contains(archetype) ||
    HoldTailArchetypes.Contains(archetype) ||
    HoldTickArchetypes.Contains(archetype) ||
    archetype == AnchorArchetype);

  public static long Beat2Ticks(double beat)
  {
    return (long)(480L * beat);
  }

  public static (int, int) UnconvertLane(double lane, double size)
  {
    double effectiveSize = Math.Max(size, 0.5);
    double laneStart = Math.Clamp(lane - effectiveSize + 5.5 + 0.5, 0, 11);
    double laneEnd = Math.Clamp(lane + effectiveSize + 5.5 - 1 + 0.5, 0, 11);
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
      NoteCategory.Hidden => NoteBaseType.HiddenConnection,
      NoteCategory.Skip => NoteBaseType.HiddenConnection,
      _ => NoteBaseType.Base,
    };
  }

  private static NoteDirection GetDirection(Entity entity)
  {
    if (!entity.TryGetValue("direction", out double dirValue))
    {
      return NoteDirection.Default;
    }

    return (int)dirValue switch
    {
      0 => NoteDirection.Default, // UP_OMNI
      1 => NoteDirection.Left,    // UP_LEFT
      2 => NoteDirection.Right,   // UP_RIGHT
      3 => NoteDirection.Default, // DOWN_OMNI
      4 => NoteDirection.Left,    // DOWN_LEFT
      5 => NoteDirection.Right,   // DOWN_RIGHT
      _ => NoteDirection.Default
    };
  }

  private static NoteLineType GetLineType(Entity entity)
  {
    if (!entity.TryGetValue("connectorEase", out double easeValue))
    {
      return NoteLineType.Linear;
    }

    return (int)easeValue switch
    {
      2 => NoteLineType.EaseIn,
      4 => NoteLineType.EaseIn,
      3 => NoteLineType.EaseOut,
      5 => NoteLineType.EaseOut,
      _ => NoteLineType.Linear,
    };
  }

  public class HoldChainMember
  {
    public bool IsFirst;
    public bool IsLast;
    public int? GuideSegmentKind;
    public Entity Previous;
    public Entity Next;
  }

  public static Dictionary<Entity, HoldChainMember> BuildHoldChainMap(Entity[] entities)
  {
    Dictionary<string, Entity> byName = entities
      .Where(e => !string.IsNullOrEmpty(e.name))
      .ToDictionary(e => e.name, e => e);

    List<Entity> holdEntities = entities.Where(e => IsHoldArchetype(e.archetype)).ToList();

    HashSet<Entity> hasParent = new HashSet<Entity>();
    foreach (Entity e in holdEntities)
    {
      if (e.HasNext(out string next) && byName.TryGetValue(next, out Entity target))
      {
        hasParent.Add(target);
      }
    }

    Dictionary<Entity, HoldChainMember> result = new Dictionary<Entity, HoldChainMember>();

    foreach (Entity entity in holdEntities)
    {
      bool hasNext = entity.HasNext(out _);
      bool isHead = hasNext && !hasParent.Contains(entity);
      if (!isHead) continue;

      List<Entity> chainEntities = new List<Entity>();
      Entity current = entity;
      while (true)
      {
        chainEntities.Add(current);
        if (!current.HasNext(out string nextName)) break;
        if (!byName.TryGetValue(nextName, out Entity nextEntity)) break;
        current = nextEntity;
      }

      if (chainEntities.Count < 2) continue;

      RegisterHoldChain(chainEntities, result);
    }

    return result;
  }

  private static void RegisterHoldChain(List<Entity> chainEntities, Dictionary<Entity, HoldChainMember> result)
  {
    int? guideKind = null;
    foreach (Entity e in chainEntities)
    {
      if (e.TryGetValue("segmentKind", out double kindValue))
      {
        int kind = (int)kindValue;
        if (kind >= 101 && kind <= 108 && guideKind == null)
        {
          guideKind = kind;
        }
      }
    }

    if (guideKind != null && guideKind != SegmentKindGreen && guideKind != SegmentKindYellow)
    {
      guideKind = SegmentKindGreen;
    }

    for (int i = 0; i < chainEntities.Count; i++)
    {
      result[chainEntities[i]] = new HoldChainMember
      {
        IsFirst = i == 0,
        IsLast = i == chainEntities.Count - 1,
        GuideSegmentKind = guideKind,
        Previous = i > 0 ? chainEntities[i - 1] : null,
        Next = i < chainEntities.Count - 1 ? chainEntities[i + 1] : null,
      };
    }
  }

  public static Note ProcessHoldNote(Entity entity, int id, HoldChainMember member,
    int previousConnectionId, int nextConnectionId)
  {
    long ticks = Beat2Ticks(entity.TryGetValue("#BEAT", out double beat) ? beat : 0);
    (int, int) lanes = UnconvertLane(
      entity.TryGetValue("lane", out double lane) ? lane : 0,
      entity.TryGetValue("size", out double size) ? size : 1);

    bool isCritical = entity.archetype.Contains("Critical");
    bool isTrace = entity.archetype.Contains("Trace") || entity.archetype.Contains("TraceTick");
    bool isFlick = entity.archetype.Contains("Flick");
    bool isAnchor = entity.archetype == AnchorArchetype;
    bool isTick = entity.archetype.Contains("Tick");
    NoteType type = isCritical ? NoteType.Critical : NoteType.Default;
    NoteDirection direction = isFlick ? GetDirection(entity) : NoteDirection.Default;

    NoteCategory category;

    if (member.GuideSegmentKind != null)
    {
      category = member.IsFirst ? NoteCategory.Guide
        : member.IsLast ? NoteCategory.GuideEnd
        : NoteCategory.GuideHidden;
    }
    else if (isAnchor)
    {
      category = (member.IsFirst || member.IsLast) ? NoteCategory.Hidden : NoteCategory.Skip;
    }
    else if (isTick)
    {
      category = isTrace ? NoteCategory.FrictionLong : NoteCategory.Connection;
    }
    else if (member.IsLast && isFlick)
    {
      category = isTrace ? NoteCategory.FrictionFlick : NoteCategory.Flick;
    }
    else
    {
      category = isTrace ? NoteCategory.FrictionLong : NoteCategory.Long;
    }

    NoteBaseType noteBaseType = GetNoteBaseType(category);
    NoteLineType lineType = member.IsLast ? NoteLineType.Linear : GetLineType(entity);
    bool isSkip = category == NoteCategory.Skip;

    return new Note(
      id,
      ticks,
      lanes.Item1,
      lanes.Item2,
      category,
      type,
      1.0,
      lineType,
      noteBaseType,
      previousConnectionId,
      nextConnectionId,
      direction,
      isSkip
    );
  }
}