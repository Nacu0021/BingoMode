﻿using BingoMode.BingoSteamworks;
using BingoMode.Challenges;
using Expedition;
using RWCustom;
using Steamworks;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BingoMode
{
    public static class BingoSaveFile
    {
        public static void Apply()
        {
            On.Expedition.ExpeditionCoreFile.ToString += ExpeditionCoreFile_ToString;
            On.Expedition.ExpeditionCoreFile.FromString += ExpeditionCoreFile_FromString;
        }

        private static void ExpeditionCoreFile_FromString(On.Expedition.ExpeditionCoreFile.orig_FromString orig, ExpeditionCoreFile self, string saveString)
        {
            orig.Invoke(self, saveString);
            Load();
        }

        private static string ExpeditionCoreFile_ToString(On.Expedition.ExpeditionCoreFile.orig_ToString orig, ExpeditionCoreFile self)
        {
            Save();
            return orig.Invoke(self);
        }

        public static void Save()
        {
            if (Custom.rainWorld.options == null || BingoData.BingoSaves.Count == 0) return;

            string text = "";
            for (int i = 0; i < BingoData.BingoSaves.Count; i++)
            {
                BingoData.BingoSaveData saveData = BingoData.BingoSaves.ElementAt(i).Value;
                text += BingoData.BingoSaves.ElementAt(i).Key + "#" + saveData.size.ToString();
                if (SteamFinal.IsSaveMultiplayer(saveData))
                {
                    text +=
                    "#" +
                    saveData.team +
                    "#" +
                    saveData.hostID.GetSteamID64() +
                    "#" +
                    (saveData.isHost ? "1" : "0") +
                    "#" +
                    saveData.playerWhiteList +
                    "#" +
                    (saveData.lockout ? "1" : "0") +
                    "#" +
                    (saveData.showedWin ? "1" : "0") +
                    "#" +
                    (saveData.firstCycleSaved ? "1" : "0") +
                    "#" +
                    (saveData.passageUsed ? "1" : "0");
                }
                else
                {
                    text +=
                    "#" +
                    (saveData.showedWin ? "1" : "0") +
                    "#" +
                    saveData.team +
                    "#" +
                    (saveData.firstCycleSaved ? "1" : "0") +
                    "#" +
                    (saveData.passageUsed ? "1" : "0");
                }

                // Add teams string for all challenges at the end of this
                text += "#";
                List<string> teamStrings = [];
                for (int c = 0; c < ExpeditionData.allChallengeLists[BingoData.BingoSaves.ElementAt(i).Key].Count; c++)
                {
                    teamStrings.Add((ExpeditionData.allChallengeLists[BingoData.BingoSaves.ElementAt(i).Key][c] as BingoChallenge).TeamsToString());
                }
                text += string.Join("|", teamStrings);

                if (i < BingoData.BingoSaves.Count - 1)
                {
                    text += "<>";
                }
            }

            File.WriteAllText(Application.persistentDataPath + 
                Path.DirectorySeparatorChar.ToString() + 
                "Bingo" + 
                Path.DirectorySeparatorChar.ToString() + 
                "bingo" + 
                Mathf.Abs(Custom.rainWorld.options.saveSlot) + 
                ".txt", 
                text);
        }

        public static void Load()
        {
            if (Custom.rainWorld.options == null) return;
            Plugin.logger.LogFatal(ExpeditionData.challengeList.Count);

            string path = Application.persistentDataPath +
                Path.DirectorySeparatorChar.ToString() +
                "Bingo" +
                Path.DirectorySeparatorChar.ToString() +
                "bingo" +
                Mathf.Abs(Custom.rainWorld.options.saveSlot) +
                ".txt";

            if (!File.Exists(path)) return;

            BingoData.BingoSaves = [];
            string data = File.ReadAllText(path);

            string[] array = Regex.Split(data, "<>");
            for (int i = 0; i < array.Length; i++)
            {
                Plugin.logger.LogMessage(array[i]);
                string[] array2 = array[i].Split('#');
                SlugcatStats.Name slug = new(array2[0]);
                int size = int.Parse(array2[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                try
                {
                    if (array2.Length > 6)
                    {
                        int team = int.Parse(array2[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                        SteamNetworkingIdentity hostIdentity = new SteamNetworkingIdentity();
                        hostIdentity.SetSteamID64(ulong.Parse(array2[3], NumberStyles.Any, CultureInfo.InvariantCulture));
                        bool isHost = array2[4] == "1";
                        bool lockout = array2[6] == "1";
                        bool showedWin = false;
                        bool firstCycleSaved = false;
                        bool passageUsed = false;
                        if (array2.Length > 7)
                        {
                            showedWin = array2[7] == "1";
                            if (array2.Length > 8)
                            {
                                firstCycleSaved = array2[8] == "1";
                                passageUsed = array2[9] == "1";
                            }
                        }

                        Plugin.logger.LogMessage($"Loading multiplayer bingo save from string: Team-{team}, Host-{hostIdentity.GetSteamID()}, IsHost-{isHost}, Connected players-{array2[5]}, ShowedWin-{showedWin}, FirstCycleSaved-{firstCycleSaved}, PassageUsed={passageUsed}");

                        BingoData.BingoSaves[slug] = new(size, team, hostIdentity, isHost, array2[5], lockout, showedWin, firstCycleSaved, passageUsed);
                    }
                    else
                    {
                        bool showedWin = false;
                        int team = SteamTest.team;
                        bool firstCycleSaved = false;
                        bool passageUsed = false;
                        if (array2.Length > 2)
                        {
                            showedWin = array2[2] == "1";
                            if (array2.Length > 3)
                            {
                                team = int.Parse(array2[3], NumberStyles.Any, CultureInfo.InvariantCulture);
                                if (array2.Length > 4)
                                {
                                    firstCycleSaved = array2[4] == "1";
                                    passageUsed = array2[5] == "1";
                                }
                            }
                        }

                        Plugin.logger.LogMessage($"Loading singleplayer bingo save from string: Team-{team}, ShowedWin-{showedWin}, FirstCycleSaved-{firstCycleSaved}, PassageUsed={passageUsed}");

                        BingoData.BingoSaves[slug] = new(size, showedWin, team, firstCycleSaved, passageUsed);
                    }
                    string teamString = array2[array2.Length - 1];
                    string[] teams = teamString.Split('|');
                    int next = 0;
                    for (int x = 0; x < size; x++)
                    {
                        for (int y = 0; y < size; y++)
                        {
                            (ExpeditionData.allChallengeLists[slug][next] as BingoChallenge).TeamsFromString(teams[next]);
                            next++;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Plugin.logger.LogWarning("Failed to load save " + e);
                    BingoData.BingoSaves[new(array2[0])] = new(size, false, 0, false, false);
                }
            }
        }
    }
}
