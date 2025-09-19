using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using SampSharpGameMode1.Civilisation;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable IDE0051 // Disable useless private members

namespace SampSharpGameMode1.Commands
{
    public class NpcTestCommands
    {
        private enum NextClipMapAction
        {
            SetPosition,
            MoveTo
        }
        private static int lastSelectedNpcId = -1;
        private static NextClipMapAction nextClipMapAction;


        readonly static Dictionary<string, Action<Player, Npc>> CoreList = new()
        {
            { "Print infos ...", Core_PrintInfos },
            { "Destroy", Core_Destroy },
            { "Spawn", Core_Spawn },
            { "Respawn", Core_Respawn },
        };
        readonly static Dictionary<string, Action<Player, Npc>> PosAndMovementList = new()
        {
            { "Print infos ...", PosAndMove_PrintInfos },
            { "Set position (map marker)" , PosAndMove_SetPosToMapMarker},
            { "Set position (next to player)", PosAndMove_SetPosNextToPlayer },
            { "Set rotation (need to dev a Vector3 parser)", PosAndMove_SetRot },
            { "Set facing angle", PosAndMove_SetFacingAngle },
            { "Set virtual world", PosAndMove_SetVirtualWord },
            { "Move to map marker", PosAndMove_MoveToMapMarker },
            { "Move to player", PosAndMove_MoveToPlayer },
            { "Stop move", PosAndMove_StopMoving }
        };
        readonly static Dictionary<string, Action<Player, Npc>> AppearanceList = new()
        {
            { "Print infos ...", Appearance_PrintInfos },
            { "Set skin", Appearance_SetSkin },
            { "Set interior", Appearance_SetInterior },
        };
        readonly static Dictionary<string, Action<Player, Npc>> HealthAndCombatList = new()
        {
            { "Print infos ...", HealthAndCombat_PrintInfos },
            { "Set health", HealthAndCombat_SetHealth },
            { "Set armour", HealthAndCombat_SetArmour },
            { "Toggle invulnerable", HealthAndCombat_ToggleInvulnerable },
        };
        readonly static Dictionary<string, Action<Player, Npc>> WeaponAndCombatList = new()
        {
            { "Print infos ...", WeaponAndCombat_PrintInfos },
            { "Set weapon", WeaponAndCombat_SetWeapon },
            { "Set ammo", WeaponAndCombat_SetAmmo },
            { "Set ammo in clip", WeaponAndCombat_SetAmmoInClip },
            { "Set keys [TODO do not use]", null },
            { "Melee attack", WeaponAndCombat_MeleeAttack },
            { "Stop Melee attack", WeaponAndCombat_StopMeleeAttack },
            { "Set fighting style", WeaponAndCombat_SetFightingStyle },
            { "Toggle reloading", WeaponAndCombat_ToggleReloading },
            { "Toggle infinite ammo", WeaponAndCombat_ToggleInfiniteAmmo },
            { "Shoot", WeaponAndCombat_Shoot },
            { "Aim at object", WeaponAndCombat_AimAt },
            { "Aim at player", WeaponAndCombat_AimAtPlayer },
            { "Stop aim", WeaponAndCombat_StopAim },
            { "Set weapon accuracy", WeaponAndCombat_SetWeaponAccuracy },
            { "Get weaon accuracy", WeaponAndCombat_GetWeaponAccuracy },
        };
        readonly static Dictionary<string, Action<Player, Npc>> VehicleList = new()
        {
            { "Print infos ...", Vehicle_PrintInfos },
            { "Enter vehicle", Vehicle_EnterVehicle },
            { "Exit vehicle", Vehicle_ExitVehicle},
            { "Put in vehicle", Vehicle_PutInVehicle},
            { "Remove from vehicle", Vehicle_RemoveFromVehicle },
            { "Use vehicle siren", Vehicle_UseSiren},
            { "Set vehicle health", Vehicle_SetHealth },
            { "Set vehicle hydra thrusters", Vehicle_SetHydraThrusters },
            { "Toggle vehicle gear state", Vehicle_ToggleGearState },
            { "Set train speed", Vehicle_SetTrainSpeed },
            { "Honk", Vehicle_Honk },
        };
        readonly static Dictionary<string, Action<Player, Npc>> AnimationList = new()
        {
            { "Print infos ...", Animation_PrintInfos },
            { "Reset animation", Animation_ResetAnimation },
            { "Set animation", Animation_SetAnimation},
            { "Apply animation", Animation_ApplyAnimation},
            { "Clear animations", Animation_ClearAnimations},
            { "SetSpecialAction", Animation_SetSpecialAction},
        };
        readonly static Dictionary<string, Action<Player, Npc>> PathList = new()
        {
            { "Print infos ...", Paths_PrintInfos },
            { "Start HUD", Paths_StartHUD },
            { "Stop HUD", Paths_StopHUD },
            { "Move to current path", Paths_MoveToPath },
        };

        readonly static Dictionary<string, Dictionary<string, Action<Player, Npc>>> methodDictionaries = new()
        {
            { "Core", CoreList },
            { "Position and movement", PosAndMovementList },
            { "Appearance", AppearanceList },
            { "Health & Combat", HealthAndCombatList },
            { "Weapon & Combat", WeaponAndCombatList },
            { "Vehicle", VehicleList },
            { "Animation", AnimationList },
            { "Path", PathList },
        };

        public class NpcTestHUD : HUD
        {
            Npc.Path path = null;
            List<DynamicCheckpoint> checkpoints = new();
            public NpcTestHUD(Player player) : base(player, "npcpathtest.json")
            {
                layers["base"].SetClickable("label_create");
                layers["base"].SetClickable("label_destroy");
                layers["base"].SetClickable("label_addpoint");
                layers["base"].SetClickable("label_removepoint");
                layers["base"].SetClickable("label_clear");
                layers["base"].TextdrawClicked += NpcTestHUD_TextdrawClicked;
                layers["base"].AutoUpdate = true;
                if (Npc.Path.All.Any())
                    path = Npc.Path.All.First();
            }

            public void SetInClickableMode()
            {
                player.CancelSelectTextDraw();
                player.SelectTextDraw(ColorPalette.Primary.Lighten.GetColor());
            }

            private void NpcTestHUD_TextdrawClicked(object sender, TextdrawLayer.TextdrawEventArgs e)
            {
                switch(e.TextdrawName)
                {
                    case "label_create":
                        {
                            if (!Npc.Path.All.Any())
                            {
                                path = Npc.Path.Create(1);
                                UpdateStatus();
                                player.SendClientMessage("Path created");
                            }
                            else
                                player.SendClientMessage(Color.Red, "There is already a Path, only one is supported at once");
                            break;
                        }
                    case "label_destroy":
                        {
                            if (Npc.Path.All.Any())
                            {
                                Npc.Path.DestroyAll();
                                checkpoints.Clear();
                                player.SendClientMessage("All Path destroyed");
                            }
                            else
                                player.SendClientMessage(Color.Red, "There is no Path");
                            break;
                        }
                    case "label_addpoint":
                        {
                            if (Npc.Path.All.Any())
                            {
                                Npc.Path.Find(1).AddPoint(player.Position, 5.0f);
                                checkpoints.Add(new DynamicCheckpoint(player.Position, 5.0f, player: player));
                                UpdateStatus();
                                player.SendClientMessage("Path point added");
                            }
                            else
                                player.SendClientMessage(Color.Red, "There is no Path");
                            break;
                        }
                    case "label_removepoint":
                        {
                            if (Npc.Path.All.Any())
                            {
                                // TODO afficher tous les points dans des TD dupliqués ?
                                //Npc.Path.Find(1).RemovePoint()
                                //checkpoints.Add(new DynamicCheckpoint(player.Position, 5.0f, player: player));
                                UpdateStatus();
                                player.SendClientMessage("todo");
                            }
                            else
                                player.SendClientMessage(Color.Red, "There is no Path");
                            break;
                        }
                    case "label_clear":
                        {
                            if (Npc.Path.All.Any())
                            {
                                Npc.Path.Find(1).Clear();
                                checkpoints.Clear();
                                UpdateStatus();
                                player.SendClientMessage("Path point all cleared");
                            }
                            else
                                player.SendClientMessage(Color.Red, "There is no Path");
                            break;
                        }
                }
            }

