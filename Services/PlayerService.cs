using ScarletHooks.Data;
using System.Collections.Generic;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using System.Linq;
using ScarletHooks.Systems;

namespace ScarletHooks.Services;

public static class PlayerService {
  public static readonly Dictionary<string, PlayerData> PlayerNames = [];
  public static readonly Dictionary<ulong, PlayerData> PlayerIds = [];
  public static readonly Dictionary<NetworkId, PlayerData> PlayerNetworkIds = [];
  public static readonly List<PlayerData> AllPlayers = [];

  public static void Initialize() {
    ClearCache();
    EntityQueryBuilder queryBuilder = new(Allocator.Temp);

    queryBuilder.AddAll(ComponentType.ReadOnly<User>());
    queryBuilder.AddAll(ComponentType.ReadOnly<NetworkId>());
    queryBuilder.WithOptions(EntityQueryOptions.IncludeDisabled);

    EntityQuery query = Core.EntityManager.CreateEntityQuery(ref queryBuilder);

    try {
      var userEntities = query.ToEntityArray(Allocator.Temp);

      foreach (var entity in userEntities) {
        SetPlayerCache(entity);
      }
    } catch (System.Exception e) {
      Core.Log.LogError(e);
    } finally {
      query.Dispose();
      queryBuilder.Dispose();
    }
  }

  public static void ClearCache() {
    PlayerNames.Clear();
    PlayerIds.Clear();
  }

  public static void SetPlayerCache(Entity userEntity, bool isOffline = false) {
    var userData = userEntity.Read<User>();
    var name = userData.CharacterName.ToString();
    var networkId = userEntity.Read<NetworkId>();

    if (!PlayerIds.ContainsKey(userData.PlatformId)) {
      PlayerData newData = new();
      PlayerNames[name.ToLower()] = newData;
      PlayerIds[userData.PlatformId] = newData;
      PlayerNetworkIds[networkId] = newData;
      AllPlayers.Add(newData);
    }

    var playerData = PlayerIds[userData.PlatformId];

    if (!string.IsNullOrEmpty(playerData.Name) && playerData.Name != name) {
      PlayerNames.Remove(playerData.Name.ToLower());
      PlayerNames[name.ToLower()] = playerData;
    }

    if (playerData.NetworkId.IsValid && playerData.NetworkId != networkId) {
      PlayerNetworkIds.Remove(playerData.NetworkId);
      PlayerNetworkIds[networkId] = playerData;
    }

    playerData.Name = name;
    playerData.PlatformID = userData.PlatformId;
    playerData.IsOnline = !isOffline && userData.IsConnected;
    playerData.UserEntity = userEntity;
    playerData.CharacterEntity = userData.LocalCharacter._Entity;
    playerData.NetworkId = networkId;

    if (isOffline) {
      PlayerNetworkIds.Remove(networkId);
      MessageDispatchSystem.HandleLogoutMessage(playerData.Name, playerData.ClanName);
    } else {
      MessageDispatchSystem.HandleLoginMessage(playerData.Name, playerData.ClanName);
    }
  }

  public static void ClearOfflinePlayers() {
    AllPlayers.RemoveAll(p => {
      var remove = !p.IsOnline;

      PlayerIds.Remove(p.PlatformID);
      PlayerNames.Remove(p.Name.ToLower());
      PlayerNetworkIds.Remove(p.UserEntity.Read<NetworkId>());

      return remove;
    });
  }

  public static List<PlayerData> GetAdmins() {
    return [.. AllPlayers.Where(p => p.IsAdmin)];
  }

  public static bool TryGetById(ulong platformId, out PlayerData playerData) {
    return PlayerIds.TryGetValue(platformId, out playerData);
  }

  public static bool TryGetByName(string name, out PlayerData playerData) {
    return PlayerNames.TryGetValue(name.ToLower(), out playerData);
  }

  public static bool TryGetByNetworkId(NetworkId networkId, out PlayerData playerData) {
    return PlayerNetworkIds.TryGetValue(networkId, out playerData);
  }
}


