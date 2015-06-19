
// For debugging, should be false to catch errors. Can decide when live whether to show or not.
var EQA_HideErrors = false;

var Backups = {};

function BackupField(Field)
{
    var _el = el(Field);

    if (_el)
    {
        Backups[_el.id] = _el.value;
    }
    else
    {
        if (!EQA_HideErrors)
        {
            alert("BackupField: Field [" + Field + "] not found for backing up value from.");
        }
    }
}

function RestoreField(Field, OptionalAssociatedDropDownList)
{
    var _el = el(Field);

    if (_el)
    {
        _el.value = Backups[_el.id];
        
        if (OptionalAssociatedDropDownList)
        {
            matchListToField(OptionalAssociatedDropDownList, Field);
        }
    }
    else
    {
        if (!EQA_HideErrors)
        {
            alert("RestoreField: Field [" + Field + "] not found for restoring value to.");
        }
    }
}

function CopyField(Source, Destination)
{
    return (el(Destination).value = el(Source).value);
}

function IsString(Variable)
{
    return (typeof Variable == "string" || Variable instanceof String);
}

function IsBlank(Variable)
{
    if (Variable == null)
    {
        return true;
    }

    if (Variable == "")
    {
        return true;
    }

    return false;
}

function StringSame(String1, String2)
{
    if (IsString(String1) && IsString(String2))
    {
        return String1.toUpperCase() == String2.toUpperCase();
    }

    if (!EQA_HideErrors)
    {
        alert("StringSame: Invalid string argument passed!");
    }

    return null;
}

// Utility function to return the element with the specified id.
// If id is already the element and not the name, return the element.
function el(id)
{
    if (IsString(id))
    {
        var _result = document.getElementById(id);

        if (!_result)
        {
            if (EQA_HideErrors)
            {
                // No such element. Create a dummy one to avoid an error.

                _result = document.createElement("input");
                _result.type = "text";
            }
            else
            {
                alert("el: Element [" + elid(id) + "] not found!");
            }
        }

        return _result;
    }
    else
    {
        return id;
    }
}

function elid(id)
{
    if (IsString(id))
    {
        return id;
    }
    else
    {
        return el(id).id;
    }
}

function hideEl(id)
{
    var _element = el(id);

    if (_element)
    {
        el(id).style.visibility = "hidden";
    }
    else if (!EQA_HideErrors)
    {
        alert("hideEl: Element [" + elid(id) + "] not found!");
    }

    return _element;
}

function showEl(id)
{
    var _element = el(id);

    if (_element)
    {
        el(id).style.visibility = "visible";
    }
    else if (!EQA_HideErrors)
    {
        alert("showEl: Element [" + elid(id) + "] not found!");
    }

    return _element;
}

function restoreEl(id)
{
    var _element = el(id);

    if (_element)
    {
        el(id).style.visibility = "";
    }
    else if (!EQA_HideErrors)
    {
        alert("restoreEl: Element [" + elid(id) + "] not found!");
    }

    return _element;
}

function SelectedValue(DropDownList)
{
    var _list = el(DropDownList);

    if (!DropDownList)
    {
        if (!EQA_HideErrors)
        {
            alert("SelectedValue: List [" + elid(DropDownList) + "] not found!");
        }

        return "";
    }

    var _opts = _list.options;

    if (!_opts)
    {
        if (!EQA_HideErrors)
        {
            alert("SelectedValue: Element [" + elid(DropDownList) + "] has no DropDownList options!");
        }

        return "";
    }

    var _index = _list.selectedIndex;

    if (_index < 0)
    {
        return "";
    }

    return _opts[_index].value;
}

function matchListToValue(list, value)
{
    var _list = el(list);

    if (!_list)
    {
        if (!EQA_HideErrors)
        {
            alert("matchListToValue: List [" + elid(list) + "] not found!");
        }

        return;
    }

    if (_list.selectedIndex == undefined)
    {
        if (!EQA_HideErrors)
        {
            alert("matchListToValue: Element [" + elid(list) + "] does not have DropDownList options!");
        }

        return;
    }

    if (StringSame(_list.id, "EffectFormulasList"))
    {
        if ((value > 0) && (value < 100) && (value != 60) && (value != 70))
        {
            value = 1; // (1 - 99)
        }
        else if((value > 1000) && (value < 2000))
        {
            value = 1001; // (1001 - 1999)
        }
        else if ((value > 2000) && (value < 3000))
        {
            value = 2001; // (2001 - 2999)
        }
    }

    if ((_list.selectedIndex < 0) || (_list.options[_list.selectedIndex].field != value))
    {
        for (var _i = 0; _i < _list.options.length; _i++)
        {
            if (_list.options[_i].value == value)
            {
                _list.selectedIndex = _i;
                break;
            }
        }

        if (_i >= _list.options.length)
        {
            _list.selectedIndex = -1;
        }
    }
}

function matchListToField(list, field)
{
    var _field = el(field);

    if (_field)
    {
        matchListToValue(list, _field.value);
    }
    else if (!EQA_HideErrors)
    {
        alert("matchListToField: Cannot match list [" + elid(list) + "] to nonexistent field [" + elid(field) + "]");
    }
}

function matchFieldToList(field, list)
{
    var _field = el(field);
    var _list = el(list);

    if (!_list && !EQA_HideErrors)
    {
        alert("matchFieldToList: List [" + elid(list) + "] not found!");
    }

    if (!_field && !EQA_HideErrors)
    {
        alert("matchFieldToList: Field [" + elid(field) + "] not found!");
    }

    if (_field && _list)
    {
        var _value = SelectedValue(_list);

        if (_field.value != _value)
        {
            _field.value = _value;
        }
    }
}

// Example URLs:
//
// SpellName/99917
// ItemName/1001
function FillElementWithRequest(URL, Element)
{
    $.ajax({
        url: SpellRootPath + "/Get/" + URL,
        type: "GET",
        async: false,
        context: Element,
        success: function (result)
        {
            var _comment = result.indexOf("<!--");
            if (_comment >= 0)
            {
                result = result.substr(0, _comment);
            }
            this.innerHTML = result;
        }
    });
}

function FillFieldWithRequest(URL, Element)
{
    $.ajax({
        url: SpellRootPath + "/Get/" + URL,
        type: "GET",
        async: false,
        context: Element,
        success: function (result)
        {
            var _comment = result.indexOf("<!--");
            if (_comment >= 0)
            {
                result = result.substr(0, _comment);
            }
            this.value = result;
        }
    });
}

function CopyIDsToNames(FormName)
{
    var _form = el(FormName);

    if (!_form)
    {
        return;
    }

    var _fields = _form.elements;

    for (var _fieldNum = 0, _field; _field = _fields[_fieldNum++];)
    {
        switch (_field.tagName)
        {
            case "INPUT":
            case "SELECT":
            case "TEXTAREA":
            case "BUTTON":
                if (!IsBlank(_field.id) && IsBlank(_field.name))
                {
                    _field.name = _field.id;
                }
                break;
        }
    }
}