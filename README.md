# ScarletHooks

ScarletHooks is a V Rising server mod that enables advanced webhook integration for chat messages. It allows admins to configure multiple webhooks for admin, public, and clan-specific notifications, with fine-grained control over which messages are sent and how they are batched. All features are accessible via chat commands and can be customized in the config file.

**Note:** This mod is not exclusive to Discord webhooks; you can use it with other services as well.

---

## Support & Donations

<a href='https://ko-fi.com/F2F21EWEM7' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://storage.ko-fi.com/cdn/kofi6.png?v=6' alt='Buy Me a Coffee at ko-fi.com' /></a>

---

## Features

- **Advanced Webhook Integration:**  
  Send chat messages and notifications to multiple webhooks, including admin, public, and clan-specific endpoints.  

- **Granular Message Routing:**  
  Configure which types of messages (global, local, clan, whisper) are sent to each webhook. Admin and public webhooks can be toggled for each message type, while clans can have their own dedicated webhooks for clan chat.

- **Dynamic Clan Webhooks:**  
  Add or remove clan webhooks dynamically via in-game commands. Each clan can have its own webhook URL, managed through the config file and easily updated or reloaded without restarting the server.

- **In-Game Command Management:**  
  Most webhook and settings management can be performed through chat commands by server admins *(Except for setting webhook URLs and intervals)*. Commands allow adding/removing clans, reloading settings or webhooks, listing all configured webhooks, and toggling settings.

- **Batching and Rate Limiting:**  
  Messages are queued and sent at configurable intervals to avoid rate limiting by webhook services. Batching can be enabled to combine multiple messages into a single webhook request, reducing spam and improving reliability.

- **Automatic Retry and Failure Handling:**  
  If a webhook fails, the system will automatically retry sending messages after a configurable delay. After several consecutive failures, the message queue for that webhook is cleared to prevent server overload.

- **Persistent Configuration:**  
  All webhook URLs and clan mappings are saved and loaded automatically from disk, ensuring settings persist across server restarts.

- **Flexible Configuration File:**  
  Fine-tune all aspects of the mod via the `ScarletHooks.cfg` file, including intervals, enabled message types, and webhook URLs.

- **Status and Debug Commands:**  
  View current settings, all configured webhooks, and the status of the message dispatch system directly from in-game chat.

- **Safe Start/Stop Controls:**  
  Admins can start or forcibly stop the message dispatch system at any time, clearing all message queues if needed.

---

## Usage

Most management is done via chat commands. Only admins can use these commands.

**Note:** Some settings can only be changed through the config file due to character length limitations in the in-game chat or for other reasons.

<details>
<summary>Show Commands</summary>

### Webhook Management

- `.hooks add <clan-name> | .hooks add <player-name>`  
  Add a clan to the webhook list.  
  *(You must set the webhook URL in the config file and use the reload command after adding.)*

- `.hooks remove <clan-name> | .hooks remove <player-name>`  
  Remove a clan from the webhook list.

- `.hooks reload settings`  
  Reload all settings from the config file.

- `.hooks reload webhooks`  
  Reload all webhook URLs from the saved file.

- `.hooks reload`  
  Reload both settings and webhooks.

- `.hooks list`  
  List all configured webhooks *(admin, public, and clans)*.

### Settings

- `.hooks settings <setting> <true|false>`  
  Change a boolean setting *(except for webhook URLs and intervals, which are handled via the config file)*.

- `.hooks settings`  
  Show current settings and their values.

- `.hooks forcestop`  
  Stop the message dispatch system and clear all cache.

- `.hooks start`  
  Starts the message dispatch system if it has been manually stopped.

</details>

---

## Installation

### Requirements

This mod requires the following dependencies:

