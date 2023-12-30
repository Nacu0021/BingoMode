﻿namespace BingoMode
{
    public class BingoEnums
    {
        public static ProcessManager.ProcessID BingoWinScreen;
        public static ProcessManager.ProcessID BingoLoseScreen;
        //public static SLOracleBehaviorHasMark.MiscItemType GreenNeuron;

        public static void Register()
        {
            BingoWinScreen = new("BingoWinScreen", true);
            BingoLoseScreen = new("BingoLoseScreen", true);
            //GreenNeuron = new("GreenNeuron", true);
        }
    }
}
