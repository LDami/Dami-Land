using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharp.GameMode.World;
using SampSharp.Streamer;
using SampSharp.Streamer.World;
using SampSharpGameMode1.Civilisation;
using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable IDE0051 // Disable useless private members

namespace SampSharpGameMode1.Commands
{
    class NpcTestCommands
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
            { "Set keys", null },
            { "Melee attack", WeaponAndCombat_MeleeAttack },
            { "Stop Melee attack", WeaponAndCombat_StopMeleeAttack },
            { "Set fighting style", WeaponAndCombat_SetFightingStyle },
            { "Toggle reloading", WeaponAndCombat_ToggleReloading },
            { "Toggle infinite ammo", WeaponAndCombat_ToggleInfiniteAmmo },
        };

        readonly static Dictionary<string, Dictionary<string, Action<Player, Npc>>> methodDictionaries = new()
        {
            { "Core", CoreList },
            { "Position and movement", PosAndMovementList },
            { "Appearance", AppearanceList },
            { "Health & Combat", HealthAndCombatList },
            { "Weapon & Combat", WeaponAndCombatList },
        };


        [Command("npc-create")]
        private static void NpcCreateCommand(Player player, string npcname)
        {
            Npc npc = Npc.Create(npcname);
            if (npc.Id == BasePlayer.InvalidId)
            {
                player.SendClientMessage(Color.Red, "Unable to create the NPC, try with another name");
                return;
            }
            npc.Spawned += (sender, e) =>
            {
                player.SendClientMessage("the NPC spawned!");
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
        }

        [Command("npc")]
        private static void NpcCommand(Player player)
        {
            if(!Npc.All.Any())
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
                                                        searchDialogResult.Add(new string[] { str.Key , methodDictionary.Key });
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
            coreInfoDialog.Response += (sender, coreInfoDialogResponse) => ShowDialogFromDictionary(player, npc, "Core", CoreList);
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
            npc.MoveToPlayer(player);
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
            appearanceInfoDialog.Response += (sender, appearanceInfoDialogResponse) => ShowDialogFromDictionary(player, npc, "Appearance", AppearanceList);
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
            healthAndCombatInfoDialog.Response += (sender, healthAndCombatInfoDialogResponse) => ShowDialogFromDictionary(player, npc, "Health & Combat", HealthAndCombatList);
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
            weaponAndCombatInfoDialog.Response += (sender, weaponAndCombatInfoDialogResponse) => ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
            weaponAndCombatInfoDialog.Show(player);
        }
        private static void WeaponAndCombat_SetWeapon(Player player, Npc npc)
        {
            ListDialog giveWeaponDialog = new("Select weapon to give", "Give", "Cancel");
            List<string> weapons = Enum.GetValues(typeof(Weapon)).Cast<Weapon>().Select(v => v.ToString()).ToList();
            giveWeaponDialog.AddItems(weapons);
            giveWeaponDialog.Response += (sender, giveWeaponDialogResponse) =>
            {
                // TODO to fix: gives the wrong weapon (gives the index of array, but weapon enum is not constantly incrementing)
                npc.Weapon = (Weapon)(giveWeaponDialogResponse.ListItem - 1);
                player.SendClientMessage($"NPC {npc.Name} has now {weapons[giveWeaponDialogResponse.ListItem]}");
                ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
            };
            giveWeaponDialog.Show(player);
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
            ListDialog<BasePlayer> targetDialog = new("Select target", "Select", "Cancel");
            targetDialog.AddItems(BasePlayer.All);
            targetDialog.Response += (sender, targetDialogResponse) =>
            {
                if (targetDialogResponse.DialogButton == DialogButton.Left && targetDialogResponse.ItemValue != null)
                {
                    ListDialog entityCheckFlagDialog = new("Select entity check flag", "Shoot !", "Cancel");
                    List<string> entityCheckFlags = Enum.GetValues(typeof(NPCEntityCheck)).Cast<NPCEntityCheck>().Select(v => v.ToString()).ToList();
                    entityCheckFlagDialog.AddItems(entityCheckFlags);
                    entityCheckFlagDialog.Response += (sender, entityCheckFlagDialogResponse) =>
                    {
                        if (targetDialogResponse.DialogButton == DialogButton.Left)
                        {
                            npc.Shoot(npc.Weapon, targetDialogResponse.ItemValue.Id, BulletHitType.Player, targetDialogResponse.ItemValue.Position, Vector3.Zero, true,
                                (NPCEntityCheck)entityCheckFlagDialogResponse.ListItem);
                            player.SendClientMessage($"NPC {npc.Name} is shooting {targetDialogResponse.ItemValue.Name}");
                        }
                        else
                            ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
                    };
                }
                else
                    ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
            };
        }
        private static void WeaponAndCombat_AimtAt(Player player, Npc npc)
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
                            npc.AimAt(targetDialogResponse.ItemValue.Position, false, 0, true, Vector3.Zero, (NPCEntityCheck)entityCheckFlagDialogResponse.ListItem);
                            player.SendClientMessage($"NPC {npc.Name} is aimint at {targetDialogResponse.ItemValue.Position}");
                            ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
                        }
                        else
                            ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
                    };
                }
                else
                    ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
            };
        }
        private static void WeaponAndCombat_AimtAtPlayer(Player player, Npc npc)
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
                            npc.AimAtPlayer(targetDialogResponse.ItemValue, false, 0, true, Vector3.Zero, Vector3.Zero, (NPCEntityCheck)entityCheckFlagDialogResponse.ListItem);
                            player.SendClientMessage($"NPC {npc.Name} is aiming at {targetDialogResponse.ItemValue.Name}");
                            ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
                        }
                        else
                            ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
                    };
                }
                else
                    ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
            };
        }
        private static void WeaponAndCombat_StopAim(Player player, Npc npc)
        {
            npc.StopAim();
            player.SendClientMessage($"NPC {npc.Name} stop aim");
            ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
        }
        private static void WeaponAndCombat_SetWeaponAccuracy(Player player, Npc npc)
        {
            InputDialog weaponDialog = new("Weapon accuracy", "Enter the weapon name", false, "Set", "Cancel");
            weaponDialog.Response += (sender, weaponDialogResponse) =>
            {
                if(weaponDialogResponse.DialogButton == DialogButton.Left)
                {
                    if (Enum.TryParse(typeof(Weapon), weaponDialogResponse.InputText, out object weapon))
                    {
                        InputDialog accuracyDialog = new("ammo", "Enter the accuracy (between 0.0 and 1.0)", false, "Set", "Cancel");
                        accuracyDialog.Response += (sender, meleeAttacktimeDialogResponse) =>
                        {
                            if (weaponDialogResponse.DialogButton == DialogButton.Left)
                            {
                                if (float.TryParse(meleeAttacktimeDialogResponse.InputText, out float accuracy))
                                {
                                    if (accuracy < 0.0f || accuracy > 1.0f)
                                        player.SendClientMessage(Color.Red, "The accuracy must be between 0.0 and 1.0");
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
                            else
                                ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
                        };
                        accuracyDialog.Show(player);
                    }
                    else
                        player.SendClientMessage(Color.Red, "Invalid to parse the given weapon");
                }
                else
                    ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
            };
            weaponDialog.Show(player);
        }
        private static void WeaponAndCombat_GetWeaponAccuracy(Player player, Npc npc)
        {
            InputDialog weaponDialog = new("Weapon accuracy", "Enter the weapon name", false, "Set", "Cancel");
            weaponDialog.Response += (sender, weaponDialogResponse) =>
            {
                if (weaponDialogResponse.DialogButton == DialogButton.Left)
                {
                    if (Enum.TryParse(typeof(Weapon), weaponDialogResponse.InputText, out object weapon))
                    {
                        player.SendClientMessage($"NPC {npc.Name} Accuracy for {weapon} = {npc.GetWeaponAccuracy((Weapon)weapon)}");
                        ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
                    }
                    else
                        player.SendClientMessage(Color.Red, "Invalid to parse the given weapon");
                }
                else
                    ShowDialogFromDictionary(player, npc, "Weapon & Combat", WeaponAndCombatList);
            };
            weaponDialog.Show(player);
        }

        #endregion

        private static void ShowDialogFromDictionary(Player player, Npc npc, string title, Dictionary<string, Action<Player, Npc>> dict)
        {
            // Update items to set current values in item
            List<string> updatedKeys = new(dict.Keys);
            for(int i = 0; i < updatedKeys.Count; i++)
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
            if(lastSelectedNpcId != -1)
            {
                Npc npc = Npc.Find(lastSelectedNpcId);
                if(npc != null)
                {
                    if (nextClipMapAction == NextClipMapAction.SetPosition)
                    {
                        npc.Position = e.Position;
                        (sender as Player).Notificate("NPC position set");
                    }
                    else if(nextClipMapAction == NextClipMapAction.MoveTo)
                    {
                        npc.Move(e.Position);
                        (sender as Player).Notificate("NPC move to position set");
                    }
                }
                (sender as Player).ClickMap -= Player_ClickMap;
            }
        }
    }
}
