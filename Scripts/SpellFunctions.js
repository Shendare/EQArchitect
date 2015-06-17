/*
 *    Spell Functions
 *
 *    v1.0 - 5/8/2015
 *
 */

var MaxLevel = 99;

var SpellRootPath = "/";
var ClassLevelFieldName = "Classes";
var EffectField_ID = "EffectID";
var EffectField_Formula = "EffectFormula";
var EffectField_Base = "EffectBase";
var EffectField_Limit = "EffectLimit";
var EffectField_Max = "EffectMax";
var EffectField_Data = "EffectData";

var EffectListControl = "EffectIDsList";

function GetMinClassLevel()
{
    if (ClassLevelFieldName == "")
    {
        return 0;
    }

    var _min = 255;
    var lev;
    
    for (var c = 1; c < 17; c++)
    {
        lev = document.getElementById(ClassLevelFieldName + c).value * 1;

        if (lev < _min)
        {
            _min = lev;
        }
    }

    return _min;
}

/*

Bugs: Server/Spells.cpp, CalcBuffDuration_formula()

line ~2645:

    - i = (int)ceil(duration / 5.0f * 3);
    + i = (int)ceil(level / 5.0f * 3);

Looks like simple wrong variable typo, especially considering the next line. This would match with Lucy after change.

Additional Duration Formulas, per Lucy and raid NPC casting behavior research:

    - 13 and 14 are just like 12 and 15
    - 50 really is permanent, as far as duration. These spells are cancelled other ways (casting/combat for perm invis, non-lev zones for lev, etc.)
    - 51 is just like 50. It's used for aura spells. The target is affected as long as they're in range of the aura. No time limit.

*/

function GetDurationText(duration, level, duration2, level2)
{
    if ((level2) && (level != level2) && (duration != duration2))
    {
        return GetDurationText(duration) + " (@L" + level + ") to " + GetDurationText(duration2) + " (@L" + level2 + ')';
    }

    if (duration == 0)
    {
        return "Instant";
    }

    if (duration < 0)
    {
        return "Error";
    }

    if (duration < 10)
    {
        return duration + " tick" + ((duration > 1) ? "s" : "") + " (" + (duration * 6) + "s)";
    }
    else
    {
        var minutes = (duration / 10.0);
        var hours = "";

        if (minutes >= 60)
        {
            if (minutes >= 120)
            {
                hours = Math.floor(minutes / 60) + " hours";
            }
            else
            {
                hours = "1 hour"
            }

            minutes -= (60 * Math.floor(minutes / 60));

            if (minutes == 0)
            {
                return hours;
            }
            else
            {
                hours += " ";
            }
        }

        return hours + minutes + " minute" + ((minutes == 1.0) ? "" : "s");
    }
}

function GetDurationAtLevel(duration, formula, level)
{
    switch (formula)
    {
        case 0:
            return 0;
            break;
        case 1:
            return Math.min(Math.max(1, Math.ceil(level / 2.0)), duration);
            break;
        case 2:
            return Math.min(Math.max(1, Math.ceil(level * 3.0 / 5.0)), duration);
            break;
        case 3:
            return Math.min(Math.max(level * 30, 1), duration);
            break;
        case 4:
            return (duration <= 0) ? 50 : duration;
            break;
        case 5:
            return ((duration < 1) || (duration > 2)) ? 3 : duration;
            break;
        case 6: // Identical to 1 in emu. No spells use it.
            return Math.min(Math.max(1, Math.ceil(level / 2.0)), duration);
            break;
        case 7:
            return (duration != 0) ? duration : Math.max(level, 1);
            break;
        case 8:
            return Math.min(level + 10, duration);
            break;
        case 9:
            return Math.min(level * 2 + 10, duration);
            break;
        case 10:
            return Math.min(level * 3 + 10, duration);
            break;
        case 11:
            return Math.min((level + 3) * 30, duration);
        case 12:
        case 13:
        case 14:
        case 15:
            return duration;
            break;
        case 50:
        case 51:
            return 72000;
            break;
        case 3600:
            return (duration == 0) ? 3600 : duration;
            break;
    }

    return -1;
}

