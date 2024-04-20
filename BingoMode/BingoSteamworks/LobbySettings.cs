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
    public class LobbySettings
    {
        public enum AllowUnlocks
        {
            Any,
            Inherited,
            None
        }

        public bool lockout;
        public bool gameMode;
        public bool friendsOnly;
        public AllowUnlocks perks;
        public AllowUnlocks burdens;
        public List<string> bannedMods; //Ids
        public int maxPlayers;

        public LobbySettings()
        {
            bannedMods = [];
        }

        public void Reset()
        {
            lockout = false;
            perks = AllowUnlocks.Any;
            burdens = AllowUnlocks.Any;
            bannedMods.Clear();
            maxPlayers = 0;
        }
    }
}
