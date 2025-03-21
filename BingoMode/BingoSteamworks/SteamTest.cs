﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Expedition;
using RWCustom;
using Steamworks;
using UnityEngine;

namespace BingoMode.BingoSteamworks
{
    using BingoMenu;

    internal class SteamTest
    {
        public static int team = 0;
        public static CSteamID CurrentLobby;
        public static LobbyFilters CurrentFilters;
        public static bool MultiplayerEnabled;

        public static SteamNetworkingIdentity selfIdentity;

        protected static Callback<SteamNetworkingMessagesSessionRequest_t> sessionRequest;
        protected static Callback<LobbyChatUpdate_t> lobbyChatUpdate;
        protected static Callback<LobbyDataUpdate_t> lobbyDataUpdate;
        protected static Callback<GameLobbyJoinRequested_t> lobbyJoinRequested;

        public static CallResult<LobbyMatchList_t> lobbyMatchList = new();
        public static CallResult<LobbyCreated_t> lobbyCreated = new();
        public static CallResult<LobbyEnter_t> lobbyEntered = new();

        public static void Apply()
        {
            CurrentFilters = new LobbyFilters("", 1, false);

            if (SteamManager.Initialized) 
            {
                sessionRequest = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(OnSessionRequested);
                lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
                lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
                lobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
                SteamNetworkingSockets.GetIdentity(out selfIdentity);
                MultiplayerEnabled = true;
            }

            BingoData.globalSettings.gamemode = BingoData.BingoGameMode.Bingo;
            BingoData.globalSettings.perks = LobbySettings.AllowUnlocks.None;
            BingoData.globalSettings.burdens = LobbySettings.AllowUnlocks.None;
        }

        public static void CreateLobby(int maxPlayers)
        {
            SteamAPICall_t call = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, maxPlayers);
            lobbyCreated.Set(call, OnLobbyCreated);
            BingoData.MultiplayerGame = true;
        }

        public static void GetJoinableLobbies()
        {
            

            SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
            if (CurrentFilters.text != "") SteamMatchmaking.AddRequestLobbyListStringFilter("name", CurrentFilters.text, ELobbyComparison.k_ELobbyComparisonEqualToOrGreaterThan);
            SteamMatchmaking.AddRequestLobbyListResultCountFilter(100);
            SteamAPICall_t call = SteamMatchmaking.RequestLobbyList();
            lobbyMatchList.Set(call, OnLobbyMatchList);
        }

        public static void LeaveLobby()
        {
            SteamMatchmaking.LeaveLobby(CurrentLobby);
            if (BingoData.globalMenu != null && BingoHooks.bingoPage.TryGetValue(BingoData.globalMenu, out var page))
            {
                page.Switch(false, false);
            }
            
            CurrentLobby = default;
            BingoData.MultiplayerGame = false;
        }

        public static void OnLobbyJoinRequested(GameLobbyJoinRequested_t callback)
        {
            if (BingoData.globalMenu != null && BingoHooks.bingoPage.TryGetValue(BingoData.globalMenu, out var page))
            {
                if (BingoData.globalMenu.currentPage != 4)
                {
                    if (page.grid == null)
                    {
                        BingoHooks.GlobalBoard.GenerateBoard(BingoHooks.GlobalBoard.size);
                        if (page.grid != null)
                        {
                            page.grid.RemoveSprites();
                            page.RemoveSubObject(page.grid);
                            page.grid = null;
                        }
                        page.grid = new BingoGrid(BingoData.globalMenu, page, new(BingoData.globalMenu.manager.rainWorld.screenSize.x / 2f, BingoData.globalMenu.manager.rainWorld.screenSize.y / 2f), 500f);
                        page.subObjects.Add(page.grid);
                    }
                    page.multiButton.Clicked();
                    BingoData.globalMenu.UpdatePage(4);
                    BingoData.globalMenu.MovePage(new Vector2(1500f, 0f));
                }
                if (page.slideStep == -1f)
                {
                    page.multiButton.buttonBehav.greyedOut = false;
                    page.multiButton.Clicked();
                }

                var call = SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
                lobbyEntered.Set(call, OnLobbyEntered);
            }
            else
            {
                Custom.rainWorld.processManager.ShowDialog(new InfoDialog(Custom.rainWorld.processManager, "Please enter the expedition menu to accept an invite."));
            }
        }

