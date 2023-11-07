﻿using BepInEx;
using System.Security.Permissions;
using System.Security;
using Steamworks;
using System.Text;

#pragma warning disable CS0618
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace BingoMode
{
    using BingoSteamworks;
    using System;
    using System.Runtime.InteropServices;

    [BepInPlugin("nacu.bingomode", "Expedition Bingo", "0.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static bool AppliedAlreadyDontDoItAgainPlease;
        internal static BepInEx.Logging.ManualLogSource logger;

        public void OnEnable()
        {
            logger = Logger;
            On.RainWorld.OnModsInit += OnModsInit;
            On.RainWorld.Update += RainWorld_Update;
        }

        public void RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
        {
            orig.Invoke(self);

            // Receiving data from yuh
            //uint size;
            //while (SteamNetworking.IsP2PPacketAvailable(out size))
            //{
            //    Plugin.logger.LogMessage("PACKET AVAILABLE");
            //    byte[] data = new byte[size];
            //
            //    if (SteamNetworking.ReadP2PPacket(data, size, out uint bytesRead, out CSteamID remoteID))
            //    {
            //        // Converting the byte array to a string
            //        char[] chars = new char[bytesRead / sizeof(char)];
            //        Buffer.BlockCopy(data, 0, chars, 0, data.Length);
            //        string message = new string(chars, 0, chars.Length);
            //        Plugin.logger.LogMessage("RECEIVED MESSAGE FROM " + remoteID + " READING " + message);
            //        InnerWorkings.MessageReceived(message);
            //    }
            //}

            // How the fuck does this work
            IntPtr[] messges = new IntPtr[16];
            int messages = SteamNetworkingMessages.ReceiveMessagesOnChannel(0, messges, messges.Length);
            if (messages > 0)
            {
                for (int i = 0; i < messages; i++)
                {
                    SteamNetworkingMessage_t netMessage = Marshal.PtrToStructure<SteamNetworkingMessage_t>(messges[i]);

                    Plugin.logger.LogMessage("RECEIVED MESSAG???");

                    byte[] data = new byte[netMessage.m_cbSize];
                    Marshal.Copy(netMessage.m_pData, data, 0, data.Length);
                    char[] chars = new char[data.Length / sizeof(char)];
                    Buffer.BlockCopy(data, 0, chars, 0, data.Length);
                    string message = new string(chars, 0, chars.Length);
                    Plugin.logger.LogMessage(message);
                    InnerWorkings.MessageReceived(message);
                }
            }
        }

        public void OnDisable()
        {
            logger = null;
        }

        public static void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld raingame)
        {
            orig(raingame);

            if (!AppliedAlreadyDontDoItAgainPlease)
            {
                AppliedAlreadyDontDoItAgainPlease = true;

                SteamTest.Apply();

                Futile.atlasManager.LoadAtlas("Atlases/bingomode");
                BingoEnums.Register();
                
                BingoHooks.Apply();
            }
        }
    }
}