            public void UpdateStatus()
            {
                layers["base"].SetTextdrawText("pointcount", "Points: " + path.PointCount);
                layers["base"].SetTextdrawText("isvalidpath", "Is valid = " + path.IsValid.ToString());
                layers["base"].SetTextdrawText("pointindex", "Current point: " + path.PointIndex);
            }
        }


        [Command("npc-create")]
        private static void NpcCreateCommand(Player player, string npcname)
        {
            if (Npc.Create(npcname))
            {
                Task.Run(() => WaitNpcToConnectAsync(player, npcname));
            }
            else
                player.SendClientMessage(Color.Red, "Unable to create NPC");
        }
        private static async Task WaitNpcToConnectAsync(Player player, string name)
        { // Le thread n'as pas accès à Npc.All ? Le Npc est bien créé avec l'id 49 mais avec aucune donnée, et pas de nom
          // crash si on tente d'accéder à Npc.All avec debugger
            await Task.Run(() =>
            {
                const int maxTries = 3;
                int tries = 0;
                Npc npc = null;
                while (tries++ < maxTries && npc == null)
                {
                    Thread.Sleep(500);
                    if(Npc.All.Count() > 1)
                    {
                        npc = Npc.All.First(n => n.Name == name);
                    }
                }
                if (npc != null)
                {

                    npc.Spawned += (sender, e) =>
                    {
                        player.SendClientMessage("the NPC spawned !");
                    };
                    npc.FinishedMove += (sender, e) =>
                    {
                        player.SendClientMessage("the NPC reached you !");
                    };
                    npc.Died += (sender, e) =>
                    {
                        player.SendClientMessage("the NPC died !");
                    };
                    npc.TakeDamage += (sender, e) =>
                    {
                        player.SendClientMessage("the NPC takes " + e.Amount + " damage in " + e.BodyPart.ToString());
                    };
                    Core_Spawn(player, npc);
                    PosAndMove_SetPosNextToPlayer(player, npc);
                }
                else
                    player.SendClientMessage(Color.Red, "Unable to get NPC connection");
            });
        }

        [Command("npc")]
        private static void NpcCommand(Player player)
        {
            if (!Npc.All.Any())
            {
                player.SendClientMessage(Color.Red, "Create a npc first with /npc-create");
                return;
            }
            ListDialog<Npc> selectNpcDialog = new("Select a NPC", "Select", "Cancel");
            selectNpcDialog.AddItems(Npc.All);
            selectNpcDialog.Response += (sender, selectNpcDialogResponse) =>
            {
                if (selectNpcDialogResponse.ItemValue != null)
                {
                    lastSelectedNpcId = selectNpcDialogResponse.ItemValue.Id;
                    ListDialog actionDialog = new("Select the action", "Select", "Cancel");
                    List<string> actionList = new()
                    {
                        "Search ...",
                    };
                    actionList.AddRange(methodDictionaries.Keys);
                    actionDialog.AddItems(actionList);
                    actionDialog.Response += (sender, actionDialogResponse) =>
                    {
                        if (actionDialogResponse.DialogButton == DialogButton.Left)
                        {
                            switch (actionDialogResponse.ListItem)
                            {
                                case 0: // Search
                                    {
                                        InputDialog searchDialog = new("Enter de text to search", "Searh item", false, "Search", "Cancel");
                                        searchDialog.Response += (sender, searchDialogResponse) =>
                                        {
                                            if (actionDialogResponse.DialogButton == DialogButton.Left)
                                            {
                                                TablistDialog searchDialogResult = new("Search results", 2, "Select", "Cancel");

                                                Dictionary<string, Action<Player, Npc>> resultList = new();
                                                foreach (KeyValuePair<string, Dictionary<string, Action<Player, Npc>>> methodDictionary in methodDictionaries)
                                                {
                                                    foreach (KeyValuePair<string, Action<Player, Npc>> str in methodDictionary.Value.Where(txt => txt.Key.Contains(searchDialogResponse.InputText)))
                                                    {
                                                        searchDialogResult.Add(new string[] { str.Key, methodDictionary.Key });
                                                        resultList.Add(str.Key, str.Value);
                                                    }
                                                }
                                                searchDialogResult.Response += (sender, searchDialogResultResponse) =>
                                                {
                                                    if (searchDialogResultResponse.DialogButton == DialogButton.Left)
                                                    {
                                                        resultList[resultList.Keys.ToList()[searchDialogResultResponse.ListItem]](player, selectNpcDialogResponse.ItemValue);
                                                    }
                                                };
                                                searchDialogResult.Show(player);
                                            }
                                        };
                                        searchDialog.Show(player);
                                        break;
                                    }
                                case 1: // Core
                                    {
                                        ShowDialogFromDictionary(player, selectNpcDialogResponse.ItemValue, "Core", CoreList);
                                        break;
                                    }
                                case 2: // Position and movement
                                    {
                                        ShowDialogFromDictionary(player, selectNpcDialogResponse.ItemValue, "Position and movement", PosAndMovementList);
                                        break;
                                    }
                                case 3: // Appearance
                                    {
                                        ShowDialogFromDictionary(player, selectNpcDialogResponse.ItemValue, "Appearance", AppearanceList);
                                        break;
                                    }
                                case 4: // Health & Combat
                                    {
                                        ShowDialogFromDictionary(player, selectNpcDialogResponse.ItemValue, "Health & Combat", HealthAndCombatList);
                                        break;
                                    }
                                case 5: // Weapon & Combat
                                    {
                                        ShowDialogFromDictionary(player, selectNpcDialogResponse.ItemValue, "Weapon & Combat", WeaponAndCombatList);
                                        break;
                                    }
                                case 6: // Vehicle
                                    {
                                        ShowDialogFromDictionary(player, selectNpcDialogResponse.ItemValue, "Vehicle", VehicleList);
                                        break;
                                    }
                                case 7: // Animation
                                    {
                                        ShowDialogFromDictionary(player, selectNpcDialogResponse.ItemValue, "Animation", AnimationList);
                                        break;
                                    }
                                case 8: // Path
                                    {
                                        ShowDialogFromDictionary(player, selectNpcDialogResponse.ItemValue, "Path", PathList);
                                        break;
                                    }
                            }
                        }
                    };
                    actionDialog.Show(player);
                }
            };
            selectNpcDialog.Show(player);
        }

        #region Core

