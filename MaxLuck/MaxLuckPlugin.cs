using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;

#nullable enable

namespace MaxLuck
{
    [ApiVersion(2, 1)]
    public class MaxLuckPlugin : TerrariaPlugin
    {
        public override string Name => "MaxLuck";
        public override string Author => "MonoGutsy";
        public override string Description => "Guaranteed drops system - every NPC drops all possible loot from its entire loot table.";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);

        private MaxLuckConfig _config;
        private string _configPath;
        private int _updateCounter;
        private int _spawnEnforceCounter;
        private bool _eventCheckedThisPhase;
        private int _eventCooldownCounter;

        private static readonly HashSet<int> RareCreatureIds = new HashSet<int>
        {
            NPCID.Nymph,
            NPCID.Tim,
            NPCID.RuneWizard,
            NPCID.DoctorBones,
            NPCID.UndeadMiner,
            NPCID.TheGroom,
            NPCID.TheBride,
            NPCID.Pinky,
            NPCID.GoblinSummoner,
            NPCID.IceGolem,
            NPCID.SandElemental,
            NPCID.Moth,
            NPCID.Mimic,
            NPCID.IceMimic,
            NPCID.PresentMimic,
            NPCID.Medusa,
            NPCID.Mothron,
        };

        private static readonly HashSet<int> EventEnemyIds = new HashSet<int>
        {
            NPCID.PirateDeckhand, NPCID.PirateCorsair, NPCID.PirateDeadeye,
            NPCID.PirateCrossbower, NPCID.PirateCaptain, NPCID.PirateShip,
            NPCID.Parrot, NPCID.PirateShipCannon,

            NPCID.Eyezor, NPCID.Frankenstein, NPCID.SwampThing,
            NPCID.Vampire, NPCID.VampireBat, NPCID.CreatureFromTheDeep,
            NPCID.Fritz, NPCID.ThePossessed, NPCID.Reaper,
            NPCID.Butcher, NPCID.DeadlySphere, NPCID.DrManFly,
            NPCID.Nailhead, NPCID.Psycho, NPCID.Mothron,

            NPCID.GoblinPeon, NPCID.GoblinThief, NPCID.GoblinWarrior,
            NPCID.GoblinSorcerer, NPCID.GoblinArcher, NPCID.GoblinSummoner,

            NPCID.MisterStabby, NPCID.SnowmanGangsta, NPCID.SnowBalla,

            NPCID.MartianSaucerCore, NPCID.Scutlix, NPCID.ScutlixRider,
            NPCID.MartianWalker, NPCID.MartianDrone, NPCID.MartianTurret,
            NPCID.GigaZapper, NPCID.MartianEngineer, NPCID.MartianOfficer,
            NPCID.RayGunner, NPCID.GrayGrunt, NPCID.BrainScrambler,

            NPCID.BloodZombie, NPCID.Drippler, NPCID.TheGroom, NPCID.TheBride,
            NPCID.CorruptBunny, NPCID.CrimsonBunny, NPCID.BloodNautilus,
            NPCID.BloodEelHead, NPCID.GoblinShark, NPCID.BloodSquid,

            NPCID.Scarecrow1, NPCID.Scarecrow2, NPCID.Scarecrow3,
            NPCID.Scarecrow4, NPCID.Scarecrow5, NPCID.Scarecrow6,
            NPCID.Scarecrow7, NPCID.Scarecrow8, NPCID.Scarecrow9, NPCID.Scarecrow10,
            NPCID.Splinterling, NPCID.Hellhound, NPCID.Poltergeist,
            NPCID.HeadlessHorseman, NPCID.MourningWood, NPCID.Pumpking,

            NPCID.PresentMimic, NPCID.Flocko, NPCID.GingerbreadMan,
            NPCID.ZombieElf, NPCID.ZombieElfBeard, NPCID.ZombieElfGirl,
            NPCID.ElfArcher, NPCID.Nutcracker, NPCID.NutcrackerSpinning,
            NPCID.ElfCopter, NPCID.Krampus, NPCID.Yeti,
            NPCID.Everscream, NPCID.SantaNK1, NPCID.IceQueen,

            NPCID.DD2OgreT2, NPCID.DD2OgreT3, NPCID.DD2Betsy,
        };

        private static readonly HashSet<int> JunkItemIds = new HashSet<int>
        {
            ItemID.OldShoe,
            ItemID.FishingSeaweed,
            ItemID.TinCan,
        };

