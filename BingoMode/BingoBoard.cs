﻿using BingoMode.BingoSteamworks;
using BingoMode.Challenges;
using Expedition;
using Menu;
using RWCustom;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BingoMode
{
    public class BingoBoard
    {
        public ExpeditionCoreFile core;
        public Challenge[,] challengeGrid; // The challenges will be treated as coordinates on a grid for convenience
        public List<IntVector2> currentWinLine; // A list of grid coordinates
        public int size;
        public List<Challenge> recreateList;

        public BingoBoard()
        {
            size = 5;
            currentWinLine = [];
            recreateList = [];
        }

        public void GenerateBoard(int size, bool changeSize = false)
        {
            Plugin.logger.LogMessage("Generating board");
            Challenge[,] ghostGrid = new Challenge[size, size];
            BingoData.FillPossibleTokens(ExpeditionData.slugcatPlayer);
            ExpeditionData.ClearActiveChallengeList();
            if (changeSize)
            { 
                ghostGrid = challengeGrid;
            }
            challengeGrid = new Challenge[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (changeSize)
                    {
                        if (!(i + 1 > ghostGrid.GetLength(0) || j + 1 > ghostGrid.GetLength(1)) && ghostGrid[i, j] != null)
                        {
                            challengeGrid[i, j] = ghostGrid[i, j];
                            if (!ExpeditionData.challengeList.Contains(challengeGrid[i, j])) ExpeditionData.challengeList.Add(challengeGrid[i, j]);
                            continue;
                        }
                    }
                    if (challengeGrid[i, j] != null)
                    {
                        continue;
                    }
                    challengeGrid[i, j] = RandomBingoChallenge(x: i, y: j);
                }
            }
            SteamTest.UpdateOnlineBingo();
            UpdateChallenges();
        }

        public void UpdateChallenges()
        {
            foreach (Challenge c in ExpeditionData.challengeList)
            {
                c.UpdateDescription();
            }
            ExpeditionMenu self = BingoData.globalMenu;
            if (self != null && BingoHooks.bingoPage.TryGetValue(self, out var page) && page.grid != null)
            {
                if (page.grid != null)
                {
                    page.grid.RemoveSprites();
                    page.RemoveSubObject(page.grid);
                    page.grid = null;
                }
                page.grid = new BingoGrid(self, page, new(self.manager.rainWorld.screenSize.x / 2f, self.manager.rainWorld.screenSize.y / 2f), 500f);
                page.subObjects.Add(page.grid);
                if (SteamTest.CurrentLobby != default && SteamMatchmaking.GetLobbyOwner(SteamTest.CurrentLobby).m_SteamID != SteamTest.selfIdentity.GetSteamID64())
                {
                    page.grid.Switch(true);
                }
            }
        }

        public bool CheckWin(int t, bool checkLose = false, List<IntVector2> overrideArray = null) // Checks whether a team won or cant win
        {
            bool won = false;
            currentWinLine = [];
            bool lockout = BingoData.BingoSaves.ContainsKey(ExpeditionData.slugcatPlayer) && BingoData.BingoSaves[ExpeditionData.slugcatPlayer].lockout;

            // Vertical lines
            for (int i = 0; i < size; i++)
            {
                bool line = true;
                for (int j = 0; j < size; j++)
                {
                    var ch = challengeGrid[i, j];
                    if (checkLose)
                    {
                        if ((ch as BingoChallenge).TeamsFailed[t])
                        {
                            line = false;
                        }
                        if (line && lockout && (ch as BingoChallenge).TeamsCompleted.Any(x => x == true) && !(ch as BingoChallenge).TeamsCompleted[t])
                        {
                            line = false;
                        }
                    }
                    else
                    {
                        line &= (ch as BingoChallenge).TeamsCompleted[t];
                    }
                    //line &= checkLose ? !(ch as BingoChallenge).TeamsFailed[t] && (!lockout || ((ch as BingoChallenge).TeamsCompleted.Any(x => x == true) && !(ch as BingoChallenge).TeamsCompleted[t])) : (ch as BingoChallenge).TeamsCompleted[t];
                    if (line) currentWinLine.Add(new IntVector2(i, j));
                }
                won = line;
                if (won)
                {
                    if (!checkLose) Plugin.logger.LogMessage("Vertical win");
                    break;
                }
                else currentWinLine.Clear();
            }

            // Horizontal lines
            if (!won)
            {
                for (int i = 0; i < size; i++)
                {
                    bool line = true;
                    for (int j = 0; j < size; j++)
                    {
                        var ch = challengeGrid[j, i];
                        if (checkLose)
                        {
                            if ((ch as BingoChallenge).TeamsFailed[t])
                            {
                                line = false;
                            }
                            if (line && lockout && (ch as BingoChallenge).TeamsCompleted.Any(x => x == true) && !(ch as BingoChallenge).TeamsCompleted[t])
                            {
                                line = false;
                            }
                        }
                        else
                        {
                            line &= (ch as BingoChallenge).TeamsCompleted[t];
                        }
                        //line &= checkLose ? !(ch as BingoChallenge).TeamsFailed[t] && !ch.hidden : (ch as BingoChallenge).TeamsCompleted[t];
                        if (line) currentWinLine.Add(new IntVector2(j, i));
                    }
                    won = line;
                    if (won)
                    {
                        if (!checkLose) Plugin.logger.LogMessage("Horizontal win");
                        break;
                    }
                    else currentWinLine.Clear();
                }
            }

            // Diagonal line 1
            if (!won)
            {
                bool line = true;
                for (int i = 0; i < size; i++)
                {
                    var ch = challengeGrid[i, i];
                    if (checkLose)
                    {
                        if ((ch as BingoChallenge).TeamsFailed[t])
                        {
                            line = false;
                        }
                        if (line && lockout && (ch as BingoChallenge).TeamsCompleted.Any(x => x == true) && !(ch as BingoChallenge).TeamsCompleted[t])
                        {
                            line = false;
                        }
                    }
                    else
                    {
                        line &= (ch as BingoChallenge).TeamsCompleted[t];
                    }
                    //line &= checkLose ? !(ch as BingoChallenge).TeamsFailed[t] && !ch.hidden : (ch as BingoChallenge).TeamsCompleted[t];
                    if (line) currentWinLine.Add(new IntVector2(i, i));
                }
                won = line;
                if (won)
                {
                    if (!checkLose) Plugin.logger.LogMessage("Diagonal 1 win");
                }
                else currentWinLine.Clear();
            }

            // Diagonal line 2
            if (!won)
            {
                bool line = true;
                for (int i = 0; i < size; i++)
                {
                    var ch = challengeGrid[size - 1 - i, i];
                    if (checkLose)
                    {
                        if ((ch as BingoChallenge).TeamsFailed[t])
                        {
                            line = false;
                        }
                        if (line && lockout && (ch as BingoChallenge).TeamsCompleted.Any(x => x == true) && !(ch as BingoChallenge).TeamsCompleted[t])
                        {
                            line = false;
                        }
                    }
                    else
                    {
                        line &= (ch as BingoChallenge).TeamsCompleted[t];
                    }
                    //line &= checkLose ? !(ch as BingoChallenge).TeamsFailed[t] && !ch.hidden : (ch as BingoChallenge).TeamsCompleted[t];
                    if (line) currentWinLine.Add(new IntVector2(size - 1 - i, i));
                }
                won = line;
                if (won)
                {
                    if (!checkLose) Plugin.logger.LogMessage("Diagnoal 2 win");
                }
                else currentWinLine.Clear();
            }

            if (overrideArray != null)
            {
                foreach (var coord in currentWinLine)
                {
                    overrideArray.Add(coord);
                }
            }
            currentWinLine = [];
            return won;
        }

        public Challenge RandomBingoChallenge(Challenge type = null, bool ignore = false, int x = 1, int y = -1)
        {
            if (BingoData.availableBingoChallenges == null)
            {
                ChallengeOrganizer.SetupChallengeTypes();
            }

            List<Challenge> list = [];
            list.AddRange(BingoData.availableBingoChallenges);
            if (type is not BingoHellChallenge) list.RemoveAll(x => x is BingoHellChallenge);
            if (type != null) list.RemoveAll(x => x.GetType() != type.GetType());

        resette:
            Challenge ch = list[UnityEngine.Random.Range(0, list.Count)];
            if (!ch.ValidForThisSlugcat(ExpeditionData.slugcatPlayer))
            {
                list.Remove(ch);
                goto resette;
            }
            ch = ch.Generate();

            if (ExpeditionData.challengeList.Count > 0 && type == null && !ignore)
            {
                for (int i = 0; i < ExpeditionData.challengeList.Count; i++)
                {
                    if (!ExpeditionData.challengeList[i].Duplicable(ch))
                    {
                        list.Remove(ch);
                        ch = null;
                        goto resette;
                    }
                }
            }

            if (x != -1 && y != -1 && (ch as BingoChallenge).ReverseChallenge() && ReverseCollisionCheck(x, y))
            {
                list.Remove(ch);
                goto resette;
            }
            if (ch == null) ch = (Activator.CreateInstance(BingoData.availableBingoChallenges.Find((Challenge c) => c.GetType().Name == "BingoKillChallenge").GetType()) as Challenge).Generate(); ;
            if (!ExpeditionData.challengeList.Contains(ch) && !ignore) ExpeditionData.challengeList.Add(ch);
            return ch;
        }

        public bool ReverseCollisionCheck(int x, int y)
        {
            // Horizontal check
            for (int i = 0; i < size; i++)
            {
                if (challengeGrid[i, y] != null && (challengeGrid[i, y] as BingoChallenge).ReverseChallenge()) return true;
            }
            // Vertical check
            for (int i = 0; i < size; i++)
            {
                if (challengeGrid[x, i] != null && (challengeGrid[x, i] as BingoChallenge).ReverseChallenge()) return true;
            }
            // Horizontal 1 check
            if (x == y)
            {
                for (int i = 0; i < size; i++)
                {
                    if (challengeGrid[i, i] != null && (challengeGrid[i, i] as BingoChallenge).ReverseChallenge()) return true;
                }
            }
            // Horizontal 2 check
            if (size - 1 - y == x)
            {
                for (int i = 0; i < size; i++)
                {
                    if (challengeGrid[size - 1 - i, i] != null && (challengeGrid[size - 1 - i, i] as BingoChallenge).ReverseChallenge()) return true;
                }
            }

            return false;
        }

        public void RecreateFromList()
        {
            Plugin.logger.LogMessage("Recreating from list " + (recreateList != null ? recreateList.Count : "SHITS NULL"));
            Plugin.logger.LogMessage("Size of rec: " + size);
            if (recreateList != null && Mathf.RoundToInt(Mathf.Sqrt(recreateList.Count)) == size)
            {
                Plugin.logger.LogMessage("Went through");
                challengeGrid = new Challenge[size, size];
                int next = 0;
                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        //if (recreateList.Count < next + 1)
                        //{
                        //    challengeGrid[i, j] = RandomBingoChallenge();
                        //}
                        //else 
                        challengeGrid[i, j] = recreateList[next];
                        //(challengeGrid[i, j] as IBingoChallenge).Index = next;
                        Plugin.logger.LogMessage($"Recreated {recreateList[next]} at: {i}, {j}. Challenge - {challengeGrid[i, j]}");
                        next++;
                    }
                }
                Plugin.logger.LogMessage("Recreated list from thinj yipe");
                SteamTest.UpdateOnlineBingo();
                UpdateChallenges();
            }
            recreateList = [];
        }

        public void SetChallenge(int x, int y, Challenge newChallenge, int index)
        {
            try
            {
                int g1 = index == -1 ? ExpeditionData.challengeList.IndexOf(challengeGrid[x, y]) : index;
                Plugin.logger.LogMessage("Inserting ch to " + g1);
                ExpeditionData.challengeList.Remove(challengeGrid[x, y]);
                challengeGrid[x, y] = newChallenge;
                ExpeditionData.challengeList.Insert(g1, challengeGrid[x, y]);
                SteamTest.UpdateOnlineBingo();
            }
            catch (Exception e)
            {
                Plugin.logger.LogError("Invalid bingo board coordinates or challenge null :( " + e);
            }
        }

        public override string ToString()
        {
            string text = ExpeditionData.slugcatPlayer.value + "_" + string.Join("bChG", ExpeditionData.challengeList);
            return text;
        }
        
        public void FromString(string text)
        {
            if (string.IsNullOrEmpty(text) || !text.Contains("bChG") || !text.Contains('_')) return;
            string slug = text.Substring(0, text.IndexOf("_"));
            text = text.Substring(text.IndexOf("_") + 1);
            Plugin.logger.LogMessage(slug + " Bingo board from string:\n" + text);
            if (slug.ToLowerInvariant() != ExpeditionData.slugcatPlayer.value.ToLowerInvariant())
            {
                if (BingoData.globalMenu != null) BingoData.globalMenu.manager.ShowDialog(new InfoDialog(BingoData.globalMenu.manager, $"Slugcat mismatch\nSelected slugcat: {ExpeditionData.slugcatPlayer.value}\nProvided Slugcat: {slug}\n\nPlease paste a board from the same slugcat that's currently selected."));
                return;
            }

            string last = ToString();
            try
            {
                if (ExpeditionData.allChallengeLists.ContainsKey(ExpeditionData.slugcatPlayer) && ExpeditionData.allChallengeLists[ExpeditionData.slugcatPlayer] != null) ExpeditionData.allChallengeLists[ExpeditionData.slugcatPlayer].Clear();
                string[] challenges = Regex.Split(text, "bChG");
                size = Mathf.RoundToInt(Mathf.Sqrt(challenges.Length));
                int next = 0;
                challengeGrid = new Challenge[size, size];
                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        try
                        {
                            string[] array11 = Regex.Split(challenges[next], "~");
                            string type = array11[0];
                            string text2 = array11[1];
                            Challenge challenge = (Challenge)Activator.CreateInstance(BingoData.availableBingoChallenges.Find((Challenge c) => c.GetType().Name == type).GetType());
                            challenge.FromString(text2);
                            ExpLog.Log(challenge.description);
                            if (!ExpeditionData.allChallengeLists.ContainsKey(ExpeditionData.slugcatPlayer))
                            {
                                ExpeditionData.allChallengeLists.Add(ExpeditionData.slugcatPlayer, new List<Challenge>());
                            }
                            ExpeditionData.allChallengeLists[ExpeditionData.slugcatPlayer].Add(challenge);
                            challengeGrid[i, j] = challenge;
                        }
                        catch (Exception ex)
                        {
                            Plugin.logger.LogError("ERROR: Problem recreating challenge \"" + challenges[next] + "\" with reflection in bingoboard.fromstring: " + ex.Message);
                        }
                        next++;
                    }
                }
                UpdateChallenges();
            }
            catch
            {
                FromString(last);
            }
        }

        public void CompleteChallengeAt(int x, int y)
        {
            challengeGrid[x, y].CompleteChallenge();
        }

        public Challenge GetChallenge(int x, int y)
        {
            if (x < challengeGrid.GetLength(0) && y < challengeGrid.GetLength(1)) return challengeGrid[x, y];
            return null;
        }

        public string GetBingoState()
        {
            string state = "";
            for (int i = 0; i < challengeGrid.GetLength(0); i++)
            {
                for (int j = 0; j < challengeGrid.GetLength(1); j++)
                {
                    state += "<>" + (challengeGrid[i, j] as BingoChallenge).TeamsToString();
                }
            }
            if (state != "") state = state.Substring(2);
            return state;
        }

        public void InterpretBingoState(string state)
        {
            if (challengeGrid == null) { Plugin.logger.LogError("CHALLENGE GRID IS NULL!! Returning"); return; }

            string[] challenges = Regex.Split(state, "<>");
            Plugin.logger.LogMessage("All challenges count from interpret bingo state: " + challenges.Length);

            int next = 0;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (challengeGrid[i, j] == null)
                    {
                        next++;
                        continue;
                    }
                    BingoChallenge ch = challengeGrid[i, j] as BingoChallenge;
                    string currentTeamsString = ch.TeamsToString();
                    string newTeamsString = challenges[next];

                    //Plugin.logger.LogFatal($"Comparing {currentTeamsString} to {newTeamsString}");
                    // All the switch statements to make it 100% clear, obviously can be shortened down
                    if (currentTeamsString != newTeamsString)
                    {
                        for (int k = 0; k < currentTeamsString.Length; k++)
                        {
                            if (currentTeamsString[k] != newTeamsString[k])
                            {
                                switch (newTeamsString[k])
                                {
                                    case '0':
                                        switch (currentTeamsString[k])
                                        {
                                            case '0':
                                                // Do nothing
                                                break;
                                            case '1':
                                                ch.OnChallengeDepleted(k);
                                                break;
                                            case '2':
                                                // Do nothing
                                                break;
                                        }
                                        break;
                                    case '1':
                                        // If lockout
                                        if (BingoData.BingoSaves.ContainsKey(ExpeditionData.slugcatPlayer) && BingoData.BingoSaves[ExpeditionData.slugcatPlayer].lockout)
                                        {
                                            // If its the same team
                                            if (SteamTest.team == k || SteamTest.team == 8 || ch.ReverseChallenge())
                                            {
                                                switch (currentTeamsString[k])
                                                {
                                                    case '0':
                                                        ch.OnChallengeCompleted(k);
                                                        break;
                                                    case '1':
                                                        // Do nothing
                                                        break;
                                                    case '2':
                                                        // Do nothing
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                switch (currentTeamsString[k])
                                                {
                                                    case '0':
                                                        ch.OnChallengeLockedOut(k);
                                                        break;
                                                    case '1':
                                                        // Do nothing
                                                        break;
                                                    case '2':
                                                        ch.OnChallengeLockedOut(k);
                                                        break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            switch (currentTeamsString[k])
                                            {
                                                case '0':
                                                    ch.OnChallengeCompleted(k);
                                                    break;
                                                case '1':
                                                    // Do nothing
                                                    break;
                                                case '2':
                                                    // Do nothing
                                                    break;
                                            }
                                        }
                                        break;
                                    case '2':
                                        switch (currentTeamsString[k])
                                        {
                                            case '0':
                                                ch.OnChallengeFailed(k);
                                                break;
                                            case '1':
                                                ch.OnChallengeFailed(k);
                                                break;
                                            case '2':
                                                // Do nothing
                                                break;
                                        }
                                        break;
                                }

                                // This was the code before the switch hell
                                //if (currentTeamsString[k] == '1')
                                //{
                                //    //if (SteamTest.team != 8 && 
                                //    //    k != SteamTest.team &&
                                //    //    !ch.ReverseChallenge() && 
                                //    //    BingoData.BingoSaves.ContainsKey(ExpeditionData.slugcatPlayer) && 
                                //    //    BingoData.BingoSaves[ExpeditionData.slugcatPlayer].lockout)
                                //    //{
                                //    //    ch.OnChallengeLockedOut(k);
                                //    //}
                                //    ch.OnChallengeCompleted(k);
                                //}
                                //else
                                //{
                                //    ch.OnChallengeFailed(k);
                                //}
                            }
                        }
                    }
                    next++;
                }
            }
        }
    }
}
