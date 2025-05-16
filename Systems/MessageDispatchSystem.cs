using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ProjectM.Network;
using ScarletHooks.Data;
using ScarletHooks.Services;

namespace ScarletHooks.Systems;

public static class MessageDispatchSystem {
  private static readonly HttpClient _httpClient = new();

  public static string AdminWebHookUrl => Settings.Get<string>("AdminWebhookURL");
  public static string PublicWebHookUrl => Settings.Get<string>("PublicWebhookURL");
  public static string LoginWebhookURL => Settings.Get<string>("LoginWebhookURL");
  public static float MessageInterval => Settings.Get<float>("MessageInterval");
  public static float OnFailInterval => Settings.Get<float>("OnFailInterval");
  public static bool EnableBatching => Settings.Get<bool>("EnableBatching");
  public static Dictionary<string, string> ClanWebHookUrls { get; set; } = new();
  public static readonly ConcurrentDictionary<string, Queue<string>> MessageQueues = new();
  public static readonly ConcurrentDictionary<string, DateTime> LastUsed = new();
  private static readonly ConcurrentDictionary<string, int> ConsecutiveFailures = new();
  private const int MaxFailuresBeforeDiscard = 5;
  private static CancellationTokenSource _cts;
  public static bool ShowRunning = false;
  private static bool _isRunning = false;

  public static void Initialize() {
    _isRunning = true;
    LoadFromFile();

    _cts = new CancellationTokenSource();
    _ = Task.Run(() => ProcessQueueLoop(_cts.Token));
  }

  public static void Shutdown() {
    _cts?.Cancel();
    _isRunning = false;
  }

  public static void ForceShutdown() {
    _cts?.Cancel();
    _isRunning = false;
    ClearAll();
  }

  public static void ClearAll() {
    MessageQueues.Clear();
    LastUsed.Clear();
    ConsecutiveFailures.Clear();
  }

  public static void LoadFromFile() {
    if (Database.FileExists("ClanWebHookUrls")) {
      ClanWebHookUrls = Database.Load<Dictionary<string, string>>("ClanWebHookUrls");
    }
  }

  public static void HandleMessage(string content, string playerName, ChatMessageType messageType, string clanName, string targetName) {
    if (string.IsNullOrEmpty(content) || !_isRunning) return;

    switch (messageType) {
      case ChatMessageType.Global:
        GlobalMessage(content, playerName, clanName); break;

      case ChatMessageType.Local:
        LocalMessage(content, playerName, clanName); break;

      case ChatMessageType.Team:
        ClanMessage(content, playerName, clanName); break;

      case ChatMessageType.Whisper:
        WhisperMessage(content, playerName, targetName, clanName); break;

      default: break;
    }
  }

  public static void HandleLoginMessage(string playerName, string clanName) {
    if (string.IsNullOrEmpty(playerName) || !_isRunning) return;

    var format = Settings.Get<string>("LoginMessageFormat");

    if (string.IsNullOrEmpty(format)) return;

    var resultMessage = format.Replace("{playerName}", playerName).Replace("{clanName}", clanName);

    if (Settings.Get<bool>("AdminLoginMessages")) {
      AddToQueue(AdminWebHookUrl, resultMessage);
    }

    if (Settings.Get<bool>("PublicLoginMessages")) {
      AddToQueue(PublicWebHookUrl, resultMessage);
    }

    if (!string.IsNullOrEmpty(clanName) && Settings.Get<bool>("ClanLoginMessages") && ClanWebHookUrls.TryGetValue(clanName, out var url)) {
      AddToQueue(url, resultMessage);
    }

    AddToQueue(LoginWebhookURL, resultMessage);
  }

  public static void HandleLogoutMessage(string playerName, string clanName) {
    if (string.IsNullOrEmpty(playerName) || !_isRunning) return;

    var format = Settings.Get<string>("LogoutMessageFormat");

    if (string.IsNullOrEmpty(format)) return;

    var resultMessage = format.Replace("{playerName}", playerName).Replace("{clanName}", clanName);

    if (Settings.Get<bool>("AdminLoginMessages")) {
      AddToQueue(AdminWebHookUrl, resultMessage);
    }

    if (Settings.Get<bool>("PublicLoginMessages")) {
      AddToQueue(PublicWebHookUrl, resultMessage);
    }

    if (!string.IsNullOrEmpty(clanName) && Settings.Get<bool>("ClanLoginMessages") && ClanWebHookUrls.TryGetValue(clanName, out var url)) {
      AddToQueue(url, resultMessage);
    }

    AddToQueue(LoginWebhookURL, resultMessage);
  }

  public static void GlobalMessage(string content, string playerName, string clanName) {
    SendMessage(content, playerName, clanName, "GlobalPrefix", "AdminGlobalMessages", "PublicGlobalMessages");
  }

  public static void LocalMessage(string content, string playerName, string clanName) {
    SendMessage(content, playerName, clanName, "LocalPrefix", "AdminLocalMessages", "PublicLocalMessages");
  }

  public static void ClanMessage(string content, string playerName, string clanName) {
    if (string.IsNullOrEmpty(clanName)) return;

    string prefix = BuildPrefix("ClanPrefix", playerName, clanName);

    if (Settings.Get<bool>("AdminClanMessages"))
      AddToQueue(AdminWebHookUrl, $"{prefix} {content}");

    if (Settings.Get<bool>("PublicClanMessages"))
      AddToQueue(PublicWebHookUrl, $"{prefix} {content}");

    if (ClanWebHookUrls.TryGetValue(clanName, out var url)) {
      AddToQueue(url, $"{prefix} {content}");
    }
  }

