if !(isNil "EXT_var_extensionResponses") exitWith {};


diag_log "Initializing Extension Test for C# .NET";

// Init variables
EXT_var_extensionResponses = createHashMap;
EXT_var_extensionName = "ArmaExtension";


_version = EXT_var_extensionName callExtension "version";

diag_log formatText ["VERSION: %1",_version];



_request = ["Numeric",[5+5]];
_return = [_request] call EOE_fnc_callExtensionAsync;