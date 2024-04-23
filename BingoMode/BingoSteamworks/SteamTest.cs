﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Expedition;
using Steamworks;
using UnityEngine;

namespace BingoMode.BingoSteamworks
{
    internal class SteamTest
    {
        public static List<SteamNetworkingIdentity> LobbyMembers = new ();
        public static List<SteamNetworkingIdentity> TeamMembers = new ();
        public static int team = -1;
        public static CSteamID CurrentLobby;
        //public static List<CSteamID> JoinableLobbies = new ();
        public static LobbyFilters CurrentFilters;

        public static SteamNetworkingIdentity selfIdentity;

        protected static Callback<SteamNetworkingMessagesSessionRequest_t> sessionRequest;
        protected static Callback<LobbyChatUpdate_t> lobbyChatUpdate;
        protected static Callback<LobbyDataUpdate_t> lobbyDataUpdate;

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
                SteamNetworkingSockets.GetIdentity(out selfIdentity);
                Plugin.logger.LogMessage(selfIdentity.GetSteamID());
                //CSteamID nacu = (CSteamID)76561198140779563;
                //SteamNetworkingIdentity nacku = new();
                //nacku.SetSteamID(nacu);
                //CSteamID ridg = (CSteamID)76561198357253101;
                //SteamNetworkingIdentity ridgg = new();
                //ridgg.SetSteamID(ridg);
                //
                //if (selfIdentity.GetSteamID() == nacu)
                //{
                //    Plugin.logger.LogMessage("Init send message from nacu!");
                //
                //    try
                //    {
                //        InnerWorkings.SendMessage("hi i am nacku and im yes", ridgg);
                //        InnerWorkings.SendMessage("another one!", ridgg);
                //    }
                //    catch (Exception e)
                //    {
                //        Plugin.logger.LogError("FAILED TO SEND MESSAGE AS NACU " + e);
                //    }
                //
                //    //CreateLobby();
                //}
                //else
                //{
                //    Plugin.logger.LogMessage("Init send message from ridg!");
                //    try
                //    {
                //        InnerWorkings.SendMessage("hi i am righ and im aweosme", nacku);
                //        InnerWorkings.SendMessage("another one!", nacku);
                //    }
                //    catch (Exception e)
                //    {
                //        Plugin.logger.LogError("FAILED TO SEND MESSAGE AS RIDG " + e);
                //    }
                //
                //    //LookAndJoinFirstLobby();
                //}

                //SteamNetworkingUtils.SteamNetworkingIdentity_ParseString(out var idelti, bluh);
            }

