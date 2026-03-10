using JetBrains.Annotations;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.UnitLogic.Mechanics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using HarmonyLib;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Blueprints.Classes.Selection;
using static Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using System.Xml.Linq;
using Kingmaker.UnitLogic.Class.Kineticist.Properties;
using Kingmaker.UnitLogic;
using UnityEngine;
using System.Threading;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker;

namespace ChainInfusion.Utilities
{
    public class Kin_Element
    {
        private string Name;
        private string GUID;
        private PhysicalDamageForm P;
        private DamageEnergyType E;
        private BlueprintAbility Base_form;
        private BlueprintAbility Blast;
        private BlueprintProjectile Projectile;
        private BlueprintItemWeapon Weapon;
        private SpellDescriptor SpellDescriptor;
        private BlueprintItemWeapon kb_energy_weapon = Main.Library.Get<BlueprintItemWeapon>("4d3265a5b9302ee4cab9c07adddb253f");
        private BlueprintItemWeapon kb_physical_weapon = Main.Library.Get<BlueprintItemWeapon>("65951e1195848844b8ab8f46d942f6e8");
        private bool SpellResistance;
        private int Cost;
        private bool Half_bonus;
        private bool isComposite;

        public Kin_Element(string name, string guid, BlueprintAbility base_form, BlueprintAbility blast, BlueprintProjectile projectile, 
            PhysicalDamageForm p = 0, DamageEnergyType e = (DamageEnergyType)255, int cost = 0, SpellDescriptor spellDescriptor = SpellDescriptor.None)
        {

            if (p != 0) { this.Weapon = kb_physical_weapon; this.SpellResistance = false; this.Half_bonus = false;  }
            else if (e != (DamageEnergyType)255) { this.Weapon = kb_energy_weapon; this.SpellResistance = true; this.Half_bonus = true; }
            this.isComposite = (cost == 2) ? true : false;

            this.Name = name;
            this.GUID = guid;
            this.P = p;
            this.E = e;
            this.Base_form = base_form;
            this.Blast = blast;
            this.Projectile = projectile;
            this.SpellDescriptor = spellDescriptor;
            this.Cost = cost;
        }

        public string name { get => Name; }
        public string guid { get => GUID; }
        public PhysicalDamageForm physical { get => P; }
        public DamageEnergyType energy { get => E; }
        public BlueprintAbility baseform { get => Base_form; }
        public BlueprintAbility blast { get => Blast; }
        public BlueprintProjectile proj { get => Projectile; }
        public BlueprintItemWeapon weapon { get => Weapon; }
        public SpellDescriptor spelldesc { get => SpellDescriptor; }
        public bool spellresist { get => SpellResistance; }
        public int cost {  get => Cost; }
        public bool half_bonus { get => Half_bonus; }
        public bool iscomposite { get => isComposite; }

    }
    internal class Kineticist
    {

        /// <summary>
        /// 1) make BlueprintAbility
        /// 2) set SpellResistance
        /// 3) make components with helpers (step1 to 9)
        /// 4) set m_Parent to XBlastBase with Helper.AddToAbilityVariants
        /// Logic for dealing damage. Will make a composite blast, if both p and e are set. How much damage is dealt is defined in step 2.
        /// </summary>
        public static AbilityEffectRunAction Step1_run_damage(out ActionList actions, PhysicalDamageForm p = 0, DamageEnergyType e = (DamageEnergyType)255, SavingThrowType save = SavingThrowType.Unknown, bool isAOE = false, bool isComposite = false)
        {
            ContextDiceValue dice = (p == 0 && e != (DamageEnergyType)255 ) ? Helpers.CreateContextDiceValue(DiceType.D6, AbilityRankType.DamageDice, AbilityRankType.DamageBonus) 
                : Helpers.CreateContextDiceValue(DiceType.D6, AbilityRankType.DamageDice, AbilitySharedValue.Damage);

            List<ContextAction> list = new List<ContextAction>(2);

            bool isBoth = (p != 0) && (e != (DamageEnergyType)255);

            if (p != 0)
                list.Add(Helpers.CreateContextActionDealDamage(p, dice, isAOE, isAOE, false, isBoth, isBoth));
            if (e != (DamageEnergyType)255)
                list.Add(Helpers.CreateContextActionDealDamage(e, dice, isAOE, isAOE, false, isBoth, isBoth));

            var runaction = Helpers.CreateRunActions(list.ToArray());
            actions = runaction.Actions;
            return runaction;
        }

        /// <summary>
        /// Defines damage dice. Set twice for composite blasts that are pure energy or pure physical. You shouldn't need half at all.
        /// </summary>
        public static ContextRankConfig Step2_rank_dice(bool twice = false)
        {
            var progression = ContextRankProgression.AsIs;
            if (twice) progression = ContextRankProgression.MultiplyByModifier;
            var KineticBlastFeature = Main.Library.Get<BlueprintFeature>("93efbde2764b5504e98e6824cab3d27c");

            var rankdice = Helpers.CreateContextRankConfig(
                baseValueType: ContextRankBaseValueType.FeatureRank,
                type: AbilityRankType.DamageDice,
                progression: progression,
                stepLevel: twice ? 2 : 0,
                feature: KineticBlastFeature); //KineticBlastFeature
            return rankdice;
        }

