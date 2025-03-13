// Insert request id to function
private _requestId = if (_fireAndForget) then {-1} else {call EOE_fnc_createId};
_request set [0,((_request#0) + "|" + str(_requestId))];