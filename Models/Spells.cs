using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Odbc;
using System.Text;
using System.Web;
using System.Web.Security;
//using MySql.Data.MySqlClient;

namespace EQArchitect.Models
{
    public class Spells
    {

        /*
        string connStr;
        MySqlConnection cnn;
        MySqlCommand cmd;
         */

        public class SpellsParameters
        {
            public int ClassID = 0;
            public string ClassNick = "";
            public string ClassName = "";

            public DataTable SpellsList = null;

            public string LevelHeading = "Level";

            public string DebugString = "";

            public string SaveStatus = "";

            public object RawField(string FieldName)
            {
                return RawField(FieldName, 0);
            }
            public object RawField(string FieldName, int RowNumber)
            {
                if ((SpellsList == null) || (RowNumber < 0) || (SpellsList.Rows.Count <= RowNumber) || !SpellsList.Columns.Contains(FieldName))
                {
                    return null;
                }

                return SpellsList.Rows[RowNumber][FieldName];
            }
            
            public int IntField(string FieldName)
            {
                return DB.ToInt(RawField(FieldName));
            }

            public string TextField(string FieldName)
            {
                try
                {
                    return RawField(FieldName).ToString();
                }
                catch
                {
                    return "";
                }
            }

            public PageNav Items = new PageNav();

            public int GetAnimationCategory()
            {
                return EQInfo.SpellAnimCategories.FindIDFromNick(EQInfo.SpellAnimations.Type(IntField("spellanim")));
            }

            public string GetEffectData(int Slot)
            {
                switch (IntField("effectid" + Slot.ToString()))
                {
                    case 32: // Summon Item
                        return DB.GetText("SELECT name FROM items WHERE id=" + TextField("effect_base_value" + Slot.ToString()));
                    case 58: // Illusion
                        return EQInfo.Races.Name(IntField("effect_limit_base" + Slot.ToString()));
                    case 85: // Add Melee Proc
                    case 289: // Case New Spell when Wearing Off
                        return DB.GetText("SELECT name FROM spells_new WHERE id=" + TextField("effect_base_value" + Slot.ToString()));
                    case 33: // Summon Mage Pet
                    case 71: // Summon Necromancer Pet
                    case 83: // Teleport To:
                    case 106: // Summon Beastlord Pet
                        return TextField("teleport_zone");
                    case 184: // Increase (skill) hit chance
                        return EQInfo.Skills.Name(IntField("effect_limit_value" + Slot.ToString()));
                    default:
                        return "";
                }
            }
        }

        public SpellsParameters Parameters = new SpellsParameters();
        
        public enum SpellsAction
        {
            Index = 1,
            Edit = 2
        }
        