        public MaxLuckPlugin(Main game) : base(game)
        {
            _config = new MaxLuckConfig();
            _configPath = Path.Combine(TShock.SavePath, "MaxLuckConfig.json");
            _updateCounter = 0;
            _spawnEnforceCounter = 0;
            _eventCheckedThisPhase = false;
            _eventCooldownCounter = 0;
        }

        public override void Initialize()
        {
            LoadConfig();

            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnPlayerJoin);
            ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilled);

            Commands.ChatCommands.Add(new Command("maxluck.admin", MaxLuckCmd, "maxluck")
            {
                HelpText = "Manages the MaxLuck plugin. Usage: /maxluck [on|off|reload|status]"
            });

            TShock.Log.ConsoleInfo("[MaxLuck] Guaranteed Drops System initialized.");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnPlayerJoin);
                ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilled);
            }
            base.Dispose(disposing);
        }

        private void LoadConfig()
        {
            try
            {
                _config = MaxLuckConfig.Read(_configPath);
                TShock.Log.ConsoleInfo("[MaxLuck] Configuration loaded successfully.");
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[MaxLuck] Unexpected error loading configuration: {ex.Message}");
                _config = new MaxLuckConfig();
            }
        }

        private void OnPlayerJoin(GreetPlayerEventArgs args)
        {
            if (_config == null || !_config.Enabled)
                return;

            var player = TShock.Players[args.Who];
            if (player != null && player.Active && player.RealPlayer && player.TPlayer != null)
            {
                player.TPlayer.luck = _config.LuckValue;
            }
        }

        private void OnNpcKilled(NpcKilledEventArgs args)
        {
            if (_config == null || !_config.Enabled || !_config.GuaranteedDrops.Enabled)
                return;

            NPC npc = args.npc;
            if (npc == null)
                return;

            if (!ShouldProcessNpc(npc))
                return;

            try
            {
                List<IItemDropRule> rules = Main.ItemDropsDB.GetRulesForNPCID(npc.type, false);

                if (rules == null || rules.Count == 0)
                {
                    if (_config.GuaranteedDrops.LogDrops)
                    {
                        TShock.Log.ConsoleInfo($"[MaxLuck] NPC {npc.FullName} (ID:{npc.type}) has no drop rules in database.");
                    }
                    return;
                }

                var allDrops = new List<GuaranteedDrop>();
                foreach (var rule in rules)
                {
                    ResolveDropRule(rule, allDrops, npc);
                }

                if (allDrops.Count == 0)
                {
                    if (_config.GuaranteedDrops.LogDrops)
                    {
                        TShock.Log.ConsoleInfo($"[MaxLuck] NPC {npc.FullName} (ID:{npc.type}) - rules resolved but no items extracted.");
                    }
                    return;
                }

                var droppedItems = new HashSet<int>();

                foreach (var drop in allDrops)
                {
                    if (_config.GuaranteedDrops.ExcludedItemIds.Contains(drop.ItemId))
                        continue;

                    if (_config.GuaranteedDrops.PreventDuplicates && !IsCoinOrStackable(drop.ItemId))
                    {
                        if (droppedItems.Contains(drop.ItemId))
                            continue;
                    }

                    int stack = drop.MinStack;
                    if (drop.MaxStack > drop.MinStack)
                    {
                        stack = Main.rand.Next(drop.MinStack, drop.MaxStack + 1);
                    }

                    if (stack < 1) stack = 1;

                    SpawnItem(npc, drop.ItemId, stack);
                    droppedItems.Add(drop.ItemId);

                    if (_config.GuaranteedDrops.LogDrops)
                    {
                        TShock.Log.ConsoleInfo($"[MaxLuck] Guaranteed drop: {Lang.GetItemNameValue(drop.ItemId)} x{stack} from {npc.FullName}");
                    }
                }

                if (_config.GuaranteedDrops.LogDrops)
                {
                    TShock.Log.ConsoleInfo($"[MaxLuck] Total guaranteed items for {npc.FullName}: {droppedItems.Count}");
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[MaxLuck] Error processing guaranteed drops for NPC {npc.type}: {ex.Message}");
            }
        }

        private bool ShouldProcessNpc(NPC npc)
        {
            if (npc.boss)
            {
                return _config.GuaranteedDrops.ApplyToBosses;
            }

            if (EventEnemyIds.Contains(npc.type))
            {
                return _config.GuaranteedDrops.GuaranteedEventLoot;
            }

            if (RareCreatureIds.Contains(npc.type))
            {
                return _config.GuaranteedDrops.GuaranteedRareCreatureLoot;
            }

            return true;
        }

        private void ResolveDropRule(IItemDropRule rule, List<GuaranteedDrop> drops, NPC npc)
        {
            if (rule == null)
                return;

            if (rule is CommonDrop commonDrop)
            {
                drops.Add(new GuaranteedDrop(commonDrop.itemId, commonDrop.amountDroppedMinimum, commonDrop.amountDroppedMaximum));
            }
            else if (rule is CommonDropNotScalingWithLuck commonNoLuck)
            {
                drops.Add(new GuaranteedDrop(commonNoLuck.itemId, commonNoLuck.amountDroppedMinimum, commonNoLuck.amountDroppedMaximum));
            }
            else if (rule is ItemDropWithConditionRule condDrop)
            {
                drops.Add(new GuaranteedDrop(condDrop.itemId, condDrop.amountDroppedMinimum, condDrop.amountDroppedMaximum));
            }
            else if (rule is DropBasedOnExpertMode expertDrop)
            {
                ResolveDropRule(expertDrop.ruleForNormalMode, drops, npc);
                ResolveDropRule(expertDrop.ruleForExpertMode, drops, npc);
            }
            else if (rule is DropBasedOnMasterMode masterDrop)
            {
                ResolveDropRule(masterDrop.ruleForDefault, drops, npc);
                ResolveDropRule(masterDrop.ruleForMasterMode, drops, npc);
            }
            else if (rule is OneFromRulesRule oneFromRules)
            {
                foreach (var subRule in oneFromRules.options)
                {
                    ResolveDropRule(subRule, drops, npc);
                }
            }
            else if (rule is OneFromOptionsDropRule oneFromOptions)
            {
                foreach (int itemId in oneFromOptions.dropIds)
                {
                    drops.Add(new GuaranteedDrop(itemId, 1, 1));
                }
            }
            else if (rule is OneFromOptionsNotScaledWithLuckDropRule oneFromOptionsNoLuck)
            {
                foreach (int itemId in oneFromOptionsNoLuck.dropIds)
                {
                    drops.Add(new GuaranteedDrop(itemId, 1, 1));
                }
            }
            else if (rule is DropOneByOne dropOneByOne)
            {
                drops.Add(new GuaranteedDrop(
                    dropOneByOne.itemId,
                    dropOneByOne.parameters.MinimumItemDropsCount,
                    dropOneByOne.parameters.MaximumItemDropsCount));
            }
            else if (rule is DropPerPlayerOnThePlayer perPlayer)
            {
                drops.Add(new GuaranteedDrop(perPlayer.itemId, 1, 1));
            }
            else if (rule is LeadingConditionRule leadingCondition)
            {
                if (leadingCondition.ChainedRules != null)
                {
                    foreach (var chained in leadingCondition.ChainedRules)
                    {
                        ResolveDropRule(chained.RuleToChain, drops, npc);
                    }
                }
            }
            else if (rule is DropLocalPerClientAndResetsNPCMoneyTo0 localDrop)
            {
                drops.Add(new GuaranteedDrop(localDrop.itemId, localDrop.amountDroppedMinimum, localDrop.amountDroppedMaximum));
            }
            else
            {
                TryExtractItemViaReflection(rule, drops);
            }

            if (rule.ChainedRules != null)
            {
                foreach (var chained in rule.ChainedRules)
                {
                    if (chained?.RuleToChain != null)
                    {
                        ResolveDropRule(chained.RuleToChain, drops, npc);
                    }
                }
            }
        }

        private void TryExtractItemViaReflection(IItemDropRule rule, List<GuaranteedDrop> drops)
        {
            try
            {
                var type = rule.GetType();

                var itemIdField = type.GetField("itemId", BindingFlags.Public | BindingFlags.Instance);
                if (itemIdField != null && itemIdField.FieldType == typeof(int))
                {
                    int itemId = (int)itemIdField.GetValue(rule)!;
                    if (itemId > 0)
                    {
                        int minStack = 1, maxStack = 1;
                        var minField = type.GetField("amountDroppedMinimum", BindingFlags.Public | BindingFlags.Instance);
                        var maxField = type.GetField("amountDroppedMaximum", BindingFlags.Public | BindingFlags.Instance);

                        if (minField != null && minField.FieldType == typeof(int))
                            minStack = (int)minField.GetValue(rule)!;
                        if (maxField != null && maxField.FieldType == typeof(int))
                            maxStack = (int)maxField.GetValue(rule)!;

                        drops.Add(new GuaranteedDrop(itemId, minStack, maxStack));
                    }
                }

                var dropIdsField = type.GetField("dropIds", BindingFlags.Public | BindingFlags.Instance);
                if (dropIdsField != null && dropIdsField.FieldType == typeof(int[]))
                {
                    int[]? ids = (int[]?)dropIdsField.GetValue(rule);
                    if (ids != null)
                    {
                        foreach (int id in ids)
                        {
                            if (id > 0)
                                drops.Add(new GuaranteedDrop(id, 1, 1));
                        }
                    }
                }

                var optionsField = type.GetField("options", BindingFlags.Public | BindingFlags.Instance);
                if (optionsField != null && typeof(IItemDropRule[]).IsAssignableFrom(optionsField.FieldType))
                {
                    var options = (IItemDropRule[]?)optionsField.GetValue(rule);
                    if (options != null)
                    {
                        foreach (var sub in options)
                            ResolveDropRule(sub, drops, null!);
                    }
                }

                var rulesField = type.GetField("rules", BindingFlags.Public | BindingFlags.Instance);
                if (rulesField != null && typeof(IItemDropRule[]).IsAssignableFrom(rulesField.FieldType))
                {
                    var rules = (IItemDropRule[]?)rulesField.GetValue(rule);
                    if (rules != null)
                    {
                        foreach (var sub in rules)
                            ResolveDropRule(sub, drops, null!);
                    }
                }
            }
            catch
            {
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (_config == null || !_config.Enabled)
                return;

            if (_config.EnemySpawns.Enabled)
            {
                _spawnEnforceCounter++;
                if (_spawnEnforceCounter >= _config.EnemySpawns.EnforceInterval)
                {
                    _spawnEnforceCounter = 0;
                    EnforceSpawnRates();
                }
            }

            if (_config.Events.Enabled && _config.Events.IncreaseRareEvents)
            {
                ProcessEventTriggers();
            }

            _updateCounter++;
            if (_updateCounter >= _config.UpdateInterval)
            {
                _updateCounter = 0;
                ApplyPeriodicPlayerEnhancements();
            }

            if (_eventCooldownCounter > 0)
                _eventCooldownCounter--;
        }

        private void EnforceSpawnRates()
        {
            if (_config.EnemySpawns.BaseSpawnRateOverride > 0)
            {
                NPC.defaultSpawnRate = _config.EnemySpawns.BaseSpawnRateOverride;
            }
            else if (_config.EnemySpawns.SpawnRateMultiplier > 0)
            {
                int baseRate = 600;
                int newRate = (int)(baseRate / _config.EnemySpawns.SpawnRateMultiplier);
                NPC.defaultSpawnRate = Math.Max(newRate, 1);
            }

            if (_config.EnemySpawns.MaxSpawnsOverride > 0)
            {
                NPC.defaultMaxSpawns = _config.EnemySpawns.MaxSpawnsOverride;
            }
            else if (_config.EnemySpawns.MaxSpawnsMultiplier > 0)
            {
                int baseMax = 5;
                NPC.defaultMaxSpawns = (int)(baseMax * _config.EnemySpawns.MaxSpawnsMultiplier);
            }
        }

        private void ProcessEventTriggers()
        {
            if (Main.time < 60)
            {
                if (!_eventCheckedThisPhase && _eventCooldownCounter <= 0)
                {
                    _eventCheckedThisPhase = true;
                    if (Main.dayTime)
                        TryTriggerDaytimeEvents();
                    else
                        TryTriggerNighttimeEvents();
                }
            }
            else
            {
                _eventCheckedThisPhase = false;
            }
        }

        private void TryTriggerDaytimeEvents()
        {
            if (Main.hardMode && !Main.eclipse)
            {
                if (Main.rand.NextDouble() < _config.Events.EclipseChance)
                {
                    Main.eclipse = true;
                    TSPlayer.All.SendData(PacketTypes.WorldInfo);
                    if (_config.Events.AnnounceEvents)
                        TSPlayer.All.SendMessage("A solar eclipse is happening!", new Microsoft.Xna.Framework.Color(255, 128, 0));
                    _eventCooldownCounter = _config.Events.EventCheckCooldown;
                    return;
                }
            }

            if (Main.hardMode && Main.invasionType == 0)
            {
                if (Main.rand.NextDouble() < _config.Events.PirateInvasionChance)
                {
                    Main.StartInvasion(InvasionID.PirateInvasion);
                    TSPlayer.All.SendData(PacketTypes.WorldInfo);
                    if (_config.Events.AnnounceEvents)
                        TSPlayer.All.SendMessage("Pirates are approaching from the west!", new Microsoft.Xna.Framework.Color(128, 0, 255));
                    _eventCooldownCounter = _config.Events.EventCheckCooldown;
                    return;
                }
            }

            if (NPC.downedBoss2 && Main.invasionType == 0)
            {
                if (Main.rand.NextDouble() < _config.Events.GoblinArmyChance)
                {
                    Main.StartInvasion(InvasionID.GoblinArmy);
                    TSPlayer.All.SendData(PacketTypes.WorldInfo);
                    if (_config.Events.AnnounceEvents)
                        TSPlayer.All.SendMessage("A goblin army is approaching from the west!", new Microsoft.Xna.Framework.Color(0, 200, 0));
                    _eventCooldownCounter = _config.Events.EventCheckCooldown;
                    return;
                }
            }

            if (!Main.slimeRain && Main.invasionType == 0 && !Main.eclipse)
            {
                if (Main.rand.NextDouble() < _config.Events.SlimeRainChance)
                {
                    Main.StartSlimeRain(true);
                    TSPlayer.All.SendData(PacketTypes.WorldInfo);
                    if (_config.Events.AnnounceEvents)
                        TSPlayer.All.SendMessage("Slime is falling from the sky!", new Microsoft.Xna.Framework.Color(0, 128, 255));
                    _eventCooldownCounter = _config.Events.EventCheckCooldown;
                    return;
                }
            }
        }

        private void TryTriggerNighttimeEvents()
        {
            if (!Main.bloodMoon)
            {
                if (Main.rand.NextDouble() < _config.Events.BloodMoonChance)
                {
                    Main.bloodMoon = true;
                    TSPlayer.All.SendData(PacketTypes.WorldInfo);
                    if (_config.Events.AnnounceEvents)
                        TSPlayer.All.SendMessage("The Blood Moon is rising...", new Microsoft.Xna.Framework.Color(255, 0, 0));
                    _eventCooldownCounter = _config.Events.EventCheckCooldown;
                }
            }
        }

        private void ApplyPeriodicPlayerEnhancements()
        {
            foreach (var player in TShock.Players)
            {
                if (player == null || !player.Active || !player.RealPlayer || player.TPlayer == null)
                    continue;

                player.TPlayer.luck = _config.LuckValue;

                if (_config.Fishing.Enabled)
                {
                    player.TPlayer.fishingSkill += _config.Fishing.FishingPowerBonus;

                    if (_config.Fishing.ApplyFishingBuffs)
                    {
                        int duration = _config.Fishing.BuffDuration;
                        player.SetBuff(BuffID.Fishing, duration, true);
                        player.SetBuff(BuffID.Sonar, duration, true);
                        player.SetBuff(BuffID.Crate, duration, true);
                    }
                }
            }
        }

        private void SpawnItem(NPC npc, int itemId, int stack)
        {
            if (itemId <= 0 || itemId >= ItemID.Count)
                return;

            int itemIndex = Item.NewItem(
                npc.GetItemSource_Loot(),
                (int)npc.position.X,
                (int)npc.position.Y,
                npc.width,
                npc.height,
                itemId,
                stack
            );

            if (itemIndex >= 0)
            {
                NetMessage.SendData((int)PacketTypes.ItemDrop, -1, -1, null, itemIndex, 1f);
            }
        }

        private bool IsCoinOrStackable(int itemId)
        {
            if (itemId == ItemID.CopperCoin || itemId == ItemID.SilverCoin ||
                itemId == ItemID.GoldCoin || itemId == ItemID.PlatinumCoin)
                return true;

            if (itemId == ItemID.Heart || itemId == ItemID.Star)
                return true;

            if (itemId == ItemID.Gel || itemId == ItemID.PinkGel)
                return true;

            return false;
        }

        private void MaxLuckCmd(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                ShowStatus(args.Player);
                return;
            }

            string subCmd = args.Parameters[0].ToLower();

            switch (subCmd)
            {
                case "on":
                    if (_config.Enabled)
                    {
                        args.Player.SendErrorMessage("MaxLuck is already enabled.");
                    }
                    else
                    {
                        _config.Enabled = true;
                        _config.Write(_configPath);
                        args.Player.SendSuccessMessage("MaxLuck has been enabled.");
                        if (_config.BroadcastOnToggle)
                            TSPlayer.All.SendInfoMessage($"{args.Player.Name} has enabled MaxLuck.");
                    }
                    break;

                case "off":
                    if (!_config.Enabled)
                    {
                        args.Player.SendErrorMessage("MaxLuck is already disabled.");
                    }
                    else
                    {
                        _config.Enabled = false;
                        _config.Write(_configPath);
                        args.Player.SendSuccessMessage("MaxLuck has been disabled.");
                        NPC.defaultSpawnRate = 600;
                        NPC.defaultMaxSpawns = 5;
                        if (_config.BroadcastOnToggle)
                            TSPlayer.All.SendInfoMessage($"{args.Player.Name} has disabled MaxLuck.");
                    }
                    break;

                case "reload":
                    LoadConfig();
                    args.Player.SendSuccessMessage("MaxLuck configuration reloaded.");
                    break;

                case "status":
                    ShowStatus(args.Player);
                    break;

                default:
                    args.Player.SendErrorMessage("Invalid syntax. Usage: /maxluck [on|off|reload|status]");
                    break;
            }
        }

        private void ShowStatus(TSPlayer player)
        {
            player.SendInfoMessage("═══ MaxLuck - Guaranteed Drops System ═══");
            player.SendInfoMessage($"Plugin: {(_config.Enabled ? "[c/00FF00:ENABLED]" : "[c/FF0000:DISABLED]")}");
            player.SendInfoMessage($"Luck Value: [c/FFD700:{_config.LuckValue}]");
            player.SendInfoMessage("─── Guaranteed Drops ───");
            player.SendInfoMessage($"  System: {StatusTag(_config.GuaranteedDrops.Enabled)}");
            player.SendInfoMessage($"  Bosses: {StatusTag(_config.GuaranteedDrops.ApplyToBosses)}");
            player.SendInfoMessage($"  Event Loot: {StatusTag(_config.GuaranteedDrops.GuaranteedEventLoot)}");
            player.SendInfoMessage($"  Rare Creatures: {StatusTag(_config.GuaranteedDrops.GuaranteedRareCreatureLoot)}");
            player.SendInfoMessage($"  Fishing Loot: {StatusTag(_config.GuaranteedDrops.GuaranteedFishingLoot)}");
            player.SendInfoMessage($"  Prevent Duplicates: {StatusTag(_config.GuaranteedDrops.PreventDuplicates)}");
            player.SendInfoMessage($"  Excluded Items: {_config.GuaranteedDrops.ExcludedItemIds.Count}");
            player.SendInfoMessage("─── Spawns ───");
            player.SendInfoMessage($"  Rate: {_config.EnemySpawns.SpawnRateMultiplier}x | Max: {_config.EnemySpawns.MaxSpawnsMultiplier}x");
            player.SendInfoMessage("─── Fishing ───");
            player.SendInfoMessage($"  Power Bonus: +{_config.Fishing.FishingPowerBonus} | Buffs: {StatusTag(_config.Fishing.ApplyFishingBuffs)}");
            player.SendInfoMessage("─── Events ───");
            player.SendInfoMessage($"  Enhanced: {StatusTag(_config.Events.IncreaseRareEvents)}");
            player.SendInfoMessage("───────────────────────────────────────");
            player.SendInfoMessage("Usage: /maxluck [on|off|reload|status]");
        }

        private string StatusTag(bool enabled)
        {
            return enabled ? "[c/00FF00:ON]" : "[c/FF0000:OFF]";
        }
    }

    internal struct GuaranteedDrop
    {
        public int ItemId;
        public int MinStack;
        public int MaxStack;

        public GuaranteedDrop(int itemId, int minStack, int maxStack)
        {
            ItemId = itemId;
            MinStack = Math.Max(1, minStack);
            MaxStack = Math.Max(minStack, maxStack);
        }
    }
}
