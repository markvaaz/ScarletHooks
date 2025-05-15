using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;

namespace ScarletHooks.Data;

public static class Settings {
  public static readonly string ConfigPath = Path.Combine(Paths.ConfigPath, "ScarletAuras");
  private static readonly Dictionary<string, object> Entries = [];
  private static readonly List<string> OrderedSections = ["General", "Admin", "Public"];

  public static void Initialize() {
    Add("General", "MessageInterval", 0.2f, "Interval in seconds between sending messages.\nUseful to prevent rate limiting when sending messages to webhooks.");
    Add("General", "OnFailInterval", 2f, "Interval in seconds to wait before retrying after a webhook failure.");
    Add("General", "AdminWebhookURL", "null", "Admin Webhook URL. All messages configured for admin will be sent to this address.");
    Add("General", "PublicWebhookURL", "null", "Public Webhook URL. All messages configured for public will be sent to this address.");
    Add("General", "EnableBatching", true, "Enable or disable batching messages to avoid rate limiting.\nUseful for large servers or when many messages are sent at once.");

    Add("Admin", "AdminGlobalMessages", true, "Enable or disable sending global chat messages to the Admin Webhook.");
    Add("Admin", "AdminLocalMessages", true, "Enable or disable sending local chat messages to the Admin Webhook.");
    Add("Admin", "AdminClanMessages", true, "Enable or disable sending clan chat messages to the Admin Webhook.");
    Add("Admin", "AdminWhisperMessages", true, "Enable or disable sending whisper messages to the Admin Webhook.");

    Add("Public", "PublicGlobalMessages", true, "Enable or disable sending global chat messages to the Public Webhook.");
    Add("Public", "PublicLocalMessages", false, "Enable or disable sending local chat messages to the Public Webhook.");
    Add("Public", "PublicClanMessages", false, "Enable or disable sending clan chat messages to the Public Webhook.");
    Add("Public", "PublicWhisperMessages", false, "Enable or disable sending whisper messages to the Public Webhook.");

    ReorderSections();
  }

  public static void Reload() {
    Entries.Clear();
    Plugin.Instance.Config.Reload();
    Initialize();
  }

  private static void Add<T>(string section, string key, T defaultValue, string description) {
    var entry = InitConfigEntry(section, key, defaultValue, description);
    Entries[key] = entry;
  }

  public static T Get<T>(string key) => (Entries[key] as ConfigEntry<T>).Value;

  public static void Set<T>(string key, T value) => (Entries[key] as ConfigEntry<T>).Value = value;

  public static bool Has(string key) => Entries.ContainsKey(key);

  static ConfigEntry<T> InitConfigEntry<T>(string section, string key, T defaultValue, string description) {
    var entry = Plugin.Instance.Config.Bind(section, key, defaultValue, description);

    var pluginConfigPath = Path.Combine(Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");

    if (File.Exists(pluginConfigPath)) {
      var config = new ConfigFile(pluginConfigPath, true);
      if (config.TryGetEntry(section, key, out ConfigEntry<T> existingEntry)) {
        entry.Value = existingEntry.Value;
      }
    }

    return entry;
  }

  private static void ReorderSections() {
    var configPath = Path.Combine(Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");
    if (!File.Exists(configPath)) return;

    var lines = File.ReadAllLines(configPath).ToList();
    var sectionsContent = new Dictionary<string, List<string>>();
    string currentSection = "";

    foreach (var line in lines) {
      if (line.StartsWith("[")) {
        currentSection = line.Trim('[', ']');
        sectionsContent[currentSection] = new List<string> { line };
      } else if (!string.IsNullOrWhiteSpace(currentSection)) {
        sectionsContent[currentSection].Add(line);
      }
    }

    using var writer = new StreamWriter(configPath, false);
    foreach (var section in OrderedSections) {
      if (sectionsContent.TryGetValue(section, out var content)) {
        foreach (var line in content) writer.WriteLine(line);
        writer.WriteLine();
      }
    }
  }
}
