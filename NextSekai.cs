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
}