        public static void OnSessionRequested(SteamNetworkingMessagesSessionRequest_t callback)
        {
            SteamNetworkingMessages.AcceptSessionWithUser(ref callback.m_identityRemote);
            
        }

        public static string ActiveModsToString()
        {
            string text = "";

            foreach (var mod in ModManager.ActiveMods)
            {
                text += mod.id + "|" + mod.name + "<bMd>";
            }

            if (!string.IsNullOrEmpty(text)) text = text.Substring(0, text.Length - 5);
            
            return text;
        }

        public static void OnLobbyCreated(LobbyCreated_t result, bool bIOFailure)
        {
            if (bIOFailure) { Plugin.logger.LogError("OnLobbyCreated bIOfailure"); return; }
            if (result.m_eResult != EResult.k_EResultOK)
            {
                Plugin.logger.LogError("Failed to create the lobby!!");
                return;
            }
                        
            if (BingoData.globalSettings.perks == LobbySettings.AllowUnlocks.None) ExpeditionGame.activeUnlocks.RemoveAll(x => x.StartsWith("unl-"));
            if (BingoData.globalSettings.burdens == LobbySettings.AllowUnlocks.None) ExpeditionGame.activeUnlocks.RemoveAll(x => x.StartsWith("bur-"));
            team = 0;
                        
            CSteamID lobbyID = (CSteamID)result.m_ulSteamIDLobby;
            CurrentLobby = lobbyID;
            string hostName = SteamFriends.GetPersonaName();
            SteamMatchmaking.SetLobbyData(lobbyID, "mode", "BingoMode");
            SteamMatchmaking.SetLobbyData(lobbyID, "name", hostName + "'s lobby");
            SteamMatchmaking.SetLobbyData(lobbyID, "isHost", hostName);
            SteamMatchmaking.SetLobbyData(lobbyID, "hostID", selfIdentity.GetSteamID64().ToString());
            SteamMatchmaking.SetLobbyData(lobbyID, "slugcat", ExpeditionData.slugcatPlayer.value);
            SteamMatchmaking.SetLobbyData(lobbyID, "gamemode", ((int)BingoData.globalSettings.gamemode).ToString());
            SteamMatchmaking.SetLobbyData(lobbyID, "friendsOnly", BingoData.globalSettings.friendsOnly ? "1" : "0");
            SteamMatchmaking.SetLobbyData(lobbyID, "hostMods", BingoData.globalSettings.hostMods ? ActiveModsToString() : "none");
            SteamMatchmaking.SetLobbyData(lobbyID, "perks", ((int)BingoData.globalSettings.perks).ToString());
            SteamMatchmaking.SetLobbyData(lobbyID, "burdens", ((int)BingoData.globalSettings.burdens).ToString());
            SteamMatchmaking.SetLobbyMemberData(lobbyID, "playerTeam", team.ToString());
            SteamMatchmaking.SetLobbyData(lobbyID, "perkList", Expedition.Expedition.coreFile.ActiveUnlocksString(ExpeditionGame.activeUnlocks.Where(x => x.StartsWith("unl-")).ToList()));
            SteamMatchmaking.SetLobbyData(lobbyID, "burdenList", Expedition.Expedition.coreFile.ActiveUnlocksString(ExpeditionGame.activeUnlocks.Where(x => x.StartsWith("bur-")).ToList()));
            SteamMatchmaking.SetLobbyData(lobbyID, "lobbyVersion", Plugin.VERSION);
            SteamMatchmaking.SetLobbyData(lobbyID, "randomSeed", UnityEngine.Random.Range(1000, 10000).ToString());
            SteamMatchmaking.SetLobbyData(lobbyID, "nextPlayerIndex", "1");
            // other settings idk
            UpdateOnlineBingo();
            SteamMatchmaking.SetLobbyJoinable(lobbyID, true);

            if (BingoData.globalMenu != null && BingoHooks.bingoPage.TryGetValue(BingoData.globalMenu, out var page))
            {
                page.Switch(true, true);
            }
        }

