using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

/// <summary>
/// Summary description for EQInfo
/// </summary>
namespace EQArchitect
{
    public class EQInfo
    {
        public const byte MaxLevel = 90; // For stat ranges calculations

        public enum DDLFieldType
        {
            Name = 1,
            Nick = 2,
            Type = 3
        };

        public static EQList PlayerClasses = new EQList("player_classes.csv");
        public static EQList FullClasses = new EQList("full_class_names.csv");
        public static EQList Skills = new EQList("skills.csv");
        public static EQList TargetTypes = new EQList("target_types.csv");
        public static EQList ResistTypes = new EQList("resistance_types.csv");
        public static EQList Animations = new EQList("animation_descriptions.csv");
        public static EQList Races = new EQList("races.csv");
        public static EQList Zones = new EQList("zones.csv");
        public static EQList SpellEffects = new EQList("spell_effects.csv");
        public static EQList SpellAnimCategories = new EQList("spell_animation_categories.csv");
        public static EQList SpellAnimations = new EQList("spell_animation_descriptions.csv", new string[] { "{0}", "O", "Unknown" });
        public static EQList SpellBeneficials = new EQList("spell_beneficial_detrimental.csv");
        public static EQList SpellCategories = new EQList("spell_categories.csv");
        public static EQList SpellDurationFormulas = new EQList("spell_duration_formulas.csv");
        public static EQList SpellEffectFormulas = new EQList("spell_effect_formulas.csv");

        public static EQList ItemNamesAndIcons = new EQList("SELECT `id`,`name`,`icon` FROM `items` ORDER BY `id`;", "ItemNamesAndIcons", "items");
        public static EQList SpellNamesAndIcons = new EQList("SELECT `id`,`name`,`new_icon`,`goodEffect` FROM `spells_new` ORDER BY `id`;", "SpellNamesAndIcons", "spells_new");

        // NOTE: Used for calculating bonus damage from AAs, but currently ignored by server. Always treated as 0 when loading spells_new from database.
        public static EQList DamageShieldTypes = new EQList("damage_shield_types.csv");

        // Table: merc_spell_list_entries - 2^0 (1) ... 2^16 (65536)
        public static EQList MercSpellTypeFlags = new EQList("mercenary_spell_types.csv");

        public static DateTime SpellEffectTypesLastUpdated;
        public static string Type1SpellEffects;
        public static string Type2SpellEffects;

        public static List<EQList> EQLists;
        
        public static DateTime ListsLastUpdated;

        static EQInfo()
        {
            EQLists = new List<EQList>();

            ItemNamesAndIcons.ExportToBrowser = false;
            SpellNamesAndIcons.ExportToBrowser = false;

            EQLists.Add(PlayerClasses);
            EQLists.Add(FullClasses);
            EQLists.Add(Skills);
            EQLists.Add(TargetTypes);
            EQLists.Add(ResistTypes);
            EQLists.Add(Animations);
            EQLists.Add(Races);
            EQLists.Add(Zones);
            EQLists.Add(SpellEffects);
            EQLists.Add(SpellAnimCategories);
            EQLists.Add(SpellAnimations);
            EQLists.Add(SpellBeneficials);
            EQLists.Add(SpellCategories);
            EQLists.Add(SpellDurationFormulas);
            EQLists.Add(SpellEffectFormulas);
            EQLists.Add(DamageShieldTypes);
            EQLists.Add(MercSpellTypeFlags);
            EQLists.Add(ItemNamesAndIcons);
            EQLists.Add(SpellNamesAndIcons);
            
            CheckLists();
        }

        // Quickly check to see if any of our lists have been changed since the last page view.
        public static void CheckLists()
        {
            foreach (EQList _list in EQLists)
            {
                    
                _list.ReloadIfChanged();

                if (_list.LastUpdated > ListsLastUpdated)
                {
                    ListsLastUpdated = _list.LastUpdated;
                }
            }

            // Build list of Type 1 & 2 Spell Effects for easy Javascript access.
            if (SpellEffects.LastUpdated != SpellEffectTypesLastUpdated)
            {
                StringBuilder _eff1list = new StringBuilder("*");
                StringBuilder _eff2list = new StringBuilder("*");

                for (int _i = 0; _i < SpellEffects.Count; _i++)
                {
                    switch (SpellEffects.Field(_i + SpellEffects.FirstID, "Type"))
                    {
                        case "1":
                            _eff1list.Append(_i.ToString());
                            _eff1list.Append('*');
                            break;
                        case "2":
                            _eff2list.Append(_i.ToString());
                            _eff2list.Append('*');
                            break;
                    }
                }

                Type1SpellEffects = _eff1list.ToString();
                Type2SpellEffects = _eff2list.ToString();

                SpellEffectTypesLastUpdated = SpellEffects.LastUpdated;
            }
        }
    }