        private static void Core_PrintInfos(Player player, Npc npc)
        {
            TablistDialog coreInfoDialog = new("Core infos", 2, "Select", "Cancel")
            {
                new string[] { "Is valid", npc.IsValid.ToString() },
                new string[] { "Is dead", (!npc.IsAlive).ToString() },
            };
            coreInfoDialog.Response += (sender, _) => ShowDialogFromDictionary(player, npc, "Core", CoreList);
            coreInfoDialog.Show(player);
        }
        private static void Core_Destroy(Player player, Npc npc)
        {
            npc.Destroy();
            player.SendClientMessage($"NPC {npc.Name} destroyed");
            ShowDialogFromDictionary(player, npc, "Core", CoreList);
        }
        private static void Core_Spawn(Player player, Npc npc)
        {
            npc.Spawn();
            player.SendClientMessage($"NPC {npc.Name} spawned");
            ShowDialogFromDictionary(player, npc, "Core", CoreList);
        }
        private static void Core_Respawn(Player player, Npc npc)
        {
            npc.Respawn();
            player.SendClientMessage($"NPC {npc.Name} respawned");
            ShowDialogFromDictionary(player, npc, "Core", CoreList);
        }
        #endregion
        #region Position and movements
        private static void PosAndMove_PrintInfos(Player player, Npc npc)
        {
            TablistDialog coreInfoDialog = new("Pos & movement infos", 2, "Select", "Cancel")
            {
                new string[] { "Position", npc.Position.ToString() },
                new string[] { "Rotation", npc.Rotation.ToString() },
                new string[] { "Facing angle", npc.Angle.ToString() },
                new string[] { "Virtual world", npc.VirtualWorld.ToString() },
                new string[] { "Is moving", npc.IsMoving.ToString() },
            };
            coreInfoDialog.Response += (sender, coreInfoDialogResponse) => ShowDialogFromDictionary(player, npc, "Position and movement", PosAndMovementList);
            coreInfoDialog.Show(player);
        }
        private static void PosAndMove_SetPosToMapMarker(Player player, Npc npc)
        {
            nextClipMapAction = NextClipMapAction.SetPosition;
            player.ClickMap += Player_ClickMap;
            player.SendClientMessage("Click on the map to set the position of the NPC");
        }
        private static void PosAndMove_SetPosNextToPlayer(Player player, Npc npc)
        {
            npc.Position = player.Position + new Vector3(0, 2, 0.1);
            player.SendClientMessage($"NPC {npc.Name} Position set to the player");
            ShowDialogFromDictionary(player, npc, "Position and movement", PosAndMovementList);
        }
        private static void PosAndMove_SetRot(Player player, Npc npc)
        {
            player.Notificate("TODO");
            ShowDialogFromDictionary(player, npc, "Position and movement", PosAndMovementList);
        }
        private static void PosAndMove_SetFacingAngle(Player player, Npc npc)
        {
            InputDialog angleDialog = new("Enter facing angle", "Facing Angle in degrees", false, "Set", "Cancel");
            angleDialog.Response += (sender, angleDialogResponse) =>
            {
                if (float.TryParse(angleDialogResponse.InputText, out float value))
                {
                    npc.Angle = value;
                    player.SendClientMessage($"NPC {npc.Name} facing angle set to " + value);
                }
                else
                    player.SendClientMessage(Color.Red, "Invalid to parse the given number");
                ShowDialogFromDictionary(player, npc, "Position and movement", PosAndMovementList);
            };
            angleDialog.Show(player);
        }
        private static void PosAndMove_SetVirtualWord(Player player, Npc npc)
        {
            InputDialog vwDialog = new("Enter virtual world", "Virtual World ID", false, "Set", "Cancel");
            vwDialog.Response += (sender, vwDialogResponse) =>
            {
                if (int.TryParse(vwDialogResponse.InputText, out int value))
                {
                    npc.VirtualWorld = value;
                    player.SendClientMessage($"NPC {npc.Name} virtual world set to " + value);
                }
                else
                    player.SendClientMessage(Color.Red, "Invalid to parse the given number");
                ShowDialogFromDictionary(player, npc, "Position and movement", PosAndMovementList);
            };
            vwDialog.Show(player);
        }
        private static void PosAndMove_MoveToMapMarker(Player player, Npc npc)
        {
            nextClipMapAction = NextClipMapAction.MoveTo;
            player.ClickMap += Player_ClickMap;
            player.SendClientMessage("Click on the map to set the position to move to");
        }
        private static void PosAndMove_MoveToPlayer(Player player, Npc npc)
        {
            npc.MoveToPlayer(player, npc.InAnyVehicle ? NPCMoveType.Drive : NPCMoveType.Jog);
            player.SendClientMessage($"NPC {npc.Name} is following you");
            ShowDialogFromDictionary(player, npc, "Position and movement", PosAndMovementList);
        }
        private static void PosAndMove_StopMoving(Player player, Npc npc)
        {
            npc.StopMove();
            player.SendClientMessage($"NPC {npc.Name} stop moving");
            ShowDialogFromDictionary(player, npc, "Position and movement", PosAndMovementList);
        }
        #endregion
        #region Appearance
        private static void Appearance_PrintInfos(Player player, Npc npc)
        {
            TablistDialog appearanceInfoDialog = new("Appearance infos", 2, "Select", "Cancel")
            {
                new string[] { "Is streamed in by the player", npc.IsStreamedIn(player).ToString() },
                new string[] { "Is streamed in by somebody", npc.IsAnyStreamedIn().ToString() },
                new string[] { "Interior id", npc.Interior.ToString() },
            };
            appearanceInfoDialog.Response += (sender, _) => ShowDialogFromDictionary(player, npc, "Appearance", AppearanceList);
            appearanceInfoDialog.Show(player);
        }
        private static void Appearance_SetSkin(Player player, Npc npc)
        {
            InputDialog skinDialog = new("Skin", "Enter the Skin ID", false, "Set", "Cancel");
            skinDialog.Response += (sender, skinDialogResponse) =>
            {
                if (int.TryParse(skinDialogResponse.InputText, out int skinId))
                {
                    if (skinId < 0 || skinId > 311)
                        player.SendClientMessage(Color.Red, "The skin ID is invalid");
                    else
                    {
                        npc.SetSkin(skinId);
                        player.SendClientMessage($"NPC {npc.Name} Set skin to " + skinId);
                    }
                }
                else
                    player.SendClientMessage(Color.Red, "Invalid to parse the given number");
                ShowDialogFromDictionary(player, npc, "Appearance", AppearanceList);
            };
            skinDialog.Show(player);
        }
        private static void Appearance_SetInterior(Player player, Npc npc)
        {
            InputDialog interiorDialog = new("interior", "Enter the Interior ID", false, "Set", "Cancel");
            interiorDialog.Response += (sender, interiorDialogResponse) =>
            {
                if (int.TryParse(interiorDialogResponse.InputText, out int interiorId))
                {
                    if (interiorId < 1)
                        player.SendClientMessage(Color.Red, "The interior ID is invalid");
                    else
                    {
                        npc.Interior = interiorId;
                        player.SendClientMessage($"NPC {npc.Name} Set interior to " + interiorId);
                    }
                }
                else
                    player.SendClientMessage(Color.Red, "Invalid to parse the given number");
                ShowDialogFromDictionary(player, npc, "Appearance", AppearanceList);
            };
            interiorDialog.Show(player);
        }
        #endregion
        #region Health & Combat
        private static void HealthAndCombat_PrintInfos(Player player, Npc npc)
        {
            TablistDialog healthAndCombatInfoDialog = new("Health And Combat infos", 2, "Select", "Cancel")
            {
                new string[] { "Health", npc.Health.ToString() },
                new string[] { "Armour", npc.Armour.ToString() },
                new string[] { "Invulnerable", npc.Invulnerable.ToString() },
            };
            healthAndCombatInfoDialog.Response += (sender, _) => ShowDialogFromDictionary(player, npc, "Health & Combat", HealthAndCombatList);
            healthAndCombatInfoDialog.Show(player);
        }
        private static void HealthAndCombat_SetHealth(Player player, Npc npc)
        {
            InputDialog healthDialog = new("health", "Enter the health value", false, "Set", "Cancel");
            healthDialog.Response += (sender, healthDialogResponse) =>
            {
                if (int.TryParse(healthDialogResponse.InputText, out int health))
                {
                    if (health < 0)
                        player.SendClientMessage(Color.Red, "The health must be greater than 0");
                    else
                    {
                        npc.Health = health;
                        player.SendClientMessage($"NPC {npc.Name} Set health to " + health);
                    }
                }
                else
                    player.SendClientMessage(Color.Red, "Invalid to parse the given number");
                ShowDialogFromDictionary(player, npc, "Health & Combat", HealthAndCombatList);
            };
            healthDialog.Show(player);
        }
        private static void HealthAndCombat_SetArmour(Player player, Npc npc)
        {
            InputDialog armourDialog = new("armour", "Enter the armour value", false, "Set", "Cancel");
            armourDialog.Response += (sender, armourDialogResponse) =>
            {
                if (int.TryParse(armourDialogResponse.InputText, out int armour))
                {
                    if (armour < 0)
                        player.SendClientMessage(Color.Red, "The armour must be greater than 0");
                    else
                    {
                        npc.Armour = armour;
                        player.SendClientMessage($"NPC {npc.Name} Set armour to " + armour);
                    }
                }
                else
                    player.SendClientMessage(Color.Red, "Invalid to parse the given number");
                ShowDialogFromDictionary(player, npc, "Health & Combat", HealthAndCombatList);
            };
            armourDialog.Show(player);
        }
        private static void HealthAndCombat_ToggleInvulnerable(Player player, Npc npc)
        {
            npc.Invulnerable = !npc.Invulnerable;
            player.SendClientMessage($"NPC {npc.Name} is now " + (npc.Invulnerable ? "invulnerable" : "vulnerable"));
            ShowDialogFromDictionary(player, npc, "Health & Combat", HealthAndCombatList);
        }