        public static void OnLobbyEntered(LobbyEnter_t callback, bool bIOFailure)
        {
            if (bIOFailure) { Plugin.logger.LogError("OnLobbyEntered bIOfailure"); return; }
            if (callback.m_EChatRoomEnterResponse != 1)
            {
                Plugin.logger.LogError("Failed to enter lobby " + callback.m_ulSteamIDLobby + "! " + callback.m_EChatRoomEnterResponse);
                return;
            }
            if (selfIdentity.GetSteamID() == SteamMatchmaking.GetLobbyOwner((CSteamID)callback.m_ulSteamIDLobby)) return;
            CSteamID lobbyID = (CSteamID)callback.m_ulSteamIDLobby;
            CurrentLobby = lobbyID;
            
            if (BingoData.globalMenu != null)
            {
                string slug = SteamMatchmaking.GetLobbyData(lobbyID, "slugcat");
                int valveIndex = ExpeditionGame.playableCharacters.IndexOf(new (slug, false));
                if (valveIndex != -1)
                {
                    BingoData.globalMenu.currentSelection = valveIndex;
                    BingoData.globalMenu.characterSelect.UpdateSelectedSlugcat(valveIndex);
                }
                else
                {
                    Plugin.logger.LogError("UNAVAILABLE SLUGCAT DETECTED: " + slug);
                    LeaveLobby();
                    return;
                }
            }

            FetchLobbySettings();

            team = 0;

            
            SteamMatchmaking.SetLobbyMemberData(lobbyID, "playerTeam", team.ToString());
            SteamMatchmaking.SetLobbyMemberData(lobbyID, "playerIndex", SteamMatchmaking.GetLobbyData(CurrentLobby, "nextPlayerIndex"));
            SteamMatchmaking.SetLobbyMemberData(lobbyID, "ready", "0");
            

            string challenjes = SteamMatchmaking.GetLobbyData(lobbyID, "challenges");
            try
            {
                BingoHooks.GlobalBoard.FromString(challenjes);
            }
            catch (Exception e)
            {
                Plugin.logger.LogError(e + "\nFAILED TO RECREATE BINGO BOARD FROM STRING FROM LOBBY: " + challenjes);
                LeaveLobby();
                return;
            }

            if (BingoData.globalMenu != null && BingoHooks.bingoPage.TryGetValue(BingoData.globalMenu, out var page))
            {
                page.Switch(true, false);
            }
        }

        public static void FetchLobbySettings()
        {
            int gamjs = int.Parse(SteamMatchmaking.GetLobbyData(CurrentLobby, "gamemode").Trim(), NumberStyles.Any);
            BingoData.globalSettings.gamemode = (BingoData.BingoGameMode)gamjs;

            int perjs = int.Parse(SteamMatchmaking.GetLobbyData(CurrentLobby, "perks").Trim(), NumberStyles.Any);
            
            int burjens = int.Parse(SteamMatchmaking.GetLobbyData(CurrentLobby, "burdens").Trim(), NumberStyles.Any);
            BingoData.globalSettings.perks = (LobbySettings.AllowUnlocks)perjs;
            BingoData.globalSettings.burdens = (LobbySettings.AllowUnlocks)burjens;
                        
            if (BingoData.globalSettings.perks != LobbySettings.AllowUnlocks.Any)
            {
                ExpeditionGame.activeUnlocks.RemoveAll(x => x.StartsWith("unl-"));

                string[] perks = Regex.Split(SteamMatchmaking.GetLobbyData(CurrentLobby, "perkList"), "><");
                FetchUnlocks(perks);
            }
            if (BingoData.globalSettings.burdens != LobbySettings.AllowUnlocks.Any)
            {
                ExpeditionGame.activeUnlocks.RemoveAll(x => x.StartsWith("bur-"));

                string[] burdens = Regex.Split(SteamMatchmaking.GetLobbyData(CurrentLobby, "burdenList"), "><");
                FetchUnlocks(burdens);
            }

            BingoData.globalSettings.hostMods = SteamMatchmaking.GetLobbyData(CurrentLobby, "hostMods") != "none";
            BingoData.globalSettings.friendsOnly = SteamMatchmaking.GetLobbyData(CurrentLobby, "friendsOnly") == "1";
        }

        public static void FetchUnlocks(string[] unlockables)
        {
            foreach (string unlock in unlockables)
            {
                ExpeditionGame.activeUnlocks.Add(unlock);
            }
        }