    public class EQList
    {
        public const string DefaultUnknown = "Unknown ({0})";

        public string ListName;

        protected string Source;
        protected string SourceTable;

        protected int StartingID;
        protected int HighestID;
        protected int LastSelectedIndex = -1;

        public string[] ColumnNames;
        protected Dictionary<string, int> ColumnFromName;
        public Dictionary<int, string[]> Items;

        protected int IDField;
        protected int NickField;
        protected Dictionary<string, int> IDFromNick;
        protected Dictionary<int, List<SelectListItem>> SelectLists;

        protected string[] Unknown;
        
        public int ColumnCount;
        public DateTime LastUpdated;

        public bool ExportToBrowser = true;

        protected static string ListPath = HttpContext.Current.Server.MapPath("~/lists/");

        // All column types are optional. If Col_ID=0, then the IDs will automatically increment for each entry, starting with 0.
        public EQList(string Query, string ListName, string SourceTable) : this(Query, ListName, SourceTable, null) { }
        public EQList(string Query, string ListName, string SourceTable, string[] UnknownPatterns)
        {
            this.Source = Query;

            this.ListName = ListName;

            this.Unknown = UnknownPatterns;

            this.SourceTable = SourceTable;

            this.LoadDB();
        }
        public EQList(string Filename) : this(Filename, null) { }
        public EQList(string Filename, string[] UnknownPatterns)
        {
            this.Source = Filename;

            // Build ListName from Filename. "this_list_file.csv" becomes "ThisListFile"
            this.ListName = "";
            string[] _filenameParts = Filename.Split('_');
            foreach (string _part in _filenameParts)
            {
                this.ListName += _part.Substring(0, 1).ToUpper() + _part.Substring(1).ToLower();
            }

            int _dot = this.ListName.LastIndexOf('.');

            if (_dot >= 0)
            {
                this.ListName = this.ListName.Substring(0, _dot).Trim(); ;
            }

            this.Unknown = UnknownPatterns;

            this.Load();
        }

