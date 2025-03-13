



addMissionEventHandler ["ExtensionCallback",{
	params ["_name",["_function","[]"],["_data","[]"]];

	if (_name == "EdenOnlineExtension") then {
		/*
			TYPES:
			0 = callback
			1 = callFunction
			2 = callCommand
		*/
		diag_log _this;
		_function = parseSimpleArray _function;
		_data = parseSimpleArray _data;
		_splitted = (_function splitString "|") params ["_type",["_id",-1],["_success",false]];
		if (_type == "ASYNC_RESPONSE") then {
			_id = (_splitted select 1);
			_success = _splitted select 2;
			if !(_id in EXT_var_extensionResponses) then {
				[(format ["ERROR: ID %1 not found!",_id]), 1,5] call BIS_fnc_3DENNotification;
				diag_log format ["ERROR: ID %1 not found!",_id];
			} else {
				EXT_var_extensionResponses set [_id,parseSimpleArray _data,call compile _success];
			};
		} else {
			if (_type == "callfunction" || _type == "callCommand") then {
				_function = _splitted select 1;
				diag_log formatText ["_type:%1 | _function:%2 | _params: %3",_type,_function,_params];
				if (_type == "callfunction") then {
					_function = "EXT_fnc_" + _function;
					_data call (missionNamespace getVariable [_function,{diag_log ("ERROR: Function: " + _function + ", was not found!")}]);
				} else {
					_data remoteExec [_function,0];
				};
			};
		};
	};
}];

// TODO ADD ALL 3DEN EVENTS


EOE_var_eventsReady = true;