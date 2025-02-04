﻿using PKHeX.Core;
using TeraFinder.Core;
using System.IO.Compression;

namespace TeraFinder.Plugins;

internal static class ImportUtil
{
    public static bool ImportNews(SaveFile sav, 
                                ref EncounterRaid9[]? dist,
                                ref EncounterRaid9[]? mighty,
                                ref Dictionary<ulong, List<Reward>>? distFixedRewards,
                                ref Dictionary<ulong, List<Reward>>? distLotteryRewards,
                                string language,
                                string path = "",
                                bool plugin = false)
    {
        var isRaid = false;
        var isOutbreak = false;
        var zip = false;

        if (sav is not SAV9SV)
            return false;

        var strings = GenerateDictionary().TranslateInnerStrings(language);

        if (path.Equals(""))
        {
            var dialog = new OpenFileDialog
            {
                Title = strings["ImportNews.Title"],
                Filter = $"{strings["ImportNews.Filter"]} (*.*)|*.*",
                FileName = strings["ImportNews.FolderSelection"],
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (!Path.GetExtension(dialog.FileName).Equals(".zip"))
                    path = Path.GetDirectoryName(dialog.FileName)!;
                else
                    path = dialog.FileName;
            }
            else return false;
        }

        if (File.Exists(path))
            if (Path.GetExtension(path).Equals(".zip"))
            {
                var tmp = $"{Path.GetDirectoryName(path)}\\tmp";
                ZipFile.ExtractToDirectory(path, tmp);
                path = tmp;
                zip = true;
            }

        if (Directory.Exists(path))
            if (IsValidFolderRaid(path))
                isRaid = true;
            else if (IsValidFolderOutbreak(path))
                isOutbreak = true;

        if (isRaid)
            return FinalizeImportRaid(path, sav, zip, ref dist, ref mighty, ref distFixedRewards, ref distLotteryRewards, strings);

        if (isOutbreak)
            return FinalizeImportOutbreak(path, sav, zip, strings);

        if (plugin || zip)
        {
            if (zip) DeleteFilesAndDirectory(path);
            MessageBox.Show(strings["ImportNews.InvalidSelection"]);
        }

        return false;
    }

    private static bool IsValidFolderRaid(string path)
    {
        if (!File.Exists($"{path}\\Identifier.txt"))
            return false;
        if (!File.Exists($"{path}\\Files\\event_raid_identifier") &&
            !File.Exists($"{path}\\Files\\event_raid_identifier_1_3_0") &&
            !File.Exists($"{path}\\Files\\event_raid_identifier_2_0_0"))
            return false;
        if (!File.Exists($"{path}\\Files\\fixed_reward_item_array") && 
            !File.Exists($"{path}\\Files\\fixed_reward_item_array_1_3_0") &&
            !File.Exists($"{path}\\Files\\fixed_reward_item_array_2_0_0"))
            return false;
        if (!File.Exists($"{path}\\Files\\lottery_reward_item_array") && 
            !File.Exists($"{path}\\Files\\lottery_reward_item_array_1_3_0") &&
            !File.Exists($"{path}\\Files\\lottery_reward_item_array_2_0_0"))
            return false;
        if (!File.Exists($"{path}\\Files\\raid_enemy_array") && 
            !File.Exists($"{path}\\Files\\raid_enemy_array_1_3_0") &&
            !File.Exists($"{path}\\Files\\raid_enemy_array_2_0_0"))
            return false;
        if (!File.Exists($"{path}\\Files\\raid_priority_array") && 
            !File.Exists($"{path}\\Files\\raid_priority_array_1_3_0") &&
            !File.Exists($"{path}\\Files\\raid_priority_array_2_0_0"))
            return false;

        return true;
    }

    private static bool IsValidFolderOutbreak(string path)
    {
        if (!File.Exists($"{path}\\Identifier.txt"))
            return false;

        if (!File.Exists($"{path}\\Files\\pokedata_array_2_0_0"))
            return false;

        if (!File.Exists($"{path}\\Files\\zone_main_array_2_0_0"))
            return false;

        if (!File.Exists($"{path}\\Files\\zone_su1_array_2_0_0"))
            return false;

        return true;
    }

    private static void DeleteFilesAndDirectory(string targetDir)
    {
        string[] files = Directory.GetFiles(targetDir);
        string[] dirs = Directory.GetDirectories(targetDir);

        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (string dir in dirs)
            DeleteFilesAndDirectory(dir);

        Directory.Delete(targetDir, false);
    }