        public static void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
        {
            
            string text = "";
            switch (callback.m_rgfChatMemberStateChange)
            {
                case 0x0001:
                    text = "entered";
                    
                    SteamNetworkingIdentity newMember = new SteamNetworkingIdentity();
                    newMember.SetSteamID((CSteamID)callback.m_ulSteamIDUserChanged);

                    if (SteamMatchmaking.GetLobbyOwner(CurrentLobby) == selfIdentity.GetSteamID())
                    {
                        int tim = int.Parse(SteamMatchmaking.GetLobbyData(CurrentLobby, "nextPlayerIndex"), NumberStyles.Any) + 1;
                        SteamMatchmaking.SetLobbyData(CurrentLobby, "nextPlayerIndex", tim.ToString());
                    }

                    if (Custom.rainWorld.processManager.upcomingProcess == ProcessManager.ProcessID.Game) break;

                    bool isHost = SteamMatchmaking.GetLobbyOwner((CSteamID)callback.m_ulSteamIDLobby) == selfIdentity.GetSteamID();
                    if (BingoData.globalMenu != null && BingoHooks.bingoPage.TryGetValue(BingoData.globalMenu, out var page2) && page2.inLobby)
                    {
                        page2.UpdateLobbyHost(isHost);
                        page2.ResetPlayerLobby();
                    }
                    if (isHost)
                    {
                        string hostName = SteamFriends.GetPersonaName();
                        SteamMatchmaking.SetLobbyData((CSteamID)callback.m_ulSteamIDLobby, "isHost", hostName);
                        SteamMatchmaking.SetLobbyData((CSteamID)callback.m_ulSteamIDLobby, "hostID", selfIdentity.GetSteamID64().ToString()); ;
                        SteamMatchmaking.SetLobbyData((CSteamID)callback.m_ulSteamIDLobby, "hostMods", BingoData.globalSettings.hostMods ? ActiveModsToString() : "none");
                    }

                    break;
                case 0x0002:
                case 0x0004:
                case 0x0008:
                case 0x0010:
                    text = "left";
                    

                    if (Custom.rainWorld.processManager.upcomingProcess == ProcessManager.ProcessID.Game) break;
                    if (SteamMatchmaking.GetLobbyData(CurrentLobby, "startGame") != "") break;
                    if (BingoData.globalMenu != null && BingoHooks.bingoPage.TryGetValue(BingoData.globalMenu, out var page3) && page3.inLobby)
                    {
                        page3.ResetPlayerLobby();
                    }

                    break;
            }
            
        }

        public static void OnLobbyDataUpdate(LobbyDataUpdate_t callback)
        {
            if (callback.m_bSuccess == 0 || CurrentLobby == (CSteamID)0 || callback.m_ulSteamIDLobby == 0) return;
            if (callback.m_ulSteamIDLobby == callback.m_ulSteamIDMember)
            {
                if (selfIdentity.GetSteamID() == SteamMatchmaking.GetLobbyOwner((CSteamID)callback.m_ulSteamIDLobby)) return;
                string den = SteamMatchmaking.GetLobbyData(CurrentLobby, "startGame");
                if (den != "")
                {
                    if (Custom.rainWorld.processManager.upcomingProcess == ProcessManager.ProcessID.Game || Custom.rainWorld.processManager.currentMainLoop.ID == ProcessManager.ProcessID.Game)
                    {
                        return;
                    }

                    if (BingoData.globalMenu != null && BingoHooks.bingoPage.TryGetValue(BingoData.globalMenu, out var page))
                    {
                        
                        BingoData.BingoDen = den;
                        page.Singal(null, "STARTBINGO");
                    }
                    return;
                }

                string challenjes = SteamMatchmaking.GetLobbyData(CurrentLobby, "challenges");
                try
                {
                    BingoHooks.GlobalBoard.FromString(challenjes);
                }
                catch (Exception e)
                {
                    Plugin.logger.LogError(e + "\nFAILED TO RECREATE BINGO BOARD FROM STRING FROM LOBBY: " + challenjes);
                    LeaveLobby();
                    return;
                }

                FetchLobbySettings();
            }
            else
            {
                bool isHost = SteamMatchmaking.GetLobbyOwner((CSteamID)callback.m_ulSteamIDLobby) == selfIdentity.GetSteamID();
                if (BingoData.globalMenu != null && BingoHooks.bingoPage.TryGetValue(BingoData.globalMenu, out var page2) && page2.inLobby)
                {
                    page2.UpdateLobbyHost(isHost); 
                    page2.ResetPlayerLobby();
                }
                if (isHost)
                {
                    string hostName = SteamFriends.GetPersonaName();
                    SteamMatchmaking.SetLobbyData((CSteamID)callback.m_ulSteamIDLobby, "isHost", hostName);
                    SteamMatchmaking.SetLobbyData((CSteamID)callback.m_ulSteamIDLobby, "hostID", selfIdentity.GetSteamID64().ToString()); ;
                    SteamMatchmaking.SetLobbyData((CSteamID)callback.m_ulSteamIDLobby, "hostMods", BingoData.globalSettings.hostMods ? ActiveModsToString() : "none");
                }
            }
        }

