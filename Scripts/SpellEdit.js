// SpellEdit.js

function DDL(DropdownListName, ListName, SelectedValue, IDField, TextField, TypeFilter, PrependSeparator)
{
    PopulateDropdownList(DropdownListName, ListName, SelectedValue, IDField, TextField, TypeFilter, PrependSeparator);
}

function SpellIcon_Flash()
{
    if (!selectedIcon)
    {
        selectedIcon = el("SpellIcon_" + el("IconID").value);
    }

    if (selectedIcon)
    {
        selectedIcon.className = StringSame(selectedIcon.className, "IconSelected") ? "IconSelected2" : "IconSelected";
    }
}

function Window_Close(id)
{
    hideEl(id);
}

function Windows_CloseAll(event)
{
    var item = event.srcElement ? event.srcElement : event.target;

    switch (item.tagName.toLowerCase())
    {
        case "img":
        case "video":
        case "button":
        case "input":
        case "select":
            break;
        default:
            Window_Close("SpellIcons");
            //Window_Close("SpellEffects"); Apply/Cancel buttons for this one.
            //Window_Close("SpellDuration"); Apply/Cancel buttons for this one.
            el("SpellAnimPreview").pause();
            Window_Close("SpellAnimPreview");
            break;
    }
}

function SpellIcon_Choose()
{
    var _panel = el("SpellIcons");

    _panel.scrollTop = el("SpellIcon_" + el("IconID").value).offsetTop - 52;

    showEl(_panel);

    return false;
}

function SpellIcon_Apply(IconNum)
{
    var _iconField = el("IconID");

    if (IconNum != _iconField.value)
    {
        el("SpellIcon_" + _iconField.value).className = "";

        el("SpellIcon_" + IconNum).className = "IconSelected";

        el("HeaderSpellIcon").src = "/EQArchitect/icons/" + IconNum + ".gif";
        el("SpellIcon").src = "/EQArchitect/icons/" + IconNum + ".gif";
        el("favicon").href = "/EQArchitect/icons/" + IconNum + ".gif";

        _iconField.value = IconNum;

        selectedIcon = null;
    }

    return false;
}

function SpellAnim_Preview()
{
    var _anim = el("AnimID").value;

    if (_anim != "")
    {
        el("SpellAnimPreview").src = "/EQArchitect/spell_anims/" + _anim + ".mp4";
        el("SpellAnimPreview").load();

        showEl("SpellAnimPreview");

        setTimeout("document.getElementById(\"SpellAnimPreview\").play();", 1);
    }

    return false;
}

function SpellAnimCat_Changed(FromDropDown)
{
    if (FromDropDown)
    {
        matchFieldToList("AnimIDCategory", "AnimCategoriesList");

        el("AnimCategoriesList").blur();
    }

    var _cat = el("AnimIDCategory").value * 1;

    for (var _i = 0; _i < 99; _i++)
    {
        var _list = document.getElementById("AnimList" + _i);

        if (_list)
        {
            _list.style.visibility = (_i == _cat) ? "visible" : "";
        }
        else
        {
            break;
        }
    }

    SpellAnim_Changed();

    return false;
}

function SpellAnim_Changed()
{
    // Changed Spell Animation

    matchFieldToList("AnimID", "AnimList" + el("AnimIDCategory").value);

    return false;
}

function Name_Changed()
{
    el("HeaderSpellName").innerHTML = el("Name").value;
}

function Duration_Choose()
{
    CopyField("DurFormulaActual", "DurFormula");
    CopyField("DurationActual", "Duration");

    Duration_Changed(false);

    showEl("SpellDuration");

    return false;
}

function Duration_Changed(fromDropDown)
{
    if (fromDropDown)
    {
        matchFieldToList("DurFormula", "DurFormulasList");
    }
    else
    {
        matchListToField("DurFormulasList", "DurFormula");
    }

    el("DurationPreview").innerHTML = GetDurationDescription(el("Duration").value, el("DurFormula").value);
}

function Duration_Apply()
{
    Window_Close("SpellDuration");

    CopyField("Duration", "DurationActual");
    CopyField("DurFormula", "DurFormulaActual");

    var _formula = el("DurFormula").value;

    el("TextSpellDuration").innerHTML = GetDurationDescription(el("Duration").value, _formula);

    _formula = (_formula != "0");

    // If duration changes, effects may change (Nuke to DoT, Heal to HoT, etc.)
    for (var _i = 1; _i <= 12; _i++)
    {
        Effect_DescribeForSlot(_i, _formula);
    }

    return false;
}

