
// RETURNS [data] (ARRAY) if success NOTHING if failed!

params ["_request"];


diag_log formatText ["REQUEST: %1",_request];
private _extReturn = "EdenOnlineExtension" callExtension _request;
private _return = parseSimpleArray(_extReturn#0);

//---- HANDLE ERROR
if (_extReturn#1 < 0 || _extReturn#2 > 0) exitWith {
	diag_log formatText ["ERROR: %1",_return];
	if (_extReturn#1 < 0 && {(_return#0) isEqualType ""}) then {
		[_return#0, 1,5] call BIS_fnc_3DENNotification;
	} else {
		["Unknown error! Open Log(s) for more info!", 1,5] call BIS_fnc_3DENNotification; // _return#2 error
	};
};
diag_log formatText ["SUCCESS: %1",_return];
_return