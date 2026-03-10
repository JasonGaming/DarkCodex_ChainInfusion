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
            var fire_base = Library.Get<BlueprintAbility>("83d5873f306ac954cad95b6aeeeb2d8c");
            var fire_blast_ab = Library.Get<BlueprintAbility>("7b4f0c9a06db79345b55c39b2d5fb510");
            var fire_projectile = Library.Get<BlueprintProjectile>("30a5f408ea9d163418c86a7107fc4326");

            var bf_base = Library.Get<BlueprintAbility>("d29186edb20be6449b23660b39435398");
            var bf_blast_ab = Library.Get<BlueprintAbility>("322911b79eabdb64f8b079c7a2d95e68");
            var bf_projectile = Library.Get<BlueprintProjectile>("e72b90d2ddaae204297120233a74b236");

            var plasma_base = Library.Get<BlueprintAbility>("9afdc3eeca49c594aa7bf00e8e9803ac");
            var plasma_blast_ab = Library.Get<BlueprintAbility>("a5631955254ae5c4d9cc2d16870448a2");
            var plasma_projectile = Library.Get<BlueprintProjectile>("fcfbceb1cc0e9764a9c11aca509bf2d4");

            var magma_base = Library.Get<BlueprintAbility>("8c25f52fce5113a4491229fd1265fc3c");
            var magma_blast_ab = Library.Get<BlueprintAbility>("a0f05637428cbca4bab8bc9122b9e3b9");
            var magma_projectile = Library.Get<BlueprintProjectile>("e3954fb66cad910408998a29a38fd926");

            var steam_base = Library.Get<BlueprintAbility>("3baf01649a92ae640927b0f633db7c11");
            var steam_blast_ab = Library.Get<BlueprintAbility>("08eb2ade31670b843879d8841b32d629");
            var steam_projectile = Library.Get<BlueprintProjectile>("36e5df234b905d34f8f5ff542b1f21b8");

            var earth_base = Library.Get<BlueprintAbility>("e53f34fb268a7964caf1566afb82dadd");
            var earth_blast_ab = Library.Get<BlueprintAbility>("b28c336c10eb51c4a8ded0258d5742e1");
            var earth_projectile = Library.Get<BlueprintProjectile>("c28e153e8c212c1458ec2ee4092a794f");

            var sandstorm_base = Library.Get<BlueprintAbility>("b93e1f0540a4fa3478a6b47ae3816f32");
            var sandstorm_blast_ab = Library.Get<BlueprintAbility>("7b8a4a256d4f3dc4d99192bbaabcb307");
            var sandstorm_projectile = Library.Get<BlueprintProjectile>("b9e055b9f33aafe49807c44855c4f349");

            var metal_base = Library.Get<BlueprintAbility>("6276881783962284ea93298c1fe54c48");
            var metal_blast_ab = Library.Get<BlueprintAbility>("665cfd3718c4f284d80538d85a2791c9");
            var metal_projectile = Library.Get<BlueprintProjectile>("85e879aeb4b82994eb989874726790e8");

            var mud_base = Library.Get<BlueprintAbility>("e2610c88664e07343b4f3fb6336f210c");
            var mud_blast_ab = Library.Get<BlueprintAbility>("3236a9e26e23b364e8951ee9e92554e8");
            var mud_projectile = Library.Get<BlueprintProjectile>("8193876bdd95bea4d98ab27a12acf374");

            var water_base = Library.Get<BlueprintAbility>("d663a8d40be1e57478f34d6477a67270");
            var water_blast_ab = Library.Get<BlueprintAbility>("e3f41966c2d662a4e9582a0497621c46");
            var water_projectile = Library.Get<BlueprintProjectile>("06e268d6a2b5a3a438c2dd52d68bfef6");

            var cwater_base = Library.Get<BlueprintAbility>("4e2e066dd4dc8de4d8281ed5b3f4acb6");
            var cwater_blast_ab = Library.Get<BlueprintAbility>("40681ea748d98f54ba7f5dc704507f39");
            var cwater_projectile = Library.Get<BlueprintProjectile>("bc49ca6e75929ff469711761c36229b1");

            var cold_base = Library.Get<BlueprintAbility>("7980e876b0749fc47ac49b9552e259c1");
            var cold_blast_ab = Library.Get<BlueprintAbility>("f6d32ecd20ebacb4e964e2ece1c70826");
            var cold_projectile = Library.Get<BlueprintProjectile>("e82d266e0c068ab418a163fc41c40731");

            var ice_base = Library.Get<BlueprintAbility>("403bcf42f08ca70498432cf62abee434");
            var ice_blast_ab = Library.Get<BlueprintAbility>("519e36decde7c964d87c2ffe4d3d8459");
            var ice_projectile = Library.Get<BlueprintProjectile>("6064be4a016527443a96d1d02e16d8fb");

            var blizzard_base = Library.Get<BlueprintAbility>("16617b8c20688e4438a803effeeee8a6");
            var blizzard_blast_ab = Library.Get<BlueprintAbility>("27f582dcef8206142b01e27ad521e6a4");
            var blizzard_projectile = Library.Get<BlueprintProjectile>("76678c1d607067b4f9e035d54ec08f67");

            var air_base = Library.Get<BlueprintAbility>("0ab1552e2ebdacf44bb7b20f5393366d");
            var air_blast_ab = Library.Get<BlueprintAbility>("31f668b12011e344aa542aa07ab6c8d9");
            var air_projectile = Library.Get<BlueprintProjectile>("e093b08cd4cafe946962b339faf2310a");

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
                    new Kin_Element(name: "ChainFireInfAbility", guid: "41ade4d3ae9444e2b89024cf9cbe7f70", base_form: fire_base, blast: fire_blast_ab,
                    projectile: fire_projectile, spellDescriptor: SpellDescriptor.Fire, e: DamageEnergyType.Fire),

                    new Kin_Element(name: "ChainBFInfAbility", guid: "3283651b16364842ab83ca1790669280", base_form: bf_base, blast: bf_blast_ab,
                    projectile: bf_projectile, spellDescriptor: SpellDescriptor.Fire, e: DamageEnergyType.Fire, cost: 2),

                    new Kin_Element(name: "ChainPlasmaInfAbility", guid: "c99fc1522fb24404be714f50da3dce77", base_form: plasma_base, blast: plasma_blast_ab,
                    projectile: plasma_projectile, spellDescriptor: SpellDescriptor.Fire, p: PhysicalDamageForm.Bludgeoning, e: DamageEnergyType.Fire, cost: 2),

                    new Kin_Element(name: "ChainMagmaInfAbility", guid: "5d8bf23e98d34296b394d6aeb624d366", base_form: magma_base, blast: magma_blast_ab,
                    projectile: magma_projectile, spellDescriptor: SpellDescriptor.Fire, p: PhysicalDamageForm.Bludgeoning, e: DamageEnergyType.Fire, cost: 2),

                    new Kin_Element(name: "ChainSteamInfAbility", guid: "2a62c1409d1240189c6caa036f4107e9", base_form: steam_base, blast: steam_blast_ab,
                    projectile: steam_projectile, spellDescriptor: SpellDescriptor.Fire, p: PhysicalDamageForm.Bludgeoning, e: DamageEnergyType.Fire, cost: 2),

                    new Kin_Element(name: "ChainEarthInfAbility", guid: "a65693d640df41509149bf0d8400405c", base_form: earth_base, blast: earth_blast_ab,
                    projectile: earth_projectile, p: PhysicalDamageForm.Bludgeoning | PhysicalDamageForm.Piercing | PhysicalDamageForm.Slashing),

                    new Kin_Element(name: "ChainSandInfAbility", guid: "6087dcb47e424166b38ac2014661d27f", base_form: sandstorm_base, blast: sandstorm_blast_ab,
                    projectile: sandstorm_projectile, p: PhysicalDamageForm.Piercing | PhysicalDamageForm.Slashing, cost: 2),

                    new Kin_Element(name: "ChainMetalInfAbility", guid: "a5cd6c9352ff433b83e42c0b56562a8f", base_form: metal_base, blast: metal_blast_ab,
                    projectile: metal_projectile, p: PhysicalDamageForm.Bludgeoning | PhysicalDamageForm.Piercing | PhysicalDamageForm.Slashing, cost: 2),

                    new Kin_Element(name: "ChainMudInfAbility", guid: "0acb56c9d1b843a2b62961ae30ac1f09", base_form: mud_base, blast: mud_blast_ab,
                    projectile: mud_projectile, p: PhysicalDamageForm.Bludgeoning, cost: 2),

                    new Kin_Element(name: "ChainWaterInfAbility", guid: "5f9c87cde11c48e09d305ee43caeb627", base_form: water_base, blast: water_blast_ab,
                    projectile: water_projectile, p: PhysicalDamageForm.Bludgeoning),

                    new Kin_Element(name: "ChainCWaterInfAbility", guid: "b54e5c26987a4fffaaf774e368168681", base_form: cwater_base, blast: cwater_blast_ab,
                    projectile: cwater_projectile, spellDescriptor: SpellDescriptor.Electricity, p: PhysicalDamageForm.Bludgeoning, e: DamageEnergyType.Electricity, cost: 2),

                    new Kin_Element(name: "ChainColdInfAbility", guid: "91d57ef5a6064fdcb75c9d4bbda1904a", base_form: cold_base, blast: cold_blast_ab,
                    projectile: cold_projectile, spellDescriptor: SpellDescriptor.Cold, e: DamageEnergyType.Cold),

                    new Kin_Element(name: "ChainIceInfAbility", guid: "9ef81460d41d4b11babe897704e468be", base_form: ice_base, blast: ice_blast_ab,
                    projectile: ice_projectile, spellDescriptor: SpellDescriptor.Cold, p: PhysicalDamageForm.Piercing, e: DamageEnergyType.Cold, cost: 2),

                    new Kin_Element(name: "ChainBlizzInfAbility", guid: "8ceaf75dddb2423c81872bf7ee2ccdec", base_form: blizzard_base, blast: blizzard_blast_ab,
                    projectile: blizzard_projectile, spellDescriptor: SpellDescriptor.Cold, p: PhysicalDamageForm.Piercing, e: DamageEnergyType.Cold, cost: 2),

                    new Kin_Element(name: "ChainAirInfAbility", guid: "945b7c55b0b748aa9b8ce5dc7bcca1eb", base_form: air_base, blast: air_blast_ab,
                    projectile: air_projectile, p: PhysicalDamageForm.Bludgeoning),

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

                var kineticist_class = Library.Get<BlueprintCharacterClass>("42a455d9ec1ad924d889272429eb8391");
                var infusion_selection = Library.Get<BlueprintFeatureSelection>("58d6f8e9eea63f6418b107ce64f315ea");
                var icon = Library.Get<BlueprintAbility>("645558d63604747428d55f0dd3a4cb58").Icon;
                var elemental_focus = Library.Get<BlueprintFeatureSelection>("1f3a15a3ae8a5524ab8b97f469bf4e3d");

                BlueprintFeature chain_infusion_feature = Helpers.CreateFeature("ChainInfusionFeature",
                    "Chain Infusion",
                   "Element: universal\nType: form infusion\nLevel: 4\nBurn: 3\nAssociated Blasts: any\nSaving Throw: none\n"
                    + "Your kinetic blast leaps from target to target. When you hit a target with your infused blast, " +
                    "you can attempt a ranged touch attack against an additional target that is within 30 feet of the first. " +
                    "Each additional attack originates from the previous target, which could alter cover and other conditions. " +
                    "Each additional target takes 1d6 fewer points of damage than the last, and you can’t chain the blast back to a previous target. " +
                    "You can continue chaining your blasts until it misses or it's reduced to a single damage die.",
                   "f942f82c01c34c7da5f1131f5484e8b4",
                   (icon),
                   FeatureGroup.KineticBlastInfusion,
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
