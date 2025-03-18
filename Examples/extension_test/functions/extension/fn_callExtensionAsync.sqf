
// RETURNS [data] (ARRAY) if success and NOTHING if failed or _fireAndForget used

params ["_request",["_fireAndForget",false],["_timeout",1]];


// Insert request id to function
private _requestId = if (_fireAndForget) then {-1} else {call EXT_fnc_createId};
_request set [0,((_request#0) + "|" + str(_requestId))];

// Call Extension
diag_log formatText ["REQUEST ASYNC: %1",_request];
private _extReturn = EXT_var_extensionName callExtension _request;
if (_extReturn#1 < 0 || _extReturn#2 > 0) exitWith { // ERROR (Should not be possible)
	EXT_var_extensionResponses deleteAt _requestId;
};
if (_fireAndForget) exitWith { EXT_var_extensionResponses deleteAt _requestId; };



private _success = false;
private _tries = _timeout * 100;
private _return = ["Request timed out!"];
while {_tries > 0} do {
	_data = EXT_var_extensionResponses get _requestId;
	if (!isNil "_data") exitWith {
		_success = ((_data select 1) == 0); // Code 0 == SUCCESS
		_return = _data select 0;
	};
	_tries = _tries - 1;
	uiSleep 0.01;
};

EXT_var_extensionResponses deleteAt _requestId;


if !(_success) exitWith {
	diag_log formatText ["ERROR: %1",_return];
};

diag_log formatText ["SUCCESS WITH DATA: %1",_return];

_return