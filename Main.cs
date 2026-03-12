using Epic.OnlineServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics;
using ChainInfusion.Utilities;
using ModMaker;
using ModMaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityModManagerNet;
using static ChainInfusion.Utilities.SettingsWrapper;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.UnitLogic.Class.Kineticist;
using JetBrains.Annotations;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using static Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell;
using System.Runtime.CompilerServices;
using Kingmaker.Localization;
using System.Xml.Schema;


namespace ChainInfusion
{
#if (DEBUG)
    [EnableReloading]
#endif
    static class Main
    {
        // ALL CREDITS belong to Truinto for developing Fumi's Codex and Dark Codex (WOTR)!!!!

        // all I did was tweak methods and classes to ensure functionality with Kingmaker, I did not include any original code
        // Other code was originally pulled from KingmakerHarmony2Template, which also depends on ModMaker
        // lines regarding Menu are commented out because this mod doesn't need settings
        // remove comments if you wish to work with ModMaker.Utility.MenuManager.cs

        public static bool Enabled;
        public static LocalizationManager<DefaultLanguage> Local;
        public static ModManager<Core, Settings> Mod;
        //public static MenuManager Menu;
        internal static LibraryScriptableObject Library;
        public static int classLevel;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            //HarmonyLib.Harmony.DEBUG = true;
            Local = new LocalizationManager<DefaultLanguage>();
            Mod = new ModManager<Core, Settings>();
            //Menu = new MenuManager();
            modEntry.OnToggle = OnToggle;
#if (DEBUG)
            modEntry.OnUnload = Unload;
            return true;
        }

        static bool Unload(UnityModManager.ModEntry modEntry)
        {
            Mod.Disable(modEntry, true);
            //Menu = null;
            Mod = null;
            Local = null;
            return true;
        }
#else
            return true;
        }
#endif
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;
            if (value)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                Local.Enable(modEntry);
                Mod.Enable(modEntry, assembly);
                //Menu.Enable(modEntry, assembly);
                ModPath = modEntry.Path;
            }
            else
            {
                //Menu.Disable(modEntry);
                Mod.Disable(modEntry, false);
                Local.Disable(modEntry);
                ReflectionCache.Clear();
            }
            return true;
        }

        internal static Exception Error(String message)
        {
            Mod.Error(message);
            return new InvalidOperationException(message);
        }

        private static Kin_Element[] init()
        {
            // decided to save performance by just accessing all elements and their necessary attributes via GUID, instead of traversing a complex tree multiple times 
            // it was tedious and it looks ugly but i don't mind

            var electric_base = Library.Get<BlueprintAbility>("45eb571be891c4c4581b6fcddda72bcd");
            var electric_blast_ab = Library.Get<BlueprintAbility>("24f26ac07d21a0e4492899085d1302f6");
            var lightning_bolt = Library.Get<BlueprintProjectile>("c7734162c01abdc478418bfb286ed7a5");

            var tstorm_base = Library.Get<BlueprintAbility>("b813ceb82d97eed4486ddd86d3f7771b");
            var tstorm_blast_ab = Library.Get<BlueprintAbility>("fc432e7a63f5a3545a93118af13bcb89");
            var tstorm_projectile = Library.Get<BlueprintProjectile>("0a47cc1408ebda749880ff96afb90137");

            // I know this looks horrible but there isn't a way where I can get away with using loops to repeat object instantiation, since each element
            // uses different assets, damage types, and spell descriptors
            Kin_Element[] elements =
            {
                    new Kin_Element(name: "ChainElectricInfAbility", guid: "9ddb6e2dc6404c8c85f431759d6dd818", base_form: electric_base, blast: electric_blast_ab,
                    projectile: lightning_bolt, spellDescriptor: SpellDescriptor.Electricity, e: DamageEnergyType.Electricity),

                    new Kin_Element(name: "ChainTStormInfAbility", guid: "22bbe617f4a94747b82530fb229047fc", base_form: tstorm_base, blast: tstorm_blast_ab,
                    projectile: tstorm_projectile, spellDescriptor: SpellDescriptor.Electricity, p: PhysicalDamageForm.Bludgeoning, e: DamageEnergyType.Electricity, cost: 2),
                };
            return elements;
        }

        [HarmonyPatch(typeof(LibraryScriptableObject), "LoadDictionary")]
        static class LibraryScriptableObject_LoadDictionary_Patch
        {
            static bool loaded = false;
            static void Postfix()
            {
                if (loaded) return;
                loaded = true;

                var electric_blast = Library.Get<BlueprintFeature>("c2c28b6f6f000314eb35fff49bb99920");
                var tstorm_blast = Library.Get<BlueprintFeature>("295080cf4691df9438f58ff5ce79ee65");

                var kineticist_class = Library.Get<BlueprintCharacterClass>("42a455d9ec1ad924d889272429eb8391");
                var infusion_selection = Library.Get<BlueprintFeatureSelection>("58d6f8e9eea63f6418b107ce64f315ea");
                var icon = Library.Get<BlueprintAbility>("645558d63604747428d55f0dd3a4cb58").Icon;
                var elemental_focus = Library.Get<BlueprintFeatureSelection>("1f3a15a3ae8a5524ab8b97f469bf4e3d");

                BlueprintFeature chain_infusion_feature = Helpers.CreateFeature("ChainInfusionFeature",
                    "Chain Infusion",
                   "Element: air\nType: form infusion\nLevel: 4\nBurn: 3\nAssociated Blasts: electric, thunderstorm\nSaving Throw: none\n"
                    + "Your electric blast leaps from target to target. When you hit a target with your infused blast, " +
                    "you can attempt a ranged touch attack against an additional target that is within 30 feet of the first. " +
                    "Each additional attack originates from the previous target, which could alter cover and other conditions. " +
                    "Each additional target takes 1d6 fewer points of damage than the last, and you can’t chain the blast back to a previous target. " +
                    "You can continue chaining your blasts until it misses or it's reduced to a single damage die.",
                   "6c21030891c54b0bbb206b11678f4ee2",
                   (icon),
                   FeatureGroup.KineticBlastInfusion,
                   Helpers.PrerequisiteFeaturesFromList(new BlueprintFeature[] { electric_blast, tstorm_blast }, true),
                   Helpers.PrerequisiteClassLevel(kineticist_class, 8), //change to 8 later
                   Helpers.PrerequisiteFeature(elemental_focus));
                chain_infusion_feature.IsClassFeature = true;

                Kin_Element[] chainVariations = init();
                foreach (Kin_Element element in chainVariations) { Kineticist.createChainInfusion(chain_infusion_feature, element); }

                Helpers.AppendAndReplace(ref infusion_selection.AllFeatures, chain_infusion_feature);
            }
        }
    }
}