        public void Load()
        {
            string _filePath = ListPath + Source;

            this.ColumnNames = null;
            this.ColumnFromName = new Dictionary<string, int>();
            this.Items = new Dictionary<int, string[]>();
            this.SelectLists = null;

            this.IDField = -1;
            this.NickField = -1;
            this.IDFromNick = new Dictionary<string, int>();

            this.StartingID = int.MaxValue;
            this.HighestID = int.MinValue;
            this.LastSelectedIndex = -1;

            this.ColumnCount = -1;

            if (!File.Exists(_filePath))
            {
                // HttpContext.Current.Response.Write("<p>ERROR: No file - " + _filePath + "</p>\r\n");

                return;
            }

            DateTime _fileTime = new FileInfo(_filePath).LastWriteTimeUtc;
            StreamReader _file = new StreamReader(_filePath);

            int _curindex = 0;
            int _lastID = -1;
            string _line;
            bool _ColumnDefinitions = false;

            int _id;

            while (!_file.EndOfStream)
            {
                do
                {
                    _line = _file.ReadLine().Trim();

                    if ((_line.Length > 0) && (_line[0] == ';'))
                    {
                        // Comment line. Is it our field definitions?

                        if ((_curindex == 0) && (ColumnNames == null))
                        {
                            _ColumnDefinitions = true; // Yes!
                            _line = _line.Substring(1).TrimStart();
                        }
                        else
                        {
                            _line = null; // Don't parse comment lines for actual data
                        }
                    }
                    else if ((_line.Length == 0) && (ColumnCount > 1)) // Empty line and we're expecting multiple fields
                    {
                        _line = null; // Skip it.
                    }
                }
                while ((_line == null) && (!_file.EndOfStream));

                if ((_line == null) || _line.Equals("EOF", StringComparison.CurrentCultureIgnoreCase))
                {
                    break; // Ran out of file. We're done.
                }

                string _field = null;
                List<string> _fields = new List<string>();

                int _col = 0; // Current column (1-4)
                int _pos = 0; // Current character position in the line of text
                int _del = -1; // Next delimeter position
                int _len = _line.Length;

                _id = _curindex;

                while (_pos < _len)
                {
                    while ((_pos < _len) && (_line[_pos] == ' '))
                    {
                        _pos++; // Ignore whitespace before our field
                    }

                    if (_pos >= _len)
                    {
                        break; // Ran out of line before the next field started?
                    }

                    if (_line[_pos] == '\"') // Quote-enclosed string. Read to the next quote.
                    {
                        _del = _pos + 1;
                        while (_del < _len)
                        {
                            if (_line[_del] == '\"')
                            {
                                // End of the field. Read to the next one.
                                while (_del < _len)
                                {
                                    if (_line[_del] == ',')
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        _del++;
                                    }
                                }

                                break;
                            }
                            else
                            {
                                _del++;
                            }
                        }

                        _field = _line.Substring(_pos, _del - _pos).TrimEnd();
                    }
                    else
                    {
                        // Non-quote-enclosed string. Read to the next comma.

                        _del = _pos;
                        while (_del < _len)
                        {
                            if (_line[_del] == ',')
                            {
                                break;
                            }
                            else
                            {
                                _del++;
                            }
                        }

                        _field = _line.Substring(_pos, _del - _pos);
                    }

                    if ((_field.Length > 0) && (_field[0] == '\"'))
                    {
                        // Remove enclosing quotes.

                        int _close = _field.LastIndexOf('\"');

                        if ((_close < 0) || (_close == 0))
                        {
                            _field = _field.Substring(1).Trim(); // No closing quote?
                        }
                        else
                        {
                            _field = _field.Substring(1, _close - 1);
                        }
                    }

                    if ((_field.Length > 0) && ((_field[0] == ' ') || (_field[_field.Length - 1] == ' ')))
                    {
                        _field = _field.Trim();
                    }

                    if (_ColumnDefinitions)
                    {
                        if (IDField < 0)
                        {
                            if (_field.EndsWith("ID", StringComparison.CurrentCultureIgnoreCase))
                            {
                                IDField = _col;
                            }
                        }

                        if (NickField < 0)
                        {
                            if (_field.EndsWith("Nick", StringComparison.CurrentCultureIgnoreCase))
                            {
                                NickField = _col;
                            }
                        }

                        ColumnFromName[_field.ToLower()] = _col;
                    }

                    _fields.Add(_field);

                    _pos = _del + 1;
                    _col++;
                }

                if (ColumnCount < 0)
                {
                    ColumnCount = _fields.Count;

                    if (_ColumnDefinitions)
                    {
                        // We have defined column names. Awesome.

                        ColumnNames = _fields.ToArray();
                    }
                    else
                    {
                        // We don't have defined column names. Name them by number for Javascript referencing.
                        
                        List<string> _fieldNames = new List<string>();

                        for (_col = 0; _col < ColumnCount; _col++)
                        {
                            _fieldNames.Add(_col.ToString());
                        }

                        ColumnNames = _fieldNames.ToArray();
                    }
                }

                if (_ColumnDefinitions)
                {
                    _ColumnDefinitions = false;
                }
                else
                {
                    while (_fields.Count < ColumnCount)
                    {
                        _fields.Add(""); // Normalize field counts for each row
                    }

                    if (IDField < 0)
                    {
                        _id = _curindex; // No ID fields in list. ID is the row number, starting with 0.
                    }
                    else if (!int.TryParse(_fields[IDField], out _id))
                    {
                        _id = _lastID + 1; // Bad or missing ID field. Make it the last one we know about plus one.

                        _fields[IDField] = _id.ToString(); // Make sure it's reflected in the Items[] array as well.
                    }

                    if (_id < StartingID)
                    {
                        StartingID = _id;
                    }

                    if (_id > HighestID)
                    {
                        HighestID = _id;
                    }

                    if (NickField >= 0)
                    {
                        IDFromNick[_fields[NickField].ToLower()] = _id;
                    }

                    Items[_id] = _fields.ToArray();

                    _lastID = _id;
                    _curindex++;
                }
            }

            _file.Close();

            this.LastUpdated = _fileTime;
        }

