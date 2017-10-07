﻿using Discord;
using Discord.Audio;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DASB {
    public class Config {
        public static Config Init(string path) {
            Config config;
            if (File.Exists(path)) {
                try {
                    config = new Config(File.ReadAllText(path));
                } catch (Exception ex) {
                    Utils.Log(LogSeverity.Error, typeof(Config), "Failed to parse configuration", ex);
                    return null;
                }
            } else {
                Utils.Log(LogSeverity.Warning, typeof(Config), "Configuration file not found, writing prototype to " + path);
                config = new Config();

            }
            File.WriteAllText(path, config.ToString());
            return config;
        }

        /// <summary>
        /// bot's token to identify and log into bot account, required
        /// </summary>
        public string botToken = null;
        /// <summary>
        /// bot's owner id (your id!)
        /// if set, the bot attempts to join its owner's voice channel on startup
        /// </summary>
        public ulong? ownerId = null;
        /// <summary>
        /// flag wether the bot should speak in voice chat
        /// </summary>
        public bool autoSpawnEnabled = false;
        /// <summary>
        /// bot's guild id for spawning in voice chat when starting
        /// </summary>
        public List<ulong> autoSpawnGuildIds = new List<ulong>();

        /// <summary>
        /// flag wether the bot should speak in voice chat
        /// </summary>
        public bool speakEnabled = true;
        /// <summary>
        /// audio recording device name to pipe data into discord voice
        /// special values: null for default recording device
        /// </summary>
        public string speakRecordingDevice = null;
        /// <summary>
        /// network audio stream encoding hint for the bot when speaking, default is mixed voice/music
        /// can be any of: "Voice", "Music", "Mixed"
        /// </summary>
        public AudioApplication speakAudioType = AudioApplication.Mixed;
        /// <summary>
        /// network audio stream bit rate for the bot when speaking, default is 96 KiBit
        /// special values: null to use channel's bit rate
        /// </summary>
        public int? speakBitRate = 96 * 1024;
        /// <summary>
        /// network audio stream buffer size in milliseconds for the bot when speaking, default is 1000 ms
        /// </summary>
        public int speakBufferMillis = 1000;

        /// <summary>
        /// flag wether the bot should listen in voice chat
        /// </summary>
        public bool listenEnabled = false;
        /// <summary>
        /// audio playback device name to pipe data from discord voice
        /// special values: null for default playback device
        /// </summary>
        public string listenPlaybackDevice = null;

        /// <summary>
        /// flag wether the bot should execute and respond to commands
        /// </summary>
        public bool commandsEnabled = true;
        /// <summary>
        /// bot's language implementation
        /// </summary>
        public string commandsBotAgent = "tomoko";
        /// <summary>
        /// default command permissions, may be overridden by per-user permissions
        /// </summary>
        public HashSet<string> commandsDefaultPermissions = new HashSet<string>() { "help", "permissions", "!assign" };
        /// <summary>
        /// command permissions for each user
        /// </summary>
        public PermissionDictionary<ulong> commandsUserPermissions = new PermissionDictionary<ulong>();

        public Config() {

        }

        public Config(string json) {
            Load(JToken.Parse(json));
        }

        public void Load(JToken root) {
            JToken node = root;
            Action<string> push;
            Action pop;
            {
                var stack = new Stack<JToken>();
                stack.Push(root);
                // push child node:
                push = x => stack.Push(node = node[x]);
                // pop node:
                pop = () => { stack.Pop(); node = stack.Peek(); };
            }

            // #general
            push("general");
            botToken = (string)node["botToken"];
            ownerId = (ulong?)node["ownerId"];

            // #autoSpawn
            push("autoSpawn");
            autoSpawnEnabled = (bool)node["enabled"];
            autoSpawnGuildIds = new List<ulong>();
            foreach (var entry in (JArray)node["guildIds"]) {
                autoSpawnGuildIds.Add(entry.Value<ulong>());
            }
            pop();
            // #general

            pop();
            // #

            // #voice
            push("voice");

            // #voice - speak
            push("speak");
            speakEnabled = (bool)node["enabled"];
            speakRecordingDevice = (string)node["recordingDevice"];
            speakAudioType = (AudioApplication)Enum.Parse(typeof(AudioApplication), (string)node["audioType"]);
            speakBitRate = (int?)node["bitRate"];
            speakBufferMillis = (int)node["bufferMillis"];
            pop();
            // #voice

            // #voice - listen
            push("listen");
            listenEnabled = (bool)node["enabled"];
            listenPlaybackDevice = (string)node["playbackDevice"];
            pop();
            // #voice

            pop();
            // #

            // #text
            push("text");

            // #text - commands
            push("commands");
            commandsEnabled = (bool)node["enabled"];
            commandsBotAgent = (string)node["botAgent"];
            commandsDefaultPermissions = new HashSet<string>();
            foreach (var defaultPermission in ((string)node["defaultPermissions"]).Split(' ')) {
                commandsDefaultPermissions.Add(defaultPermission);
            }
            commandsUserPermissions = Utils.PermissionDictionaryFromJson<ulong>(node["userPermissions"]);
            pop();
            // #text

            pop();
            // #
        }

        public JToken ToJson() {
            JToken node = new JObject();
            Action<string> push;
            Action pop;
            {
                var stack = new Stack<JToken>();
                stack.Push(node);
                // push new child node:
                push = x => stack.Push(node[x] = (node = new JObject()));
                // pop node:
                pop = () => { stack.Pop(); node = stack.Peek(); };
            }

            // #general
            push("general");
            node["botToken"] = botToken;
            node["ownerId"] = ownerId;

            // #autoSpawn
            push("autoSpawn");
            node["enabled"] = autoSpawnEnabled;
            {
                var entries = new JArray();
                foreach (var autoSpawnGuildId in autoSpawnGuildIds) {
                    entries.Add(autoSpawnGuildId);
                }
                node["guildIds"] = entries;
            }
            pop();
            // #general

            pop();
            // #

            // #voice
            push("voice");

            // #voice - speak
            push("speak");
            node["enabled"] = speakEnabled;
            node["recordingDevice"] = speakRecordingDevice;
            node["audioType"] = Enum.GetName(typeof(AudioApplication), speakAudioType);
            node["bitRate"] = speakBitRate;
            node["bufferMillis"] = speakBufferMillis;
            pop();
            // #voice

            // #voice - listen
            push("listen");
            node["enabled"] = listenEnabled;
            if (listenEnabled) {
                Utils.Log(LogSeverity.Warning, typeof(Config), "This feature isn't supported yet.");
                listenEnabled = false;
            }
            node["playbackDevice"] = listenPlaybackDevice;
            pop();
            // #voice

            pop();
            // #

            // #text
            push("text");

            // #text - commands
            push("commands");
            node["enabled"] = commandsEnabled;
            node["botAgent"] = commandsBotAgent;
            node["defaultPermissions"] = string.Join(" ", commandsDefaultPermissions);
            node["userPermissions"] = Utils.ToJson(commandsUserPermissions);
            pop();
            // #text

            pop();
            // #

            return node;
        }

        public override string ToString() {
            return ToJson().ToString(Formatting.Indented);
        }
    }
}