        public static void OnLobbyMatchList(LobbyMatchList_t result, bool bIOFailure)
        {
            if (bIOFailure) { Plugin.logger.LogError("OnLobbyMatchList bIOfailure"); return; }
            if (result.m_nLobbiesMatching < 1)
            {
                
                return;
            }
            
            List<CSteamID> frens = [];
            int frenCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
            if (frenCount != -1)
            {
                for (int f = 0; f < frenCount; f++)
                {
                    frens.Add(SteamFriends.GetFriendByIndex(f, EFriendFlags.k_EFriendFlagImmediate));
                }
            } 

            List<CSteamID> JoinableLobbies = new();
            for (int i = 0; i < result.m_nLobbiesMatching; i++)
            {
                var lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
                if (SteamMatchmaking.GetLobbyData(lobbyID, "mode") != "BingoMode") continue;
                if (SteamMatchmaking.GetLobbyData(lobbyID, "friendsOnly") == "1" || CurrentFilters.friendsOnly)
                {
                    CSteamID owner = (CSteamID)ulong.Parse(SteamMatchmaking.GetLobbyData(lobbyID, "hostID"), NumberStyles.Any);
                    //
                    if (frens.Count > 0 && !frens.Contains(owner))
                    {
                        continue;
                    }
                }
                if (ExpeditionGame.playableCharacters.IndexOf(new(SteamMatchmaking.GetLobbyData(lobbyID, "slugcat"))) == -1) continue;
                JoinableLobbies.Add(lobbyID);
                //
                //var call = SteamMatchmaking.JoinLobby(lobbyID);
                //lobbyEntered.Set(call, OnLobbyEntered);
            }
            if (JoinableLobbies.Count > 0 && BingoData.globalMenu != null && BingoHooks.bingoPage.TryGetValue(BingoData.globalMenu, out var page))
            {
                page.AddLobbies(JoinableLobbies);
            }
        }

        public static void UpdateOnlineBingo()
        {
            if (CurrentLobby == default) return;

            try
            {
                string asfgas = BingoHooks.GlobalBoard.ToString();
                
                SteamMatchmaking.SetLobbyData(CurrentLobby, "challenges", asfgas);
            }
            catch (Exception e)
            {
                Plugin.logger.LogError(e + "\nFAILED TO UPDATE ONLINE BINGO BOARD");
            }
            
        }

        public static void UpdateOnlineSettings()
        {
            SteamMatchmaking.SetLobbyData(CurrentLobby, "gamemode", ((int)BingoData.globalSettings.gamemode).ToString());
            SteamMatchmaking.SetLobbyData(CurrentLobby, "friendsOnly", BingoData.globalSettings.friendsOnly ? "1" : "0");
            SteamMatchmaking.SetLobbyData(CurrentLobby, "perks", ((int)BingoData.globalSettings.perks).ToString());
            SteamMatchmaking.SetLobbyData(CurrentLobby, "burdens", ((int)BingoData.globalSettings.burdens).ToString());
        }

        public static void SetMemberTeam(CSteamID persone, int newTeam)
        {
            SteamNetworkingIdentity receiver = new SteamNetworkingIdentity();
            receiver.SetSteamID(persone);
            InnerWorkings.SendMessage("%" + newTeam, receiver);
        }
    }
}
