using System.Linq;
using System.Text.Json.Serialization;

namespace NextSekai
{
  public class LevelData
  {
    [JsonPropertyName("bgmOffset")]
    public double bgmOffset;

    [JsonPropertyName("entities")]
    public Entity[] entities;
  }

  public class Entity
  {
    [JsonPropertyName("name")]
    public string name;

    [JsonPropertyName("archetype")]
    public string archetype;

    [JsonPropertyName("data")]
    public Data[] data;
  }

  public class Data
  {
    [JsonPropertyName("name")]
    public string name;

    [JsonPropertyName("value")]
    public double value;

    [JsonPropertyName("ref")]
    public string _ref;
  }

  public static class EntityExtensions
  {
    public static bool TryGetValue(this Entity entity, string name, out double value)
    {
      Data data = entity.data?.FirstOrDefault(d => d.name == name);
      if (data != null)
      {
        value = data.value;
        return true;
      }
      value = 0;
      return false;
    }

    public static bool TryGetRef(this Entity entity, string name, out string reference)
    {
      Data data = entity.data?.FirstOrDefault(d => d.name == name && d._ref != null);
      if (data != null)
      {
        reference = data._ref;
        return true;
      }
      reference = null;
      return false;
    }

    public static bool HasNext(this Entity entity, out string next) => entity.TryGetRef("next", out next);
  }
}