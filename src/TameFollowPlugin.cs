using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Oddhatter.TameFollow
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInProcess("valheim.exe")]
    public sealed class TameFollowPlugin : BaseUnityPlugin
    {
        public const string Guid = "com.oddhatter.tamefollow";
        public const string Name = "TameFollow";
        public const string Version = "1.0.0";

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> Enabled;

        /// <summary>Wait this long after the last file event before reloading (editors write in several steps).</summary>
        private const float ConfigReloadDelay = 0.5f;

        private Harmony _harmony;
        private FileSystemWatcher _configWatcher;
        private volatile bool _configFileChanged; // set on a thread-pool thread by the watcher
        private float _configReloadAt = -1f;
        private bool _configLockedLogged;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind("General", "Enabled", true,
                "true: pressing Use on any tamed creature that can follow (lox, asksvin, boar, ...) toggles follow / stay, " +
                "like wolves. false: vanilla, where those creatures are only petted.");

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(TameFollowPlugin).Assembly);
            foreach (System.Reflection.MethodBase method in _harmony.GetPatchedMethods())
            {
                Log.LogInfo($"Patched {method.DeclaringType?.Name}.{method.Name}");
            }
            WatchConfigFile();
            Log.LogInfo($"{Name} {Version} loaded.");
        }

        private void Update()
        {
            if (_configFileChanged)
            {
                _configFileChanged = false;
                _configReloadAt = Time.realtimeSinceStartup + ConfigReloadDelay;
            }
            if (_configReloadAt >= 0f && Time.realtimeSinceStartup >= _configReloadAt)
            {
                _configReloadAt = -1f;
                ReloadConfigFromDisk();
            }
        }

        private void OnDestroy()
        {
            _configWatcher?.Dispose();
            _harmony?.UnpatchSelf();
        }

        /// <summary>
        /// Picks up edits to the .cfg file made while the game runs (text editor, r2modman config editor).
        /// Enabled is read on every interaction, so a reload applies immediately.
        /// </summary>
        private void WatchConfigFile()
        {
            string path = Config.ConfigFilePath;
            _configWatcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path))
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                IncludeSubdirectories = false,
            };
            _configWatcher.Changed += OnConfigFileEvent;
            _configWatcher.Created += OnConfigFileEvent;
            _configWatcher.Renamed += OnConfigFileEvent;
            _configWatcher.EnableRaisingEvents = true;
        }

        private void OnConfigFileEvent(object sender, FileSystemEventArgs e)
        {
            _configFileChanged = true; // handled on the main thread in Update
        }

        private void ReloadConfigFromDisk()
        {
            if (!File.Exists(Config.ConfigFilePath))
            {
                return;
            }
            // ConfigFile.Reload sets each entry; with SaveOnConfigSet every changed value would rewrite the file
            // and re-trigger this watcher. The file already holds these values, so don't save while reloading.
            bool saveOnSet = Config.SaveOnConfigSet;
            Config.SaveOnConfigSet = false;
            try
            {
                Config.Reload();
                _configLockedLogged = false;
                Log.LogInfo("Configuration reloaded from disk.");
            }
            catch (IOException ex)
            {
                // File still being written or locked by an editor: keep retrying, but only log once.
                if (!_configLockedLogged)
                {
                    _configLockedLogged = true;
                    Log.LogWarning($"Config file busy, will retry: {ex.Message}");
                }
                _configReloadAt = Time.realtimeSinceStartup + ConfigReloadDelay;
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to reload configuration: {ex.Message}");
            }
            finally
            {
                Config.SaveOnConfigSet = saveOnSet;
            }
        }
    }
}
