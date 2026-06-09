using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace MaxLuck
{
    public class GuaranteedDropsConfig
    {
        [JsonProperty("Enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("ApplyToBosses")]
        public bool ApplyToBosses { get; set; } = true;

        [JsonProperty("GuaranteedEventLoot")]
        public bool GuaranteedEventLoot { get; set; } = true;

        [JsonProperty("GuaranteedRareCreatureLoot")]
        public bool GuaranteedRareCreatureLoot { get; set; } = true;

        [JsonProperty("GuaranteedFishingLoot")]
        public bool GuaranteedFishingLoot { get; set; } = true;

        [JsonProperty("LootRollCount")]
        public int LootRollCount { get; set; } = 10;

        [JsonProperty("LogDrops")]
        public bool LogDrops { get; set; } = false;

        [JsonProperty("PreventDuplicates")]
        public bool PreventDuplicates { get; set; } = true;

        [JsonProperty("ExcludedItemIds")]
        public List<int> ExcludedItemIds { get; set; } = new List<int>();
    }

    public class EnemySpawnsConfig
    {
        [JsonProperty("Enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("SpawnRateMultiplier")]
        public float SpawnRateMultiplier { get; set; } = 5.0f;

        [JsonProperty("MaxSpawnsMultiplier")]
        public float MaxSpawnsMultiplier { get; set; } = 3.0f;

        [JsonProperty("RareEnemyMultiplier")]
        public float RareEnemyMultiplier { get; set; } = 5.0f;

        [JsonProperty("EnforceInterval")]
        public int EnforceInterval { get; set; } = 10;

        [JsonProperty("BaseSpawnRateOverride")]
        public int BaseSpawnRateOverride { get; set; } = 0;

        [JsonProperty("MaxSpawnsOverride")]
        public int MaxSpawnsOverride { get; set; } = 0;
    }

    public class FishingConfig
    {
        [JsonProperty("Enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("FishingPowerBonus")]
        public int FishingPowerBonus { get; set; } = 500;

        [JsonProperty("ApplyFishingBuffs")]
        public bool ApplyFishingBuffs { get; set; } = true;

        [JsonProperty("BuffDuration")]
        public int BuffDuration { get; set; } = 600;
    }

    public class EventsConfig
    {
        [JsonProperty("Enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("IncreaseRareEvents")]
        public bool IncreaseRareEvents { get; set; } = true;

        [JsonProperty("EclipseChance")]
        public float EclipseChance { get; set; } = 0.15f;

        [JsonProperty("BloodMoonChance")]
        public float BloodMoonChance { get; set; } = 0.15f;

        [JsonProperty("PirateInvasionChance")]
        public float PirateInvasionChance { get; set; } = 0.05f;

        [JsonProperty("GoblinArmyChance")]
        public float GoblinArmyChance { get; set; } = 0.05f;

        [JsonProperty("SlimeRainChance")]
        public float SlimeRainChance { get; set; } = 0.08f;

        [JsonProperty("EventCheckCooldown")]
        public int EventCheckCooldown { get; set; } = 60;

        [JsonProperty("AnnounceEvents")]
        public bool AnnounceEvents { get; set; } = true;
    }

    public class MaxLuckConfig
    {
        [JsonProperty("Enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("LuckValue")]
        public float LuckValue { get; set; } = 1.0f;

        [JsonProperty("UpdateInterval")]
        public int UpdateInterval { get; set; } = 60;

        [JsonProperty("BroadcastOnToggle")]
        public bool BroadcastOnToggle { get; set; } = true;

        [JsonProperty("GuaranteedDrops")]
        public GuaranteedDropsConfig GuaranteedDrops { get; set; } = new GuaranteedDropsConfig();

        [JsonProperty("EnemySpawns")]
        public EnemySpawnsConfig EnemySpawns { get; set; } = new EnemySpawnsConfig();

        [JsonProperty("Fishing")]
        public FishingConfig Fishing { get; set; } = new FishingConfig();

        [JsonProperty("Events")]
        public EventsConfig Events { get; set; } = new EventsConfig();

        public static MaxLuckConfig Read(string path)
        {
            if (!File.Exists(path))
            {
                var config = new MaxLuckConfig();
                config.Write(path);
                return config;
            }

            try
            {
                string json = File.ReadAllText(path);
                var config = JsonConvert.DeserializeObject<MaxLuckConfig>(json) ?? new MaxLuckConfig();
                config.Write(path);
                return config;
            }
            catch (Exception ex)
            {
                TShockAPI.TShock.Log.ConsoleError($"[MaxLuck] Failed to read config file: {ex.Message}");
                return new MaxLuckConfig();
            }
        }

        public void Write(string path)
        {
            try
            {
                string directory = Path.GetDirectoryName(path) ?? "";
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                TShockAPI.TShock.Log.ConsoleError($"[MaxLuck] Failed to write config file: {ex.Message}");
            }
        }
    }
}