        public bool LoadDB()
        {
            DataTable _data = DB.GetData(this.Source);

            this.ColumnCount = (_data == null) ? 0 : _data.Columns.Count;
            this.ColumnNames = (this.ColumnCount == 0) ? null : new string[this.ColumnCount];
            this.ColumnFromName = new Dictionary<string, int>();
            this.Items = new Dictionary<int, string[]>();
            this.SelectLists = null;

            this.IDField = -1;
            this.NickField = -1;
            this.IDFromNick = new Dictionary<string, int>();

            this.StartingID = int.MaxValue;
            this.HighestID = int.MinValue;
            this.LastSelectedIndex = -1;

            int _col = 0;

            if (_data == null)
            {
                return false;
            }

            foreach (DataColumn _column in _data.Columns)
            {
                string _colName = _column.ColumnName;

                if (IDField < 0)
                {
                    if (_colName.EndsWith("ID", StringComparison.CurrentCultureIgnoreCase))
                    {
                        IDField = _col;
                    }
                }

                if (NickField < 0)
                {
                    if (_colName.EndsWith("Nick", StringComparison.CurrentCultureIgnoreCase))
                    {
                        NickField = _col;
                    }
                }

                ColumnNames[_col] = _colName;
                ColumnFromName[_colName.ToLower()] = _col++;
            }

            List<string> _fields = new List<string>();
            int _curIndex = 0;
            int _id = 0;
            int _lastID = 0;

            foreach (DataRow _row in _data.Rows)
            {
                _fields.Clear();
                
                for (_col = 0; _col < _data.Columns.Count; _col++)
                {
                    _fields.Add(_row[_col].ToString());
                }

                if (IDField < 0)
                {
                    _id = _curIndex; // No ID fields in list. ID is the row number, starting with 0.
                }
                else if (!int.TryParse(_fields[IDField], out _id))
                {
                    _id = _lastID + 1; // Bad or missing ID field. Make it the last one we know about plus one.

                    _fields[IDField] = _id.ToString(); // Make sure it's reflected in the Items[] array as well.
                }

                if (_id < StartingID)
                {
                    StartingID = _id;
                }

                if (_id > HighestID)
                {
                    HighestID = _id;
                }

                if (NickField >= 0)
                {
                    IDFromNick[_fields[NickField].ToLower()] = _id;
                }

                Items[_id] = _fields.ToArray();

                _lastID = _id;
                _curIndex++;
            }

            return true;
        }

        public bool ReloadIfChanged()
        {
            if (SourceTable != null)
            {
                return false; // Don't hit against the database for list updates with every page view. Better to manually run EQList.LoadDB() when changes are made.
            }
            
            if (GetLastUpdateTime() != LastUpdated)
            {
                if (SourceTable == null)
                {
                    Load();
                }
                else
                {
                    LoadDB();
                }

                return true;
            }

            return false;
        }

        public DateTime GetLastUpdateTime()
        {
            if (SourceTable == null)
            {
                return new FileInfo(ListPath + Source).LastWriteTimeUtc;
            }
            else
            {
                string _dbName = (string)HttpContext.Current.Session["DBServerDatabase"];

                //return (DateTime)DB.GetDataValue("SELECT UPDATE_TIME FROM information_schema.tables WHERE TABLE_SCHEMA='{0}' AND TABLE_NAME='{1}';".Replace("{0}", _dbName).Replace("{1}", SourceTable));

                object _time = DB.GetDataValue("SELECT UPDATE_TIME FROM information_schema.tables WHERE TABLE_SCHEMA='{0}' AND TABLE_NAME='{1}';".Replace("{0}", _dbName).Replace("{1}", SourceTable));

                DateTime _datetime;
                
                if (_time == null)
                {
                    _datetime = new DateTime(0);
                }
                else
                {
                    if (!DateTime.TryParse(_time.ToString(), out _datetime))
                    {
                        _datetime = new DateTime(0);
                    }
                }
                
                return _datetime;
            }
        }

        public int Count { get { return (HighestID + 1 - StartingID); } }

        public int FirstID { get { return StartingID; } }

        public int LastID { get { return HighestID; } }

        public string Filename { get { return Source; } }

        public string Field(int ID, string FieldName)
        {
            int _index = 0;

            if (!ColumnFromName.TryGetValue(FieldName.ToLower(), out _index))
            {
                return "";
            }

            return Field(ID, _index);
        }
        public string Field(int ID, int Index)
        {
            if (Index < 0)
            {
                return "";
            }

            string[] _item = null;
            string _value = "";

            if (Items.TryGetValue(ID, out _item))
            {
                if (Index < _item.Length)
                {
                    _value = _item[Index];
                }
                else
                {
                    return "";
                }
            }

            if (_value == "")
            {
                if ((Unknown == null) || (Index >= Unknown.Length))
                {
                    return string.Format(DefaultUnknown, ID);
                }

                return string.Format(Unknown[Index], ID);
            }

            return _value;
        }

