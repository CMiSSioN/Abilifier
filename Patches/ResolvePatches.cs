﻿using System;
using System.Collections.Generic;
using System.Linq;
using Abilifier.Framework;
using static Abilifier.Framework.GlobalVars;
using BattleTech;
using BattleTech.Save;
using BattleTech.Save.Test;
using BattleTech.UI;
using Harmony;
using SVGImporter;
using UnityEngine;
using Text = Localize.Text;
using CustAmmoCategories;
using CustomAmmoCategoriesPatches;

namespace Abilifier.Patches
{
    public class ResolvePatches
    {
        public class Resolve_StatePatches
        {
            [HarmonyPatch(typeof(SGCharacterCreationCareerBackgroundSelectionPanel), "Done")]
            public static class SGCharacterCreationCareerBackgroundSelectionPanel_Done_Patch
            {
                public static bool Prepare() => Mod.modSettings.enableResolverator;
                public static void Postfix(SGCharacterCreationCareerBackgroundSelectionPanel __instance)
                {
                    var sim = UnityGameInstance.BattleTechGame.Simulation;
                    if (sim == null) return;

                    if (!sim.Commander.pilotDef.PilotTags.Any(x => x.StartsWith(rGUID)))
                    {
                        sim.Commander.pilotDef.PilotTags.Add(
                            $"RESOLVERATOR_{sim.Commander.Description.Id}{Guid.NewGuid()}");
                    }
                    foreach (var p in sim.PilotRoster)
                    {
                        if (!p.pilotDef.PilotTags.Any(x => x.StartsWith(rGUID)))
                        {
                            p.pilotDef.PilotTags.Add($"RESOLVERATOR_{p.Description.Id}{Guid.NewGuid()}");
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(SimGameState), "Dehydrate",
                new Type[] {typeof(SimGameSave), typeof(SerializableReferenceContainer)})]
            public static class SGS_Dehydrate_Patch
            {
                public static bool Prepare() => Mod.modSettings.enableResolverator;
                public static void Prefix(SimGameState __instance)
                {
                    if (!__instance.Commander.pilotDef.PilotTags.Any(x => x.StartsWith(rGUID)))
                    {
                        __instance.Commander.pilotDef.PilotTags.Add(
                            $"RESOLVERATOR_{__instance.Commander.Description.Id}{Guid.NewGuid()}");
                    }

                    foreach (var p in __instance.PilotRoster)
                    {

                        if (!p.pilotDef.PilotTags.Any(x => x.StartsWith(rGUID)))
                        {
                            p.pilotDef.PilotTags.Add($"RESOLVERATOR_{p.Description.Id}{Guid.NewGuid()}");
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(SimGameState), "Rehydrate", new Type[] {typeof(GameInstanceSave)})]
            public static class SGS_Rehydrate_Patch
            {
                public static bool Prepare() => Mod.modSettings.enableResolverator;
                public static void Postfix(SimGameState __instance)
                {
                    if (!__instance.Commander.pilotDef.PilotTags.Any(x => x.StartsWith(rGUID)))
                    {
                        __instance.Commander.pilotDef.PilotTags.Add(
                            $"RESOLVERATOR_{__instance.Commander.Description.Id}{Guid.NewGuid()}");
                    }

                    foreach (var p in __instance.PilotRoster)
                    {

                        if (!p.pilotDef.PilotTags.Any(x => x.StartsWith(rGUID)))
                        {
                            p.pilotDef.PilotTags.Add($"RESOLVERATOR_{p.Description.Id}{Guid.NewGuid()}");
                        }
                    }
                }
            }
        }

        public class Resolve_CombatPatches
        { 
            [HarmonyPatch(typeof(CombatHUDMoraleBar), "RefreshTooltip", new Type[] {})]
            public static class CombatHUDMoraleBar_RefreshTooltip
            {
                public static bool Prepare() => Mod.modSettings.enableResolverator;

                public static bool Prefix(CombatHUDMoraleBar __instance)
                {
                    var sim = UnityGameInstance.BattleTechGame.Simulation;
                    if (sim == null) return true;

                    CombatHUDSidePanelHoverElement componentInChildren = __instance.GetComponentInChildren<CombatHUDSidePanelHoverElement>(true);
                    if (componentInChildren != null)
                    {
                        if (__instance.Combat.EncounterLayerData.SupportedContractTypeValue.UsesFury)
                        {
                            componentInChildren.Title = new Localize.Text(__instance.Combat.Constants.CombatUIConstants.FuryBarDescription.Name, Array.Empty<object>());
                            componentInChildren.Description = new Localize.Text(__instance.Combat.Constants.CombatUIConstants.FuryBarDescription.Details, Array.Empty<object>());
                            return false;
                        }
                        MoraleConstantsDef activeMoraleDef = __instance.Combat.Constants.GetActiveMoraleDef(__instance.Combat);
                        componentInChildren.Title = new Localize.Text(__instance.Combat.Constants.CombatUIConstants.MoraleBarDescription.Name, Array.Empty<object>());
                        var unit = __instance.HUD.selectedUnit;
                        var totalBaseline = (float)__instance.Combat.LocalPlayerTeam.BaselineMoraleGain;
                        var totalAfterModifiers = Mathf.RoundToInt(totalBaseline);
                        if (unit != null)
                        {
                            var baselineUnitMoraleGain = unit.GetResolveRoundBaseMod();
                            totalBaseline += baselineUnitMoraleGain;
                            var resolveGenBaseMult = unit.GetResolveGenBaseMult();
                            totalAfterModifiers = Mathf.RoundToInt(totalBaseline * resolveGenBaseMult);
                        }
                        
                        
                        componentInChildren.Description = new Localize.Text(__instance.Combat.Constants.CombatUIConstants.MoraleBarDescription.Details, new object[]
                        {
                            //__instance.Combat.LocalPlayerTeam.BaselineMoraleGain,
                            totalAfterModifiers,
                            __instance.Combat.LocalPlayerTeam.CollectSimGameBaseline(activeMoraleDef),
                            __instance.Combat.LocalPlayerTeam.CollectUnitBaseline(activeMoraleDef),
                            __instance.Combat.LocalPlayerTeam.CollectBiggestMoraleMod(activeMoraleDef)
                        });
                    }
                    return false;
                }
            }//patch ability init at pilot (everywhere Init(combat) to make dummy mechcomponent that contains reference to actor; patch everywhere else to ignore it if it isnt really a component


            [HarmonyPatch(typeof(Pilot), "AddAbility", new Type[] { typeof(string) })]
            public static class Pilot_AddAbility
            {
                public static void Postfix(Pilot __instance, string abilityName)
                {//probably pointless, since only used for turrets
                    if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                    var component = __instance.ParentActor.GenerateDummyActorAbilityComponent();
                    if (component == null) return;
                    var ability = __instance.Abilities.FirstOrDefault(x => x.Def.Id == abilityName);
                    if (ability == null) return;
                    ability.parentComponent = component;
                }
            }
            
            [HarmonyPatch(typeof(Ability), "ProcessDetailString")]
            public static class Ability_ProcessDetailString
            {
                public static bool Prefix(Ability ability, ref Text __result)
                {
                    var combat = UnityGameInstance.BattleTechGame.Combat;
                    if (combat == null) return true;
                    AbstractActor actor;
                    if (ability.parentComponent == null) return true;
                    if (ability.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                    if (ability.parentComponent.parent != null)
                    {
                        actor = ability.parentComponent.parent;
                    }
                    else
                    {
                        var subStringGUID = ability.parentComponent.GUID.Substring(20);
                        actor = combat.FindActorByGUID(subStringGUID);
                    }
                    
                    if (actor == null) return true;

                    string text = Localize.Strings.T(ability.Def.Description.Details);
                    List<object> list = new List<object>();
                    text = text.Replace("[FloatParam1]", "{0}");
                    list.Add(new Text("{0}", new object[] { ability.Def.FloatParam1 }));
                    text = text.Replace("[FloatParam2]", "{1}");
                    list.Add(new Text("{0}", new object[] { ability.Def.FloatParam2 }));
                    text = text.Replace("[IntParam1]", "{2}");
                    list.Add(new Text("{0}", new object[] { ability.Def.IntParam1 }));
                    text = text.Replace("[IntParam2]", "{3}");
                    list.Add(new Text("{0}", new object[] { ability.Def.IntParam2 }));
                    text = text.Replace("[StringParam1]", "{4}");
                    list.Add(new Text("{0}", new object[] { ability.Def.StringParam1 }));
                    text = text.Replace("[StringParam2]", "{5}");
                    list.Add(new Text("{0}", new object[] { ability.Def.StringParam2 }));
                    text = text.Replace("[ActivationCooldown]", "{6}");
                    list.Add(new Text("{0}", new object[] { ability.Def.ActivationCooldown }));
                    text = text.Replace("[DurationActivations]", "{7}");
                    list.Add(new Text("{0}", new object[] { ability.Def.DurationActivations }));
                    text = text.Replace("[ActivationETA]", "{8}");
                    list.Add(new Text("{0}", new object[] { ability.Def.ActivationETA }));
                    text = text.Replace("[NumberOfUses]", "{9}");
                    list.Add(new Text("{0}", new object[] { ability.Def.NumberOfUses }));
                    text = text.Replace("[ResolveCost]", "{10}");
                    list.Add(new Text("{0}", new object[] { Mathf.RoundToInt(ability.Def.getAbilityDefExtension().ResolveCost * actor.GetResolveCostBaseMult()) }));
                    List<EffectData> effectData = ability.Def.EffectData;
                    if (list.Count <= 0)
                    {
                        __result = new Text(text, Array.Empty<object>());
                        return false;
                    }
                    __result = new Text(text, list.ToArray());
                    return false;
                }
            }

            [HarmonyPatch(typeof(CombatHUDSidePanelHoverElement), "InitForSelectionState", new Type[] {typeof(SelectionType), typeof(AbstractActor) })]
            public static class CombatHUDSidePanelHoverElement_InitForSelectionState
            {

                [HarmonyPriority(Priority.Last)]
                public static void Postfix(CombatHUDSidePanelHoverElement __instance, SelectionType SelectionType, AbstractActor actor)
                {
                    var constants = __instance.HUD.Combat.Constants;
                    if (SelectionType == SelectionType.FireMorale)
                    {
                        __instance.Title = new Text(constants.CombatUIConstants.MoraleAttackDescription.Name, Array.Empty<object>());
                        if (actor == null)
                        {
                            __instance.Description = new Text(constants.CombatUIConstants.MoraleAttackDescription.Details, Array.Empty<object>());
                            return;
                        }
                        bool usesFury = __instance.HUD.Combat.EncounterLayerData.SupportedContractTypeValue.UsesFury;
                        __instance.Description = new Text("{0}{1}", new object[]
                        {
                            constants.CombatUIConstants.MoraleAttackDescription.Details,
                            usesFury ? constants.CombatUIConstants.MoraleCostAttackDescriptionFury.Details : (actor.HasLowMorale ? actor.ParseResolveDetailsFromConstants(true, 0, constants)?.ToString() : 
                                (actor.HasHighMorale ? actor.ParseResolveDetailsFromConstants(true, 2, constants)?.ToString() :
                                    actor.ParseResolveDetailsFromConstants(true, 1, constants)?.ToString()))
                        });
                    }

                    if (SelectionType == SelectionType.ConfirmMorale)
                    {
                        __instance.Title = new Text(constants.CombatUIConstants.MoraleDefendDescription.Name, Array.Empty<object>());
                        if (actor == null)
                        {
                            __instance.Description = new Text(constants.CombatUIConstants.MoraleDefendDescription.Details, Array.Empty<object>());
                            return;
                        }
                        __instance.Description = new Text("{0}{1}", new object[]
                        {
                            constants.CombatUIConstants.MoraleDefendDescription.Details,
                            actor.HasLowMorale ? actor.ParseResolveDetailsFromConstants(false, 0, constants)?.ToString() :
                                (actor.HasHighMorale ? actor.ParseResolveDetailsFromConstants(false, 2, constants)?.ToString() :
                                    actor.ParseResolveDetailsFromConstants(false, 1, constants)?.ToString())
                        });
                    }
                }
            }

            [HarmonyPatch(typeof(SelectionStateConfirmMorale), "FireButtonString", MethodType.Getter)]
            public static class SelectionStateConfirmMorale_FireButtonString_Getter
            {
                [HarmonyPriority(Priority.Last)]
                public static void Postfix(SelectionStateConfirmMorale __instance, ref string __result)
                {
                    if (__instance.HUD.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                    var constants = __instance.Combat.Constants;
                    var actor = __instance.SelectedActor;
                    __result = Localize.Strings.T("{0}{1}", new object[]
                    {
                        constants.CombatUIConstants.MoraleDefendDescription.Details,
                        actor.HasLowMorale ? actor.ParseResolveDetailsFromConstants(false, 0, constants)?.ToString() :
                            (actor.HasHighMorale ? actor.ParseResolveDetailsFromConstants(false, 2, constants)?.ToString() :
                                actor.ParseResolveDetailsFromConstants(false, 1, constants)?.ToString())
                    });
                }
            }


            [HarmonyPatch(typeof(Team), "AddUnit", new Type[] {typeof(AbstractActor)})]
            public static class Team_AddUnit
            {
                public static bool Prepare() => Mod.modSettings.enableResolverator;
                [HarmonyPriority(Priority.Last)]
                public static void Postfix(Team __instance, AbstractActor unit)
                {
                    if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                    //still need to make AI GUID end with aiPilotFlag
                    var p = unit.GetPilot();
                    if (!p.pilotDef.PilotTags.Any(x => x.StartsWith(rGUID)))
                    {
                        p.pilotDef.PilotTags.Add(
                            $"{rGUID}{p.Description.Id}{Guid.NewGuid()}{aiPilotFlag}"); //changed to sys NewGuid instead of simguid for skirmish compatibility
                        Mod.modLog.LogMessage($"Added {p.Callsign} rGUID tag");
                    }

                    var pKey = p.Fetch_rGUID();
                    Mod.modLog.LogMessage($"Fetched {p.Callsign} rGUID");

                    if (PilotResolveTracker.HolderInstance.pilotResolveDict.ContainsKey(pKey)) return;
                    PilotResolveTracker.HolderInstance.pilotResolveDict.Add(pKey, new PilotResolveInfo());
                    Mod.modLog.LogMessage($"{p.Callsign} missing, added to pilotResolveDict and initialized at 0 resolve");

                    if (!PilotResolveTracker.HolderInstance.pilotResolveDict.TryGetValue(pKey,
                            out var actorResolveInfo)) return;

                    var maxMod = unit.StatCollection.GetValue<int>("maxResolveMod");
                    actorResolveInfo.PilotMaxResolve = CombatGameConstants
                        .GetInstance(UnityGameInstance.BattleTechGame)
                        .MoraleConstants.MoraleMax + maxMod;

                    Mod.modLog.LogMessage($"{p.Callsign} Max Resolve: {actorResolveInfo.PilotMaxResolve}. {maxMod} from maxResolveMod");

                    //add actorlink to active abilities
                    foreach (var ability in unit.GetPilot().Abilities)
                    {
                        var component = unit.GenerateDummyActorAbilityComponent();
                        if (component == null) return;
                        ability.parentComponent = component;
                        Framework.Logger.LogTrace($"Team_AddUnit: Ability {ability.Def.Id} added dummy parentComponent with actor link ID {ability.parentComponent.GUID}");
                    }
                }
            }

            [HarmonyPatch(typeof(AttackDirector), "ResolveSequenceMorale",
                new Type[] {typeof(string), typeof(AttackDirector.AttackSequence)})]
            public static class AttackDirector_ResolveSequenceMorale
            {
                public static bool Prepare() => Mod.modSettings.enableResolverator;
                public static bool Prefix(AttackDirector __instance, string VOQueueId,
                    AttackDirector.AttackSequence sequence)
                {
                    if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
#if !NO_CAE 
                    if (Mod.modSettings.disableResolveAttackGround && sequence.isTerrainAttackSequence()) return false;
#endif
                    var attacker = sequence.attacker;
                    var dictionary = new Dictionary<AbstractActor, int> {{attacker, 0}};
                    Team team = attacker.team;
                    MoraleConstantsDef activeMoraleDef =
                        __instance.Combat.Constants.GetActiveMoraleDef(__instance.Combat);
                    bool flag = false;
                    bool flag2 = false;
                    bool flag3 = false;
                    bool flag4 = false;
                    bool flag5 = false;
                    bool flag6 = false;
                    bool flag7 = false;

                    foreach (var t in sequence.allAffectedTargetIds)
                    {
                        ICombatant combatant = __instance.Combat.FindCombatantByGUID(t);
                        if (combatant == null || combatant.UnitType == UnitType.Building) continue;
                        var guid = combatant.GUID;

                        var team2 = combatant.team;

                        var targetActor = combatant as AbstractActor;

                        if (!dictionary.ContainsKey(targetActor))
                        {
                            dictionary.Add(targetActor, 0);
                        }


                        bool flag8 = combatant.team.IsFriendly(sequence.attacker.team);
                        bool flag9 = team == team2;
                        if (!flag && sequence.GetAttackCritsCount(guid) > 0)
                        {
                            flag = true;
                            AttackDirector.attackLogger.Log(
                                $"MORALE: attack caused critical hit (+{activeMoraleDef.ChangeEnemyCrit})");
                            Mod.modLog.LogMessage(
                                $"MORALE: attack caused critical hit (+{activeMoraleDef.ChangeEnemyCrit})");
                            if (!flag8 && !flag9)
                            {
                                PilotResolveTracker.HolderInstance.ModifyPendingMoraleForUnit(ref dictionary, attacker,
                                    activeMoraleDef.ChangeEnemyCrit);
                            }
                        }

                        if (!flag2 && sequence.GetAttackCausedAmmoExplosion(guid))
                        {
                            flag2 = true;
                            AttackDirector.attackLogger.Log(
                                $"MORALE: attack caused ammo explosion (+{activeMoraleDef.ChangeEnemyAmmoExplodes}/{activeMoraleDef.ChangeAllyAmmoExplodes})");
                            Mod.modLog.LogMessage(
                                $"MORALE: attack caused ammo explosion (+{activeMoraleDef.ChangeEnemyAmmoExplodes}/{activeMoraleDef.ChangeAllyAmmoExplodes})");
                            PilotResolveTracker.HolderInstance.ModifyPendingMoraleOverride(ref dictionary, attacker,
                                targetActor,
                                activeMoraleDef.ChangeEnemyAmmoExplodes, activeMoraleDef.ChangeAllyAmmoExplodes,
                                flag8, flag9);
                        }

                        if (!flag3 && sequence.GetAttackDestroyedWeapon(guid))
                        {
                            flag3 = true;
                            AttackDirector.attackLogger.Log(
                                $"MORALE: attack destroyed weapon (+{activeMoraleDef.ChangeEnemyWeaponDestroyed}/{activeMoraleDef.ChangeAllyWeaponDestroyed})");
                            Mod.modLog.LogMessage(
                                $"MORALE: attack destroyed weapon (+{activeMoraleDef.ChangeEnemyWeaponDestroyed}/{activeMoraleDef.ChangeAllyWeaponDestroyed})");
                            PilotResolveTracker.HolderInstance.ModifyPendingMoraleOverride(ref dictionary, attacker,
                                targetActor,
                                activeMoraleDef.ChangeEnemyWeaponDestroyed,
                                activeMoraleDef.ChangeAllyWeaponDestroyed, flag8, flag9);
                        }

                        if (!flag4 && sequence.GetAttackDestroyedAnyLocation(guid))
                        {
                            flag4 = true;
                            AttackDirector.attackLogger.Log(
                                $"MORALE: attack destroyed location (+{activeMoraleDef.ChangeEnemyLocationDestroyed}/{activeMoraleDef.ChangeAllyLocationDestroyed})");
                            Mod.modLog.LogMessage(
                                $"MORALE: attack destroyed location (+{activeMoraleDef.ChangeEnemyLocationDestroyed}/{activeMoraleDef.ChangeAllyLocationDestroyed})");
                            PilotResolveTracker.HolderInstance.ModifyPendingMoraleOverride(ref dictionary, attacker,
                                targetActor,
                                activeMoraleDef.ChangeEnemyLocationDestroyed,
                                activeMoraleDef.ChangeAllyLocationDestroyed, flag8, flag9);
                        }

                        if (sequence.GetArmorDamageDealt(guid) >
                            combatant.StartingArmor * activeMoraleDef.ThresholdMajorArmor)
                        {
                            AttackDirector.attackLogger.Log(
                                $"MORALE: attack damaged more than {activeMoraleDef.ThresholdMajorArmor * 100f}% of starting armor (+{activeMoraleDef.ThresholdMajorArmor * 100f}/{activeMoraleDef.ChangeEnemyMajorArmorDamage})");
                            Mod.modLog.LogMessage(
                                $"MORALE: attack damaged more than {activeMoraleDef.ThresholdMajorArmor * 100f}% of starting armor (+{activeMoraleDef.ThresholdMajorArmor * 100f}/{activeMoraleDef.ChangeEnemyMajorArmorDamage})");
                            PilotResolveTracker.HolderInstance.ModifyPendingMoraleOverride(ref dictionary, attacker,
                                targetActor,
                                activeMoraleDef.ChangeEnemyMajorArmorDamage,
                                activeMoraleDef.ChangeAllyMajorArmorDamage, flag8, flag9);
                        }
                        else if (!flag5 && sequence.GetArmorDamageDealt(guid) >
                            combatant.StartingArmor * activeMoraleDef.ThresholdMinorArmor)
                        {
                            flag5 = true;
                            AttackDirector.attackLogger.Log(
                                $"MORALE: attack damaged more than {activeMoraleDef.ThresholdMinorArmor * 100f}% of starting armor (+{activeMoraleDef.ThresholdMinorArmor * 100f})");
                            Mod.modLog.LogMessage(
                                $"MORALE: attack damaged more than {activeMoraleDef.ThresholdMinorArmor * 100f}% of starting armor (+{activeMoraleDef.ThresholdMinorArmor * 100f})");
                            PilotResolveTracker.HolderInstance.ModifyPendingMoraleOverride(ref dictionary, attacker,
                                targetActor,
                                activeMoraleDef.ChangeEnemyMinorArmorDamage,
                                activeMoraleDef.ChangeAllyMinorArmorDamage, flag8, flag9);
                        }

                        if (!flag6 && sequence.GetAttackCausedKnockdown(guid))
                        {
                            flag6 = true;
                            AttackDirector.attackLogger.Log(
                                $"MORALE: attack caused knockdown (+{activeMoraleDef.ChangeEnemyKnockedDown}/{activeMoraleDef.ChangeAllyKnockedDown})");
                            Mod.modLog.LogMessage(
                                $"MORALE: attack caused knockdown (+{activeMoraleDef.ChangeEnemyKnockedDown}/{activeMoraleDef.ChangeAllyKnockedDown})");
                            PilotResolveTracker.HolderInstance.ModifyPendingMoraleOverride(ref dictionary, attacker,
                                targetActor,
                                activeMoraleDef.ChangeEnemyKnockedDown, activeMoraleDef.ChangeAllyKnockedDown,
                                flag8, flag9);
                        }

                        if (!flag7 && sequence.meleeAttackType == MeleeAttackType.DFA &&
                            sequence.GetAttackDidDamage(guid))
                        {
                            flag7 = true;
                            AttackDirector.attackLogger.Log(
                                $"MORALE: attack was succesful DFA (+{activeMoraleDef.ChangeDFADealt}/{activeMoraleDef.ChangeDFAReceived})");
                            Mod.modLog.LogMessage(
                                $"MORALE: attack was succesful DFA (+{activeMoraleDef.ChangeDFADealt}/{activeMoraleDef.ChangeDFAReceived})");
                            PilotResolveTracker.HolderInstance.ModifyPendingMoraleOverride(ref dictionary, attacker,
                                targetActor, activeMoraleDef.ChangeDFADealt,
                                activeMoraleDef.ChangeDFAReceived, flag8, flag9);
                        }

                        if (!combatant.IsDead) continue;
                        Mech mech = combatant as Mech;
                        Vehicle vehicle = combatant as Vehicle;
                        Turret turret = combatant as Turret;
                        int num = 0;
                        int num2 = 0;
                        if (mech != null)
                        {
                            WeightClass weightClass = mech.weightClass;
                            if (weightClass <= WeightClass.MEDIUM)
                            {
                                if (weightClass != WeightClass.LIGHT)
                                {
                                    if (weightClass == WeightClass.MEDIUM)
                                    {
                                        num = activeMoraleDef.ChangeEnemyDestroyedMedium;
                                        num2 = activeMoraleDef.ChangeAllyDestroyedMedium;
                                    }
                                }
                                else
                                {
                                    num = activeMoraleDef.ChangeEnemyDestroyedLight;
                                    num2 = activeMoraleDef.ChangeAllyDestroyedLight;
                                }
                            }
                            else if (weightClass != WeightClass.HEAVY)
                            {
                                if (weightClass == WeightClass.ASSAULT)
                                {
                                    num = activeMoraleDef.ChangeEnemyDestroyedAssault;
                                    num2 = activeMoraleDef.ChangeAllyDestroyedAssault;
                                }
                            }
                            else
                            {
                                num = activeMoraleDef.ChangeEnemyDestroyedHeavy;
                                num2 = activeMoraleDef.ChangeAllyDestroyedHeavy;
                            }
                        }
                        else if (vehicle != null)
                        {
                            WeightClass weightClass = vehicle.weightClass;
                            if (weightClass <= WeightClass.MEDIUM)
                            {
                                if (weightClass != WeightClass.LIGHT)
                                {
                                    if (weightClass == WeightClass.MEDIUM)
                                    {
                                        num = activeMoraleDef.ChangeEnemyDestroyedMedium;
                                        num2 = activeMoraleDef.ChangeAllyDestroyedMedium;
                                    }
                                }
                                else
                                {
                                    num = activeMoraleDef.ChangeEnemyDestroyedLight;
                                    num2 = activeMoraleDef.ChangeAllyDestroyedLight;
                                }
                            }
                            else if (weightClass != WeightClass.HEAVY)
                            {
                                if (weightClass == WeightClass.ASSAULT)
                                {
                                    num = activeMoraleDef.ChangeEnemyDestroyedAssault;
                                    num2 = activeMoraleDef.ChangeAllyDestroyedAssault;
                                }
                            }
                            else
                            {
                                num = activeMoraleDef.ChangeEnemyDestroyedHeavy;
                                num2 = activeMoraleDef.ChangeAllyDestroyedHeavy;
                            }
                        }
                        else if (turret != null)
                        {
                            num = activeMoraleDef.ChangeEnemyDestroyedLight;
                        }

                        AttackDirector.attackLogger.Log($"MORALE: target killed (+{num}/{num2})");
                        Mod.modLog.LogMessage($"MORALE: target killed (+{num}/{num2})");
                        PilotResolveTracker.HolderInstance.ModifyPendingMoraleOverride(ref dictionary, attacker,
                            targetActor, num, num2, flag8, flag9);
                        if (sequence.isMelee)
                        {
                            AttackDirector.attackLogger.Log(
                                $"MORALE: target killed via melee (+{activeMoraleDef.ChangeEnemyDestroyedMeleeAdditional}/{activeMoraleDef.ChangeAllyDestroyedMeleeAdditional})");
                            Mod.modLog.LogMessage(
                                $"MORALE: target killed via melee (+{activeMoraleDef.ChangeEnemyDestroyedMeleeAdditional}/{activeMoraleDef.ChangeAllyDestroyedMeleeAdditional})");
                            PilotResolveTracker.HolderInstance.ModifyPendingMoraleOverride(ref dictionary, attacker,
                                targetActor,
                                activeMoraleDef.ChangeEnemyDestroyedMeleeAdditional,
                                activeMoraleDef.ChangeAllyDestroyedMeleeAdditional, flag8, flag9);
                        }
                    }


                    var ratioSuccessfulHits = sequence.RatioSuccessfulHits;
#if !NO_CAE
                    if (Mod.modSettings.disableResolveAttackGround)
                    {
                        int attackTotalShotsFired = 0;
                        int attackTotalShotsHit = 0;
                        for (int i = 0; i < sequence.weaponHitInfo.Length; i++)
                        {
                            for (int j = 0; j < sequence.weaponHitInfo[i].Length; j++)
                            {
                                var hitInfo = sequence.weaponHitInfo[i][j];
                                if (hitInfo != null)
                                {
                                    for (int k = 0; k < hitInfo?.hitLocations.Length; k++)
                                    {
                                        var wep = sequence.GetWeapon(i, j);
                                        if (!wep.InstallMineField() && !wep.AOECapable)
                                        {
                                            attackTotalShotsFired++;
                                            if (hitInfo.Value.DidShotHitChosenTarget(k))
                                            {
                                                attackTotalShotsHit++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        Framework.Logger.LogTrace($"[AttackDirector_ResolveSequenceMorale]: Recalculated shots hit for resolve: {attackTotalShotsHit} / {attackTotalShotsFired} vs unmodified {sequence.attackTotalShotsHit} / {sequence.attackTotalShotsFired}");

                        if (attackTotalShotsFired == 0)
                        {
                            attackTotalShotsFired++;
                        }
                        ratioSuccessfulHits = (float) attackTotalShotsHit / attackTotalShotsFired;
                    }

#endif

                    if (ratioSuccessfulHits > activeMoraleDef.ThresholdMajorityHit)
                    {
                        AttackDirector.attackLogger.Log(
                            $"MORALE: attack hit more than {activeMoraleDef.ThresholdMajorityHit * 100f}% of shots (+{activeMoraleDef.ThresholdMajorityHit * 100f})");
                        Mod.modLog.LogMessage(
                            $"MORALE: attack hit more than {activeMoraleDef.ThresholdMajorityHit * 100f}% of shots (+{activeMoraleDef.ThresholdMajorityHit * 100f})");
                        PilotResolveTracker.HolderInstance.ModifyPendingMoraleForUnit(ref dictionary, attacker,
                            activeMoraleDef.ChangeMajorityAttackingShotsHit);
                    }

                    if (dictionary[attacker] == 0 &&
                        ratioSuccessfulHits < activeMoraleDef.ThresholdMajorityMiss)
                    {
                        AttackDirector.attackLogger.Log(
                            $"MORALE: attack missed more than {activeMoraleDef.ThresholdMajorityMiss * 100f}% of shots (+{activeMoraleDef.ThresholdMajorityMiss * 100f})");
                        Mod.modLog.LogMessage(
                            $"MORALE: attack missed more than {activeMoraleDef.ThresholdMajorityMiss * 100f}% of shots (+{activeMoraleDef.ThresholdMajorityMiss * 100f})");

                        PilotResolveTracker.HolderInstance.ModifyPendingMoraleForUnit(ref dictionary, attacker,
                            activeMoraleDef.ChangeMajorityAttackingShotsMiss);
                    }

                    foreach (var unit in dictionary.Keys)
                    {
                        int num3 = dictionary[unit];
                        AttackDirector.attackLogger.Log(string.Format("MORALE: {1} unit change = {0:+#;-#} morale",
                            num3, unit));
                        Mod.modLog.LogMessage(string.Format("MORALE: {1} unit change = {0:+#;-#} morale", num3, unit));
                        if (num3 != 0)
                        {
                            unit.ModifyResolve(sequence, num3);
                        }
                    }

                    return false;
                }
            }

            [HarmonyPatch(typeof(Team), "ApplyBaselineMoraleGain")]
            public static class Team_ApplyBaselineMoraleGain
            {
                public static bool Prepare() => Mod.modSettings.enableResolverator;
                public static bool Prefix(Team __instance, ref List<IStackSequence> __result)
                {
                    {
                        var combat = __instance.Combat;
                        if (combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                        List<IStackSequence> list = new List<IStackSequence>();
                        if (combat.TurnDirector.IsInterleaved &&
                            (combat.Constants.GetActiveMoraleDef(combat).CanAIBeInspired ||
                             !(__instance is AITeam)))
                        {
                            var moraleLogger = Team.moraleLogger;//Traverse.Create(__instance).Field("moraleLogger").GetValue<ILog>();
                            int baselineMoraleGain = __instance.BaselineMoraleGain;
                            if (baselineMoraleGain > 0)
                            {
                                foreach (var unit in __instance.units)
                                {
                                    var baselineUnitMoraleGain =
                                        Mathf.RoundToInt(unit.GetResolveRoundBaseMod());
                                    var totalUnitBaseline = baselineMoraleGain + baselineUnitMoraleGain;
                                    unit.ModifyResolve(totalUnitBaseline);
                                    moraleLogger.Log(
                                        $"MORALE: Unit {unit.DisplayName} gains {totalUnitBaseline} baseline morale from team baseline {baselineMoraleGain} and unit flat bonus {baselineUnitMoraleGain}");
                                    Mod.modLog.LogMessage(
                                        $"MORALE: Unit {unit.DisplayName} gains {totalUnitBaseline} baseline morale from team baseline {baselineMoraleGain} and unit flat bonus {baselineUnitMoraleGain}");
                                }

                                if (__instance == combat.LocalPlayerTeam)
                                {
                                    list.Add(new DelaySequence(combat, 1f));
                                }
                            }
                            else
                            {
                                moraleLogger.Log($"MORALE: team {__instance.DisplayName} gains 0 baseline morale");
                                Mod.modLog.LogMessage(
                                    $"MORALE: team {__instance.DisplayName} gains 0 baseline morale");
                            }
                        }

                        __result = list;
                    }
                    return false;
                }
            }

            //public static MethodInfo _CHMB_RefreshMoraleBarTarget = AccessTools.Method(typeof(CombatHUDMoraleBar), "RefreshMoraleBarTarget");
            //public static MethodInfo _CHMB_Update = AccessTools.Method(typeof(CombatHUDMoraleBar), "Update");

            [HarmonyPatch(typeof(CombatHUD), "OnActorSelected",
                new Type[] {typeof(AbstractActor)})]
            public static class CombatHUD_OnActorSelected_Patch
            {
                public static bool Prepare() => Mod.modSettings.enableResolverator;
                public static void Postfix(CombatHUD __instance, AbstractActor actor)
                {
                    if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                    var tray = __instance.MechWarriorTray;
                    CombatHUDMoraleBarInstance.CHMB = tray.moraleDisplay;//Traverse.Create(tray).Property("moraleDisplay").GetValue<CombatHUDMoraleBar>();
                    //var CHMB = Traverse.Create(tray).Property("moraleDisplay").GetValue<CombatHUDMoraleBar>();

                    //_CHMB_RefreshMoraleBarTarget.Invoke(CombatHUDMoraleBarInstance.CHMB, new object[] {true });
                    tray.moraleDisplay.RefreshMoraleBarTarget(true);
                    Mod.modLog.LogMessage($"Invoked CHMB RefreshMoraleBarTarget");
                    tray.moraleDisplay.Update();
                    //_CHMB_Update.Invoke(CombatHUDMoraleBarInstance.CHMB, new object[] { });
                    Mod.modLog.LogMessage($"Invoked CHMB Update");


                }
            }

            [HarmonyPatch(typeof(CombatHUDMechwarriorTray), "Init",
                new Type[] {typeof(CombatGameState), typeof(CombatHUD)})]
            public static class CombatHUDMechwarriorTray_Init_Patch2
            {
                public static bool Prepare() => Mod.modSettings.enableResolverator || Mod.modSettings.cleanUpCombatUI;
                public static void Postfix(CombatHUDMechwarriorTray __instance, CombatGameState Combat, CombatHUD HUD)
                {
                    if (!Mod.modSettings.cleanUpCombatUI || !Mod.modSettings.usingCACabilitySelector) return;
                    GameObject.Find("AT_OuterFrameL").SetActive(false);
                    GameObject.Find("AT_OuterFrameR").SetActive(false);
                    GameObject.Find("braceL (1)").SetActive(false);
                    GameObject.Find("braceR (1)").SetActive(false);
                    GameObject.Find("actionButton_DLine1").SetActive(false);
                    GameObject.Find("actionButton_DLine2").SetActive(false);
                    GameObject.Find("actionButton_DLine3").SetActive(false);

                    // THIS IS WHERE I'll add new buttons? or dont if we want them in Active Abilities menu pooper.
                }
            }

            [HarmonyPatch(typeof(CombatHUDMechwarriorTray), "ResetAbilityButton",
                new Type[] { typeof(AbstractActor), typeof(CombatHUDActionButton), typeof(Ability), typeof(bool) })]
            public static class CombatHUDMechwarriorTray_ResetAbilityButton_Patch
            {

                public static bool Prefix(CombatHUDMechwarriorTray __instance, AbstractActor actor, CombatHUDActionButton button, Ability ability, bool forceInactive)
                {
                    bool flag = ability == null;
                    if (!flag)
                    {
                        if (forceInactive)
                        {
                            button.DisableButton();
                        }
                        else
                        {
                            bool isAbilityActivated = button.IsAbilityActivated;
                            if (isAbilityActivated)
                            {
                                button.ResetButtonIfNotActive(actor);
                            }
                            else
                            {
                                bool flag2 = !ability.IsAvailable;
                                if (flag2)
                                {
                                    button.DisableButton();
                                }
                                else
                                {
                                    bool flag3 = false;
                                    bool flag4 = false;
                                    bool flag5 = ability.Def.ActivationTime == AbilityDef.ActivationTiming.ConsumedByFiring;
                                    bool flag6 = actor.HasActivatedThisRound || (!actor.IsAvailableThisPhase && actor.Combat.TurnDirector.IsInterleaved) || actor.MovingToPosition != null || (actor.Combat.StackManager.IsAnyOrderActive && actor.Combat.TurnDirector.IsInterleaved);
                                    if (flag6)
                                    {
                                        button.DisableButton();
                                    }
                                    else
                                    {
                                        bool isShutDown = actor.IsShutDown;
                                        if (isShutDown)
                                        {
                                            bool flag7 = !flag3;
                                            if (flag7)
                                            {
                                                button.DisableButton();
                                            }
                                            else
                                            {
                                                button.ResetButtonIfNotActive(actor);
                                            }
                                        }
                                        else
                                        {
                                            bool isProne = actor.IsProne;
                                            if (isProne)
                                            {
                                                bool flag8 = !flag4;
                                                if (flag8)
                                                {
                                                    button.DisableButton();
                                                }
                                                else
                                                {
                                                    button.ResetButtonIfNotActive(actor);
                                                }
                                            }
                                            else
                                            {
                                                bool flag9 = (actor.HasFiredThisRound) && ability.Def.ActivationTime == AbilityDef.ActivationTiming.ConsumedByFiring;
                                                if (flag9)
                                                {
                                                    button.DisableButton();
                                                }
                                                else
                                                {
                                                    bool hasMovedThisRound = actor.HasMovedThisRound;
                                                    if (hasMovedThisRound)
                                                    {
                                                        bool flag10 = flag5;
                                                        if (flag10)
                                                        {
                                                            button.ResetButtonIfNotActive(actor);
                                                        }
                                                        else
                                                        {
                                                            button.DisableButton();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        button.ResetButtonIfNotActive(actor);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return false;
                }

                public static void Postfix(CombatHUDMechwarriorTray __instance, AbstractActor actor, CombatHUDActionButton button, Ability ability, bool forceInactive)
                {
                    if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                    if (actor == null || ability == null) return;
                    if (!Mod.modSettings.enableResolverator)
                    {
                        if (actor.team.Morale < ability.Def.getAbilityDefExtension().ResolveCost)
                        {
                            button.DisableButton();
                        }

                        var moraleBar = __instance.moraleDisplay;//Traverse.Create(__instance).Property("moraleDisplay").GetValue<CombatHUDMoraleBar>();
                        var predicting = moraleBar.predicting;//Traverse.Create(moraleBar).Field("predicting").GetValue<bool>();
                        if (predicting)
                        {
                            var predictWidth = moraleBar.predictWidth;//Traverse.Create(moraleBar).Field("predictWidth").GetValue<float>();
                            if (Mathf.RoundToInt(predictWidth) < ability.Def.getAbilityDefExtension().ResolveCost)
                            {
                                button.DisableButton();
                            }
                        }
                    }
                    else
                    {
                        var actorKey = actor.GetPilot().Fetch_rGUID();
                        if (!PilotResolveTracker.HolderInstance.pilotResolveDict.TryGetValue(actorKey, out var pilotResolveInfo))
                            return;
                        if (pilotResolveInfo.PilotResolve < Mathf.RoundToInt(ability.Def.getAbilityDefExtension().ResolveCost * actor.GetResolveCostBaseMult()))
                        {
                            button.DisableButton();
                        }

                        if (pilotResolveInfo.Predicting && pilotResolveInfo.PredictedResolve <
                            Mathf.RoundToInt(ability.Def.getAbilityDefExtension().ResolveCost * actor.GetResolveCostBaseMult()))
                        {
                            button.DisableButton();
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(CombatHUDWeaponPanel), "ResetAbilityButton",
                new Type[] { typeof(AbstractActor), typeof(CombatHUDActionButton), typeof(Ability), typeof(bool) })]
            public static class CombatHUDWeaponPanel_ResetAbilityButton_Patch
            {
                
                public static bool Prefix(CombatHUDWeaponPanel __instance, AbstractActor actor, CombatHUDActionButton button, Ability ability, bool forceInactive)
                {
                    bool flag = ability == null;
                    if (!flag)
                    {
                        if (forceInactive)
                        {
                            button.DisableButton();
                        }
                        else
                        {
                            bool isAbilityActivated = button.IsAbilityActivated;
                            if (isAbilityActivated)
                            {
                                button.ResetButtonIfNotActive(actor);
                            }
                            else
                            {
                                bool flag2 = !ability.IsAvailable;
                                if (flag2)
                                {
                                    button.DisableButton();
                                }
                                else
                                {
                                    bool flag3 = false;
                                    bool flag4 = false;
                                    bool flag5 = ability.Def.ActivationTime == AbilityDef.ActivationTiming.ConsumedByFiring;
                                    bool flag6 = actor.HasActivatedThisRound || (!actor.IsAvailableThisPhase && actor.Combat.TurnDirector.IsInterleaved) || actor.MovingToPosition != null || (actor.Combat.StackManager.IsAnyOrderActive && actor.Combat.TurnDirector.IsInterleaved);
                                    if (flag6)
                                    {
                                        button.DisableButton();
                                    }
                                    else
                                    {
                                        bool isShutDown = actor.IsShutDown;
                                        if (isShutDown)
                                        {
                                            bool flag7 = !flag3;
                                            if (flag7)
                                            {
                                                button.DisableButton();
                                            }
                                            else
                                            {
                                                button.ResetButtonIfNotActive(actor);
                                            }
                                        }
                                        else
                                        {
                                            bool isProne = actor.IsProne;
                                            if (isProne)
                                            {
                                                bool flag8 = !flag4;
                                                if (flag8)
                                                {
                                                    button.DisableButton();
                                                }
                                                else
                                                {
                                                    button.ResetButtonIfNotActive(actor);
                                                }
                                            }
                                            else
                                            {
                                                bool flag9 = (actor.HasFiredThisRound) && ability.Def.ActivationTime == AbilityDef.ActivationTiming.ConsumedByFiring;
                                                if (flag9)
                                                {
                                                    button.DisableButton();
                                                }
                                                else
                                                {
                                                    bool hasMovedThisRound = actor.HasMovedThisRound;
                                                    if (hasMovedThisRound)
                                                    {
                                                        bool flag10 = flag5;
                                                        if (flag10)
                                                        {
                                                            button.ResetButtonIfNotActive(actor);
                                                        }
                                                        else
                                                        {
                                                            button.DisableButton();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        button.ResetButtonIfNotActive(actor);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return false;
                }
                public static void Postfix(CombatHUDWeaponPanel __instance, AbstractActor actor, CombatHUDActionButton button, Ability ability, bool forceInactive)
                {
                    if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                    if (actor == null || ability == null) return;
                    if (!Mod.modSettings.enableResolverator)
                    {
                        if (actor.team.Morale < ability.Def.getAbilityDefExtension().ResolveCost)
                        {
                            button.DisableButton();
                        }
                        
                        var moraleBar = __instance.HUD.MechWarriorTray.moraleDisplay;// Traverse.Create(__instance).Property("moraleDisplay").GetValue<CombatHUDMoraleBar>();
                        var predicting = moraleBar.predicting;//Traverse.Create(moraleBar).Field("predicting").GetValue<bool>();
                        if (predicting)
                        {
                            var predictWidth = moraleBar.predictWidth;//Traverse.Create(moraleBar).Field("predictWidth").GetValue<float>();
                            if (Mathf.RoundToInt(predictWidth) < ability.Def.getAbilityDefExtension().ResolveCost)
                            {
                                button.DisableButton();
                            }
                        }
                    }
                    else
                    {
                        var actorKey = actor.GetPilot().Fetch_rGUID();
                        if (!PilotResolveTracker.HolderInstance.pilotResolveDict.TryGetValue(actorKey, out var pilotResolveInfo))
                            return;
                        if (pilotResolveInfo.PilotResolve < Mathf.RoundToInt(ability.Def.getAbilityDefExtension().ResolveCost * actor.GetResolveCostBaseMult()))
                        {
                            button.DisableButton();
                        }

                        if (pilotResolveInfo.Predicting && pilotResolveInfo.PredictedResolve <
                            Mathf.RoundToInt(ability.Def.getAbilityDefExtension().ResolveCost * actor.GetResolveCostBaseMult()))
                        {
                            button.DisableButton();
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(CombatHUDMechwarriorTray), "ResetMechwarriorButtons",
                new Type[] {typeof(AbstractActor)})]
            public static class CombatHUDMechwarriorTray_ResetMechwarriorButtons_Patch
            {
                public static bool Prepare() => Mod.modSettings.enableResolverator;
                public static void Postfix(CombatHUDMechwarriorTray __instance, AbstractActor actor)
                {
                    if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                    if (actor == null) return;
                    var abilityButtons = __instance.AbilityButtons;//Traverse.Create(__instance).Property("AbilityButtons").GetValue<CombatHUDActionButton[]>();
                    var moraleButtons = __instance.MoraleButtons;//Traverse.Create(__instance).Property("MoraleButtons").GetValue<CombatHUDActionButton[]>();
                    var activeMoraleDef = __instance.Combat.Constants.GetActiveMoraleDef(__instance.Combat);
                    var actorKey = actor.GetPilot().Fetch_rGUID();
                    if (!PilotResolveTracker.HolderInstance.pilotResolveDict.TryGetValue(actorKey, out var pilotResolveInfo))
                        return;
                    bool flag2 = activeMoraleDef.UseOffensivePush &&
                                 (pilotResolveInfo.PilotResolve >= Mathf.RoundToInt(actor.OffensivePushCost * actor.GetResolveCostBaseMult()) && (!pilotResolveInfo.Predicting || pilotResolveInfo.PredictedResolve >= Mathf.RoundToInt(actor.OffensivePushCost * actor.GetResolveCostBaseMult())) || Mathf.RoundToInt(actor.OffensivePushCost * actor.GetResolveCostBaseMult()) <= 0);
                    bool flag3 = activeMoraleDef.UseDefensivePush &&
                                 (pilotResolveInfo.PilotResolve >= Mathf.RoundToInt(actor.DefensivePushCost * actor.GetResolveCostBaseMult()) && (!pilotResolveInfo.Predicting || pilotResolveInfo.PredictedResolve >= Mathf.RoundToInt(actor.DefensivePushCost * actor.GetResolveCostBaseMult())) || Mathf.RoundToInt(actor.DefensivePushCost * actor.GetResolveCostBaseMult()) <= 0);
                    var flag5 = false;

                    foreach (var t in abilityButtons)
                    {
                        if (!t.IsAbilityActivated) continue;
                        if (t.Ability.Def.Resource == AbilityDef.ResourceConsumed.ConsumesFiring ||
                            t.Ability.Def.Resource == AbilityDef.ResourceConsumed.ConsumesActivation)
                        {
                            flag5 = true;
                        }
                    }

                    if (!actor.IsProne && actor.IsOperational && !actor.HasFiredThisRound &&
                        __instance.Combat.TurnDirector.IsInterleaved && !flag5)
                    {
                        if (flag2)
                        {
                            moraleButtons[0].ResetButtonIfNotActive(actor);
                            moraleButtons[0].isAutoHighlighted =
                                __instance.Combat.LocalPlayerTeam.Morale == activeMoraleDef.MoraleMax;
                        }
                        else
                        {
                            moraleButtons[0].DisableButton();
                            moraleButtons[0].isAutoHighlighted = false;
                        }

                        if (flag3)
                        {
                            moraleButtons[1].ResetButtonIfNotActive(actor);
                            if (__instance.Combat.LocalPlayerTeam.Morale == activeMoraleDef.MoraleMax)
                            {
                                moraleButtons[1].isAutoHighlighted = true;
                            }
                            else
                            {
                                moraleButtons[1].isAutoHighlighted = false;
                            }
                        }
                        else
                        {
                            moraleButtons[1].DisableButton();
                            moraleButtons[1].isAutoHighlighted = false;
                        }

                    }
                }
            }

            [HarmonyPatch(typeof(AbstractActor), "InitEffectStats")]
            public static class AbstractActor_InitEffectStats_Patch
            {
                public static void Prefix(AbstractActor __instance)
                {
                    if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                    //__instance.StatCollection.AddStatistic<float>("resolveGenTacticsMult", Mod.modSettings.resolveGenTacticsMult);
                    //__instance.StatCollection.AddStatistic<float>("resolveCostTacticsMult", Mod.modSettings.resolveCostTacticsMult);

                    __instance.StatCollection.AddStatistic<float>("resolveGenBaseMult",
                        Mod.modSettings.resolveGenBaseMult);
                    __instance.StatCollection.AddStatistic<float>("resolveCostBaseMult",
                        Mod.modSettings.resolveCostBaseMult);
                    __instance.StatCollection.AddStatistic<float>("resolveRoundBaseMod",
                        0f);

                    __instance.StatCollection.AddStatistic<int>("maxResolveMod", 0);

                    Mod.modLog.LogMessage($"Added actor stats to {__instance.GetPilot().Callsign}: resolveGenBaseMult, resolveCostBaseMult, resolveRoundBaseMod, maxResolveMod");
                }
            }

            [HarmonyPatch(typeof(CombatHUDMoraleBar), "RefreshMoraleBarTarget", new Type[] {typeof(bool)})]
            public static class CombatHUDMoraleBar_RefreshMoraleBarTarget
            {
                public static bool Prepare() => Mod.modSettings.enableResolverator;
                public static bool Prefix(CombatHUDMoraleBar __instance, bool forceRefresh)
                {
                    if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                    var selectedUnitFromTraverse = __instance.HUD.selectedUnit;// Traverse.Create(___HUD).Field("selectedUnit").GetValue<AbstractActor>();
                    if (selectedUnitFromTraverse == null)
                    {
                        return true;
                    }

                    __instance.showMoraleAsPercent = false;
                    var pilot = selectedUnitFromTraverse.GetPilot();
                    var actorKey = pilot.Fetch_rGUID();
                    if (!PilotResolveTracker.HolderInstance.pilotResolveDict.TryGetValue(actorKey, out var pilotResolveInfo))
                        return true;
                    __instance.maxMorale = pilotResolveInfo.PilotMaxResolve;
                    Mod.modLog.LogMessage($"{pilot.Callsign} ___maxMorale set to {pilotResolveInfo.PilotMaxResolve}");
                    Mod.modLog.LogMessage($"{pilot.Callsign} current Resolve is {pilotResolveInfo.PilotResolve}");

                    int num = pilotResolveInfo.PilotResolve - __instance.moralePrevious;
                    Framework.Logger.LogTrace($"RMBT: {pilot.Callsign} old resolve - {__instance.moralePrevious} = {num}");
                    if (num != 0 || forceRefresh)
                    {
                        float num2 = (float) pilotResolveInfo.PilotResolve;
                        if (__instance.moraleBar != null)
                        {
                            __instance.moraleBarPreviousWidth = __instance.moraleBar.rect.width;
                            float num3 = num2 / (float) __instance.maxMorale;
                            __instance.moraleBarTargetWidth = __instance.moraleBarMaxWidth * num3;
                            Framework.Logger.LogTrace($"RMBT: {pilot.Callsign} set ___moraleBarTargetWidth to {__instance.moraleBarTargetWidth}");
                            if (num2 >= (float) __instance.maxMorale)
                            {
                                __instance.moraleTweens.SetState(ButtonState.Highlighted, true);
                            }
                            else if (!Mathf.Approximately(__instance.moraleBarPreviousWidth, __instance.moraleBarTargetWidth))
                            {
                                __instance.moraleBarTimeLerping = 0f;
                                __instance.moraleTweens.SetState(ButtonState.Enabled, true);
                            }

                            var floatToaster = __instance.FloatieToaster;//Traverse.Create(__instance).Property("FloatieToaster").GetValue<CombatHUDFloatieStack>();
                            var moraleGainDesc = __instance.MoraleGainDescription;//Traverse.Create(__instance).Property("MoraleGainDescription").GetValue<string>();

                            floatToaster.AddFloatie(new Localize.Text(moraleGainDesc, new object[]
                            {
                                num
                            }), (num > 0) ? FloatieMessage.MessageNature.Buff : FloatieMessage.MessageNature.Neutral);

                            //var getMoraleDescMethod = Traverse.Create(__instance).Method("GetMoraleDescription",new object[] {pilotResolveInfo.PilotResolve, __instance.maxMorale });
                            var GottentMoraleDesc = __instance.GetMoraleDescription(pilotResolveInfo.PilotResolve, __instance.maxMorale);//getMoraleDescMethod.GetValue<string>());
                            __instance.HoverText.SetText(GottentMoraleDesc, Array.Empty<object>());
                        }
                        else
                        {
                            CombatHUDMoraleBar.uiLogger.LogWarning($"No morale bar in UI! new morale is {num2}");
                            Mod.modLog.LogMessage($"No morale bar in UI! new morale is {num2}");
                        }

                        __instance.moralePrevious = pilotResolveInfo.PilotResolve;
                        Framework.Logger.LogTrace($"RMBT: {pilot.Callsign} pilot resolve set to {__instance.moralePrevious} (___moralePrevious)");
                    }

                    return false;
                }
            }


            [HarmonyPatch(typeof(CombatHUDMoraleBar), "UpdateMoraleBar")]
            public static class CombatHUDMoraleBar_UpdateMoraleBar
            {
                //public static bool Prepare() => Mod.modSettings.enableResolverator;
                public static bool Prefix(CombatHUDMoraleBar __instance)
                {
                    if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                    var selectedUnitFromTraverse = __instance.HUD.selectedUnit;// Traverse.Create(___HUD).Field("selectedUnit").GetValue<AbstractActor>();
                    if (selectedUnitFromTraverse == null)
                    {
                        return true; // display not changing for some damn reason, but morale tracking publicly is working. wtf.
                    }

                    PilotResolveInfo pilotResolveInfo = new PilotResolveInfo();
                    if (Mod.modSettings.enableResolverator)
                    {
                        var pKey = selectedUnitFromTraverse.GetPilot().Fetch_rGUID();
                        PilotResolveTracker.HolderInstance.pilotResolveDict.TryGetValue(pKey, out pilotResolveInfo);
                        if (pilotResolveInfo != null) __instance.maxMorale = pilotResolveInfo.PilotMaxResolve;
                    }

                    __instance.width = __instance.moraleBarTargetWidth;

                    Framework.Logger.LogTrace($"TRACE: Moralebar max height for {selectedUnitFromTraverse.GetPilot().Callsign}: {__instance.maxMorale}, width set to {__instance.width}");

                    __instance.lerping = false;
                    if (__instance.moraleBarTimeLerping < __instance.moraleBarLerpTime)
                    {
                        __instance.lerping = true;
                        __instance.moraleBarTimeLerping += Time.deltaTime;
                        if (__instance.moraleBar != null)
                        {
                            __instance.timeFactor = __instance.moraleBarTimeLerping / __instance.moraleBarLerpTime;
                            if (__instance.timeFactor < 1f)
                            {
                                __instance.width = Mathf.SmoothStep(__instance.moraleBarPreviousWidth, __instance.moraleBarTargetWidth,
                                    __instance.timeFactor);
                            }
                            else
                            {
                                __instance.lerping = false;
                            }
                        }
                    }

                    __instance.predictWidth = __instance.width;
                    __instance.predicting = false;
                    if (Mod.modSettings.enableResolverator && pilotResolveInfo != null)
                    {
                        pilotResolveInfo.PredictedResolve = Mathf.RoundToInt(__instance.predictWidth);
                        pilotResolveInfo.Predicting = false;
                    }
                    

                    if (__instance.HUD.SelectionHandler.ActiveState != null)
                    {
                        if (__instance.HUD.SelectionHandler.ActiveState.SelectionType == SelectionType.ConfirmMorale)
                        {
                            __instance.predictWidth -= Mathf.RoundToInt(selectedUnitFromTraverse.DefensivePushCost * selectedUnitFromTraverse.GetResolveCostBaseMult()) / (float) __instance.maxMorale *
                                               __instance.moraleBarMaxWidth;
                            __instance.predictWidth = Mathf.Max(0f, __instance.predictWidth);
                            
                            __instance.predicting = true;
                            Framework.Logger.LogTrace($"TRACE: Moralebar for {selectedUnitFromTraverse.GetPilot().Callsign}: predicting width for SelectionType.ConfirmMorale (vigilance): {__instance.predictWidth}");
                        }
                        else if (__instance.HUD.SelectionHandler.ActiveState.SelectionType == SelectionType.FireMorale)
                        {
                            __instance.predictWidth -= Mathf.RoundToInt(selectedUnitFromTraverse.OffensivePushCost * selectedUnitFromTraverse.GetResolveCostBaseMult()) / (float) __instance.maxMorale *
                                               __instance.moraleBarMaxWidth;
                            __instance.predictWidth = Mathf.Max(0f, __instance.predictWidth);
                            
                            __instance.predicting = true;
                            Framework.Logger.LogTrace($"TRACE: Moralebar for {selectedUnitFromTraverse.GetPilot().Callsign}: predicting width for SelectionType.FireMorale (called shot): {__instance.predictWidth}");
                        }
                        else if (__instance.HUD.SelectionHandler.ActiveState.SelectionType == SelectionType.MWInstant)
                        {
                            var cost = __instance.HUD.SelectionHandler.ActiveState.FromButton.Ability.Def
                                .getAbilityDefExtension().ResolveCost;
                            if (Mod.modSettings.enableResolverator)
                                cost = Mathf.RoundToInt(cost * selectedUnitFromTraverse.GetResolveCostBaseMult());
                            __instance.predictWidth -= cost / (float) __instance.maxMorale *
                                                       __instance.moraleBarMaxWidth;
                            __instance.predictWidth = Mathf.Max(0f, __instance.predictWidth);
                            
                            __instance.predicting = true;
                            Framework.Logger.LogTrace($"TRACE: Moralebar for {selectedUnitFromTraverse.GetPilot().Callsign}: predicting width for other ability with morale cost: {__instance.predictWidth}");
                        }
                        else if (__instance.HUD.SelectionHandler.ActiveState.SelectionType == SelectionType.CommandTargetTwoPoints || __instance.HUD.SelectionHandler.ActiveState.SelectionType == SelectionType.CommandSpawnTarget || __instance.HUD.SelectionHandler.ActiveState.SelectionType == SelectionType.CommandBase || __instance.HUD.SelectionHandler.ActiveState.SelectionType == SelectionType.CommandInstant)
                        {
                            var cost = __instance.HUD.SelectionHandler.ActiveState.FromButton.Ability.Def
                                .getAbilityDefExtension().ResolveCost;
                            if (Mod.modSettings.enableResolverator)
                                cost = Mathf.RoundToInt(cost * selectedUnitFromTraverse.GetResolveCostBaseMult());
                            __instance.predictWidth -= cost / (float)__instance.maxMorale *
                                               __instance.moraleBarMaxWidth;
                            __instance.predictWidth = Mathf.Max(0f, __instance.predictWidth);
                            
                            __instance.predicting = true;
                            Framework.Logger.LogTrace($"TRACE: Moralebar for {selectedUnitFromTraverse.GetPilot().Callsign}: predicting width for other ability with morale cost: {__instance.predictWidth}");
                        }
                    }

                    if (__instance.predicting)
                    {
                        if (!__instance.moralePrediction.gameObject.activeSelf)
                        {
                            __instance.moralePrediction.gameObject.SetActive(true);
                        }

                        if (Mod.modSettings.enableResolverator)
                        {
                            pilotResolveInfo.PredictedResolve = Mathf.RoundToInt(__instance.predictWidth);
                            pilotResolveInfo.Predicting = true;
                        }
                        __instance.predictColor = __instance.moralePredictGraphic.color;
                        __instance.predictColor.a = Mathf.Sin(Time.realtimeSinceStartup * 5f) * 0.35f + 0.65f;
                        __instance.moralePredictGraphic.color = __instance.predictColor;
                        __instance.moralePrediction.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, __instance.width);
                        __instance.moraleBar?.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, __instance.predictWidth);
                        __instance.moraleTweens.SetState(ButtonState.Disabled, false);
                    }
                    else
                    {
                        if (__instance.moralePrediction.gameObject.activeSelf)
                        {
                            __instance.moralePrediction.gameObject.SetActive(false);
                        }

                        __instance.moraleBar?.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, __instance.width);
                    }

                    if (!__instance.lerping && !__instance.predicting)
                    {
                        __instance.ResetTweens();
                        //var resetTweens = Traverse.Create(__instance).Method("ResetTweens");
                        //resetTweens.GetValue();
                    }

                    return false;
                }
            }


            [HarmonyPatch(typeof(MoraleDefendSequence), "OnAdded")]
            public static class MoraleDefendSequence_OnAdded_Patch
            {
                public static bool Prepare() => Mod.modSettings.enableResolverator;
                public static void Prefix(MoraleDefendSequence __instance)
                {
                    if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                    var actor = __instance.owningActor;

                    __instance.owningActor.team.ModifyMorale(Mathf.RoundToInt(actor.DefensivePushCost * actor.GetResolveCostBaseMult()) *
                                                             1); // this negates Team Morale loss from vanilla method

                    actor.ModifyResolve(Mathf.RoundToInt(actor.DefensivePushCost) * -1);
                    //_CHMB_RefreshMoraleBarTarget.Invoke(CombatHUDMoraleBarInstance.CHMB, new object[] {true });
                    CombatHUDMoraleBarInstance.CHMB.RefreshMoraleBarTarget(true);
                    Mod.modLog.LogMessage($"Invoked CHMB RefreshMoraleBarTarget");
                    CombatHUDMoraleBarInstance.CHMB.Update();
                    //_CHMB_Update.Invoke(CombatHUDMoraleBarInstance.CHMB, new object[] { });
                    Mod.modLog.LogMessage($"Invoked CHMB Update");
                }
            }

            [HarmonyPatch(typeof(AttackStackSequence), "OnAdded")]
            public static class AttackStackSequence_OnAdded_Patch
            {   
                public static bool Prepare() => Mod.modSettings.enableResolverator;
                public static void Prefix(AttackStackSequence __instance)
                {
                    if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                    if (!__instance.isMoraleAttack) return;
                    var actor = __instance.owningActor;

                    __instance.owningActor.team.ModifyMorale(Mathf.RoundToInt(actor.OffensivePushCost * actor.GetResolveCostBaseMult()) *
                                                             1); // this negates Team Morale loss from vanilla method

                    actor.ModifyResolve(Mathf.RoundToInt(actor.OffensivePushCost) * -1);
                }
            }

            [HarmonyPatch(typeof(AbstractActor), "CanUseOffensivePush")]
            public static class AbstractActor_CanUseOffensivePush_Patch
            {
                public static bool Prepare() => Mod.modSettings.enableResolverator;
                public static bool Prefix(AbstractActor __instance, ref bool __result)
                {
                    if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                    var pKey = __instance.GetPilot().Fetch_rGUID();

                    if (!PilotResolveTracker.HolderInstance.pilotResolveDict.TryGetValue(pKey, out var pilotResolveInfo)) return true;
                    
                    __result = __instance.Combat.Constants.GetActiveMoraleDef(__instance.Combat).UseOffensivePush &&
                               (pilotResolveInfo.PilotResolve >= Mathf.RoundToInt(__instance.OffensivePushCost * __instance.GetResolveCostBaseMult()) || Mathf.RoundToInt(__instance.OffensivePushCost * __instance.GetResolveCostBaseMult()) <= 0);
                    return false;
                }
            }

            [HarmonyPatch(typeof(CombatHUDActionButton), "ActivateAbility", new Type[]{typeof(string), typeof(string)})]
            public static class CombatHUDActionButton_ActivateAbility_Confirmed
            {
                //public static bool Prepare() => Mod.modSettings.enableResolverator;

                public static void Postfix(CombatHUDActionButton __instance, string creatorGUID, string targetGUID)
                {
                    var combat = UnityGameInstance.BattleTechGame.Combat;
                    if (combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                    Mod.modLog.LogMessage($"Processing resolve costs for {__instance.Ability.Def.Description.Name}");
                    var HUD = __instance.HUD;//Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                    var theActor = HUD.SelectedActor ?? combat.FindActorByGUID(creatorGUID);
                    if (theActor == null) return;
                    if (!Mod.modSettings.enableResolverator)
                    {
                        var amt = -Mathf.RoundToInt(__instance.Ability.Def.getAbilityDefExtension().ResolveCost);
                        theActor.team.ModifyMorale(amt);
                    }
                    else
                    {
                        var amt = -Mathf.RoundToInt(__instance.Ability.Def.getAbilityDefExtension().ResolveCost);
                        theActor.ModifyResolve(amt);
                    }

                    HUD.MechWarriorTray.ResetMechwarriorButtons(theActor);

                    if (!Mod.modSettings.disableCalledShotExploit) return;
                    var selectionStack = HUD.selectionHandler.SelectionStack;//Traverse.Create(HUD.SelectionHandler).Property("SelectionStack").GetValue<List<SelectionState>>();
                    var moraleState = selectionStack.FirstOrDefault(x => x is SelectionStateMoraleAttack);
                    if (moraleState != null)
                    {
                        moraleState.OnInactivate();
                        moraleState.OnRemoveFromStack();
                        selectionStack.Remove(moraleState);
                    }
                }
            }

            [HarmonyPatch(typeof(CombatHUDActionButton), "ActivateAbility", new Type[] {})]
            public static class CombatHUDActionButton_ActivateAbility_noparams
            {
                public static bool Prepare() => Mod.modSettings.enableResolverator && false;

                public static void Postfix(CombatHUDActionButton __instance)
                {
                    var combat = UnityGameInstance.BattleTechGame.Combat;
                    if (combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                    Mod.modLog.LogMessage($"Processing resolve costs for {__instance.Ability.Def.Description.Name}");
                    var HUD = __instance.HUD;//Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                    var theActor = HUD.SelectedActor;
                    if (theActor == null) return;
                    if (!Mod.modSettings.enableResolverator)
                    {
                        var amt = -Mathf.RoundToInt(__instance.Ability.Def.getAbilityDefExtension().ResolveCost);
                        theActor.team.ModifyMorale(amt);
                    }
                    else
                    {
                        var amt = -Mathf.RoundToInt(__instance.Ability.Def.getAbilityDefExtension().ResolveCost);
                        theActor.ModifyResolve(amt);
                    }

                    HUD.MechWarriorTray.ResetMechwarriorButtons(theActor);

                    if (!Mod.modSettings.disableCalledShotExploit) return;
                    var selectionStack = HUD.selectionHandler.SelectionStack;//Traverse.Create(HUD.SelectionHandler).Property("SelectionStack").GetValue<List<SelectionState>>();
                    var moraleState = selectionStack.FirstOrDefault(x => x is SelectionStateMoraleAttack);
                    if (moraleState != null)
                    {
                        moraleState.OnInactivate();
                        moraleState.OnRemoveFromStack();
                        selectionStack.Remove(moraleState);
                    }
                }
            }

            [HarmonyPatch(typeof(CombatHUDActionButton), "ActivateCommandAbility", new Type[] { typeof(string), typeof(Vector3), typeof(Vector3) })]
            public static class CombatHUDActionButton_ActivateCommandAbility_Confirmed
            {
                //public static bool Prepare() => Mod.modSettings.enableResolverator;

                public static void Postfix(CombatHUDActionButton __instance, string teamGUID, Vector3 positionA, Vector3 positionB)
                {

                    if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                    Mod.modLog.LogMessage($"Processing resolve costs for {__instance.Ability.Def.Description.Name}");
                    var HUD = __instance.HUD;
                    var theActor = HUD.SelectedActor;
                    if (theActor == null) return;
                   if (!Mod.modSettings.enableResolverator)
                    {
                        var amt = -Mathf.RoundToInt(__instance.Ability.Def.getAbilityDefExtension().ResolveCost);
                        theActor.team.ModifyMorale(amt);
                    }
                    else
                    {
                        var amt = -Mathf.RoundToInt(__instance.Ability.Def.getAbilityDefExtension().ResolveCost);
                        theActor.ModifyResolve(amt);
                    }
                    //var states = Traverse.Create(HUD.SelectionHandler).Property("SelectionStack").GetValue<List<SelectionState>>();

                    HUD.MechWarriorTray.ResetMechwarriorButtons(theActor);

                    if (!Mod.modSettings.disableCalledShotExploit) return;
                    var selectionStack = HUD.selectionHandler.SelectionStack;//Traverse.Create(HUD.SelectionHandler).Property("SelectionStack").GetValue<List<SelectionState>>();
                    var moraleState = selectionStack.FirstOrDefault(x => x is SelectionStateMoraleAttack);
                    if (moraleState != null)
                    {
                        moraleState.OnInactivate();
                        moraleState.OnRemoveFromStack();
                        selectionStack.Remove(moraleState);
                    }
                }
            }


            [HarmonyPatch(typeof(CombatHUDEquipmentSlot), "ActivateAbility", new Type[] { typeof(string), typeof(string) })]
            public static class CombatHUDEquipmentSlot_ActivateAbility_Confirmed
            {
                //public static bool Prepare() => Mod.modSettings.enableResolverator;

                public static void Postfix(CombatHUDActionButton __instance, string creatorGUID, string targetGUID)
                {
                    var combat = UnityGameInstance.BattleTechGame.Combat;
                    if (combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                    Mod.modLog.LogMessage($"Processing resolve costs for {__instance.Ability.Def.Description.Name}");
                    var HUD = __instance.HUD;
                    var theActor = HUD.SelectedActor ?? combat.FindActorByGUID(creatorGUID);
                    if (theActor == null) return;
                    if (!Mod.modSettings.enableResolverator)
                    {
                        var amt = -Mathf.RoundToInt(__instance.Ability.Def.getAbilityDefExtension().ResolveCost);
                        theActor.team.ModifyMorale(amt);
                    }
                    else
                    {
                        var amt = -Mathf.RoundToInt(__instance.Ability.Def.getAbilityDefExtension().ResolveCost);
                        theActor.ModifyResolve(amt);
                    }

                    HUD.MechWarriorTray.ResetMechwarriorButtons(theActor);

                    if (!Mod.modSettings.disableCalledShotExploit) return;
                    var selectionStack = HUD.selectionHandler.SelectionStack;//Traverse.Create(HUD.SelectionHandler).Property("SelectionStack").GetValue<List<SelectionState>>();
                    var moraleState = selectionStack.FirstOrDefault(x => x is SelectionStateMoraleAttack);
                    if (moraleState != null)
                    {
                        moraleState.OnInactivate();
                        moraleState.OnRemoveFromStack();
                        selectionStack.Remove(moraleState);
                    }
                }
            }

            [HarmonyPatch(typeof(CombatHUDEquipmentSlot), "ActivateAbility", new Type[] {})]
            public static class CombatHUDEquipmentSlot_ActivateAbility_noparams
            {
                public static bool Prepare() => Mod.modSettings.enableResolverator && false;

                public static void Postfix(CombatHUDActionButton __instance)
                {
                    var combat = UnityGameInstance.BattleTechGame.Combat;
                    if (combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                    Mod.modLog.LogMessage($"Processing resolve costs for {__instance.Ability.Def.Description.Name}");
                    var HUD = __instance.HUD;
                    var theActor = HUD.SelectedActor;
                    if (theActor == null) return;
                    if (!Mod.modSettings.enableResolverator)
                    {
                        var amt = -Mathf.RoundToInt(__instance.Ability.Def.getAbilityDefExtension().ResolveCost);
                        theActor.team.ModifyMorale(amt);
                    }
                    else
                    {
                        var amt = -Mathf.RoundToInt(__instance.Ability.Def.getAbilityDefExtension().ResolveCost);
                        theActor.ModifyResolve(amt);
                    }

                    HUD.MechWarriorTray.ResetMechwarriorButtons(theActor);

                    if (!Mod.modSettings.disableCalledShotExploit) return;
                    var selectionStack = HUD.selectionHandler.SelectionStack;//Traverse.Create(HUD.SelectionHandler).Property("SelectionStack").GetValue<List<SelectionState>>();
                    var moraleState = selectionStack.FirstOrDefault(x => x is SelectionStateMoraleAttack);
                    if (moraleState != null)
                    {
                        moraleState.OnInactivate();
                        moraleState.OnRemoveFromStack();
                        selectionStack.Remove(moraleState);
                    }
                }
            }


            [HarmonyPatch(typeof(CombatHUDEquipmentSlot), "ActivateCommandAbility", new Type[] { typeof(string), typeof(Vector3), typeof(Vector3) })]
            public static class CombatHUDEquipmentSlot_ActivateCommandAbility_Confirmed
            {
                //public static bool Prepare() => Mod.modSettings.enableResolverator;

                public static void Postfix(CombatHUDActionButton __instance, string teamGUID, Vector3 positionA, Vector3 positionB)
                {
                    var combat = UnityGameInstance.BattleTechGame.Combat;
                    if (combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                    Mod.modLog.LogMessage($"Processing resolve costs for {__instance.Ability.Def.Description.Name}");
                    var HUD = __instance.HUD;
                    var theActor = HUD.SelectedActor;
                    if (theActor == null) return;
                    if (!Mod.modSettings.enableResolverator)
                    {
                        var amt = -Mathf.RoundToInt(__instance.Ability.Def.getAbilityDefExtension().ResolveCost);
                        theActor.team.ModifyMorale(amt);
                    }
                    else
                    {
                        var amt = -Mathf.RoundToInt(__instance.Ability.Def.getAbilityDefExtension().ResolveCost);
                        theActor.ModifyResolve(amt);
                    }

                    HUD.MechWarriorTray.ResetMechwarriorButtons(theActor);
                    //clear SelectionStateMoraleAttack if present? force OnInactivate

                    if (!Mod.modSettings.disableCalledShotExploit) return;
                    var selectionStack = HUD.selectionHandler.SelectionStack;//Traverse.Create(HUD.SelectionHandler).Property("SelectionStack").GetValue<List<SelectionState>>();
                    var moraleState = selectionStack.FirstOrDefault(x => x is SelectionStateMoraleAttack);
                    if (moraleState != null)
                    {
                        moraleState.OnInactivate();
                        moraleState.OnRemoveFromStack();
                        selectionStack.Remove(moraleState);
                    }
                }
            }

            [HarmonyPatch(typeof(CombatHUDActionButton), "InitButton",
                new Type[] {typeof(SelectionType), typeof(Ability), typeof(SVGAsset), typeof(string), typeof(string), typeof(AbstractActor)})]
            public static class CombatHUDActionButton_InitButton
            {
                //public static bool Prepare() => Mod.modSettings.enableResolverator ;

                public static void Postfix(CombatHUDActionButton __instance, SelectionType SelectionType, Ability Ability, SVGAsset Icon, string GUID, string Tooltip, AbstractActor actor)
                {
                    if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                    if (actor == null || __instance.Ability == null) return;
                    if (SelectionType == SelectionType.FireMorale ||
                        SelectionType == SelectionType.ConfirmMorale) return;
                    __instance.isMoraleAbility = __instance.Ability.Def.getAbilityDefExtension().ResolveCost > 0;
                    __instance.RefreshColors(actor, null);
                }
            }

            [HarmonyPatch(typeof(CombatHUD), "ShowCalledShotPopUp", new Type[] {typeof(AbstractActor), typeof(AbstractActor) })]
            public static class CombatHUD_ShowCalledShotPopUp
            {
                public static bool Prepare() => Mod.modSettings.enableResolverator && Mod.modSettings.disableCalledShotExploit && false; //disabled for now

                public static void Prefix(CombatHUD __instance)
                {
                    if (__instance.SelectedActor != null && __instance.SelectionHandler.ActiveState is SelectionStateMoraleAttack) __instance.MechWarriorTray.ResetMechwarriorButtons(__instance.SelectedActor);
                }
            }

            [HarmonyPatch(typeof(CombatSelectionHandler), "BackOutOneStep",
                new Type[] {typeof(bool)})]
            public static class CombatSelectionHandler_BackOutOneStep
            {
                public static bool Prepare() => Mod.modSettings.enableResolverator && Mod.modSettings.disableCalledShotExploit && false; //disabled for now

                public static void Postfix(CombatSelectionHandler __instance)
                {
                    var HUD = __instance.HUD;
                    if (__instance.SelectedActor != null) HUD.MechWarriorTray.ResetMechwarriorButtons(__instance.SelectedActor);
                }
            }

            [HarmonyPatch(typeof(CombatHUDEquipmentSlot), "ActivateAbility", new Type[]{})]
            public static class CombatHUDEquipmentSlot_ActivateAbility_Invoked
            {
                public static bool Prepare() => Mod.modSettings.enableResolverator && false; //disabled for now

                public static void Postfix(CombatHUDActionButton __instance)
                {
                    if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                    var cost = Mathf.RoundToInt(__instance.Ability.Def.getAbilityDefExtension().ResolveCost * __instance.HUD.selectedUnit.GetResolveCostBaseMult());
                    Mod.modLog.LogMessage($"Activating {__instance.Ability.Def.Description.Name} and setting predicted Resolve Cost to {cost}");
                    PilotResolveTracker.HolderInstance.selectedAbilityResolveCost = cost;
                }
            }

            [HarmonyPatch(typeof(CombatHUDEquipmentSlot), "DeactivateAbility", new Type[]{})]
            public static class CombatHUDEquipmentSlot_DeactivateAbility
            {
                public static bool Prepare() => Mod.modSettings.enableResolverator && false; //disabled for now

                public static void Postfix(CombatHUDActionButton __instance)
                {
                    if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                    Mod.modLog.LogMessage($"Deactivating {__instance.Ability.Def.Description.Name} and resetting predicted Resolve Cost to 0");
                    PilotResolveTracker.HolderInstance.selectedAbilityResolveCost = 0;
                }
            }

            [HarmonyPatch(typeof(SelectionStateActiveProbe), "CreateFiringOrders")]
            public static class SelectionStateActiveProbe_CreateFiringOrders
            {
                //public static bool Prepare() => Mod.modSettings.enableResolverator;
                public static void Postfix(SelectionStateActiveProbe __instance, string button)
                {
                    if (button == "BTN_FireConfirm")
                    {
                        var combat = UnityGameInstance.BattleTechGame.Combat;
                        if (combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                        var abilityDef = __instance.FromButton?.Ability?.Def;
                        if (abilityDef == null) return;
                        Mod.modLog.LogMessage($"Processing resolve costs for {abilityDef.Description.Name}");
                        var HUD = __instance.HUD;
                        var theActor = HUD.SelectedActor;
                        if (theActor == null) return;
                        if (!Mod.modSettings.enableResolverator)
                        {
                            var amt = -Mathf.RoundToInt(abilityDef.getAbilityDefExtension().ResolveCost);
                            theActor.team.ModifyMorale(amt);
                        }
                        else
                        {
                            var amt = -Mathf.RoundToInt(abilityDef.getAbilityDefExtension().ResolveCost);
                            theActor.ModifyResolve(amt);
                        }
                        HUD.MechWarriorTray.ResetMechwarriorButtons(theActor);

                        if (!Mod.modSettings.disableCalledShotExploit) return;
                        var selectionStack = HUD.selectionHandler.SelectionStack;
                        var moraleState = selectionStack.FirstOrDefault(x => x is SelectionStateMoraleAttack);
                        if (moraleState != null)
                        {
                            moraleState.OnInactivate();
                            moraleState.OnRemoveFromStack();
                            selectionStack.Remove(moraleState);
                        }
                    }
                }
            }
        }
    }
}