            BingoData.globalSettings.lockout = false;
            BingoData.globalSettings.gameMode = false;
            BingoData.globalSettings.perks = LobbySettings.AllowUnlocks.None;
            BingoData.globalSettings.burdens = LobbySettings.AllowUnlocks.Any;
        }

        public static void CreateLobby()
        {
            //JoinableLobbies.Clear();
            LobbyMembers.Clear();
            SteamAPICall_t call = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 4);
            lobbyCreated.Set(call, OnLobbyCreated);
            BingoData.MultiplayerGame = true;
        }

        public static void GetJoinableLobbies()
        {
            //JoinableLobbies.Clear();
            LobbyMembers.Clear();
            Plugin.logger.LogMessage("Getting lobby list! Friends only? " + CurrentFilters.friendsOnly);
            if (CurrentFilters.friendsOnly)
            {
                List<CSteamID> JoinableLobbies = [];

                int friends = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
                for (int i = 0; i < friends; i++)
                {
                    FriendGameInfo_t info;
                    CSteamID friendID = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                    if (SteamFriends.GetFriendGamePlayed(friendID, out info) && info.m_steamIDLobby.IsValid() && info.m_gameID == (CGameID)312520)
                    {
                        JoinableLobbies.Add(info.m_steamIDLobby);
                        Plugin.logger.LogMessage("Added friend loby");
                    }
                }

                if (BingoData.globalMenu != null && BingoHooks.bingoPage.TryGetValue(BingoData.globalMenu, out var page))
                {
                    page.AddLobbies(JoinableLobbies);
                }
                else
                {
                    Plugin.logger.LogError("FAILED TO ADD FRIENDS' LOBBIES");
                }
                return;
            }

            SteamMatchmaking.AddRequestLobbyListDistanceFilter((ELobbyDistanceFilter)CurrentFilters.distance);
            if (CurrentFilters.text != "") SteamMatchmaking.AddRequestLobbyListStringFilter("host", CurrentFilters.text, ELobbyComparison.k_ELobbyComparisonEqualToOrGreaterThan);
            SteamAPICall_t call = SteamMatchmaking.RequestLobbyList();
            lobbyMatchList.Set(call, OnLobbyMatchList);
        }

        public static void LeaveLobby()
        {
            SteamMatchmaking.LeaveLobby(CurrentLobby);
            Plugin.logger.LogMessage("Left lobby " + CurrentLobby);
            LobbyMembers.Clear();
            CurrentLobby = default;
            BingoData.MultiplayerGame = false;
        }

        public static void OnSessionRequested(SteamNetworkingMessagesSessionRequest_t callback)
        {
            SteamNetworkingMessages.AcceptSessionWithUser(ref callback.m_identityRemote);
            Plugin.logger.LogMessage("Accepted session with " + callback.m_identityRemote.GetSteamID64());
        }

        public static void OnLobbyCreated(LobbyCreated_t result, bool bIOFailure)
        {
            if (bIOFailure) { Plugin.logger.LogError("OnLobbyCreated bIOfailure"); return; }
            if (result.m_eResult != EResult.k_EResultOK)
            {
                Plugin.logger.LogError("Failed to create the lobby!!");
                return;
            }

            Plugin.logger.LogMessage("Resetting activated perks and burdens");
            if (BingoData.globalSettings.perks != LobbySettings.AllowUnlocks.Any) ExpeditionGame.activeUnlocks.RemoveAll(x => x.StartsWith("unl-"));
            if (BingoData.globalSettings.burdens != LobbySettings.AllowUnlocks.Any) ExpeditionGame.activeUnlocks.RemoveAll(x => x.StartsWith("bur-"));
            team = 0;
            Plugin.logger.LogMessage("Set team number to " + team);

            Plugin.logger.LogMessage("Lobby created with ID " + result.m_ulSteamIDLobby + "! Setting lobby data");
            CSteamID lobbyID = (CSteamID)result.m_ulSteamIDLobby;
            string hostName = SteamFriends.GetPersonaName();
            SteamMatchmaking.SetLobbyData(lobbyID, "name", hostName + "'s lobby");
            SteamMatchmaking.SetLobbyData(lobbyID, "host", hostName);
            SteamMatchmaking.SetLobbyData(lobbyID, "slugcat", ExpeditionData.slugcatPlayer.value);
            SteamMatchmaking.SetLobbyData(lobbyID, "maxPlayers", BingoData.globalSettings.maxPlayers.ToString());
            SteamMatchmaking.SetLobbyData(lobbyID, "lockout", BingoData.globalSettings.lockout ? "1" : "0");
            SteamMatchmaking.SetLobbyData(lobbyID, "gameMode", BingoData.globalSettings.gameMode ? "1" : "0");
            SteamMatchmaking.SetLobbyData(lobbyID, "perks", ((int)BingoData.globalSettings.perks).ToString());
            SteamMatchmaking.SetLobbyData(lobbyID, "burdens", ((int)BingoData.globalSettings.burdens).ToString());
            SteamMatchmaking.SetLobbyData(lobbyID, "nextTeam", (team + 1).ToString());
            // other settings idk
            CurrentLobby = lobbyID;
            UpdateOnlineBingo();
            SteamMatchmaking.SetLobbyJoinable(lobbyID, true);
        }

        public static void OnLobbyEntered(LobbyEnter_t callback, bool bIOFailure)
        {
            if (bIOFailure) { Plugin.logger.LogError("OnLobbyEntered bIOfailure"); return; }
            if (callback.m_EChatRoomEnterResponse != 1)
            {
                Plugin.logger.LogError("Failed to enter lobby " + callback.m_ulSteamIDLobby + "! " + callback.m_EChatRoomEnterResponse);
                return;
            }
            CSteamID lobbyID = (CSteamID)callback.m_ulSteamIDLobby;
            CurrentLobby = lobbyID;
            Plugin.logger.LogMessage("Entered lobby " + callback.m_ulSteamIDLobby + "! ");
            Plugin.logger.LogMessage($"Name : {SteamMatchmaking.GetLobbyData(lobbyID, "name")}\nHost : {SteamMatchmaking.GetLobbyData(lobbyID, "host")}\nTest : {SteamMatchmaking.GetLobbyData(lobbyID, "testdata")}");
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

            Plugin.logger.LogMessage("Setting lobby settings");
            BingoData.globalSettings.lockout = SteamMatchmaking.GetLobbyData(lobbyID, "lockout") == "1";
            BingoData.globalSettings.gameMode = SteamMatchmaking.GetLobbyData(lobbyID, "gameMode") == "1";
            if (!int.TryParse(SteamMatchmaking.GetLobbyData(lobbyID, "perks"), out int perjs) || int.TryParse(SteamMatchmaking.GetLobbyData(lobbyID, "burdens"), out int burjens))
            {
                Plugin.logger.LogError("FAILED TO PARSE PERKS AND OR BURDEN SETTINGS FROM LOBBY");
                LeaveLobby();
                return;
            }
            BingoData.globalSettings.perks = (LobbySettings.AllowUnlocks)perjs;
            BingoData.globalSettings.burdens = (LobbySettings.AllowUnlocks)burjens;
            Plugin.logger.LogMessage("Success!");

            Plugin.logger.LogMessage("Resetting activated perks and burdens");
            if (BingoData.globalSettings.perks != LobbySettings.AllowUnlocks.Any) ExpeditionGame.activeUnlocks.RemoveAll(x => x.StartsWith("unl-"));
            if (BingoData.globalSettings.burdens != LobbySettings.AllowUnlocks.Any) ExpeditionGame.activeUnlocks.RemoveAll(x => x.StartsWith("bur-"));
            if (!int.TryParse(SteamMatchmaking.GetLobbyData(lobbyID, "nextTeam"), out team))
            {
                Plugin.logger.LogError("FAILED TO PARSE NEXT TEAM FROM LOBBY");
                LeaveLobby();
                return;
            }
            Plugin.logger.LogMessage("Set team number to " + team); 
            if (!int.TryParse(SteamMatchmaking.GetLobbyData(lobbyID, "maxPlayers"), out int maxPayne))
            {
                Plugin.logger.LogError("FAILED TO PARSE MAX PAYNE HIT VIDEO GAME FROM LOBBY");
                LeaveLobby();
                return;
            }
            BingoData.globalSettings.maxPlayers = maxPayne;
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
            SteamNetworkingIdentity ownere = new SteamNetworkingIdentity();
            ownere.SetSteamID(SteamMatchmaking.GetLobbyOwner(lobbyID));
            LobbyMembers.Add(ownere);
            int members = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
            BingoData.MultiplayerGame = true;
            for (int i = 0; i < members; i++)
            {
                SteamNetworkingIdentity member = new SteamNetworkingIdentity();
                member.SetSteamID(SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i));
                InnerWorkings.SendMessage($"Hello im {SteamFriends.GetPersonaName()} and i joined loby!", member);
            }
        }

        public static void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
        {
            string text = "";
            switch (callback.m_rgfChatMemberStateChange)
            {
                case 0x0001:
                    SteamNetworkingIdentity newMember = new SteamNetworkingIdentity();
                    newMember.SetSteamID((CSteamID)callback.m_ulSteamIDUserChanged);
                    LobbyMembers.Add(newMember);
                    if (SteamMatchmaking.GetLobbyOwner(CurrentLobby) == selfIdentity.GetSteamID())
                    {
                        int tim = team + 1;
                        if (tim >= 3) tim = 0;
                        SteamMatchmaking.SetLobbyData(CurrentLobby, "nextTeam", tim.ToString());
                    }
                    text = "entered";

                    break;
                case 0x0002:
                    LobbyMembers.RemoveAll(x => x.GetSteamID64() == callback.m_ulSteamIDUserChanged);
                    text = "left";
                    break;
                case 0x0004:
                    LobbyMembers.RemoveAll(x => x.GetSteamID64() == callback.m_ulSteamIDUserChanged);
                    text = "disconnected from";
                    break;
                case 0x0008:
                    LobbyMembers.RemoveAll(x => x.GetSteamID64() == callback.m_ulSteamIDUserChanged);
                    text = "kicked from";
                    break;
                case 0x0010:
                    LobbyMembers.RemoveAll(x => x.GetSteamID64() == callback.m_ulSteamIDUserChanged);
                    text = "banned from";
                    break;
            }
            Plugin.logger.LogMessage($"User {callback.m_ulSteamIDUserChanged} {text} {callback.m_ulSteamIDLobby}");

            Plugin.logger.LogMessage("Current lobby members");
            foreach (var member in LobbyMembers)
            {
                Plugin.logger.LogMessage(member.GetSteamID64());
            }
        }

        public static void OnLobbyDataUpdate(LobbyDataUpdate_t callback)
        {
            if (callback.m_bSuccess == 0 || callback.m_ulSteamIDLobby != callback.m_ulSteamIDMember || selfIdentity.GetSteamID() == SteamMatchmaking.GetLobbyOwner((CSteamID)callback.m_ulSteamIDLobby)) return;

            string challenjes = SteamMatchmaking.GetLobbyData(CurrentLobby, "challenges");
            try
            {
                BingoHooks.GlobalBoard.FromString(challenjes);
            }
            catch (Exception e)
            {
                Plugin.logger.LogError(e + "\nFAILED TO RECREATE BINGO BOARD FROM STRING FROM LOBBY: " + challenjes);
                LeaveLobby();
            }
        }

        public static void OnLobbyMatchList(LobbyMatchList_t result, bool bIOFailure)
        {
            if (bIOFailure) { Plugin.logger.LogError("OnLobbyMatchList bIOfailure"); return; }
            if (result.m_nLobbiesMatching < 1)
            {
                Plugin.logger.LogError("FOUND ZERO LOBBIES!!!");
                return;
            }
            Plugin.logger.LogMessage("Found " + result.m_nLobbiesMatching + " lobbies.");
            List<CSteamID> JoinableLobbies = new();
            for (int i = 0; i < result.m_nLobbiesMatching; i++)
            {
                var lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
                JoinableLobbies.Add(lobbyID);
                //Plugin.logger.LogMessage("Found and joining lobby with ID " + lobbyID);
                //var call = SteamMatchmaking.JoinLobby(lobbyID);
                //lobbyEntered.Set(call, OnLobbyEntered);
            }
            if (JoinableLobbies.Count > 0)
            {
                Plugin.logger.LogMessage("All available lobbies:");
                foreach (var lob in JoinableLobbies)
                {
                    Plugin.logger.LogMessage(lob);
                }
            }

            if (BingoData.globalMenu != null && BingoHooks.bingoPage.TryGetValue(BingoData.globalMenu, out var page))
            {
                page.AddLobbies(JoinableLobbies);
            }
        }

        public static void UpdateOnlineBingo()
        {
            if (CurrentLobby == default) return;

            try
            {
                string asfgas = string.Join("bChG", ExpeditionData.challengeList);
                Plugin.logger.LogMessage("SETTING " + asfgas);
                SteamMatchmaking.SetLobbyData(CurrentLobby, "challenges", asfgas);
                //foreach (var id in LobbyMembers)
                //{
                //    InnerWorkings.SendMessage("*", id);
                //}
            }
            catch (Exception e)
            {
                Plugin.logger.LogError(e + "\nFAILED TO UPDATE ONLINE BINGO BOARD");
            }
        }

        public static void BroadcastCompletedChallenge(Challenge ch)
        {
            Plugin.logger.LogMessage("BROADCASTING CHALLENGE COMPLETED " + ch);
            int x = -1;
            int y = -1;
            for (int i = 0; i < BingoHooks.GlobalBoard.challengeGrid.GetLength(0); i++)
            {
                bool b = false;
                for (int j = 0; j < BingoHooks.GlobalBoard.challengeGrid.GetLength(1); j++)
                {
                    if (BingoHooks.GlobalBoard.challengeGrid[i, j] == ch)
                    {
                        x = i;
                        y = j;
                        b = true;
                        break;
                    }
                }
                if (b) break;
            }
            foreach (var id in LobbyMembers)
            {
                InnerWorkings.SendMessage($"#{x};{y}", id);
            }
            LeaveLobby();
        }

        public static void BroadcastStartGame()
        {
            if (LobbyMembers.Count > 0)
            {
                Plugin.logger.LogMessage("BROADCASTING GAME STARTING TO LOBBY " + CurrentLobby);
                foreach (var id in LobbyMembers)
                {
                    InnerWorkings.SendMessage("!", id);
                }
            }
        }
    }
}
