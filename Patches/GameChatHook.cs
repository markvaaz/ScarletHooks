using ProjectM.Network;
using ProjectM;
using Unity.Entities;
using HarmonyLib;
using Unity.Collections;
using System;
using ScarletHooks.Systems;
using ScarletHooks.Services;

namespace ScarletHooks.Patches;

[HarmonyPatch(typeof(ChatMessageSystem), nameof(ChatMessageSystem.OnUpdate))]
public static class ChatMessageSystem_Patch {
  public static void Prefix(ChatMessageSystem __instance) {
    NativeArray<Entity> entities = __instance.__query_661171423_0.ToEntityArray(Allocator.Temp);

    try {
      foreach (var entity in entities) {
        var fromData = __instance.EntityManager.GetComponentData<FromCharacter>(entity);
        var userData = __instance.EntityManager.GetComponentData<User>(fromData.User);
        var chatEventData = __instance.EntityManager.GetComponentData<ChatMessageEvent>(entity);
        var messageType = chatEventData.MessageType;

        string clanName = null;
        string targetName = null;
        if (__instance.EntityManager.Exists(userData.ClanEntity._Entity) &&
            __instance.EntityManager.HasComponent<ClanTeam>(userData.ClanEntity._Entity)) {
          clanName = __instance.EntityManager.GetComponentData<ClanTeam>(userData.ClanEntity._Entity).Name.ToString();
        }

        if (messageType == ChatMessageType.Whisper && PlayerService.TryGetByNetworkId(chatEventData.ReceiverEntity, out var playerData) && playerData != null) {
          targetName = playerData.Name;
        }

        MessageDispatchSystem.HandleMessage(content: chatEventData.MessageText.ToString(), playerName: userData.CharacterName.ToString(), messageType: messageType, clanName: clanName, targetName: targetName);
      }
    } catch (Exception e) {
      Plugin.LogInstance.LogError($"An error occurred while processing chat message: {e.Message}");
    } finally {
      entities.Dispose();
    }
  }
}