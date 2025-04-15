
// ["Numeric",[10+10]] call EXT_fnc_callExtensionAsync


params [["_function","",[""]],["_arguments",[],[[]]],["_fireAndForget",false,[false]],["_timeout",1,[0]]];

if (isNil "EXT_var_extensionRequests") then {
	private _initSuccess = call EXT_fnc_init;
	if (!_initSuccess) exitWith {};
};

// Insert request id to function
private _requestId = if (_fireAndForget) then {-1} else {call EXT_fnc_createAsyncId};

EXT_var_extensionRequests set [_requestId,(_function)];

_function = _function + "|" + str(_requestId); // Add ASYNC key to request

private _request = [_function,_arguments];

// Call Extension
diag_log formatText ["REQUEST ASYNC: %1",_request];
private _result = EXT_var_extensionName callExtension _request;
if (_result isEqualTo "" || _fireAndForget) exitWith { EXT_var_extensionRequests deleteAt _requestId; "asd"}; // Extension not found or fire and forget.


private _return = [];
if (_result isEqualType []) then {
	_return = parseSimpleArray (_result select 0);
} else {
	_return = parseSimpleArray _result;
};

private _returnMessage = _return select 0;
private _returnData = _return select 1;


if (_returnMessage == "ERROR") exitWith {
	EXT_var_extensionRequests deleteAt _requestId;
	diag_log formatText ["ERROR: %1", _return select 1];
};

diag_log formatText ["WAITING FOR REPSONSE FOR REQUEST: %1", _requestId];



_return = nil;
private _success = false;
private _tries = _timeout * 50;
private _returnData = "Request timed out!";
while {_tries > 0} do {
	if !(_requestId in EXT_var_extensionRequests) exitWith { diag_log "ERROR: Request has been canceled!"; "asdasdasd"};
	_return = EXT_var_extensionResponses get _requestId;

	if (!isNil "_return") exitWith {
		_returnData = _return select 0;
		_success = _return select 1 == 0; // Error if code: !0
	};
	
	if (canSuspend) then {uiSleep 0.05};
	_tries = _tries - 1;
};

//EXT_var_extensionResponses deleteAt _requestId;
//EXT_var_extensionRequests deleteAt _requestId;

if !(_success) exitWith { diag_log formatText ["ERROR: %1", _returnData]; _returnData};

diag_log formatText ["SUCCESS WITH DATA: %1",_returnData];

_returnData