    private static bool FinalizeImportRaid(string path, 
                                      SaveFile sv, 
                                      bool zip,
                                      ref EncounterRaid9[]? dist,
                                      ref EncounterRaid9[]? mighty,
                                      ref Dictionary<ulong, List<Reward>>? distFixedRewards,
                                      ref Dictionary<ulong, List<Reward>>? distLotteryRewards,
                                      Dictionary<string, string> strings)
    {
        try
        {
            var indexpath = Path.Combine(path, "Identifier.txt");

            var filespath = Path.Combine(path, "Files");
            var identifierpath = Path.Combine(filespath, "event_raid_identifier_2_0_0");
            var encounterspath = Path.Combine(filespath, "raid_enemy_array_2_0_0");
            var dropspath = Path.Combine(filespath, "fixed_reward_item_array_2_0_0");
            var bonuspath = Path.Combine(filespath, "lottery_reward_item_array_2_0_0");
            var prioritypath = Path.Combine(filespath, "raid_priority_array_2_0_0");

            if (!File.Exists(identifierpath))
                identifierpath = Path.Combine(filespath, "event_raid_identifier_1_3_0");
            if (!File.Exists(encounterspath))
                encounterspath = Path.Combine(filespath, "raid_enemy_array_1_3_0");
            if (!File.Exists(dropspath))
                dropspath = Path.Combine(filespath, "fixed_reward_item_array_1_3_0");
            if (!File.Exists(bonuspath))
                bonuspath = Path.Combine(filespath, "lottery_reward_item_array_1_3_0");
            if (!File.Exists(prioritypath))
                prioritypath = Path.Combine(filespath, "raid_priority_array_1_3_0");

            if (!File.Exists(identifierpath))
                identifierpath = Path.Combine(filespath, "event_raid_identifier");
            if (!File.Exists(encounterspath))
                encounterspath = Path.Combine(filespath, "raid_enemy_array");
            if (!File.Exists(dropspath))
                dropspath = Path.Combine(filespath, "fixed_reward_item_array");
            if (!File.Exists(bonuspath))
                bonuspath = Path.Combine(filespath, "lottery_reward_item_array");
            if (!File.Exists(prioritypath))
                prioritypath = Path.Combine(filespath, "raid_priority_array");

            var index = File.ReadAllText(indexpath);
            var identifierBlock = File.ReadAllBytes(identifierpath);
            var rewardItemBlock = File.ReadAllBytes(dropspath);
            var lotteryItemBlock = File.ReadAllBytes(bonuspath);
            var raidEnemyBlock = File.ReadAllBytes(encounterspath);
            var raidPriorityBlock = File.ReadAllBytes(prioritypath);

            if (zip) DeleteFilesAndDirectory(path);

            var sav = (SAV9SV)sv;

            var KBCATEventRaidIdentifier = sav.Accessor.FindOrDefault(Blocks.KBCATEventRaidIdentifier.Key);
            var KBCATFixedRewardItemArray = sav.Accessor.FindOrDefault(Blocks.KBCATFixedRewardItemArray.Key);
            var KBCATLotteryRewardItemArray = sav.Accessor.FindOrDefault(Blocks.KBCATLotteryRewardItemArray.Key);
            var KBCATRaidEnemyArray = sav.Accessor.FindOrDefault(Blocks.KBCATRaidEnemyArray.Key);
            var KBCATRaidPriorityArray = sav.Accessor.FindOrDefault(Blocks.KBCATRaidPriorityArray.Key);

            if (KBCATEventRaidIdentifier.Type is not SCTypeCode.None)
                KBCATEventRaidIdentifier.ChangeData(identifierBlock);
            else
                BlockUtil.EditBlock(KBCATEventRaidIdentifier, SCTypeCode.Object, identifierBlock);

            if (KBCATFixedRewardItemArray.Type is not SCTypeCode.None)
                KBCATFixedRewardItemArray.ChangeData(rewardItemBlock);
            else
                BlockUtil.EditBlock(KBCATFixedRewardItemArray, SCTypeCode.Object, rewardItemBlock);

            if (KBCATLotteryRewardItemArray.Type is not SCTypeCode.None)
                KBCATLotteryRewardItemArray.ChangeData(lotteryItemBlock);
            else
                BlockUtil.EditBlock(KBCATLotteryRewardItemArray, SCTypeCode.Object, lotteryItemBlock);

            if (KBCATRaidEnemyArray.Type is not SCTypeCode.None)
                KBCATRaidEnemyArray.ChangeData(raidEnemyBlock);
            else
                BlockUtil.EditBlock(KBCATRaidEnemyArray, SCTypeCode.Object, raidEnemyBlock);

            if (KBCATRaidPriorityArray.Type is not SCTypeCode.None)
                KBCATRaidPriorityArray.ChangeData(raidPriorityBlock);
            else
                BlockUtil.EditBlock(KBCATRaidPriorityArray, SCTypeCode.Object, raidPriorityBlock);

            var events = TeraUtil.GetSAVDistEncounters(sav);
            var eventsrewards = RewardUtil.GetDistRewardsTables(sav);
            dist = events[0];
            mighty = events[1];
            distFixedRewards = eventsrewards[0];
            distLotteryRewards = eventsrewards[1];

            if (KBCATRaidEnemyArray is not null)
                MessageBox.Show($"{strings["ImportNews.Success"]} [{index}]!");

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{strings["ImportNews.Error"]}\n{ex}");
            return false;
        }
    }