        public Spells(SpellsAction Action, string ClassNick, int ID)
        {
            /*
            connStr = ConfigurationManager.ConnectionStrings["MySqlConnStr"].ToString();
            cnn = new MySqlConnection(connStr);
             */

            string _classcheck = "true";
            string _orderby = "name, id";
            string _fields = "COUNT(*)";
            string _levelfield = "";
            int _count = 0;
            int _start = 0;
            string _query = "SELECT {Fields} FROM spells_new WHERE {ClassCheck} ORDER BY {OrderBy} LIMIT {Start}, {Count};";

            Parameters.ClassID = EQInfo.PlayerClasses.FindIDFromNick(ClassNick);

            if (Parameters.ClassID < 1)
            {
                Parameters.ClassID = 0;
            }
            
            switch (Action)
            {
                case SpellsAction.Index:
                    // Listing Spells

                    switch (Parameters.ClassID)
                    {
                        case 0: // No class specified
                            _classcheck = "true";
                            _levelfield = "id";
                            Parameters.LevelHeading = "ID";
                            break;
                        case 17: // Non-Player Class Spells
                            _levelfield = "id";
                            _classcheck = "(classes1+classes2+classes3+classes4+classes5+classes6+classes7+classes8+classes9+classes10+classes11+classes12+classes13+classes14+classes15+classes16)>=(250*16)";
                            _orderby = "name, id";
                            Parameters.LevelHeading = "ID";
                            break;
                        default:
                            _levelfield = "classes" + Parameters.ClassID.ToString();
                            _classcheck = "(" + _levelfield + " < 250)";
                            _orderby = _levelfield + ", name, id";
                            break;
                    }

                    _count = Parameters.Items.PerPage;
                    _start = (ID > 0) ? ((ID - 1) * _count) : 0;  // ID = Page Number when Listing
                    _fields = "{LevelField} as level, id, name, new_icon, goodEffect, skill, mana".Replace("{LevelField}", _levelfield);
                    
                    Parameters.Items.Start = _start + 1;
                    break;
                case SpellsAction.Edit:
                    // Editing a Spell

                    switch (Parameters.ClassID)
                    {
                        case 0: // No class specified
                            // Directly Editing a Spell, ID = SpellID

                            _classcheck = "id=" + ID.ToString();
                            Parameters.Items.Count = 1;
                            Parameters.Items.Start = 1;
                            break;
                        case 17: // Non-Player Class Spells
                            _classcheck = "(classes1+classes2+classes3+classes4+classes5+classes6+classes7+classes8+classes9+classes10+classes11+classes12+classes13+classes14+classes15+classes16)>=(250*16)";
                            _orderby = "name, id";
                            break;
                        default:
                            _classcheck = "(classes" + Parameters.ClassID.ToString() + " < 250)";
                            _orderby = "classes" + Parameters.ClassID.ToString() + ", name";
                            break;
                    }

                    _count = 1;
                    _fields = "*";
                    Parameters.Items.PerPage = 1;

                    if (Parameters.Items.Count == 0)
                    {
                        Parameters.Items.Count = DB.GetInt("SELECT COUNT(*) FROM spells_new WHERE " + _classcheck);
                        Parameters.Items.Start = DB.GetInt("SELECT SpellIndex FROM (SELECT id,(@rownum:=@rownum+1) as SpellIndex FROM spells_new, (SELECT @rownum:=0) AS r WHERE " + _classcheck + " ORDER BY " + _orderby + ") as SpellIndexes WHERE id=" + ID.ToString());
                        _classcheck += " AND id=" + ID.ToString();
                    }
                    else
                    {
                        Parameters.Items.Start = 1;
                    }
                    break;
            }
            
            Parameters.ClassNick = Parameters.ClassID < 1 ? "" : EQInfo.PlayerClasses.Nick(Parameters.ClassID);
            Parameters.ClassName = Parameters.ClassID < 1 ? "" : EQInfo.PlayerClasses.Name(Parameters.ClassID);

            if (Parameters.Items.Count == 0)
            {
                Parameters.Items.Count = DB.GetInt("SELECT COUNT(*) FROM spells_new WHERE " + _classcheck);
            }

            _query = _query.Replace("{ClassCheck}", _classcheck).Replace("{OrderBy}", _orderby).Replace("{Start}", _start.ToString()).Replace("{Count}", _count.ToString()).Replace("{Fields}", _fields);
            Parameters.SpellsList = DB.GetData(_query);

            Parameters.DebugString = _query;
        }

        public static string SetMessageWhitespace(string Message)
        {
            if ((Message.Length > 0) && (Message[0] != '\''))
            {
                return Message.Insert(0, " ");
            }

            return Message;
        }
        