        /// <summary>
        /// Defines bonus damage. Set half_bonus for energy blasts.
        /// </summary>
        public static ContextRankConfig Step3_rank_bonus(bool half_bonus)
        {
            var KineticistMainStatProperty = Main.Library.Get<BlueprintUnitProperty>("f897845bbbc008d4f9c1c4a03e22357a");
            var progression = ContextRankProgression.AsIs;

            if (half_bonus) progression = ContextRankProgression.Div2;

            var rankdice = Helpers.CreateContextRankConfig(
                baseValueType: ContextRankBaseValueType.CustomProperty,
                progression: progression,
                type: AbilityRankType.DamageBonus,
                stat: StatType.Constitution,
                customProperty: KineticistMainStatProperty); //KineticistMainStatProperty
            return rankdice;
        }

        /// <summary>
        /// If useMainStat is false; make DC dex based (form infusion).
        /// IF useMainStat is true; make DC con based (substance infusion).
        /// </summary>
        public static ContextCalculateAbilityParamsBasedOnClass Step4_dc(bool useMainStat = false)
        {
            var kineticist_class = Main.Library.Get<BlueprintCharacterClass>("42a455d9ec1ad924d889272429eb8391");
            var dc = Helpers.Create<ContextCalculateAbilityParamsBasedOnClass>();
            dc.StatType = StatType.Dexterity;
            dc.UseKineticistMainStat = useMainStat;
            dc.CharacterClass = kineticist_class; //KineticistClass
            return dc;
        }

        /// <summary>
        /// Creates damage tooltip from the run-action. Defines burn cost. Blast cost is 0, except for composite blasts which is 2. Talent is not used.
        /// </summary>
        public static AbilityKineticist Step5_burn(ActionList actions, int infusion = 0, int blast = 0, int talent = 0)
        {
            var comp = Helpers.Create<AbilityKineticist>();
            var cached = Helpers.Create<AbilityKineticist>();
            comp.InfusionBurnCost = infusion;
            comp.BlastBurnCost = blast;
            comp.WildTalentBurnCost = talent;

            if (actions?.Actions == null)
                return comp;

            for (int i = 0; i < actions.Actions.Length; i++)
            {
                if (!(actions.Actions[i] is ContextActionDealDamage))
                    continue;
                var action = (ContextActionDealDamage)actions.Actions[i];
                comp.CachedDamageInfo.Add(new AbilityKineticist.DamageInfo() { Value = action.Value, Type = action.DamageType, Half = action.Half });
            }
            return comp;
        }

        /// <summary>
        /// Alternative projectile. Requires attack roll, if weapon is not null.
        /// </summary>
        public static AbilityDeliverChainAttack Step7b_chain_projectile(BlueprintProjectile projectile, [CanBeNull] BlueprintItemWeapon weapon, float delay = 0f)
        {
            var result = Helpers.Create<AbilityDeliverChainAttack>();
            result.TargetsCount = Helpers.CreateContextValue(AbilityRankType.DamageDice);
            result.TargetType = TargetType.Enemy;
            result.Weapon = weapon;
            result.Projectile = projectile;
            result.DelayBetweenChain = delay;
            return result;
        }

        /// <summary>
        /// Element descriptor for energy blasts.
        /// </summary>
        public static SpellDescriptorComponent Step8_spell_description(SpellDescriptor descriptor)
        {
            var component = Helpers.Create<SpellDescriptorComponent>();
            component.Descriptor = descriptor;
            return component;
        }

        public static ContextActionChangeRankValue Decrease_Dice(AbilityRankChangeType type, AbilityRankType ranktype, ContextValue value)
        {
            var decrease = Helpers.Create<ContextActionChangeRankValue>();
            decrease.Type = type;
            decrease.RankType = ranktype;
            decrease.Value = value;
            return decrease;
        }

        public static void createChainInfusion(BlueprintFeature infusion, Kin_Element element) 
        {

            var dice = Helpers.CreateContextDiceValue(
                bonus: Helpers.CreateContextValue(AbilityRankType.DamageDice), diceCount: Helpers.CreateContextValue(AbilityRankType.DamageBonus), dice: DiceType.One);

            var chain_ab = Helpers.CreateAbility(element.name,
                        infusion.GetName(),
                        infusion.GetDescription(),
                        element.guid,
                        icon: infusion.Icon,
                        type: AbilityType.SpellLike,
                        actionType: UnitCommand.CommandType.Standard,
                        range: AbilityRange.Close,
                        duration: new LocalizedString() { ShouldProcess = false },
                        savingThrow: new LocalizedString() { ShouldProcess = false },
                        Helpers.CreateCalculateSharedValue(value: dice),
                        Step1_run_damage(out var actions, p: element.physical, e: element.energy, isAOE: false, isComposite: element.iscomposite),
                        Step2_rank_dice(element.iscomposite),
                        Step3_rank_bonus(element.half_bonus),
                        Step4_dc(),
                        Step5_burn(actions, infusion: 3, blast: element.cost),
                        Helpers.CreateAbilityCasterHasFacts(infusion),
                        Helpers.CreateAbilityShowIfCasterHasFact(infusion),
                        Step7b_chain_projectile(element.proj, element.weapon, 0.5f)
                        ).TargetEnemy(CastAnimationStyle.Kineticist);

            if (!element.spelldesc.Equals(SpellDescriptor.None)) { chain_ab.AddComponent(Step8_spell_description(element.spelldesc)); }

            chain_ab.AddComponents(element.blast.GetComponents<AbilitySpawnFx>().ToArray());
            chain_ab.SpellResistance = element.spellresist;
            Helpers.AppendAndReplace(ref actions.Actions, Decrease_Dice(AbilityRankChangeType.Add, AbilityRankType.DamageDice, -1));
            Helpers.addToAbilityVariants(element.baseform, chain_ab);
        }
    }
}