    private static bool FinalizeImportOutbreak(string path,
                                  SaveFile sv,
                                  bool zip,
                                  Dictionary<string, string> strings)
    {
        try
        {
            var indexpath = Path.Combine(path, "Identifier.txt");

            var filespath = Path.Combine(path, "Files");
            var pokedatapath = Path.Combine(filespath, "pokedata_array_2_0_0");
            var paldeazonepath = Path.Combine(filespath, "zone_main_array_2_0_0");
            var kitakamizonepath = Path.Combine(filespath, "zone_su1_array_2_0_0");

            var index = File.ReadAllText(indexpath);
            var pokeDataBlock = File.ReadAllBytes(pokedatapath);
            var paldeaZoneBlock = File.ReadAllBytes(paldeazonepath);
            var kitakamiZoneBlock = File.ReadAllBytes(kitakamizonepath);

            if (zip) DeleteFilesAndDirectory(path);

            var sav = (SAV9SV)sv;

            var KBCATOutbreakPokeData = sav.Accessor.FindOrDefault(Blocks.KBCATOutbreakPokeData.Key);
            var KBCATOutbreakZonesPaldea = sav.Accessor.FindOrDefault(Blocks.KBCATOutbreakZonesPaldea.Key);
            var KBCATOutbreakZonesKitakami = sav.Accessor.FindOrDefault(Blocks.KBCATOutbreakZonesKitakami.Key);
            var KBCATOutbreakEnabled = sav.Accessor.FindOrDefault(Blocks.KBCATOutbreakEnabled.Key);

            if (KBCATOutbreakPokeData.Type is not SCTypeCode.None)
                KBCATOutbreakPokeData.ChangeData(pokeDataBlock);

            if (KBCATOutbreakZonesPaldea.Type is not SCTypeCode.None)
                KBCATOutbreakZonesPaldea.ChangeData(paldeaZoneBlock);

            if (KBCATOutbreakZonesKitakami.Type is not SCTypeCode.None)
                KBCATOutbreakZonesKitakami.ChangeData(kitakamiZoneBlock);

            if (KBCATOutbreakEnabled.Type is not SCTypeCode.Bool2 && KBCATOutbreakEnabled.Type is not SCTypeCode.None)
                KBCATOutbreakEnabled.ChangeBooleanType(SCTypeCode.Bool2);

            if (KBCATOutbreakPokeData is not null && KBCATOutbreakPokeData.Type is not SCTypeCode.None)
                MessageBox.Show($"{strings["ImportNews.Success"]} [{index}]!");

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{strings["ImportNews.Error"]}\n{ex}");
            return false;
        }
    }

    private static Dictionary<string, string> GenerateDictionary()
    {
        var strings = new Dictionary<string, string>
        {
            { "ImportNews.Title", "Open Poké Portal News Zip file or Folder" },
            { "ImportNews.Filter", "All files" },
            { "ImportNews.FolderSelection", "Folder Selection" },
            { "ImportNews.InvalidSelection", "Invalid file(s). Aborted." },
            { "ImportNews.Success", "Succesfully imported Event" },
            { "ImportNews.Error", "Import error! Is the provided file valid?" },
        };
        return strings;
    }
}