        public int Number(int ID, string FieldName)
        {
            int _index = 0;

            if (!ColumnFromName.TryGetValue(FieldName.ToLower(), out _index))
            {
                return 0;
            }

            return Number(ID, _index);
        }
        public int Number(int ID, int Index)
        {
            if (Index < 0)
            {
                return 0;
            }

            string[] _item = null;

            if (!Items.TryGetValue(ID, out _item))
            {
                return 0;
            }

            if (Index >= _item.Length)
            {
                return 0;
            }

            int _value = 0;

            if (!int.TryParse(_item[Index], out _value))
            {
                return 0;
            }

            return _value;
        }

        public string Name(int ID) { return Field(ID, "Name"); }
        public string Nick(int ID) { return Field(ID, NickField); }
        public string Type(int ID) { return Field(ID, "Type"); }

        public int FindIDFromNick(string Nick)
        {
            int _result;

            if ((Nick == null) || (Nick.Length <= 0) || !IDFromNick.TryGetValue(Nick.Trim().ToLower(), out _result))
            {
                return -1;
            }

            return _result;
        }

        public bool IsInList(int ID)
        {
            return Items.ContainsKey(ID);
        }

        public bool IsInRange(int ID)
        {
            return (ID >= StartingID) && (ID <= HighestID);
        }

        // Using a customized version of SelectList that allows us to add an unknown selected value to the list as needed, instead of crashing.
        
        // It would be better structured to create a custom SelectList-derived class that handles a missing SelectedValue item by creating one to return, but
        // this works perfectly fine in the meantime.  The only performance hit is in searching the List for the right SelectedIndex based on SelectedValue.
        public List<SelectListItem> GetSelectList()
        {
            return GetSelectList(null, "Name", null);
        }
        public List<SelectListItem> GetSelectList(object SelectedValue)
        {
            return GetSelectList(SelectedValue, "Name", null);
        }
        public List<SelectListItem> GetSelectList(object SelectedValue, string FieldName)
        {
            return GetSelectList(SelectedValue, FieldName, null);
        }
        public List<SelectListItem> GetSelectList(object SelectedValue, string FieldName, string PrependIDFormat)
        {
            List<SelectListItem> _list = null;

            int _colIndex;

            if (!ColumnFromName.TryGetValue(FieldName.ToLower(), out _colIndex))
            {
                // Invalid field specified. We can't load this list.
                
                return new List<SelectListItem>();
            }

            if (SelectLists == null)
            {
                SelectLists = new Dictionary<int, List<SelectListItem>>();
            }
            else
            {
                SelectLists.TryGetValue(_colIndex, out _list);
            }

            if (_list == null)
            {
                // No SelectList for this column yet. Build one.
                LastSelectedIndex = -1;

                _list = new List<SelectListItem>();

                for (int _id = StartingID; _id < HighestID; _id++)
                {
                    if (IsInList(_id))
                    {
                        SelectListItem _item = new SelectListItem();
                        
                        _item.Value = _id.ToString();
                        
                        if (PrependIDFormat == null)
                        {
                            _item.Text = Field(_id, FieldName);
                        }
                        else
                        {
                            _item.Text = string.Format(PrependIDFormat, _id) + Field(_id, FieldName);
                        }

                        _list.Add(_item);
                    }
                }
            }

            if (LastSelectedIndex >= 0)
            {
                if ((SelectedValue != null) && (_list[LastSelectedIndex].Value.Equals(SelectedValue.ToString())))
                {
                    // Keeping same selected value from last time. We don't need to search the list for it again.
                }
                else
                {
                    // Selected value has changed. Unselect the previous one.
                    _list[LastSelectedIndex].Selected = false;
                    LastSelectedIndex = -1;
                }
            }

            if ((SelectedValue != null) && (LastSelectedIndex < 0))
            {
                // Find the selected value in the list, and select it.
                
                int _val = DB.ToInt(SelectedValue);
                string _value = _val.ToString();

                if (!IsInList(_val))
                {
                    // SelectedValue not in our list. Create it and select it.

                    string _text = Field(_val, "Name");

                    SelectListItem _item = new SelectListItem();

                    _item.Value = _value;
                    _item.Text = _text;

                    _list.Add(_item);

                    LastSelectedIndex = (_list.Count - 1);
                }
                else
                {
                    // SelectedValue is in our list and not yet selected. Find it and select it.
                    
                    for (int _i = 0; _i < _list.Count; _i++)
                    {
                        if (_list[_i].Value.Equals(_value))
                        {
                            LastSelectedIndex = _i;
                            break;
                        }
                    }
                }


                _list[LastSelectedIndex].Selected = true;
            }

            SelectLists[_colIndex] = _list;

            return _list;
        }
    }
}