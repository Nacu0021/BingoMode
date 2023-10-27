﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Expedition;
using Menu;
using Menu.Remix;
using UnityEngine;

namespace BingoMode
{
    using static BingoBoard;

    public class BingoPage : PositionedMenuObject
    {
        public ExpeditionMenu expMenu;
        public BingoBoard board;
        public List<BingoButton> challengeButtons;
        public int size;
        public FSprite pageTitle;
        public SymbolButton rightPage;
        public BigSimpleButton startGame;

        public BingoPage(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            expMenu = menu as ExpeditionMenu;
            board = BingoHooks.GlobalBoard;
            challengeButtons = new List<BingoButton>();
            size = board.size;
            pageTitle = new FSprite("bingotitle");
            pageTitle.SetAnchor(0.5f, 0f);
            pageTitle.x = 680f;
            pageTitle.y = 680f;
            pageTitle.shader = menu.manager.rainWorld.Shaders["MenuText"];
            Container.AddChild(pageTitle);

            rightPage = new SymbolButton(menu, this, "Big_Menu_Arrow", "GOBACK", new Vector2(783f, 685f));
            rightPage.symbolSprite.rotation = 90f;
            rightPage.size = new Vector2(45f, 45f);
            rightPage.roundedRect.size = rightPage.size;
            subObjects.Add(rightPage);

            startGame = new BigSimpleButton(menu, this, "BEGIN", "STARTBINGO",
                new Vector2(menu.manager.rainWorld.screenSize.x * 0.75f, 40f),
                new Vector2(200f, 40f), FLabelAlignment.Center, true);
            subObjects.Add(startGame);

            GenerateBoardButtons();
        }

        public void GenerateBoardButtons()
        {
            for (int i = 0; i < board.challengeGrid.GetLength(0); i++)
            {
                for (int j = 0; j < board.challengeGrid.GetLength(1); j++)
                {
                    float butSize = 500f / size;
                    float topLeft = -butSize * size / 2f;
                    Vector2 center = new (menu.manager.rainWorld.screenSize.x / 2f - butSize / 2f, menu.manager.rainWorld.screenSize.y / 2f - butSize / 2f);
                    BingoButton but = new BingoButton(menu, this, 
                        center + new Vector2(topLeft + i * butSize + butSize / 2f, -topLeft - j * butSize - butSize / 2f - 50f), new Vector2(butSize, butSize), board.challengeGrid[i, j], "TEST");
                    challengeButtons.Add(but);
                    subObjects.Add(but);
                    //Plugin.logger.LogMessage("Added new bimbo " + but + " at " + but.pos);
                }
            }
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);

            if (message == "GOBACK")
            {
                expMenu.UpdatePage(1);
                expMenu.MovePage(new Vector2(-1500f, 0f));
            }

            if (message == "STARTBINGO")
            {
                if (ModManager.JollyCoop && ModManager.CoopAvailable)
                {
                    for (int i = 1; i < menu.manager.rainWorld.options.JollyPlayerCount; i++)
                    {
                        menu.manager.rainWorld.RequestPlayerSignIn(i, null);
                    }
                    for (int j = menu.manager.rainWorld.options.JollyPlayerCount; j < 4; j++)
                    {
                        menu.manager.rainWorld.DeactivatePlayer(j);
                    }
                }

                menu.manager.arenaSitting = null;
                menu.manager.rainWorld.progression.currentSaveState = null;
                menu.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat = ExpeditionData.slugcatPlayer;
                menu.manager.rainWorld.progression.WipeSaveState(ExpeditionData.slugcatPlayer);

                ExpeditionData.startingDen = ExpeditionGame.ExpeditionRandomStarts(menu.manager.rainWorld, ExpeditionData.slugcatPlayer);

                BingoData.InitializeBingo();
                ExpeditionGame.PrepareExpedition();
                ExpeditionData.AddExpeditionRequirements(ExpeditionData.slugcatPlayer, false);
                ExpeditionData.challengeList.Clear();
                Expedition.Expedition.coreFile.Save(false);
                menu.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;
                menu.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
                menu.PlaySound(SoundID.MENU_Start_New_Game);
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            //pageTitle.SetPosition(Vector2.Lerp(owner.page.lastPos, owner.page.pos, timeStacker) + new Vector2(680f, 680f));

            pageTitle.x = Mathf.Lerp(owner.page.lastPos.x, owner.page.pos.x, timeStacker) + 680f;
            pageTitle.y = Mathf.Lerp(owner.page.lastPos.y, owner.page.pos.y, timeStacker) + 680f;
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            pageTitle.RemoveFromContainer();
        }
    }
}