function GetDurationDescription(duration, formula)
{
    var _min = GetMinClassLevel();
    var _max = MaxLevel;

    if (_min >= 250)
    {
        _min = 1;
    }

    duration *= 1;
    formula *= 1;

    var _dur1 = GetDurationAtLevel(duration, formula, _min);

    if (_dur1 < 0)
    {
        return "Unknown (Formula: " + formula + ", Duration: " + GetDurationText(duration) + ")";
    }

    var _dur2 = (_min == _max) ? _dur1 : GetDurationAtLevel(duration, formula, _max);

    switch (formula)
    {
        case 1:
            _max = Math.min(duration * 2, _max);
            break;
        case 2:
            _max = Math.min(Math.ceil(duration * 5.0 / 3.0), _max);
            break;
        case 3:
            _max = Math.ceil(duration / 30.0);
            break;
        case 6: // Identical to 1 in emu. No spells use it.
            _max = Math.min(duration * 2, _max);
            break;
        case 8:
            _max = Math.min(duration - 10, _max);
            break;
        case 9:
            _max = Math.min(Math.ceil((duration - 10) / 2.0), _max);
            break;
        case 10:
            _max = Math.min(Math.ceil((duration - 10) / 3.0), _max);
            break;
        case 11:
            _max = Math.min(Math.ceil(duration / 30) + 3, _max);
            break;
        case 50:
        case 51:
            return "No time limit";
            break;
    }

    return GetDurationText(_dur1, _min, _dur2, _max);
}

function GetEffectValueAtLevel(formula, base, limit, max, level, ismax)
{
    // Based on EQEmu code in zone/spell_effects.cpp as of May 2015

    var direction = ((max < base) && (max != 0)) ? -1 : 1; // Some formulas count downward by level instead of upward
    var obase = base;
    base = Math.abs(base);

    var value = -9999;

    switch (formula)
    {
        case 0:
            value = direction * base;
            break;
        case 60:
        case 70:
            value = direction * base / 100; // ? Used for stuns, apparently.
            break;
        case 100:
            value = direction * base;
            break;
        case 101:
            value = direction * Math.round((base + level) / 2);
            break;
        case 102:
            value = direction * (base + level);
            break;
        case 103:
            value = direction * (base + level * 2);
            break;
        case 104:
            value = direction * (base + level * 3);
            break;
        case 105:
            value = direction * (base + level * 4);
            break;
        case 106:
            value = direction * (base + level * formula); // NOTE: Added with a few new NPC spells. Formula based on Lucy's interpretation. Also added Skill 98 (NPC Innate) for the new spells.
            break;
        case 107:
            value = direction * (base + Math.round(level / 2.0));
            break;
        case 108:
            value = direction * (base + Math.round(level / 3.0));
            break;
        case 109:
            value = direction * (base + Math.round(level / 4.0));
            break;
        case 110:
            value = direction * (base + Math.round(level / 6.0));
            break;
        case 111:
            value = direction * (base + 6 * (level - 16));
            break;
        case 112:
            value = direction * (base + 8 * (level - 24));
            break;
        case 113:
            value = direction * (base + 10 * (level - 34));
            break;
        case 114:
            value = direction * (base + 15 * (level - 44));
            break;
        case 115:
            value = base + Math.max(7 * (level - 15), 0);
            break;
        case 116:
            value = base + Math.max(10 * (level - 24), 0);
            break;
        case 117:
            value = base + Math.max(13 * (level - 34), 0);
            break;
        case 118:
            value = base + Math.max(20 * (level - 44), 0);
            break;
        case 119:
            value = base + Math.round(level / 8.0);
            break;
        case 121:
            value = base + Math.round(level / 3.0);
            break;
        case 122:
            value = direction * (base - 12 * 0); // Counting down by 12 * ticks elapsed
            break;
        case 123:
            value = ismax ? Math.abs(max) : base;
            break;
        case 124:
            value = base+ Math.max(direction * 1 * (level - 50), 0);
            break;
        case 125:
            value = base+ Math.max(direction * 2 * (level - 50), 0);
            break;
        case 126:
            value = base+ Math.max(direction * 3 * (level - 50), 0);
            break;
        case 127:
            value = base+ Math.max(direction * 4 * (level - 50), 0);
            break;
        case 128:
            value = base+ Math.max(direction * 5 * (level - 50), 0);
            break;
        case 129:
            value = base+ Math.max(direction * 10 * (level - 50), 0);
            break;
        case 130:
            value = base+ Math.max(direction * 15 * (level - 50), 0);
            break;
        case 131:
            value = base+ Math.max(direction * 20 * (level - 50), 0);
            break;
        case 132:
            value = base+ Math.max(direction * 25 * (level - 50), 0);
            break;
        case 137:
            value = base;
            break;
        case 138:
            value = ismax ? -base : 0;
            break;
        case 139:
            value = base + ((level > 30) ? Math.round((level - 30) / 2.0) : 0);
            break;
        case 140:
            value = base + ((level > 30) ? (level - 30) : 0);
            break;
        case 141:
            value = base + ((level > 30) ? Math.round((3 * level - 90) / 2.0) : 0);
            break;
        case 142:
            value = base + ((level > 30) ? Math.round((2 * level - 60) / 2.0) : 0);
            break;
        case 143:
            value = base + Math.round(3 * level / 4.0);
            break;
        case 201:
        case 203:
            value = max;
            break;
        default:
            if (formula < 100)
            {
                value = direction * (base + level * formula);
            }
            else if ((formula > 1000) && (formula < 1999))
            {
                value = direction * (base - ((formula - 1000) * 12)); // TODO: Figure actual duration in.
            }
            else if ((formula >= 2000) && (formula <= 2650))
            {
                value = direction * base * (level * (formula - 2000) + 1);
            }
            break;
    }

    if (value == -9999)
    {
        // Could not determine formula/value.

        return value;
    }

    if (max != 0)
    {
        if (direction == 1)
        {
            if (value > max)
            {
                value = max;
            }
        }
        else
        {
            if (value < max)
            {
                value = max;
            }
        }
    }

    if ((obase < 0) && (value > 0))
    {
        value = -value;
    }

    return value;
}

