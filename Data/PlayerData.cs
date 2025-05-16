using Unity.Entities;
using ProjectM.Network;
using System.Text.Json.Serialization;
using ProjectM;

namespace ScarletHooks.Data;

public class PlayerData {
  [JsonIgnore]
  public string Name { get; set; } = default;
  [JsonIgnore]
  public Entity UserEntity { get; set; } = default;
  [JsonIgnore]
  public Entity CharacterEntity { get; set; } = default;
  [JsonIgnore]
  public ulong PlatformID { get; set; } = 0;
  [JsonIgnore]
  public bool IsOnline { get; set; } = false;
  [JsonIgnore]
  public NetworkId NetworkId { get; set; }
  [JsonIgnore]
  public bool IsAdmin => UserEntity.Read<User>().IsAdmin;
  public string ClanName {
    get {
      var clanEntity = UserEntity.Read<User>().ClanEntity._Entity;

      if (clanEntity.Equals(Entity.Null)) return null;

      var clanTeam = clanEntity.Read<ClanTeam>();

      if (clanTeam.Equals(default)) return null;

      return clanTeam.Name.ToString();
    }
  }
}
