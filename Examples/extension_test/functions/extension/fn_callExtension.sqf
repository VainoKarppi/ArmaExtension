
// RETURNS [data] (ARRAY) if success NOTHING if failed!

params ["_request"];


diag_log formatText ["REQUEST: %1",_request];
private _extReturn = "EdenOnlineExtension" callExtension _request;
private _returnData = parseSimpleArray(_extReturn);

//---- HANDLE ERROR
if (_returnData select 0 != "SUCCESS") exitWith {
	diag_log formatText ["ERROR: %1",_returnData select 1];
};

diag_log formatText ["SUCCESS WITH DATA: %1", _returnData select 1];

_returnData select 1