function GetEffectFormulaText(value, level, value2, level2, unitsuffix)
{
    if ((level2) && (level != level2) && (value != value2))
    {
        if (value < 0)
        {
            return -value + unitsuffix + " (@L" + level + ") to " + -value2 + unitsuffix + " (@L" + level2 + ")";
        }
        else
        {
            return value + unitsuffix + " (@L" + level + ") to " + value2 + unitsuffix + " (@L" + level2 + ")";
        }
    }

    if (value < 0)
    {
        return -value + unitsuffix;
    }
    else
    {
        return value + unitsuffix;
    }
}

function GetEffectFormulaDescription(formula, base, limit, max, divisor, unitsuffix, leveloverride)
{
    var _min = (leveloverride > 0) ? leveloverride : GetMinClassLevel();
    var _max = (leveloverride > 0) ? leveloverride : MaxLevel;

    if (_min >= 250)
    {
        _min = 1;
    }

    var _value1 = GetEffectValueAtLevel(formula, base, limit, max, _min, false);

    if (_value1 == -9999)
    {
        return "Unknown Adjustment. Formula: " + formula + ", Base: " + base + ", Limit: " + limit + ", Max: " + max;
    }

    var _value2 = (_min == _max) ? _value1 : GetEffectValueAtLevel(formula, base, limit, max, _max, true);

    base = Math.abs(base);
    limit = Math.abs(limit);
    max = Math.abs(max);

    var _max2 = 0; // Maximum effective level based on 'max' field and formula calculations.

    var _prefix = "";
    var _suffix = "";

    switch (formula)
    {
        case 101:
            _max2 = (max - base) * 2 + 1;
            break;
        case 102:
            _max2 = max - base;
            break;
        case 103:
            _max2 = Math.ceil((max - base) / 2.0);
            break;
        case 104:
            _max2 = Math.ceil((max - base) / 3.0);
            break;
        case 105:
            _max2 = Math.ceil((max - base) / 4.0);
            break;
        case 106:
            _max2 = Math.ceil((max - base) / formula);
            break;
        case 107:
            _max2 = Math.abs((max - base) * 2);
            break;
        case 108:
            _max2 = Math.abs((max - base) * 3);
            break;
        case 109:
            _max2 = Math.abs((max - base) * 4);
            break;
        case 110:
            _max2 = Math.abs((max - base) * 6);
            break;
        case 111:
            _max2 = Math.ceil((max - base) / 6.0) + 16;
            break;
        case 112:
            _max2 = Math.ceil((max - base) / 8.0) + 24;
            break;
        case 113:
            _max2 = Math.ceil((max - base) / 10.0) + 34;
            break;
        case 114:
            _max2 = Math.ceil((max - base) / 15.0) + 44;
            break;
        case 115:
            _max2 = (_value1 == base) ? _min : Math.ceil((max - base) / 7.0) + 15;
            break;
        case 116:
            _max2 = (_value1 == base) ? _min : Math.ceil((max - base) / 10.0) + 24;
            break;
        case 117:
            _max2 = (_value1 == base) ? _min : Math.ceil((max - base) / 13.0) + 34;
            break;
        case 118:
            _max2 = (_value1 == base) ? _min : Math.ceil((max - base) / 20.0) + 44;
            break;
        case 119:
            _max2 = (max - base) * 8;
            break;
        case 121:
            _max2 = (max - base) * 3;
            break;
        case 122:
            _suffix = " (Decaying)";
            break;
        case 123:
            _suffix = " (Random)";
            break;
        case 137:
            _prefix = "up to "
            _suffix = " (Based on % HP Remaining)"
            break;
        case 138:
            _suffix = " (Based on % HP if below half health)"
            break;
        default:
            if ((formula > 0) && (formula < 100))
            {
                _max2 = Math.ceil((max - base) / formula);
            }
            break;
    }

    if (_max2 > _min)
    {
        _max = Math.min(_max2, _max);
    }

    if (divisor)
    {
        _value1 = Math.round(_value1 / divisor);
        _value2 = Math.round(_value2 / divisor);
    }

    return _prefix + GetEffectFormulaText(_value1, _min, _value2, _max, unitsuffix) + _suffix;
}

