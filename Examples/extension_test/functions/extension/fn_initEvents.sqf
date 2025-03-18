



addMissionEventHandler ["ExtensionCallback",{
	params ["_name","_function",["_data","[]"]];

	if (_name == "ArmaExtension") then {

		diag_log _this;
		_splitted = (_function splitString "|") params ["_type",["_requestID",-1],["_returnCode",0]];

		_data = parseSimpleArray _data;

		if (_type == "ASYNC_RESPONSE") then {
			if (_requestID == -1) exitWith { diag_log "ERROR: Async Key not included in response!"; };

			if !(_requestID in EXT_var_extensionResponses) exitWith { diag_log format ["ERROR: ID %1 not found!",_requestID]; };
			
			EXT_var_extensionResponses set [_requestID, [_data, _returnCode]];
			
		} else {

		};
	};
}];


EOE_var_eventsReady = true;