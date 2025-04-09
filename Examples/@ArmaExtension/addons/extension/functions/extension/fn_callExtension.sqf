
// RETURNS [data] (ARRAY) if success NOTHING if failed!

if (isNil "EXT_var_extensionRequests") then {
	private _initSuccess = call EXT_fnc_init;
	if (!_initSuccess) exitWith{};
};

diag_log formatText ["REQUEST: %1", _this];
private _result = EXT_var_extensionName callExtension _this;

private _return = if (_result isEqualType []) then {
	parseSimpleArray (_result select 0)
} else {
	parseSimpleArray _result
};

private _data = (_return select 1);

if (isNil "_data") exitWith {};

if (_return select 0 == "ERROR") exitWith { diag_log formatText ["ERROR: %1", _data select 0] };

diag_log formatText ["SUCCESS WITH DATA: %1", _data select 0];

_data