        #endregion
        #region Weapon & Combat
        private static void WeaponAndCombat_PrintInfos(Player player, Npc npc)
        {
            TablistDialog weaponAndCombatInfoDialog = new("Weapon And Combat infos", 2, "Select", "Cancel")
            {
                new string[] { "Weapon", npc.Weapon.ToString() },
                new string[] { "Ammo", npc.WeaponAmmo.ToString() },
                new string[] { "Ammo in clip", npc.WeaponAmmoInClip.ToString() },
                new string[] { "Melee attacking ?", npc.IsMeleeAttacking.ToString() },
                new string[] { "Fighting style", npc.FightStyle.ToString() },
                new string[] { "Reload enabled", npc.IsReloadEnabled.ToString() },
                new string[] { "Is reloading", npc.IsReloading.ToString() },
                new string[] { "Is infinite ammo enabled", npc.IsInfiniteAmmoEnabled.ToString() },
                new string[] { "Weapon state", npc.WeaponState.ToString() },
                new string[] { "Is shooting", npc.IsShooting.ToString() },
                new string[] { "Is Aiming", npc.IsAiming().ToString() },
                new string[] { "Is Aiming at player", npc.IsAimingAtPlayer(player).ToString() },
            };
            weaponAndCombatInfoDialog.Response += (sender, _) => ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
            weaponAndCombatInfoDialog.Show(player);
        }
        private static void WeaponAndCombat_SetWeapon(Player player, Npc npc)
        {
            Task.Run(() => WeaponAndCombat_SetWeaponAsync(player, npc));
        }
        private static async Task WeaponAndCombat_SetWeaponAsync(Player player, Npc npc)
        {
            Weapon? weapon = await Utils.ShowWeaponDialog(player,
                () => ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList));
            if (weapon != null)
            {
                npc.Weapon = weapon.Value;
                player.SendClientMessage($"NPC {npc.Name} has now {weapon}");
                ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
            }
        }
        private static void WeaponAndCombat_SetAmmo(Player player, Npc npc)
        {
            InputDialog ammoDialog = new("ammo", "Enter the ammo value", false, "Set", "Cancel");
            ammoDialog.Response += (sender, ammoDialogResponse) =>
            {
                if (int.TryParse(ammoDialogResponse.InputText, out int ammo))
                {
                    if (ammo < 0)
                        player.SendClientMessage(Color.Red, "The ammo must be greater than 0");
                    else
                    {
                        npc.WeaponAmmo = ammo;
                        player.SendClientMessage($"NPC {npc.Name} Set ammo to " + ammo);
                    }
                }
                else
                    player.SendClientMessage(Color.Red, "Invalid to parse the given number");
                ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
            };
            ammoDialog.Show(player);
        }
        private static void WeaponAndCombat_SetAmmoInClip(Player player, Npc npc)
        {
            InputDialog ammoDialog = new("ammo", "Enter the ammo value", false, "Set", "Cancel");
            ammoDialog.Response += (sender, ammoDialogResponse) =>
            {
                if (int.TryParse(ammoDialogResponse.InputText, out int ammo))
                {
                    if (ammo < 0)
                        player.SendClientMessage(Color.Red, "The ammo must be greater than 0");
                    else
                    {
                        npc.WeaponAmmoInClip = ammo;
                        player.SendClientMessage($"NPC {npc.Name} Set ammo in clip to " + ammo);
                    }
                }
                else
                    player.SendClientMessage(Color.Red, "Invalid to parse the given number");
                ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
            };
            ammoDialog.Show(player);
        }
        private static void WeaponAndCombat_MeleeAttack(Player player, Npc npc)
        {
            InputDialog meleeAttacktimeDialog = new("ammo", "Enter the time", false, "Set", "Cancel");
            meleeAttacktimeDialog.Response += (sender, meleeAttacktimeDialogResponse) =>
            {
                if (int.TryParse(meleeAttacktimeDialogResponse.InputText, out int time))
                {
                    if (time < 0)
                        player.SendClientMessage(Color.Red, "The time must be greater than 0");
                    else
                    {
                        npc.MeleeAttack(time);
                        player.SendClientMessage($"NPC {npc.Name} is melee attacking for " + time);
                    }
                }
                else
                    player.SendClientMessage(Color.Red, "Invalid to parse the given number");
                ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
            };
            meleeAttacktimeDialog.Show(player);
        }
        private static void WeaponAndCombat_StopMeleeAttack(Player player, Npc npc)
        {
            npc.StopMeleeAttack();
            player.SendClientMessage($"NPC {npc.Name} stops melee attack");
            ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
        }
        private static void WeaponAndCombat_SetFightingStyle(Player player, Npc npc)
        {
            ListDialog fightStyleDialog = new("Select fighting style to set", "Set", "Cancel");
            List<string> fightingStyles = Enum.GetValues(typeof(FightStyle)).Cast<FightStyle>().Select(v => v.ToString()).ToList();
            fightStyleDialog.AddItems(fightingStyles);
            fightStyleDialog.Response += (sender, fightStyleDialogResponse) =>
            {
                npc.FightStyle = (FightStyle)Enum.Parse(typeof(FightStyle), fightingStyles[fightStyleDialogResponse.ListItem]);
                player.SendClientMessage($"{fightingStyles[fightStyleDialogResponse.ListItem]} set fight style to {npc.FightStyle}");
                ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
            };
            fightStyleDialog.Show(player);
        }
        private static void WeaponAndCombat_ToggleReloading(Player player, Npc npc)
        {
            npc.EnableReloading(!npc.IsReloadEnabled);
            player.SendClientMessage($"NPC {npc.Name} Reloading is now {npc.IsReloadEnabled}");
            ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
        }
        private static void WeaponAndCombat_ToggleInfiniteAmmo(Player player, Npc npc)
        {
            npc.EnableInfiniteAmmo(!npc.IsInfiniteAmmoEnabled);
            player.SendClientMessage($"NPC {npc.Name} IsInfinite Ammo is now {npc.IsInfiniteAmmoEnabled}");
            ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
        }
        private static void WeaponAndCombat_Shoot(Player player, Npc npc)
        {
            ListDialog<string> targetDialog = new("Select target", "Select", "Cancel");
            targetDialog.AddItems(BasePlayer.All.Select(p => p.Name));
            targetDialog.Response += (sender, targetDialogResponse) =>
            {
                BasePlayer foundPlayer = BasePlayer.All.First(p => p.Name == targetDialogResponse.ItemValue);
                if (targetDialogResponse.DialogButton == DialogButton.Left && foundPlayer != null)
                {
                    ListDialog entityCheckFlagDialog = new("Select entity check flag", "Shoot !", "Cancel");
                    List<string> entityCheckFlags = Enum.GetValues(typeof(NPCEntityCheck)).Cast<NPCEntityCheck>().Select(v => v.ToString()).ToList();
                    entityCheckFlagDialog.AddItems(entityCheckFlags);
                    entityCheckFlagDialog.Response += (sender, entityCheckFlagDialogResponse) =>
                    {
                        if (targetDialogResponse.DialogButton == DialogButton.Left)
                        {
                            npc.Shoot(npc.Weapon, foundPlayer.Id, BulletHitType.Player, foundPlayer.Position, Vector3.Zero, true,
                                (NPCEntityCheck)entityCheckFlagDialogResponse.ListItem);
                            player.SendClientMessage($"NPC {npc.Name} is shooting {foundPlayer.Name}");
                        }
                        else
                            ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
                    };
                    entityCheckFlagDialog.Show(player);
                }
                else
                    ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
            };
            targetDialog.Show(player);
        }
        private static void WeaponAndCombat_AimAt(Player player, Npc npc)
        {
            ListDialog<DynamicObject> targetDialog = new("Select target", "Select", "Cancel");
            targetDialog.AddItems(DynamicObject.All);
            targetDialog.Response += (sender, targetDialogResponse) =>
            {
                if (targetDialogResponse.DialogButton == DialogButton.Left && targetDialogResponse.ItemValue != null)
                {
                    ListDialog entityCheckFlagDialog = new("Select entity check flag", "Aim", "Cancel");
                    List<string> entityCheckFlags = Enum.GetValues(typeof(NPCEntityCheck)).Cast<NPCEntityCheck>().Select(v => v.ToString()).ToList();
                    entityCheckFlagDialog.AddItems(entityCheckFlags);
                    entityCheckFlagDialog.Response += (sender, entityCheckFlagDialogResponse) =>
                    {
                        if (targetDialogResponse.DialogButton == DialogButton.Left)
                        {
                            MessageDialog shootDialog = new("Shoot ?", "Do you want to shoot after 1000ms ?", "Yes", "No");
                            shootDialog.Response += (sender, shootDialogResponse) =>
                            {
                                if (shootDialogResponse.DialogButton == DialogButton.Left)
                                    npc.AimAt(targetDialogResponse.ItemValue.Position, true, 100, true, Vector3.Zero, (NPCEntityCheck)entityCheckFlagDialogResponse.ListItem);
                                else
                                    npc.AimAt(targetDialogResponse.ItemValue.Position, false, 100, true, Vector3.Zero, (NPCEntityCheck)entityCheckFlagDialogResponse.ListItem);
                                player.SendClientMessage($"NPC {npc.Name} is aiming at {targetDialogResponse.ItemValue.Position}");
                                ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
                            };
                            shootDialog.Show(player);
                        }
                        else
                            ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
                    };
                    entityCheckFlagDialog.Show(player);
                }
                else
                    ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
            };
            targetDialog.Show(player);
        }
        private static void WeaponAndCombat_AimAtPlayer(Player player, Npc npc)
        {
            ListDialog<BasePlayer> targetDialog = new("Select target", "Select", "Cancel");
            targetDialog.AddItems(BasePlayer.All);
            targetDialog.Response += (sender, targetDialogResponse) =>
            {
                if (targetDialogResponse.DialogButton == DialogButton.Left && targetDialogResponse.ItemValue != null)
                {
                    ListDialog entityCheckFlagDialog = new("Select entity check flag", "Aim", "Cancel");
                    List<string> entityCheckFlags = Enum.GetValues(typeof(NPCEntityCheck)).Cast<NPCEntityCheck>().Select(v => v.ToString()).ToList();
                    entityCheckFlagDialog.AddItems(entityCheckFlags);
                    entityCheckFlagDialog.Response += (sender, entityCheckFlagDialogResponse) =>
                    {
                        if (targetDialogResponse.DialogButton == DialogButton.Left)
                        {
                            MessageDialog shootDialog = new("Shoot ?", "Do you want to shoot after 1000ms ?", "Yes", "No");
                            shootDialog.Response += (sender, shootDialogResponse) =>
                            {
                                if (shootDialogResponse.DialogButton == DialogButton.Left)
                                    npc.AimAtPlayer(targetDialogResponse.ItemValue, true, 100, true, Vector3.Zero, Vector3.Zero, (NPCEntityCheck)entityCheckFlagDialogResponse.ListItem);
                                else
                                    npc.AimAtPlayer(targetDialogResponse.ItemValue, false, 100, true, Vector3.Zero, Vector3.Zero, (NPCEntityCheck)entityCheckFlagDialogResponse.ListItem);
                                player.SendClientMessage($"NPC {npc.Name} is aiming at {targetDialogResponse.ItemValue.Position}");
                                ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
                            };
                            shootDialog.Show(player);
                        }
                        else
                            ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
                    };
                    entityCheckFlagDialog.Show(player);
                }
                else
                    ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
            };
            targetDialog.Show(player);
        }
        private static void WeaponAndCombat_StopAim(Player player, Npc npc)
        {
            npc.StopAim();
            player.SendClientMessage($"NPC {npc.Name} stop aim");
            ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
        }
        private static void WeaponAndCombat_SetWeaponAccuracy(Player player, Npc npc)
        {
            Task.Run(() => WeaponAndCombat_SetWeaponAccuracyAsync(player, npc));
        }
        private static async Task WeaponAndCombat_SetWeaponAccuracyAsync(Player player, Npc npc)
        {
            Weapon? weapon = await Utils.ShowWeaponDialog(player,
                () => ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList));
            if (weapon != null)
            {
                string inputStr = await ShowInputDialog(player,
                    () => ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList),
                    "Weapon accuracy", "Enter the accuracy (between 0,0 and 1,0) for " + weapon, "Set");

