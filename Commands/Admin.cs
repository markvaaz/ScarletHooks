using VampireCommandFramework;
using ScarletHooks.Systems;
using ScarletHooks.Utils;
using ScarletHooks.Data;
using System.Collections.Generic;
using ScarletHooks.Services;

namespace ScarletHooks.Commands;

[CommandGroup("hooks")]
public static class AdminCommands {
  [Command("add")]
  public static void Add(ChatCommandContext ctx, string clanName) {
    if (string.IsNullOrEmpty(clanName)) {
      ctx.Reply("You must provide a clan name.".FormatError());
      return;
    }

    if (MessageDispatchSystem.ClanWebHookUrls.ContainsKey(clanName)) {
      ctx.Reply($"This clan already exists, please go to the config file to change the webhook url.".FormatError());
      ctx.Reply("Don't forget to reload the webhooks after changing the webhook url.".FormatError());
      return;
    }

    MessageDispatchSystem.AddClan(clanName);

    ctx.Reply($"Clan ~{clanName}~ added to the list of clans, please go to the config file to set the webhook url.".Format());
    ctx.Reply("Don't forget to reload the webhooks after changing the webhook url.".Format());
  }

  [Command("afp")]
  public static void AddFromPlayer(ChatCommandContext ctx, string playerName) {
    if (!PlayerService.TryGetByName(playerName, out PlayerData playerData) || string.IsNullOrEmpty(playerData.ClanName)) {
      ctx.Reply($"Player '{playerName}' not found or does not belong to a clan.".FormatError());
      return;
    }

    string clanName = playerData.ClanName;

    if (MessageDispatchSystem.ClanWebHookUrls.ContainsKey(clanName)) {
      ctx.Reply($"This clan already exists, please go to the config file to change the webhook url.".FormatError());
      ctx.Reply("Don't forget to reload the webhooks after changing the webhook url.".FormatError());
      return;
    }

    MessageDispatchSystem.AddClan(clanName);

    ctx.Reply($"Clan ~{clanName}~ (from player '{playerName}') added to the list of clans, please go to the config file to set the webhook url.".Format());
    ctx.Reply("Don't forget to reload the webhooks after changing the webhook url.".Format());
  }

  [Command("remove")]
  public static void Remove(ChatCommandContext ctx, string name) {
    string clanName = null;

    if (PlayerService.TryGetByName(name, out PlayerData playerData)) {
      clanName = playerData.ClanName;
    }

    if (string.IsNullOrEmpty(clanName)) {
      clanName = name;
    }

    if (string.IsNullOrEmpty(clanName)) return;

    if (!MessageDispatchSystem.ClanWebHookUrls.ContainsKey(clanName)) {
      ctx.Reply($"No clan named ~{clanName}~ is loaded.".FormatError());
      return;
    }

    MessageDispatchSystem.RemoveClan(clanName);
    ctx.Reply($"Clan ~{clanName}~ removed from the list of clans.".Format());
  }

  [Command("reload settings", description: "Reload all settings.")]
  public static void RealodSettings(ChatCommandContext ctx) {
    Settings.Reload();
    ctx.Reply("~Settings reloaded.~".Format());
  }

  [Command("reload webhooks", description: "Reload all webhooks.")]
  public static void RealodWebhooks(ChatCommandContext ctx) {
    MessageDispatchSystem.LoadFromFile();
    ctx.Reply("~Webhooks reloaded.~".Format());
  }

  [Command("reload", description: "Reload all settings and webhooks.")]
  public static void Realod(ChatCommandContext ctx) {
    Settings.Reload();
    MessageDispatchSystem.LoadFromFile();

    ctx.Reply("~Settings and webhooks reloaded.~".Format());
  }

  [Command("list", description: "List all webhooks.")]
  public static void ListClanWebHookUrls(ChatCommandContext ctx) {
    ctx.Reply($"~Admin~: {Settings.Get<string>("AdminWebhookURL")}.".Format());

    ctx.Reply($"~Public~: {Settings.Get<string>("PublicWebhookURL")}.".Format());

    ctx.Reply("List of clans webhooks: ".Format());

    foreach (var (clanName, url) in MessageDispatchSystem.ClanWebHookUrls) {
      ctx.Reply($"~{clanName}~: {url}.".Format());
    }

    ctx.Reply($"Total webhook urls: ~{MessageDispatchSystem.ClanWebHookUrls.Count + 2}~.".Format());
  }

  [Command("settings")]
  public static void ChangeSettings(ChatCommandContext ctx, string settings, bool value) {
    if (!Settings.Has(settings)) {
      ctx.Reply($"~{settings}~ does not exist.".Format());
      return;
    }

    List<string> exludedSettings = ["AdminWebhookURL", "PublicWebhookURL", "MessageInterval", "OnFailInterval"];

    if (exludedSettings.Contains(settings)) {
      ctx.Reply($"~{settings}~ cannot be changed via command.".Format());
      return;
    }

    Settings.Set(settings, value);
    ctx.Reply($"~{settings}~ changed to ~{value}~.".Format());
  }

  [Command("settings")]
  public static void ShowSettings(ChatCommandContext ctx) {
    ctx.Reply("~Current settings:~".Format());
    ctx.Reply($"AdminWebhookURL: ~{Settings.Get<string>("AdminWebhookURL")}~".Format());
    ctx.Reply($"PublicWebhookURL: ~{Settings.Get<string>("PublicWebhookURL")}~".Format());
    ctx.Reply($"MessageInterval: ~{Settings.Get<float>("MessageInterval")}~".Format());
    ctx.Reply($"OnFailInterval: ~{Settings.Get<float>("OnFailInterval")}~".Format());
    ctx.Reply($"EnableBatching: ~{Settings.Get<bool>("EnableBatching")}~".Format());

    ctx.Reply($"AdminGlobalMessages: ~{Settings.Get<bool>("AdminGlobalMessages")}~".Format());
    ctx.Reply($"AdminClanMessages: ~{Settings.Get<bool>("AdminClanMessages")}~".Format());
    ctx.Reply($"AdminLocalMessages: ~{Settings.Get<bool>("AdminLocalMessages")}~".Format());
    ctx.Reply($"AdminWhisperMessages: ~{Settings.Get<bool>("AdminWhisperMessages")}~".Format());

    ctx.Reply($"PublicGlobalMessages: ~{Settings.Get<bool>("PublicGlobalMessages")}~".Format());
    ctx.Reply($"PublicClanMessages: ~{Settings.Get<bool>("PublicClanMessages")}~".Format());
    ctx.Reply($"PublicLocalMessages: ~{Settings.Get<bool>("PublicLocalMessages")}~".Format());
    ctx.Reply($"PublicWhisperMessages: ~{Settings.Get<bool>("PublicWhisperMessages")}~".Format());
  }

  [Command("start")]
  public static void Start(ChatCommandContext ctx) {
    MessageDispatchSystem.Initialize();
    ctx.Reply("~Message dispatch system started.~".Format());
  }

  [Command("stop")]
  public static void Stop(ChatCommandContext ctx) {
    MessageDispatchSystem.ForceShutdown();
    ctx.Reply("~Message dispatch system stopped.~".Format());
  }

  [Command("forcestop")]
  public static void ForceStop(ChatCommandContext ctx) {
    MessageDispatchSystem.ForceShutdown();
    ctx.Reply("~Message dispatch system stopped and cleared all cache.~".Format());
  }
}