function GetEffectDescription(slot, spellhasduration, leveloverride)
{
    var effid = el(EffectField_ID + slot).value * 1;
    var effform = el(EffectField_Formula + slot).value * 1;
    var effbase = el(EffectField_Base + slot).value * 1;
    var efflimit = el(EffectField_Limit + slot).value * 1;
    var effmax = el(EffectField_Max + slot).value * 1;
    var effdata = el(EffectField_Data + slot).value;

    var divisor = 0; // 0 = ignore
    var hasduration = false; // Gets set to 'true' for effects that take place over a duration (X per tick)

    var list = el(EffectListControl).options;
    var effname = list ? ((effid < list.length) ? list[effid].text : "") : alert("Missing List: " + EffectListControl);

    // Type 1 - Add 'Increase X by' or 'Decrease X by' before numbers.
    var _myEffectType = 0;

    if (EffectType1List.indexOf('*' + effid + '*') >= 0)
    {
        _myEffectType = 1;
    }

    if (EffectType2List.indexOf('*' + effid + '*') >= 0)
    {
        _myEffectType = 2;
    }

    var unitsuffix = "";
    var suffix = "";

    if (!leveloverride)
    {
        leveloverride = 0;
    }
    else
    {
        leveloverride *= 1;
    }

    switch (_myEffectType)
    {
        case 0:
            break;
        case 1:
            break;
        case 2:
            // 18 - Pacify
            // 28 - IvU
            // 29 - IvA
            // 37 - Detect Hostile (Not Used)
            // 38 - Detect Magic (Not Used)
            // 39 - Detect Poison (Not Used)
            // 40 - Temporary Invulnerability
            // 41 - Destroy Target (With No Credit)
            // 44 - Lycanthropy
            // 51 - Detect Traps
            // 52 - Sense Undead
            // 53 - Sense Summoned
            // 54 - Sense Animals
            // 73 - Bind Sight
            // 94 - Limit: Drop Spell in Combat
            return effname;
            break;
    }
    switch (effid)
    {
        case 0: // Current HP
        case 15: // Current Mana
        case 192: // Hate with target (per tick)
            hasduration = spellhasduration;
            break;
        case 1: // Armor Class
            divisor = 3.4; // Verified with check in EQEmu code [zone/bonuses.cpp Mob::CalcSpellBonuses(...)] and on Live.
            break;
        case 3: // Movement Speed
        case 43: // Crippling Blow Chance
        case 87: // Vision Magnification
        case 89: // Physical Size
        case 99: // Movement Speed v2 (Formerly Root)
        case 177: // Double Attack Chance
            unitsuffix = '%';
            break;
        case 10: // Charisma
            if (((effform == 0) || (effform == 100)) && (effbase == 0))
            {
                return "[Spacer]"; // Spacer entry. "Increase CHA by 0"
            }
            break;
        case 11: // Attack Delay -> Speed (Flip)
            unitsuffix = '%';
            effbase = -effbase;
            effmax = -effmax;
            break;
        case 12: // Invisibility
        case 13: // See Invisibility
        case 14: // Water Breathing
        case 20: // Blindness
        case 27: // Cancel magic
        case 34: // Confuse (Not Used)
        case 57: // Levitation
        case 65: // Infravision (Heat Vision)
        case 66: // Ultravision (Night Vision)
            return effname + ((effbase > 0) ? " (Strength " + effbase + ")" : "");
        case 16: // NPC Frenzy Radius (No Longer Used)
        case 17: // NPC Awareness (No Longer Used)
        case 30: // NPC Frenzy Radius
        case 86: // NPC Reaction Radius
            return "Lower " + effname + " (Up to Level " + effmax + ") to " + effbase;
        case 21: // Stun
            return effname + ' ' + effmax + " for " + (effbase / 1000) + " seconds";
        case 23: // Fear
            return effname + ' ' + effmax + " (" + effbase + " Attempt" + ((effbase == 1) ? "" : "s") + ')';
        case 24: // Stamina (No longer used) (Flip)
        case 59: // Damage Shield (Flip)
            effbase = -effbase;
            effmax = Math.abs(effmax);
            break;
        case 25: // Bind Respawn Point
        case 26: // Return to Respawn Point
            return effname + " Number " + (effbase + 1);
        case 31: // Mez
            return effname + " Up to Level " + effmax + " (" + effbase + " Attempts)";
        case 32: // Summon Item
            return effname + " <a href=\"/EQArchitect/Items/" + effbase + "\" target=\"blank\">" + effdata + "</a>";
        case 33: // Summon Mage Pet
        case 71: // Summon Necromancer Pet
        case 106: // Summon Beastlord Pet
            return effname + ' ' + effdata;
        case 58: // Illusion
            if (true) // effdata == "")
            {
                findInList("RacesList", EffectField_Base + slot);
                matchToList(EffectField_Data + slot, "RacesList");

                if (el("RacesList").selectedIndex >= 0)
                {
                    effdata = el("RacesList").options[el("RacesList").selectedIndex].text;
                }
            }
            if (effdata == "")
            {
                return effname + " Race # " + effbase;
            }
            else
            {
                return effname + " " + effdata;
            }
            break;
        case 63: // Memory Blur
        case 74: // Feign Death
            return effname + " (" + effbase + "% Chance)";
        case 81: // Resurrect
            return effname + " (" + effbase + "%)";
        case 83: // Teleport
            return effname + " <a href=\"/EQArchitect/Zones/" + effdata + "\" target=\"_blank\">" + effdata + "</a> (" + el(EffectField_Base + "1").value + ", " + el(EffectField_Base + "2").value + ", " + el(EffectField_Base + "3").value + ") Facing Heading " + el(EffectField_Base + "4").value;
        case 85: // Add melee proc
        case 289: // Cast new spell when wearing off
            return effname + " <a href=\"/EQArchitect/Spells/" + effbase + "\">" + effdata + "</a>";
        case 184: // Increase Skill Hit Chance
            effname = effdata + ' ' + effname;
            unitsuffix = '%';
            break;
        case 254:
            return "";
    }

    if (effname == "")
    {
        effname = "Unknown Effect # " + effid + ", ";
    }

    var desc = effname + ' ';

    if (_myEffectType)
    {
        if ((effbase < 0) || (effmax < 0))
        {
            desc = 'Decrease ' + effname + ' by ';
        }
        else
        {
            desc = 'Increase ' + effname + ' by ';
        }
    }
    else
    {
        //desc += '(';
        //suffix = ')';
    }

    return desc + GetEffectFormulaDescription(effform, effbase, efflimit, effmax, divisor, unitsuffix, leveloverride) + (hasduration ? " per tick" : "") + suffix;
}