  public static void WhisperMessage(string content, string playerName, string targetName, string clanName) {
    string prefix = Settings.Get<string>("WhisperPrefix")
        .Replace("{playerName}", playerName)
        .Replace("{targetName}", targetName ?? "")
        .Replace("{clanName}", clanName ?? "");

    if (Settings.Get<bool>("AdminWhisperMessages"))
      AddToQueue(AdminWebHookUrl, $"{prefix} {content}");

    if (Settings.Get<bool>("PublicWhisperMessages"))
      AddToQueue(PublicWebHookUrl, $"{prefix} {content}");
  }

  private static void SendMessage(string content, string playerName, string clanName,
                                string prefixKey, string adminSettingKey, string publicSettingKey) {
    string prefix = BuildPrefix(prefixKey, playerName, clanName);

    if (Settings.Get<bool>(adminSettingKey))
      AddToQueue(AdminWebHookUrl, $"{prefix} {content}");

    if (Settings.Get<bool>(publicSettingKey))
      AddToQueue(PublicWebHookUrl, $"{prefix} {content}");
  }

  private static string BuildPrefix(string prefixKey, string playerName, string clanName) {
    return Settings.Get<string>(prefixKey)
        .Replace("{playerName}", playerName)
        .Replace("{clanName}", clanName ?? "");
  }

  public static void AddToQueue(string webhookUrl, string content) {
    if (string.IsNullOrEmpty(webhookUrl) || !IsValidUrl(webhookUrl)) return;

    var queue = MessageQueues.GetOrAdd(webhookUrl, _ => new Queue<string>());

    lock (queue) {
      queue.Enqueue(content);
    }

    LastUsed.TryAdd(webhookUrl, DateTime.MinValue);
  }

  public static void AddClan(string clanName) {
    ClanWebHookUrls[clanName] = null;
    Database.Save("ClanWebHookUrls", ClanWebHookUrls);
  }

  public static void RemoveClan(string clanName) {
    ClanWebHookUrls.Remove(clanName);
    Database.Save("ClanWebHookUrls", ClanWebHookUrls);
  }

  public static async Task<bool> TrySendMessage(string content, string webhookUrl = null) {
    if (string.IsNullOrEmpty(webhookUrl) || !IsValidUrl(webhookUrl)) {
      Console.WriteLine("Invalid webhook url.");
      return false;
    }

    var payload = new { content };
    string json = JsonSerializer.Serialize(payload);
    var contentData = new StringContent(json, Encoding.UTF8, "application/json");

    try {
      var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl) {
        Content = contentData
      };

      var response = await _httpClient.SendAsync(request);

      if (!response.IsSuccessStatusCode) {
        var errorContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Failed to send webhook. Status: {response.StatusCode}, Content: {errorContent}");

        return false;
      }
      return true;
    } catch (Exception ex) {
      Console.WriteLine($"Exception while sending webhook message: {ex.Message}");
      return false;
    }
  }

  private static async Task ProcessQueueLoop(CancellationToken token) {
    while (_isRunning && !token.IsCancellationRequested) {
      foreach (var kvp in MessageQueues) {
        var webhookUrl = kvp.Key;
        var queue = kvp.Value;

        DateTime lastUsed = LastUsed.GetOrAdd(webhookUrl, DateTime.MinValue);
        var timeSinceLastUse = DateTime.Now - lastUsed;

        if (timeSinceLastUse.TotalSeconds >= MessageInterval) {
          string content;
          int itemsToDequeue = 1;

          lock (queue) {
            if (queue.Count == 0) continue;

            if (EnableBatching) {
              content = string.Join("\n", queue);
              itemsToDequeue = queue.Count;
            } else {
              content = queue.Peek();
            }
          }

          var success = await TrySendMessage(content, webhookUrl);

          if (success) {
            lock (queue) {
              for (int i = 0; i < itemsToDequeue && queue.Count > 0; i++) {
                queue.Dequeue();
              }
            }

            if (queue.Count == 0)
              MessageQueues.TryRemove(webhookUrl, out _);

            ConsecutiveFailures[webhookUrl] = 0;
            LastUsed[webhookUrl] = DateTime.Now;
          } else {
            var failures = ConsecutiveFailures.AddOrUpdate(webhookUrl, 1, (_, count) => count + 1);
            LastUsed[webhookUrl] = DateTime.Now.AddSeconds(OnFailInterval);

            if (failures >= MaxFailuresBeforeDiscard) {
              Console.WriteLine($"[Webhook Error] Discarding queue for {webhookUrl} after {failures} failures.");
              MessageQueues.TryRemove(webhookUrl, out _);
              ConsecutiveFailures.TryRemove(webhookUrl, out _);
            }
          }
        }
      }

      try {
        await Task.Delay(TimeSpan.FromSeconds(MessageInterval), token);
      } catch (TaskCanceledException ex) {
        Console.WriteLine($"[Webhook Error] Task was canceled: {ex.Message}");
        break;
      }
    }
  }


  public static bool IsValidUrl(string url) {
    if (string.IsNullOrWhiteSpace(url)) return false;

    return Regex.IsMatch(url, "^https?:\\/\\/(?:www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b(?:[-a-zA-Z0-9()@:%_\\+.~#?&\\/=]*)$");
  }
}
