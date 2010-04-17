﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace SpellWork
{
    class SpellInfo
    {
        public static void View(RichTextBox rtb, uint spellId)
        {
            rtb.Clear();
            var spell = (from s in DBC.Spell where s.Key == spellId select s.Value).First();
            rtb.AppendText(GenerateSpellDesc(spell));
        }

        public static void BuildFamilyTree(TreeView familyTree, SpellFamilyNames spellfamily)
        {
            familyTree.Nodes.Clear();
            var spells = from Spell in DBC.Spell
                         where Spell.Value.SpellFamilyName == (uint)spellfamily
                         join sk in DBC.SkillLineAbility on Spell.Key equals sk.Value.SpellId into temp
                         from Skill in temp.DefaultIfEmpty()
                         select new
                         {
                             Spell,
                             Skill.Value.SkillId
                         };

            for (int i = 0; i < 96; i++)
            {
                uint mask_0 = 0, mask_1 = 0, mask_2 = 0;

                if (i < 32)
                    mask_0 = 1U << i;
                else if (i < 64)
                    mask_1 = 1U << (i - 32);
                else
                    mask_2 = 1U << (i - 64);

                TreeNode node = new TreeNode();
                node.Text = String.Format("0x{0:X8} {1:X8} {2:X8}", mask_2, mask_1, mask_0);
                familyTree.Nodes.Add(node);
            }

            foreach (var elem in spells)
            {
                var spell = elem.Spell.Value;
                string name = elem.SkillId != 0
                ? String.Format("+({0}) {1} ({2})_({3}, {4})", spell.ID, spell.SpellName, spell.Rank, elem.SkillId, GetSchool(spell))
                : String.Format("-({0}) {1} ({2})_({3})",      spell.ID, spell.SpellName, spell.Rank, GetSchool(spell));

                int i = 0;
                foreach(TreeNode node in familyTree.Nodes)
                {
                    uint mask_1 = 0, mask_2 = 0, mask_3 = 0;

                    if (i < 32)
                        mask_1 = 1U << i;
                    else if (i < 64)
                        mask_2 = 1U << (i - 32);
                    else
                        mask_3 = 1U << (i - 64);

                    if ((spell.SpellFamilyFlags1 & mask_1) != 0 ||
                        (spell.SpellFamilyFlags2 & mask_1) != 0 ||
                        (spell.SpellFamilyFlags3 & mask_3) != 0)
                    {
                        TreeNode child = new TreeNode();
                        child = node.Nodes.Add(name);
                        child.Name = spell.ID.ToString();
                    }
                    i++;
                }
            }
        }

        static String GetDuration(uint DurationIndex)
        {
            var query = from duration in DBC.SpellDuration where duration.Key == DurationIndex select duration;

            if (query.Count() == 0)
                return "";

            var q = query.First().Value;
            return String.Format("Duration: ID {0}  {1}, {2}, {3}\r\n", q.ID, q.Duration[0], q.Duration[1], q.Duration[2]);
        }

        static String GetRange(uint index)
        {
            var query = from rande in DBC.SpellRange where rande.Key == index select rande;

            if (query.Count() == 0)
                return "";

            var q = query.First();
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("SpellRange: ({0}) {1}: ",                    q.Key, q.Value.Description1);
            sb.AppendFormat("MinRange = {0}, MinRangeFriendly = {1}, ",   q.Value.MinRange, q.Value.MinRangeFriendly);
            sb.AppendFormat("MaxRange = {0}, MaxRangeFriendly = {1}\r\n", q.Value.MaxRange, q.Value.MaxRangeFriendly);
            
            return sb.ToString();
        }

        static String GetCastTime(SpellEntry spell)
        {
            if(spell.CastingTimeIndex ==0) return "";

            var query = from t in DBC.SpellCastTimes where t.Key == spell.CastingTimeIndex select t.Value;

            if(query.Count() == 0)
            {
                return String.Format("CastingTime({0}) = ????\r\n", spell.CastingTimeIndex);
            }
            else
            {
                return String.Format("CastingTime = {0:F}\r\n", query.First().CastTime / 1000.0f);
            }
        }

        static String GetSkillLine(uint entry)
        {
            var query =
               from skillLineAbility in DBC.SkillLineAbility
               join skillLine in DBC.SkillLine
               on skillLineAbility.Value.SkillId equals skillLine.Key
               where skillLineAbility.Value.SpellId == entry
               select new { skillLineAbility, skillLine };

            if (query.Count() == 0)
                return "";
            var skill = query.First().skillLineAbility.Value;
            var line = query.First().skillLine.Value;

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Skill ({0})", skill.SkillId);

            if (line.Name != "")
                sb.AppendFormat(" {0}", line.Name);

            if (skill.Req_skill_value > 1)
                sb.AppendFormat(", Req skill value {0}", skill.Req_skill_value);

            if (skill.Forward_spellid != 0)
                sb.AppendFormat(", Forward spell {0}", skill.Forward_spellid);

            if (skill.Min_value != 0)
                sb.AppendFormat(", Min Value {0}", skill.Min_value);

            if (skill.Max_value != 0)
                sb.AppendFormat(", Max Value {0}", skill.Max_value);

            if (skill.characterPoints[0] != 0)
                sb.AppendFormat(", Req characterPoints_0 {0}", skill.characterPoints[0]);

            if (skill.characterPoints[1] != 0)
                sb.AppendFormat(", Req characterPoints_1 {0}", skill.characterPoints[1]);

            return sb.ToString() + Environment.NewLine;
        }

        static SpellSchools GetSchool(SpellEntry spell)
        {
            if ((spell.SchoolMask & 1)   != 0) return (SpellSchools)0;
            if ((spell.SchoolMask & 2)   != 0) return (SpellSchools)1;
            if ((spell.SchoolMask & 4)   != 0) return (SpellSchools)2;
            if ((spell.SchoolMask & 8)   != 0) return (SpellSchools)3;
            if ((spell.SchoolMask & 16)  != 0) return (SpellSchools)4;
            if ((spell.SchoolMask & 32)  != 0) return (SpellSchools)5;
            if ((spell.SchoolMask & 64)  != 0) return (SpellSchools)6;
            if ((spell.SchoolMask & 128) != 0) return (SpellSchools)7;
            return (SpellSchools)0;
        }

        static String GetProcInfo(SpellEntry spell)
        {
            int i = 0;
            string str = "";
            var proc = spell.procFlags;
            while (proc != 0)
            {
                if ((proc & 1) != 0)
                    str += String.Format("  {0}\r\n", ProcFlagDesc[i]);
                i++;
                proc >>= 1;
            }
            return str;
        }

        static String GetFormInfo(ulong val, string name)// необходимо правильно реализовать
        {
            int i = 1;
            while (val != 0)
            {
                if ((val & 1) != 0)
                    name += String.Format("{0} ", (ShapeshiftForm)i);
                i++;
                val >>= 1;
            }
            return name + Environment.NewLine;
        }

        static String GetItemInfo(SpellEntry spell)
        {
            // Получить из базы данных ()
            // SELECT `entry`, `name` FROM `item_template` WHERE `spellid_1` = {0} OR `spellid_2` = {1} OR `spellid_3` = {2}
            //      OR `spellid_4` = {3} OR `spellid_5` = {4}
            return "";
        }

        static String GetAuraModTypeName(uint id, int mod)
        {
            if (id == 107 || id == 108 || mod < 29)
                return "" + (SpellModOp)mod;
            return "" + mod;
        }

        static String GetRadius(SpellEntry spell, int index)
        {
            if (spell.EffectRadiusIndex[index] != 0)
            {
                var query = from r in DBC.SpellRadius where r.Key == spell.EffectRadiusIndex[index] select r.Value.Radius;


                if (query.Count() > 0)
                    return String.Format("Radius ({0}) {0:F}\r\n", spell.EffectRadiusIndex[index], query.First());
                else
                    return String.Format("Radius ({0}) Not found\r\n", spell.EffectRadiusIndex[index]);
            }
            return "";
        }

        static String GetTriggerSpell(SpellEntry spell, int index)
        {
            StringBuilder sb = new StringBuilder();
            if (spell.EffectTriggerSpell[index] != 0)
            {
                var query = from tr in DBC.Spell where spell.ID == spell.EffectTriggerSpell[index] select tr;

                if (query.Count() > 0)
                {
                    var trigger = query.First().Value;
                    sb.AppendFormat("Trigger spell ({0}) {1}. Chance = {2}\r\n", spell.EffectTriggerSpell[index],
                        trigger.SpellName, spell.procChance);

                    if (trigger.Description != "")
                        sb.AppendFormat("{0}\r\n", trigger.Description);
                    if (trigger.ToolTip != "")
                        sb.AppendFormat("{0}\r\n", trigger.ToolTip);

                    if (trigger.procFlags != 0)
                    {
                        sb.AppendFormat("Charges - {0}  =======================================\r\n", trigger.procCharges);
                        sb.Append(GetProcInfo(trigger));
                        sb.AppendFormat("=================================================\r\n");
                    }
                }
                else
                {
                    sb.AppendFormat("Trigger spell ({0}) Not found, Chance = {0}\r\n",
                        spell.EffectTriggerSpell[index], spell.procChance);
                }
            }

            return sb.ToString();
        }

        static String GetSpellEffectInfo(SpellEntry spell)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 3; i++)
            {
                if (spell.Effect[i] == 0 && spell.EffectBasePoints[i] == 0)
                {
                    sb.AppendFormat("\r\nEffect_{0}: NO EFFECT\r\n", i);
                    return sb.ToString();
                }

                sb.AppendFormat("\r\nEffect_{0}: {1}\r\n", i, (SpellEffects)spell.Effect[i]);
                sb.AppendFormat("Base point = {0}", spell.EffectBasePoints[i] + 1/*spell.EffectDieSides[i]*/);
                if (spell.EffectRealPointsPerLevel[i]!=0)
                    sb.AppendFormat(" + lvl * {0:F}", spell.EffectRealPointsPerLevel[i]);
                if (/*spell.EffectBaseDice[i]*/1 < spell.EffectDieSides[i])
                {
                if (spell.EffectRealPointsPerLevel[i] != 0)
                    sb.AppendFormat(" to {0} + lvl * {1:F}",
                        spell.EffectBasePoints[i] + /*spell.EffectBaseDice[i]*/ 1 + spell.EffectDieSides[i], spell.EffectRealPointsPerLevel[i]);
                else
                    sb.AppendFormat(" to {0}", spell.EffectBasePoints[i] +/*+ spell.EffectBaseDice[i]*/ 1 + spell.EffectDieSides[i]);
                }

                if (spell.EffectPointsPerComboPoint[i]!=0)
                    sb.AppendFormat(" + combo * {0:F}", spell.EffectPointsPerComboPoint[i]);
                if (spell.DmgMultiplier[i] != 1)
                    sb.AppendFormat(" x {0:F}", spell.DmgMultiplier[i]);

                if (spell.EffectMultipleValue[i]!=0)
                    sb.AppendFormat("  Multiple = {0:F}", spell.EffectMultipleValue[i]);

                sb.AppendFormat("\r\nTarget A ({0}),", (Targets)spell.EffectImplicitTargetA[i]);
                sb.AppendFormat(" Target B ({0})\r\n", (Targets)spell.EffectImplicitTargetB[i]);

                if (spell.EffectApplyAuraName[i]!=0)
                    sb.AppendFormat("Aura {0}, value = {1}, misc = {2}, miscB = {3}, periodic = {4}\r\n",
                        (AuraType)spell.EffectApplyAuraName[i],
                        spell.EffectBasePoints[i] + 1, GetAuraModTypeName(spell.EffectApplyAuraName[i], spell.EffectMiscValue[i]),
                        spell.EffectMiscValueB[i], spell.EffectAmplitude[i]);
                else
                {
                    if (spell.EffectMiscValue[i] != 0)
                        sb.AppendFormat("EffectMiscValue = {0}\r\n", spell.EffectMiscValue[i]);
                    if (spell.EffectMiscValueB[i] != 0)
                        sb.AppendFormat("EffectMiscValueB = {0}\r\n", spell.EffectMiscValueB[i]);
                    if (spell.EffectAmplitude[i] != 0)
                        sb.AppendFormat("EffectAmplitude = {0}\r\n", spell.EffectAmplitude[i]);
                }

                uint[] ClassMask = new uint[3];
                switch (i)
                {
                    case 0: ClassMask[0] = spell.EffectSpellClassMaskA[i]; break;
                    case 1: ClassMask[1] = spell.EffectSpellClassMaskB[i]; break;
                    case 2: ClassMask[2] = spell.EffectSpellClassMaskC[i]; break;
                }

                if (ClassMask[0] != 0 || ClassMask[1] != 0 || ClassMask[2] != 0)
                {
                    sb.AppendFormat("SpellClassMask = {0:X8} {1:X8} {2:X8}\r\n", ClassMask[2], ClassMask[1], ClassMask[0]);

                    uint mask_0 = ClassMask[0];
                    uint mask_1 = ClassMask[1];
                    uint mask_2 = ClassMask[2];

                    var query = from Spell in DBC.Spell
                                where Spell.Value.SpellFamilyName == spell.SpellFamilyName
                                join sk in DBC.SkillLineAbility on Spell.Key equals sk.Value.SpellId into temp
                                from Skill in temp.DefaultIfEmpty()
                                select new
                                {
                                    Spell,
                                    SkillId = (Skill.Value.SkillId)
                                };

                    foreach (var row in query)
                    {
                        var s = row.Spell.Value;
                        if ((s.SpellFamilyFlags1 & mask_0) != 0 ||
                            (s.SpellFamilyFlags2 & mask_1) != 0 ||
                            (s.SpellFamilyFlags3 & mask_2) != 0)
                        {
                            var exist = (row.SkillId > 0) ? "+" : "-";
                            var name = s.Rank == "" ? s.SpellName : s.SpellName + " (" + s.Rank + ")";
                            sb.AppendFormat("    {0} {1} - {2}\r\n", exist, s.ID, name);
                        }
                    }
                }

                sb.Append(GetRadius(spell, i));
                sb.Append(GetTriggerSpell(spell, i));

                if (spell.EffectChainTarget[i] != 0)
                    sb.AppendFormat("EffectChainTarget = {0}\r\n", spell.EffectChainTarget[i]);
                if (spell.EffectItemType[i] != 0)
                    sb.AppendFormat("EffectItemType = {0}\r\n", spell.EffectItemType[i]);
                if (spell.EffectMechanic[i] != 0)
                    sb.AppendFormat("Effect Mechanic = {0} - {1}\r\n", spell.EffectMechanic[i], (Mechanics)spell.EffectMechanic[i]);
            }
            return sb.ToString();
        }

        static String GetSpellAura(SpellEntry spell)
        {
            // Для того чтобы не нагружать поцессор лишним запросом,просто сделаем проверку на входной параметр,
            // если он не != 0, тогда ищем, иначе нет смысла
            StringBuilder sb = new StringBuilder();
            if (spell.casterAuraSpell != 0)
            {
                var query = from ss in DBC.Spell where ss.Key == spell.casterAuraSpell select ss;
                if (query.Count() > 0)
                {
                    var s = query.First().Value;
                    if (s.ID > 0)
                        sb.AppendFormat("  Caster Aura Spell ({0}) {1}\r\n", spell.casterAuraSpell, s.SpellName);
                    else
                        sb.AppendFormat("  Caster Aura Spell ({0}) ?????\r\n", spell.casterAuraSpell);
                }
            }

            if (spell.targetAuraSpell != 0)
            {
                var query2 = from ss in DBC.Spell where ss.Key == spell.targetAuraSpell select ss;
                if (query2.Count() > 0)
                {
                    var s = query2.First().Value;
                    sb.AppendFormat("  Target Aura Spell ({0}) {1}\r\n", spell.targetAuraSpell, s.SpellName);
                }
            }

            if (spell.excludeCasterAuraSpell != 0)
            {
                var query3 = from ss in DBC.Spell where ss.Key == spell.excludeCasterAuraSpell select ss;
                if (query3.Count() > 0)
                {
                    var s = query3.First().Value;
                    if (s.ID != 0)
                        sb.AppendFormat("  Ex Caster Aura Spell ({0}) {1}\r\n", spell.excludeCasterAuraSpell, s.SpellName);
                    else
                        sb.AppendFormat("  Ex Caster Aura Spell ({0})\r\n", spell.excludeCasterAuraSpell);
                }
            }

            if (spell.excludeTargetAuraSpell != 0)
            {
                var query4 = from asp in DBC.Spell where asp.Key == spell.excludeTargetAuraSpell select asp;
                if (query4.Count() > 0)
                    sb.AppendFormat("  Ex Target Aura Spell ({0}) {1}\r\n", spell.excludeTargetAuraSpell,
                        query4.First().Value.SpellName);
                else
                    sb.AppendFormat("  Ex Target Aura Spell ({0})\r\n", spell.excludeTargetAuraSpell);
            }

            return sb.ToString();
        }

        static String GenerateSpellDesc(SpellEntry spell)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("ID - {0} {1} ({2})\r\n", spell.ID, spell.SpellName, spell.Rank);

            if (spell.Description != "")
                sb.AppendFormat("=================================================\r\n{0}\r\n", spell.Description);
            if (spell.ToolTip != "")
                sb.AppendFormat("ToolTip: {0}\r\n", spell.ToolTip);
            if (spell.modalNextSpell != 0)
                sb.AppendFormat("Modal Next Spell: {0}\r\n", spell.modalNextSpell);
            sb.AppendFormat("=================================================\r\n");

            sb.AppendFormat("Category = {0}, SpellIconID = {1}, activeIconID = {2}, SpellVisual_0 = {3}, SpellVisual_1 = {4}\r\n",
                spell.Category, spell.SpellIconID, spell.activeIconID, spell.SpellVisual[0], spell.SpellVisual[1]);

            sb.AppendFormat("Family {0}, flag 0x{1:X8} {2:X8} {3:X8}\r\n",
                (SpellFamilyNames)spell.SpellFamilyName, spell.SpellFamilyFlags3, spell.SpellFamilyFlags2, spell.SpellFamilyFlags1);

            sb.AppendFormat("SpellSchoolMask = {0}, DamageClass = {1}, PreventionType = {2}\r\n", GetSchool(spell),
                (SpellDmgClass)spell.DmgClass, (SpellPreventionType)spell.PreventionType);

            if (spell.Targets != 0 || spell.TargetCreatureType != 0)
                sb.AppendFormat("Targets 0x{0:X8}, Creature Type 0x{1:X8}\r\n", spell.Targets, spell.TargetCreatureType);

            if (spell.Stances != 0)
                sb.Append(GetFormInfo(spell.Stances, "Stances: "));
            if (spell.StancesNot != 0)
                sb.Append(GetFormInfo(spell.StancesNot, "Not Stances: "));

            sb.Append(GetSkillLine(spell.ID));

            if (spell.spellLevel != 0)
                sb.AppendFormat("Spell level = {0}", spell.spellLevel);
            if (spell.baseLevel != 0)
                sb.AppendFormat(", base {0}", spell.baseLevel);
            if (spell.maxLevel != 0)
                sb.AppendFormat(", max {0}", spell.maxLevel);
            if (spell.MaxTargetLevel != 0)
                sb.AppendFormat(", maxTargetLevel {0}", spell.MaxTargetLevel);

            sb.Append(Environment.NewLine);

            if (spell.EquippedItemClass != -1)
                sb.AppendFormat("EquippedItemClass {0}", spell.EquippedItemClass);
            if (spell.EquippedItemSubClassMask != 0)
                sb.AppendFormat(" Sub class mask 0x{0:X8}", spell.EquippedItemSubClassMask);
            if (spell.EquippedItemInventoryTypeMask != 0)
                sb.AppendFormat(" Inventory mask 0x{0:X8}", spell.EquippedItemInventoryTypeMask);

            if (spell.Category != 0)
                sb.AppendFormat("Category - {0}\r\n", spell.Category);
            if (spell.Dispel != 0)
                sb.AppendFormat("Dispel - {0}\r\n", spell.Dispel);
            if (spell.Mechanic != 0)
                sb.AppendFormat("Mechanic - {0} - {1}\r\n", spell.Mechanic, (Mechanics)spell.Mechanic);
            if(spell.rangeIndex != 0)
                sb.Append(GetRange(spell.rangeIndex));

            if (spell.speed != 0)
                sb.AppendFormat("Speed {0:F}\r\n", spell.speed);

            sb.AppendFormat("Attributes 0x{0:X8}, Ex 0x{1:X8}, Ex2 0x{2:X8}, Ex3 0x{3:X8}, Ex4 0x{4:X8}, Ex5 0x{5:X8}, Ex6 0x{6:X8}, ExG 0x{7:X8}\r\n",
                     spell.Attributes, spell.AttributesEx, spell.AttributesEx2, spell.AttributesEx3, spell.AttributesEx4,
                     spell.AttributesEx5, spell.AttributesEx6, spell.AttributesExG);

            if (spell.StackAmount != 0)
                sb.AppendFormat("Stackable up to {0}\r\n", spell.StackAmount);

            sb.Append(GetCastTime(spell));

            if (spell.RecoveryTime!=0 || spell.CategoryRecoveryTime!=0 || spell.StartRecoveryCategory!=0)
            {
                sb.AppendFormat("Recovery time {0}, Category Recovery time {1}, ", spell.RecoveryTime / 1000, spell.CategoryRecoveryTime / 1000);
                sb.AppendFormat("Start Recovery Category = {0}, Start Recovery Time = {1:F}\r\n", spell.StartRecoveryCategory, spell.StartRecoveryTime / 1000.0f);
            }

            sb.Append(GetDuration(spell.DurationIndex));

            if (spell.manaCost!=0 || spell.ManaCostPercentage!=0)
            {
                sb.AppendFormat("Power {0}, Cost {1}", (Powers)spell.powerType, spell.manaCost);

                if (spell.manaCostPerlevel != 0)
                    sb.AppendFormat(" + lvl*{0}", spell.manaCostPerlevel);
                if (spell.manaPerSecond != 0)
                    sb.AppendFormat(" + Per Second {0}", spell.manaPerSecond);
                if (spell.manaPerSecondPerLevel != 0)
                    sb.AppendFormat(" + lvl*{0}", spell.manaPerSecondPerLevel);
                if (spell.ManaCostPercentage != 0)
                    sb.AppendFormat(" + PCT {0}", spell.ManaCostPercentage);

                sb.Append(Environment.NewLine);
            }
            sb.AppendFormat("Interrupt Flags: 0x{0:X8}, AuraIF 0x{1:X8}, ChannelIF 0x{2:X8}\r\n",
                spell.InterruptFlags, spell.AuraInterruptFlags, spell.ChannelInterruptFlags);

            if (spell.CasterAuraState != 0 || spell.TargetAuraState != 0)
                sb.AppendFormat("CasterAuraState 0x{0:X8}, TargetAuraState 0x{1:X8}\r\n", spell.CasterAuraState, spell.TargetAuraState);
            if (spell.CasterAuraStateNot != 0 || spell.TargetAuraStateNot != 0)
                sb.AppendFormat("CasterAuraStateNot 0x{0:X8}, TargetAuraStateNot 0x{1:X8}\r\n", spell.CasterAuraStateNot, spell.TargetAuraStateNot);

            sb.Append(GetSpellAura(spell));

            if (spell.RequiresSpellFocus != 0)
                sb.AppendFormat("Requires Spell Focus {0}\r\n", spell.RequiresSpellFocus);

            if (spell.procFlags!=0)
            {
                sb.AppendFormat("Proc flag 0x{0:X8}, chance = {1}, charges - {2}\r\n",
                spell.procFlags, spell.procChance, spell.procCharges);
                sb.AppendFormat("=================================================\r\n");
                sb.Append(GetProcInfo(spell));
                sb.AppendFormat("=================================================\r\n");
            }
            else // if(spell.procCharges)
            {
                sb.AppendFormat("Chance = {0}, charges - {1}\r\n", spell.procChance, spell.procCharges);
            }
            sb.Append(GetSpellEffectInfo(spell));

            sb.Append(GetItemInfo(spell));

            return sb.ToString();
        }

        #region Desc
        static string[] ProcFlagDesc = {
            //00 0x00000001 000000000000000000000001 -
            "00 Killed by agressor that resive experience or honor",
            //01 0x00000002 000000000000000000000010 -
            "01 Kill that yields experience or honor",

            //02 0x00000004 000000000000000000000100 -
            "02 Successful melee attack",
            //03 0x00000008 000000000000000000001000 -
            "03 Taken damage from melee strike hit",

            //04 0x00000010 000000000000000000010000 -
            "04 Successful attack by Spell that use melee weapon",
            //05 0x00000020 000000000000000000100000 -
            "05 Taken damage by Spell that use melee weapon",

            //06 0x00000040 000000000000000001000000 -
            "06 Successful Ranged attack(and wand spell cast)",
            //07 0x00000080 000000000000000010000000 -
            "07 Taken damage from ranged attack",

            //08 0x00000100 000000000000000100000000 -
            "08 Successful Ranged attack by Spell that use ranged weapon",
            //09 0x00000200 000000000000001000000000 -
            "09 Taken damage by Spell that use ranged weapon",

            //10 0x00000400 000000000000010000000000 -
            "10 Successful ??? spell hit",
            //11 0x00000800 000000000000100000000000 -
            "11 Taken ??? spell hit",

            //12 0x00001000 000000000001000000000000 -
            "12 Successful ??? spell cast",
            //13 0x00002000 000000000010000000000000 -
            "13 Taken ??? spell hit",

            //14 0x00004000 000000000100000000000000 -
            "14 Successful cast positive spell",
            //15 0x00008000 000000001000000000000000 -
            "15 Taken positive spell hit",

            //16 0x00010000 000000010000000000000000 -
            "16 Successful damage from harmful spell cast (каст дамажащего спелла)",
            //17 0x00020000 000000100000000000000000 -
            "17 Taken spell damage",

            //18 0x00040000 000001000000000000000000 -
            "18 Deal periodic damage",
            //19 0x00080000 000010000000000000000000 -
            "19 Taken periodic damage",

            //20 0x00100000 000100000000000000000000 -
            "20 Taken any damage",
            //21 0x00200000 001000000000000000000000 -
            "21 On trap activation (При срабатывании ловушки)",

            //22 0x00800000 010000000000000000000000 -
            "22 Taken off-hand melee attacks",
            //23 0x00800000 100000000000000000000000 -
            "23 Successful off-hand melee attacks",

            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31"};
        #endregion
    }
}