        public static string Save(HttpRequest Request)
        {
            Dictionary<string, object> SpellFields = new Dictionary<string, object>();

            SpellFields["name"] = DB.ToText(Request.Form["Name"]);
            SpellFields["player_1"] = DB.ToText(Request.Form["Player1Actual"]);
            SpellFields["teleport_zone"] = DB.ToText(Request.Form["TeleportZone"]);
            SpellFields["you_cast"] = DB.ToText(Request.Form["CastByYou"]);
            SpellFields["other_casts"] = SetMessageWhitespace(DB.ToText(Request.Form["CastByOther"]));
            SpellFields["cast_on_you"] = DB.ToText(Request.Form["CastOnYou"]);
            SpellFields["cast_on_other"] = SetMessageWhitespace(DB.ToText(Request.Form["CastOnOther"]));
            SpellFields["spell_fades"] = DB.ToText(Request.Form["Fades"]);
            SpellFields["range"] = DB.ToInt(Request.Form["Range"]);
            SpellFields["aoerange"] = DB.ToInt(Request.Form["AERange"]);
            SpellFields["pushback"] = DB.ToInt(Request.Form["Pushback"]);
            SpellFields["pushup"] = DB.ToInt(Request.Form["Pushup"]);
            SpellFields["cast_time"] = DB.ToInt(Request.Form["CastTime"]);
            SpellFields["recovery_time"] = DB.ToInt(Request.Form["GlobalCooldown"]);
            SpellFields["recast_time"] = DB.ToInt(Request.Form["RecastTime"]);
            SpellFields["buffdurationformula"] = DB.ToInt(Request.Form["DurFormulaActual"]);
            SpellFields["buffduration"] = DB.ToInt(Request.Form["DurationActual"]);
            SpellFields["AEDuration"] = DB.ToInt(Request.Form["AEDuration"]);
            SpellFields["mana"] = DB.ToInt(Request.Form["Mana"]);
            for (int _slotNum = 1; _slotNum <= 12; _slotNum++)
            {
                string _slotStr = _slotNum.ToString();

                SpellFields["effect_base_value" + _slotStr] = DB.ToInt(Request.Form["EffectBase" + _slotStr]);
                SpellFields["effect_limit_value" + _slotStr] = DB.ToInt(Request.Form["EffectLimit" + _slotStr]);
                SpellFields["max" + _slotStr] = DB.ToInt(Request.Form["EffectMax" + _slotStr]);
                SpellFields["formula" + _slotStr] = DB.ToInt(Request.Form["EffectFormula" + _slotStr]);
                SpellFields["effectid" + _slotStr] = DB.ToInt(Request.Form["EffectID" + _slotStr]);
            }
            SpellFields["icon"] = DB.ToInt(Request.Form["OldIcon"]);
            SpellFields["memicon"] = DB.ToInt(Request.Form["OldMemIcon"]);
            /*
            for (int _slotNum = 1; _slotNum <= 4; _slotNum++)
            {
                string _slotStr = _slotNum.ToString();

                SpellFields["components" + _slotStr] = DB.ToInt(Request.Form["Component" + _slotStr]);
                SpellFields["component_counts" + _slotStr] = DB.ToInt(Request.Form["ComponentCount" + _slotStr]);
                SpellFields["NoexpendReagent" + _slotStr] = DB.ToInt(Request.Form["IsFocus" + _slotStr]);
            }
             */
            SpellFields["LightType"] = DB.ToInt(Request.Form["LightType"]);
            SpellFields["goodEffect"] =  DB.ToInt(Request.Form["IsBen"]);
            SpellFields["Activated"] = (Request.Form["IsActivated"] != "false") ? 1 : 0;
            SpellFields["resisttype"] = DB.ToInt(Request.Form["ResistType"]);
            SpellFields["targettype"] = DB.ToInt(Request.Form["TargetType"]);
            SpellFields["basediff"] = DB.ToInt(Request.Form["FizzleAdjust"]);
            SpellFields["skill"] = DB.ToInt(Request.Form["Skill"]);
            SpellFields["zonetype"] = DB.ToInt(Request.Form["ZoneType"]);
            SpellFields["EnvironmentType"] = DB.ToInt(Request.Form["EnvType"]);
            SpellFields["TimeOfDay"] = DB.ToInt(Request.Form["TimeOfDay"]);
            for (int _slotNum = 1; _slotNum <= 16; _slotNum++)
            {
                string _slotStr = _slotNum.ToString();

                SpellFields["classes" + _slotStr] = DB.ToInt(Request.Form["Classes" + _slotStr]);
            }
            SpellFields["CastingAnim"] = DB.ToInt(Request.Form["CastAnim"]);
            SpellFields["TargetAnim"] = DB.ToInt(Request.Form["TargetAnim"]);
            SpellFields["TravelType"] = DB.ToInt(Request.Form["TravelType"]);
            SpellFields["SpellAffectIndex"] = DB.ToInt(Request.Form["EffectIndex"]);
            SpellFields["disallow_sit"] = (Request.Form["DisallowSit"] != "false") ? 1 : 0;
            for (int _slotNum = 0; _slotNum <= 16; _slotNum++)
            {
                string _slotStr = _slotNum.ToString();

                SpellFields["deities" + _slotStr] = DB.ToInt(Request.Form["Deity" + _slotStr]);
            }
            //SpellFields["field142"] = DB.ToInt(Request.Form["Field142"]);
            //SpellFields["field143"] = DB.ToInt(Request.Form["Field143"]);
            SpellFields["new_icon"] = DB.ToInt(Request.Form["IconID"]);
            SpellFields["spellanim"] = DB.ToInt(Request.Form["AnimID"]);
            SpellFields["uninterruptable"] = (Request.Form["Unint"] != "false") ? 1 : 0;
            SpellFields["ResistDiff"] = DB.ToInt(Request.Form["ResistMod"]);
            SpellFields["dot_stacking_exempt"] = (Request.Form["DotStackEx"] != "false") ? 1 : 0;
            SpellFields["deleteable"] = (Request.Form["Deleteable"] != "false") ? 1 : 0;
            //SpellFields["RecourseLink"] = DB.ToInt(Request.Form[""]);
            SpellFields["no_partial_resist"] = (Request.Form["NoPartRes"] != "false") ? 1 : 0;
            //SpellFields["field152"] = DB.ToInt(Request.Form["Field152"]);
            //SpellFields["field153"] = DB.ToInt(Request.Form["Field153"]);
            SpellFields["short_buff_box"] = (Request.Form["ShortBuff"] != "false") ? 1 : 0;
            //SpellFields["descnum"] = DB.ToInt(Request.Form[""]);
            //SpellFields["typedescnum"] = DB.ToInt(Request.Form[""]);
            //SpellFields["effectdescnum"] = DB.ToInt(Request.Form[""]);
            //SpellFields["effectdescnum2"] = DB.ToInt(Request.Form[""]);
            SpellFields["npc_no_los"] = (Request.Form["NPCNoLoS"] != "false") ? 1 : 0;
            //SpellFields["field160"] = DB.ToInt(Request.Form["Field160"]);
            //SpellFields["reflectable"] = DB.ToInt(Request.Form[""]);
            //SpellFields["bonushate"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field163"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field164"] = DB.ToInt(Request.Form[""]);
            //SpellFields["ldon_trap"] = DB.ToInt(Request.Form[""]);
            SpellFields["EndurCost"] = DB.ToInt(Request.Form["Endur"]);
            SpellFields["EndurTimerIndex"] = DB.ToInt(Request.Form["EndurRate"]);
            SpellFields["IsDiscipline"] = (Request.Form["IsDisc"] != "false") ? 1 : 0;
            //SpellFields["field169"] = DB.ToInt(Request.Form["Field169"]);
            //SpellFields["field170"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field171"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field172"] = DB.ToInt(Request.Form[""]);
            //SpellFields["HateAdded"] = DB.ToInt(Request.Form[""]);
            SpellFields["EndurUpkeep"] = DB.ToInt(Request.Form["EndurUpkeep"]);
            //SpellFields["numhitstype"] = DB.ToInt(Request.Form[""]);
            //SpellFields["numhits"] = DB.ToInt(Request.Form[""]);
            //SpellFields["pvpresistbase"] = DB.ToInt(Request.Form[""]);
            //SpellFields["pvpresistcalc"] = DB.ToInt(Request.Form[""]);
            //SpellFields["pvpresistcap"] = DB.ToInt(Request.Form[""]);
            SpellFields["spell_category"] = DB.ToInt(Request.Form["Category"]);
            //SpellFields["field181"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field182"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field183"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field184"] = DB.ToInt(Request.Form[""]);
            //SpellFields["can_mgb"] = DB.ToInt(Request.Form[""]);
            //SpellFields["nodispell"] = DB.ToInt(Request.Form[""]);
            //SpellFields["npc_category"] = DB.ToInt(Request.Form[""]);
            //SpellFields["npc_usefulness"] = DB.ToInt(Request.Form[""]);
            //SpellFields["MinResist"] = DB.ToInt(Request.Form[""]);
            //SpellFields["MaxResist"] = DB.ToInt(Request.Form[""]);
            //SpellFields["viral_targets"] = DB.ToInt(Request.Form[""]);
            //SpellFields["viral_timer"] = DB.ToInt(Request.Form[""]);
            //SpellFields["nimbuseffect"] = DB.ToInt(Request.Form[""]);
            //SpellFields["ConeStartAngle"] = DB.ToInt(Request.Form[""]);
            //SpellFields["ConeStopAngle"] = DB.ToInt(Request.Form[""]);
            //SpellFields["sneaking"] = DB.ToInt(Request.Form[""]);
            //SpellFields["not_extendable"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field198"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field199"] = DB.ToInt(Request.Form[""]);
            //SpellFields["suspendable"] = DB.ToInt(Request.Form[""]);
            //SpellFields["viral_range"] = DB.ToInt(Request.Form[""]);
            //SpellFields["songcap"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field203"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field204"] = DB.ToInt(Request.Form[""]);
            //SpellFields["no_block"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field206"] = DB.ToInt(Request.Form[""]);
            //SpellFields["spellgroup"] = DB.ToInt(Request.Form[""]);
            //SpellFields["rank"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field209"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field210"] = DB.ToInt(Request.Form[""]);
            //SpellFields["CastRestriction"] = DB.ToInt(Request.Form[""]);
            //SpellFields["allowrest"] = DB.ToInt(Request.Form[""]);
            //SpellFields["InCombat"] = DB.ToInt(Request.Form[""]);
            //SpellFields["OutofCombat"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field215"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field216"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field217"] = DB.ToInt(Request.Form[""]);
            //SpellFields["aemaxtargets"] = DB.ToInt(Request.Form[""]);
            //SpellFields["maxtargets"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field220"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field221"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field222"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field223"] = DB.ToInt(Request.Form[""]);
            //SpellFields["persistdeath"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field225"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field226"] = DB.ToInt(Request.Form[""]);
            //SpellFields["min_dist"] = DB.ToInt(Request.Form[""]);
            //SpellFields["min_dist_mod"] = DB.ToInt(Request.Form[""]);
            //SpellFields["max_dist"] = DB.ToInt(Request.Form[""]);
            //SpellFields["max_dist_mod"] = DB.ToInt(Request.Form[""]);
            //SpellFields["min_range"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field232"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field233"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field234"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field235"] = DB.ToInt(Request.Form[""]);
            //SpellFields["field236"] = DB.ToInt(Request.Form[""]);

            StringBuilder _query = new StringBuilder();

            _query.Append("UPDATE `spells_new` SET ");

            bool _first = true;

            List<OdbcParameter> _parms = new List<OdbcParameter>();

            foreach (KeyValuePair<string, object> _field in SpellFields)
            {
                if (_first)
                {
                    _first = false;
                }
                else
                {
                    _query.Append(','); // Comma between each additional field
                }

                _query.Append('`');
                _query.Append(_field.Key);
                _query.Append("`=?");

                _parms.Add(new OdbcParameter(_field.Key, _field.Value));
            }

            _query.Append(" WHERE `id`=?;");
            _parms.Add(new OdbcParameter("id", DB.ToInt(Request.Form["SpellID"])));

            switch (DB.Execute(_query.ToString(), _parms))
            {
                case 0:
                    return "NNo changes found.";
                case 1:
                    return "YChanges saved.";
                default:
                    return "NError saving changes:\n\n" + HttpContext.Current.Session["DBError"];
            }
        }
    }
}