* **[BepInEx (RC2)](https://wiki.vrisingmods.com/user/bepinex_install.html)**
* **[VampireCommandFramework](https://github.com/decaprime/VampireCommandFramework/releases/tag/v0.10.0)**

Make sure both are installed and loaded **before** installing ScarletHooks.

### Manual Installation

1. Download the latest release of **ScarletHooks**.
2. Extract the contents into your `BepInEx/plugins` folder:

   ```
   <V Rising Server Directory>/BepInEx/plugins/
   ```

   Your folder should now include:

   ```
   BepInEx/plugins/ScarletHooks.dll
   ```

3. Ensure **VampireCommandFramework** is also installed in the `plugins` folder.
4. Start or restart your server.

---

## Configuration

All settings can be adjusted in the `ScarletHooks.cfg` file located in your server's `BepInEx/config` folder.

<details>
<summary>Show Settings</summary>

### General

- **MessageInterval**: Interval (in seconds) between sending messages to webhooks.  
  *Default: 0.2*

- **OnFailInterval**: Interval (in seconds) to wait before retrying after a webhook failure.  
  *Default: 2*

- **AdminWebhookURL**: Webhook URL for admin messages.  
  *Default: null*

- **PublicWebhookURL**: Webhook URL for public messages.  
  *Default: null*

- **EnableBatching**: Enable or disable batching of messages to avoid rate limiting.  
  *Default: true*

### Admin

- **AdminGlobalMessages**: Enable or disable sending global chat messages to the admin webhook.  
  *Default: true*

- **AdminLocalMessages**: Enable or disable sending local chat messages to the admin webhook.  
  *Default: true*

- **AdminClanMessages**: Enable or disable sending clan chat messages to the admin webhook.  
  *Default: true*

- **AdminWhisperMessages**: Enable or disable sending whisper messages to the admin webhook.  
  *Default: true*

### Public

- **PublicGlobalMessages**: Enable or disable sending global chat messages to the public webhook.  
  *Default: true*

- **PublicLocalMessages**: Enable or disable sending local chat messages to the public webhook.  
  *Default: false*

- **PublicClanMessages**: Enable or disable sending clan chat messages to the public webhook.  
  *Default: false*

- **PublicWhisperMessages**: Enable or disable sending whisper messages to the public webhook.  
  *Default: false*

</details>

## Guide & Troubleshooting

<details>
<summary>Click to expand</summary>

### Getting Started

#### Setting Up a Webhook

1. **Create a webhook** in your preferred service.
   - For Discord: Go to your channel settings → Integrations → Webhooks → New Webhook → Copy Webhook URL.
   - For other services: Follow their documentation to generate a webhook URL.

2. **Open the config file** at:  
   `BepInEx/config/ScarletHooks.cfg`

3. **Paste the webhook URL** into the appropriate field:
   - For admin messages, set `AdminWebhookURL = <your-webhook-url>`
   - For public messages, set `PublicWebhookURL = <your-webhook-url>`

4. **Save the config file.**

5. **Reload the settings** in-game with:  
   `.hooks reload settings`  
   or restart your server.

6. **Test the integration** by sending a chat message in-game and verifying it appears in your webhook destination.


#### Setting Up a Clan Webhook

1. In-game, use:
   `.hooks add <clan-name>`
   or
   `.hooks add <player-name>`

2. Open the config file at: `BepInEx/config/ScarletHooks/ClanWebHookUrls.json`

3. Set the webhook URL for the new clan/player entry.

4. Apply changes with the command: `.hooks reload webhooks` or restart the server.

---

### Managing the Dispatch System

* **Stop message dispatch:** `.hooks stop`

* **Force stop and clear all queues:** `.hooks forcestop`

* **Start/restart dispatch:** `.hooks start`

---

### Viewing and Changing Settings

* **See current webhooks:** `.hooks list`

* **See all mod settings:** `.hooks settings`

* **Note:** Some settings (e.g., webhook URLs and message intervals) can **only** be changed directly in the config file due to some limitations.

---

### Using Non-Discord Webhooks

Yes, ScarletHooks can work with other webhook services, but:

* You may need to download the mod’s dependencies and manually modify the POST request payload format in the `MessageDispatchSystem.cs` file to match the requirements of your webhook service.
* Some services may require additional configuration, such as authentication tokens or custom headers.

---

## Troubleshooting

### What happens if a webhook fails?

* The system keeps a queue of messages for each webhook.
* When a message is sent targeting a specific webhook, the system tries to deliver it.
* If the webhook fails **more than 5 times**, all pending messages for that webhook are discarded to prevent overload.
* The webhook itself is **not disabled** — the system will continue attempting to send any new messages to it.
* However, while the webhook is still in a "failed" state, **new messages will not be queued**. Each new message is attempted once and immediately dropped if it fails, without being added to the queue — unless the webhook starts responding successfully again.
* If a message **is successfully delivered** to that webhook, the failure counter is reset. From that point on, messages will once again be queued and retried normally until delivered.

---

### Why are my messages not being sent?

* Check if the webhook URL is set correctly in `ScarletHooks.cfg`.
* Use `.hooks list` in-game to verify the active webhooks.
* Confirm the webhook service (e.g., Discord) is online and the URL is valid.
* After changes, use `.hooks reload` or restart the server.

---

### Why isn’t a new clan webhook working?

* The webhook may not be linked in the config file.
* You may have forgotten to reload after adding it.
* Double-check the spelling of the clan name in both the command and config — it’s case sensitive.
  **Recommended**: Use the player name instead, so the mod can automatically detect the clan from the player’s data.

---

### Why can't I change settings from the game?

Some mod settings require editing the `.cfg` file directly. This includes:

* Webhook URLs
* Dispatch intervals
* Some other settings

This is due to character limitations in the in-game chat and other reasons.

</details>
