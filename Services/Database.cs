using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ScarletHooks.Services;

public static class Database {
  private static readonly string CONFIG_PATH = Path.Combine(BepInEx.Paths.ConfigPath, "ScarletHooks");

  public static void Initialize() {
    if (!Directory.Exists(CONFIG_PATH)) {
      Directory.CreateDirectory(CONFIG_PATH);
    }
  }

  public static void Save<T>(string path, T data) {
    string filePath = Path.Combine(CONFIG_PATH, $"{path}.json");

    try {
      Directory.CreateDirectory(Path.GetDirectoryName(filePath));

      string jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
      File.WriteAllText(filePath, jsonData);
    } catch (Exception ex) {
      Plugin.LogInstance.LogError($"An error occurred while saving data: {ex.Message}");
    }
  }

  public static T Load<T>(string path) {
    string filePath = Path.Combine(CONFIG_PATH, $"{path}.json");

    if (!File.Exists(filePath)) {
      Plugin.LogInstance.LogWarning($"File not found: {filePath}");
      return default;
    }

    try {
      string jsonData = File.ReadAllText(filePath);
      var deserializedData = JsonSerializer.Deserialize<T>(jsonData);

      return deserializedData;
    } catch (Exception ex) {
      Plugin.LogInstance.LogError($"An error occurred while loading data: {ex.Message}");
    }

    return default;
  }

  public static bool FileExists(string path) {
    string filePath = Path.Combine(CONFIG_PATH, $"{path}.json");
    return File.Exists(filePath);
  }

  public static List<string> ListAvailablePaths() {
    if (!Directory.Exists(CONFIG_PATH)) return new();
    var files = Directory.GetFiles(CONFIG_PATH, "*.json");
    var paths = new List<string>();
    foreach (var file in files) {
      paths.Add(Path.GetFileNameWithoutExtension(file));
    }
    return paths;
  }
}