                if (float.TryParse(inputStr, out float accuracy))
                {
                    if (accuracy < 0.0f || accuracy > 1.0f)
                        player.SendClientMessage(Color.Red, "The accuracy must be between 0,0 and 1,0");
                    else
                    {
                        npc.SetWeaponAccuracy((Weapon)weapon, accuracy);
                        player.SendClientMessage($"NPC {npc.Name} Set the accuracy for {weapon} to {accuracy}");
                        ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
                    }
                }
                else
                    player.SendClientMessage(Color.Red, "Invalid to parse the given number");
            }
        }
        private static void WeaponAndCombat_GetWeaponAccuracy(Player player, Npc npc)
        {
            Task.Run(() => WeaponAndCombat_GetWeaponAccuracyAsync(player, npc));
        }
        private static async Task WeaponAndCombat_GetWeaponAccuracyAsync(Player player, Npc npc)
        {
            Weapon? weapon = await Utils.ShowWeaponDialog(player,
                () => ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList));
            if (weapon != null)
            {
                player.SendClientMessage($"NPC {npc.Name} Accuracy for {weapon} = {npc.GetWeaponAccuracy((Weapon)weapon)}");
                ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
            }
        }
        #endregion
        #region Vehicles
        private static void Vehicle_PrintInfos(Player player, Npc npc)
        {
            TablistDialog vehicleInfoDialog = new("Vehicles infos", 2, "Select", "Cancel")
            {
                new string[] { "Vehicle", npc.Vehicle?.ToString() },
                new string[] { "Vehicle ID", npc.Vehicle?.Id.ToString() },
                new string[] { "Vehicle seat", npc.VehicleSeat.ToString() },
                new string[] { "Entering vehicle", npc.GetEnteringVehicle()?.ToString() },
                new string[] { "Is entering vehicle", npc.IsEnteringVehicle().ToString() },
                new string[] { "Is vehicle siren used", npc.IsVehicleSirenUsed().ToString() },
                new string[] { "Vehicle health", npc.Vehicle?.Health.ToString() },
                new string[] { "Vehicle hydra thrusters", npc.Vehicle?.HydraReactorAngle.ToString() },
                new string[] { "Vehicle hydra thrusters (npc)", npc.GetHydraThrusters().ToString() },
                new string[] { "Is gear up", npc.Vehicle?.IsGearUp.ToString() },
                new string[] { "Is gear up (npc)", npc.GetGearState().ToString() },
                new string[] { "Vehicle train speed", npc.GetVehicleTrainSpeed().ToString() },
            };
            vehicleInfoDialog.Response += (sender, _) => ShowDialogFromDictionary(player, npc, "Vehicle", VehicleList);
            vehicleInfoDialog.Show(player);
        }
        private static void Vehicle_EnterVehicle(Player player, Npc npc)
        {
            Task.Run(() => Vehicle_EnterVehicleAsync(player, npc));
        }
        private static async Task Vehicle_EnterVehicleAsync(Player player, Npc npc)
        {
            // Get vehicle ID
            string inputStr = await ShowInputDialog(player,
                () => ShowDialogFromDictionary(player, npc, "Vehicle", VehicleList),
                "Vehicle ID", "Enter the vehicle ID", "Select");

            if (int.TryParse(inputStr, out int vehID))
            {
                BaseVehicle foundVeh = BaseVehicle.Find(vehID);
                if (foundVeh == null)
                    player.SendClientMessage(Color.Red, "Invalid vehicle id");
                else
                {
                    // Get seat
                    inputStr = await ShowInputDialog(player,
                        () => ShowDialogFromDictionary(player, npc, "Vehicle", VehicleList),
                        "Vehicle seat", "Enter the vehicle seat", "Select");

                    if (int.TryParse(inputStr, out int vehSeat))
                    {
                        if (vehSeat >= 0 && vehSeat <= VehicleModelInfo.ForVehicle(foundVeh).SeatCount)
                        {
                            if (npc.EnterInVehicle(foundVeh, vehSeat, NPCMoveType.Walk))
                            {
                                player.SendClientMessage($"NPC {npc.Name} enters in vehicle {foundVeh.Id} in seat {vehSeat}");
                                ShowDialogFromDictionary(player, npc, "Vehicle", VehicleList);
                            }
                        }
                        else
                            player.SendClientMessage(Color.Red, "Invalid seat number");
                    }
                    else
                        player.SendClientMessage(Color.Red, "Invalid to parse the given number");
                }
            }
            else
                player.SendClientMessage(Color.Red, "Invalid to parse the given number");
        }
        private static void Vehicle_ExitVehicle(Player player, Npc npc)
        {
            if (npc.ExitFromVehicle())
            {
                player.SendClientMessage($"NPC {npc.Name} exits from his vehicle");
                ShowDialogFromDictionary(player, npc, "Vehicle", VehicleList);
            }
        }
        private static void Vehicle_PutInVehicle(Player player, Npc npc)
        {
            Task.Run(() => Vehicle_PutInVehicleAsync(player, npc));
        }
        private static async Task Vehicle_PutInVehicleAsync(Player player, Npc npc)
        {
            // Get vehicle ID
            string inputStr = await ShowInputDialog(player,
                () => ShowDialogFromDictionary(player, npc, "Vehicle", VehicleList),
                "Vehicle ID", "Enter the vehicle ID", "Select");

            if (int.TryParse(inputStr, out int vehID))
            {
                BaseVehicle foundVeh = BaseVehicle.Find(vehID);
                if (foundVeh == null)
                    player.SendClientMessage(Color.Red, "Invalid vehicle id");
                else
                {
                    // Get seat
                    inputStr = await ShowInputDialog(player,
                        () => ShowDialogFromDictionary(player, npc, "Vehicle", VehicleList),
                        "Vehicle seat", "Enter the vehicle seat", "Select");

                    if (int.TryParse(inputStr, out int vehSeat))
                    {
                        if (vehSeat >= 0 && vehSeat <= VehicleModelInfo.ForVehicle(foundVeh).SeatCount)
                        {
                            if (npc.PutInVehicle(foundVeh, vehSeat))
                            {
                                player.SendClientMessage($"NPC {npc.Name} has been put in vehicle {foundVeh.Id} in seat {vehSeat}");
                                ShowDialogFromDictionary(player, npc, "Vehicle", VehicleList);
                            }
                        }
                        else
                            player.SendClientMessage(Color.Red, "Invalid seat number");
                    }
                    else
                        player.SendClientMessage(Color.Red, "Invalid to parse the given number");
                }
            }
            else
                player.SendClientMessage(Color.Red, "Invalid to parse the given number");
        }
        private static void Vehicle_RemoveFromVehicle(Player player, Npc npc)
        {
            if (npc.ExitFromVehicle())
            {
                player.SendClientMessage($"NPC {npc.Name} has been removed from his vehicle");
                ShowDialogFromDictionary(player, npc, "Vehicle", VehicleList);
            }
        }
        private static void Vehicle_UseSiren(Player player, Npc npc)
        {
            if (npc.InAnyVehicle)
            {
                if (npc.UseVehicleSiren(!npc.IsVehicleSirenUsed()))
                {
                    player.SendClientMessage($"NPC {npc.Name} siren state = " + npc.IsVehicleSirenUsed());
                    ShowDialogFromDictionary(player, npc, "Vehicle", VehicleList);
                }
            }
            else
                player.SendClientMessage(Color.Red, "The NPC is not in a vehicle");
        }
        private static void Vehicle_SetHealth(Player player, Npc npc)
        {
            if (npc.InAnyVehicle)
                Task.Run(() => Vehicle_SetHealthAsync(player, npc));
            else
                player.SendClientMessage(Color.Red, "The NPC is not in a vehicle");
        }
        private static async Task Vehicle_SetHealthAsync(Player player, Npc npc)
        {
            string inputStr = await ShowInputDialog(player,
                () => ShowDialogFromDictionary(player, npc, "Vehicle", VehicleList),
                "Vehicle health", "Enter the vehicle health (between 0,0 and 1000,0)", "Select");

            if (float.TryParse(inputStr, out float health))
            {
                if (health < 0.0f || health > 1000.0f)
                {
                    player.SendClientMessage(Color.Red, "Invalid health number");
                }
                else
                {
                    npc.Vehicle.Health = health;
                    player.SendClientMessage($"NPC {npc.Name} vehicle health set to " + health);
                    ShowDialogFromDictionary(player, npc, "Vehicle", VehicleList);
                }
            }
            else
                player.SendClientMessage(Color.Red, "Invalid to parse the given number");
        }
        private static void Vehicle_SetHydraThrusters(Player player, Npc npc)
        {
            if (npc.InAnyVehicle && npc.Vehicle.Model == VehicleModelType.Hydra)
                Task.Run(() => Vehicle_SetHydraThrustersAsync(player, npc));
            else
                player.SendClientMessage(Color.Red, "The NPC is not in a hydra");
        }
        private static async Task Vehicle_SetHydraThrustersAsync(Player player, Npc npc)
        {
            string inputStr = await ShowInputDialog(player,
                () => ShowDialogFromDictionary(player, npc, "Vehicle", VehicleList),
                "Hydra thrusters", "Enter the thrusters angle (0 or 1)", "Select");

            if (int.TryParse(inputStr, out int angle))
            {
                if (angle != 0 && angle != 1)
                {
                    player.SendClientMessage(Color.Red, "Invalid angle number");
                }
                else
                {
                    npc.SetHydraThrusters(angle);
                    player.SendClientMessage($"NPC {npc.Name} vehicle hydra thrusters set to " + npc.Vehicle.HydraReactorAngle);
                    ShowDialogFromDictionary(player, npc, "Vehicle", VehicleList);
                }
            }
            else
                player.SendClientMessage(Color.Red, "Invalid to parse the given number");
        }
        private static void Vehicle_ToggleGearState(Player player, Npc npc)
        {
            if (npc.InAnyVehicle && npc.Vehicle.ModelInfo.Category == VehicleCategory.Airplane)
                npc.SetGearState(npc.GetGearState() == 0 ? 1 : 0);
            else
                player.SendClientMessage(Color.Red, "The NPC is not in a hydra");
        }
        private static void Vehicle_SetTrainSpeed(Player player, Npc npc)
        {
            if (npc.InAnyVehicle && (npc.Vehicle.Model == VehicleModelType.FreightTrain || npc.Vehicle.Model == VehicleModelType.BrownstreakTrain))
                Task.Run(() => Vehicle_SetTrainSpeedAsync(player, npc));
            else
                player.SendClientMessage(Color.Red, "The NPC is not in a train");
        }
        private static async Task Vehicle_SetTrainSpeedAsync(Player player, Npc npc)
        {
            string inputStr = await ShowInputDialog(player,
                () => ShowDialogFromDictionary(player, npc, "Vehicle", VehicleList),
                "Train speed", "Enter speed", "Select");

            if (float.TryParse(inputStr, out float speed))
            {
                if (speed < -1000)
                {
                    player.SendClientMessage(Color.Red, "Invalid speed number");
                }
                else
                {
                    npc.SetVehicleTrainSpeed(speed);
                    player.SendClientMessage($"NPC {npc.Name} vehicle train speed set to " + speed);
                    ShowDialogFromDictionary(player, npc, "Vehicle", VehicleList);
                }
            }
            else
                player.SendClientMessage(Color.Red, "Invalid to parse the given number");
        }
        private static void Vehicle_Honk(Player player, Npc npc)
        {
            npc.SetKeys(0, 0, Keys.Crouch);
            player.SendClientMessage($"NPC {npc.Name} honked !");
            ShowDialogFromDictionary(player, npc, "Vehicle", VehicleList);
            SampSharp.GameMode.SAMP.Timer t = new(1000, false);
            t.Tick += (sender, e) => npc.SetKeys(0, 0, 0);
        }
        #endregion
        #region Animations
        private static void Animation_PrintInfos(Player player, Npc npc)
        {
            npc.GetAnimation(out int animId, out float delta, out bool loop, out bool lockX, out bool lockY, out bool freeze, out int time);
            string animationString = $"animId: {animId} ; delta: {delta} ; loop: {loop} ; lockX: {lockX} ; lockY {lockY} ; freeze: {freeze} ; int: {time}";
            TablistDialog animationInfoDialog = new("Animations infos", 2, "Select", "Cancel")
            {
                new string[] { "Animation", animationString },
                new string[] { "Special action", npc.SpecialAction.ToString() },
            };
            animationInfoDialog.Response += (sender, _) => ShowDialogFromDictionary(player, npc, "Animation", AnimationList);
            animationInfoDialog.Show(player);
        }
        private static void Animation_ResetAnimation(Player player, Npc npc)
        {
            npc.ResetAnimation();
            player.SendClientMessage($"NPC {npc.Name} has reset his animation");
        }
        private static void Animation_SetAnimation(Player player, Npc npc)
        {
            Task.Run(() => Animation_SetAnimationAsync(player, npc));
        }
        private static async Task Animation_SetAnimationAsync(Player player, Npc npc)
        {
            string inputStr = await ShowInputDialog(player,
                () => ShowDialogFromDictionary(player, npc, "Animation", AnimationList),
                "Animation ID", "Enter the animation ID", "Select");

            if (int.TryParse(inputStr, out int animId))
            {
                if (animId < 0)
                {
                    player.SendClientMessage(Color.Red, "Invalid animId number");
                }
                else
                {
                    npc.SetAnimation(animId, 4.1f, true, false, false, false, 0);
                    player.SendClientMessage($"NPC {npc.Name} set animation to " + animId);
                    ShowDialogFromDictionary(player, npc, "Animation", AnimationList);
                }
            }
            else
                player.SendClientMessage(Color.Red, "Invalid to parse the given number");
        }
        private static void Animation_ApplyAnimation(Player player, Npc npc)
        {
            Task.Run(() => Animation_ApplyAnimationAsync(player, npc));
        }
        private static async Task Animation_ApplyAnimationAsync(Player player, Npc npc)
        {
            string animlib = await ShowInputDialog(player,
                () => ShowDialogFromDictionary(player, npc, "Animation", AnimationList),
                "Animation lib", "Enter the animation library", "Select");

            if (animlib.Length > 0)
            {
                string animname = await ShowInputDialog(player,
                    () => ShowDialogFromDictionary(player, npc, "Animation", AnimationList),
                    "Animation name", "Enter the animation name", "Select");
                if (animname.Length > 0)
                {
                    npc.ApplyAnimation(animlib, animname, 4.1f, true, false, false, false, 0);
                    player.SendClientMessage($"NPC {npc.Name} applied animation to " + animlib + " " + animname);
                    ShowDialogFromDictionary(player, npc, "Animation", AnimationList);
                }
                else
                    player.SendClientMessage(Color.Red, "Abort: animname was empty");
            }
            else
                player.SendClientMessage(Color.Red, "Abort: animlib was empty");
        }
        private static void Animation_ClearAnimations(Player player, Npc npc)
        {
            npc.ClearAnimations();
            player.SendClientMessage($"NPC {npc.Name} cleared all animations");
        }
        private static void Animation_SetSpecialAction(Player player, Npc npc)
        {
            Task.Run(() => Animation_SetSpecialActionAsync(player, npc));
        }
        private static async Task Animation_SetSpecialActionAsync(Player player, Npc npc)
        {
            SpecialAction? specialAction = await Utils.ShowSpecialActionDialog(player,
                () => ShowDialogFromDictionary(player, npc, "Animation", AnimationList));
            if (specialAction != null)
            {
                npc.SpecialAction = specialAction.Value;
                player.SendClientMessage($"NPC {npc.Name} has now special action = {specialAction}");
                ShowDialogFromDictionary(player, npc, "Animation", AnimationList);
            }
        }
        #endregion
        #region Paths
        private static void Paths_PrintInfos(Player player, Npc npc)
        {
            TablistDialog pathInfoDialog = new("Animations infos", 2, "Select", "Cancel")
            {
                new string[] { "Path count", Npc.Path.All.Count().ToString() },
                new string[] { "Special action", npc.SpecialAction.ToString() },
            };
            foreach(Npc.Path path in Npc.Path.All)
            {
                pathInfoDialog.Add(new string[] { path.Id.ToString(), path.ToString() });
            }
            pathInfoDialog.Response += (sender, _) => ShowDialogFromDictionary(player, npc, "Path", PathList);
            pathInfoDialog.Show(player);
        }
        private static void Paths_StartHUD(Player player, Npc npc)
        {
            if (player.npcTestHUD == null)
            {
                player.npcTestHUD = new NpcTestHUD(player);
            }
            player.npcTestHUD.SetInClickableMode();
        }
        private static void Paths_StopHUD(Player player, Npc npc)
        {
            if (player.npcTestHUD != null)
            {
                player.CancelSelectTextDraw();
                player.npcTestHUD.Unload();
                player.npcTestHUD = null;
            }
        }
        private static void Paths_MoveToPath(Player player, Npc npc)
        {
            Task.Run(() => Paths_MoveToPathAsync(player, npc));
        }
        private static async Task Paths_MoveToPathAsync(Player player, Npc npc)
        {
            NPCMoveType? moveType = await Utils.ShowNPCMoveTypeDialog(player,
                () => ShowDialogFromDictionary(player, npc, "Path", PathList));
            if(moveType != null)
            {
                npc.MoveByPath(Npc.Path.Find(1), moveType.Value);
            }
        }
        #endregion


        private static async Task<string> ShowInputDialog(Player player, Action methodToCallIfCancel, string title, string bodyText, string buttonLeft)
        {
            InputDialog inputDialog = new(title, bodyText, false, buttonLeft, "Cancel");
            DialogResponseEventArgs inputDialogResponse = await inputDialog.ShowAsync(player);

            if (inputDialogResponse.DialogButton == DialogButton.Left)
            {
                return inputDialogResponse.InputText;
            }
            else
                methodToCallIfCancel();
            return "";
        }
        private static void ShowDialogFromDictionary(Player player, Npc npc, string title, Dictionary<string, Action<Player, Npc>> dict)
        {
            // Update items to set current values in item
            List<string> updatedKeys = new(dict.Keys);
            for (int i = 0; i < updatedKeys.Count; i++)
            {
                string newVal = updatedKeys[i];

                if (updatedKeys[i] == "Set facing angle") AddValueAtEnd(ref newVal, npc.Angle);
                if (updatedKeys[i] == "Set virtual world") AddValueAtEnd(ref newVal, npc.VirtualWorld);
                if (updatedKeys[i] == "Move to map marker") AddValueAtEnd(ref newVal, npc.IsMoving ? "moving" : "not moving");
                if (updatedKeys[i] == "Move to player") AddValueAtEnd(ref newVal, npc.IsMoving ? "moving" : "not moving");
                if (updatedKeys[i] == "Stop move") AddValueAtEnd(ref newVal, npc.IsMoving ? "moving" : "not moving");

                if (updatedKeys[i] == "Set interior") AddValueAtEnd(ref newVal, npc.Interior);

                if (updatedKeys[i] == "Set health") AddValueAtEnd(ref newVal, npc.Health);
                if (updatedKeys[i] == "Set armour") AddValueAtEnd(ref newVal, npc.Armour);
                if (updatedKeys[i] == "Toggle invulnerable") AddValueAtEnd(ref newVal, npc.Invulnerable);

                if (updatedKeys[i] == "Set weapon") AddValueAtEnd(ref newVal, npc.Weapon);
                if (updatedKeys[i] == "Set ammo") AddValueAtEnd(ref newVal, npc.WeaponAmmo);
                if (updatedKeys[i] == "Set ammo in clip") AddValueAtEnd(ref newVal, npc.WeaponAmmoInClip);
                if (updatedKeys[i] == "Melee attack") AddValueAtEnd(ref newVal, npc.IsMeleeAttacking ? "attacking" : "not attacking");
                if (updatedKeys[i] == "Stop Melee attack") AddValueAtEnd(ref newVal, npc.IsMeleeAttacking ? "attacking" : "not attacking");
                if (updatedKeys[i] == "Set fighting style") AddValueAtEnd(ref newVal, npc.FightStyle);
                if (updatedKeys[i] == "Toggle reloading") AddValueAtEnd(ref newVal, npc.IsReloadEnabled);
                if (updatedKeys[i] == "Toggle infinite ammo") AddValueAtEnd(ref newVal, npc.IsInfiniteAmmoEnabled);
                if (updatedKeys[i] == "Shoot") AddValueAtEnd(ref newVal, npc.IsShooting ? "shooting" : "not shooting");
                if (updatedKeys[i] == "Aim at object") AddValueAtEnd(ref newVal, npc.IsAiming() ? "aiming" : "not aiming");
                if (updatedKeys[i] == "Aim at player") AddValueAtEnd(ref newVal, npc.IsAimingAtPlayer(player) ? "aiming" : "not aiming");
                if (updatedKeys[i] == "Stom aim") AddValueAtEnd(ref newVal, npc.IsAiming() ? "aiming" : "not aiming");

                if (updatedKeys[i] == "Use vehicle siren") AddValueAtEnd(ref newVal, npc.IsVehicleSirenUsed() ? "on" : "off");
                if (updatedKeys[i] == "Set vehicle health") AddValueAtEnd(ref newVal, npc.Vehicle?.Health);
                if (updatedKeys[i] == "Set vehicle hydra thrusters") AddValueAtEnd(ref newVal, npc.GetHydraThrusters());
                if (updatedKeys[i] == "Toggle vehicle gear state") AddValueAtEnd(ref newVal, npc.GetGearState());
                if (updatedKeys[i] == "Set train speed") AddValueAtEnd(ref newVal, npc.GetVehicleTrainSpeed());

                if (updatedKeys[i] == "Start HUD") AddValueAtEnd(ref newVal, (player.npcTestHUD == null) ? "Off" : "On");


                updatedKeys[i] = newVal;
            }

            ListDialog dialog = new(title, "Select", "Cancel");
            dialog.AddItems(updatedKeys);
            dialog.Response += (sender, dialogResponse) =>
            {
                if (dialogResponse.DialogButton == DialogButton.Left)
                {
                    dict[dict.Keys.ToList()[dialogResponse.ListItem]](player, npc);
                }
                else
                    NpcCommand(player);
            };
            dialog.Show(player);
        }
        private static void AddValueAtEnd(ref string str, object value)
        {
            str += $" ({value})";
        }
        private static void Player_ClickMap(object sender, SampSharp.GameMode.Events.PositionEventArgs e)
        {
            if (lastSelectedNpcId != -1)
            {
                Npc npc = Npc.Find(lastSelectedNpcId);
                if (npc != null)
                {
                    if (nextClipMapAction == NextClipMapAction.SetPosition)
                    {
                        npc.Position = e.Position;
                        (sender as Player).Notificate("NPC position set");
                    }
                    else if (nextClipMapAction == NextClipMapAction.MoveTo)
                    {
                        npc.Move(e.Position, npc.InAnyVehicle ? NPCMoveType.Drive : NPCMoveType.Jog);
                        (sender as Player).Notificate("NPC move to position set");
                    }
                }
                (sender as Player).ClickMap -= Player_ClickMap;
            }
        }
    }
}