function Duration_Cancel()
{
    Window_Close("SpellDuration");

    CopyField("DurFormulaActual", "DurFormula");
    CopyField("DurationActual", "Duration");

    return false;
}

function Effect_ChooseForSlot(slot)
{
    el("EffectSlotBeingEdited").value = slot;
    el("EffectSlotHeader").innerHTML = slot;

    CopyField("EffectID" + slot, "EffectID" + "");
    CopyField("EffectFormula" + slot, "EffectFormula" + "");
    CopyField("EffectBase" + slot, "EffectBase" + "");
    CopyField("EffectLimit" + slot, "EffectLimit" + "");
    CopyField("EffectMax" + slot, "EffectMax" + "");

    EffectLayout_Reset();
    EffectLayout_Prep();

    EffectID_Changed(false);

    showEl("SpellEffects");

    return false;
}

function Effect_DescribeForSlot(slot, hasduration)
{
    el("TextEffect" + slot).innerHTML = GetEffectDescription(slot, hasduration);

    return false;
}

function Effect_Apply()
{
    Window_Close("SpellEffects");

    switch (el("EffectID").value)
    {
        case "58": // Illusion
            matchFieldToList("EffectBase", "RacesList");
            break;
        case "83": // Teleport To:
        case "106": // Beastlord Pet
            CopyField("EffectData", "TeleportZone");
            break;
    }

    var _slot = el("EffectSlotBeingEdited").value;

    CopyField("EffectID" + "", "EffectID" + _slot);
    CopyField("EffectFormula" + "", "EffectFormula" + _slot);
    CopyField("EffectBase" + "", "EffectBase" + _slot);
    CopyField("EffectLimit" + "", "EffectLimit" + _slot);
    CopyField("EffectMax" + "", "EffectMax" + _slot);

    Effect_DescribeForSlot(_slot, el("DurFormula").value != 0);

    EffectLayout_Reset();

    return false;
}

function Effect_Cancel()
{
    Window_Close("SpellEffects");

    EffectLayout_Reset();

    return false;
}

function SpellEdit_ReceiveData(DataKey, Data)
{
    switch (DataKey)
    {
        case "SummonItemEffect":
            el("EffectData").value = Data;
            el("EffectPreview").innerHTML = GetEffectDescription("", el("DurFormulaActual").value != "0", SelectedValue("EffectPreviewLevel"));
            break;
    }
}

function Effect_Changed()
{
    switch (el("EffectID").value)
    {
        case "32": // Summon Item
            RequestData(MakeGetURL("ItemName/" + el(EffectField_Base).value), "SummonItemEffect", SpellEdit_ReceiveData);
            return;
    }

    el("EffectPreview").innerHTML = GetEffectDescription("", el("DurFormulaActual").value != "0", SelectedValue("EffectPreviewLevel"));
}

function EffectID_Changed(fromDropDown)
{
    if (fromDropDown)
    {
        matchFieldToList("EffectID", "EffectIDsList");
        matchFieldToList("EffectFormula", "EffectFormulasList");
    }

    EffectLayout_Reset();
    EffectLayout_Prep();
}

function EffectLayout_Prep()
{
    switch (el("EffectID").value)
    {
        case "16": // NPC Frenzy Radius (Not Used)
        case "17": // NPC Awareness (Not Used)
        case "21": // Stun
        case "23": // Fear
            el(EffectField_Max + "Label").innerHTML = "Max NPC Level:";
            break;
        case "32": // Summon Item
            el(EffectField_Base + "Label").innerHTML = "ItemID:";
            break;
        case "58": // Illusion
            hideEl(EffectField_Base);
            hideEl(EffectField_Base + "Label");
            hideEl(EffectField_Max);
            hideEl(EffectField_Max + "Label");

            matchListToValue("RacesList", CopyField(EffectField_Base, EffectField_Data));

            showEl("RacesList");
            showEl("RacesListLabel");
            break;
        case "83": // Teleport To:
            el(EffectField_Data).className = "form-control NumberField ShortField";
            CopyField("TeleportZone", EffectField_Data);

            hideEl(EffectField_Base + "Label");
            hideEl(EffectField_Base);
            hideEl(EffectField_Max + "Label");
            hideEl(EffectField_Max);
            hideEl(EffectField_Data + "Label");
            hideEl(EffectField_Data);
            hideEl("EffectPreviewLevelLabel");
            hideEl("EffectPreviewLevel");

            showEl(EffectField_Data + "LabelB1").innerHTML = "X:";
            showEl(EffectField_Data + "B1").value = el(EffectField_Base + "1").value;
            showEl(EffectField_Data + "LabelB2").innerHTML = "Y:";
            showEl(EffectField_Data + "B2").value = el(EffectField_Base + "2").value;
            showEl(EffectField_Data + "LabelB3").innerHTML = "Z:";
            showEl(EffectField_Data + "B3").value = el(EffectField_Base + "3").value;
            showEl(EffectField_Data + "LabelB4").innerHTML = "Heading:";
            showEl(EffectField_Data + "B4").value = el(EffectField_Base + "4").value;

            Backups["EffectField_Base"] = EffectField_Base;

            EffectField_Base = EffectField_Data + "B";

            matchListToField("ZonesList", EffectField_Data);

            showEl("ZonesList");
            showEl("ZonesListLabel");
            break;
        case "85": // Add Melee Proc
            el(EffectField_Base + "Label").innerHTML = "SpellID:";
            break;
        case "33": // Summon Mage Pet
        case "71": // Summon Necromancer Pet
        case "106": // Summon Beastlord Pet
            el(EffectField_Data + "Label").innerHTML = "PetID:";
            CopyField("TeleportZone", EffectField_Data);
            break;
        case "184": // Increase (skill) hit chance
            _limitlabel.innerHTML = "SkillID:";
            break;
        default:
            break;
    }

    Effect_Changed();
}

function EffectLayout_Reset()
{
    // Set default labels
    restoreEl(EffectField_Data + "Label");
    restoreEl(EffectField_Data).className = "form-control TextField ShortField";
    restoreEl(EffectField_Base + "Label").innerHTML = "Base:";
    restoreEl(EffectField_Base);
    restoreEl(EffectField_Limit + "Label").innerHTML = "Limit:";
    restoreEl(EffectField_Limit);
    restoreEl(EffectField_Max + "Label").innerHTML = "Max:";
    restoreEl(EffectField_Max);

    restoreEl("RacesList");
    restoreEl("RacesListLabel");
    restoreEl("ZonesList");
    restoreEl("ZonesListLabel");

    for (var _labelIndex = 1; _labelIndex < 5; _labelIndex++)
    {
        restoreEl(EffectField_Data + "LabelB" + _labelIndex);
        restoreEl(EffectField_Data + "B" + _labelIndex);
    }

    restoreEl("EffectPreviewLevelLabel");
    restoreEl("EffectPreviewLevel");

    matchListToField("EffectIDsList", "EffectID");
    matchListToField("EffectFormulasList", "EffectFormula");

    if (Backups["EffectField_Base"])
    {
        EffectField_Base = Backups["EffectField_Base"];
    }
}

function ZonesList_Changed(fromDropDown)
{
    var _list = el("ZonesList");
    var _zoneNick;

    if (fromDropDown)
    {
        var _zoneID = SelectedValue(_list);

        if (Zones[_zoneID] != undefined)
        {
            _zoneNick = Zones[_zoneID]["Nick"];

            el("EffectData").value = _zoneNick;
        }
    }
    else
    {
        _zoneNick = el("EffectData");

        for (var _zone = 0; _zone < _list.options.length; _zone++)
        {
            var _option = _list.options[_zone];
            var _id = _option.value;

            _option.selected = (Zones[_id] && StringSame(Zones[_id]["Nick"], _zoneNick));
        }
    }

    Effect_Changed();
}

function RacesList_Changed(fromDropDown)
{
    if (fromDropDown)
    {
        matchFieldToList("EffectData", "RacesList");
    }
    else
    {
        matchListToField("RacesList", "EffectData");
    }
}

function Beneficial_SetFor(item, IsDet)
{
    item = el(item);

    item.className = item.className.replace("BenSpell", "").replace("DetSpell", "").replace("  ", " ") + (IsDet ? " DetSpell" : " BenSpell")
}

function Beneficial_Changed()
{
    matchFieldToList("IsBen", "IsBenList");

    var _isDet = el("IsBen").value == "0";

    Beneficial_SetFor("HeaderSpellIcon", _isDet);
    Beneficial_SetFor("SpellIcon", _isDet);
}

function Animation_Choose(Type)
{
    el("Button" + Type + "Anim").blur();

    return false;
}