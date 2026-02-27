// !!! WARNING !!!
//
// DO NOT CHANGE THE "XXXX" PART!
// THIS IS AUTOMATICALLY UPDATED TO YOUR EXTENSION NAME BASED ON ASSEMBLY NAME!
//
// !!!!!!!!!!!!!!!

"XXXX" callExtension "version";


// Returns list of available methods in the extension, and data types: [["Version",[["ip","String",false],["port","Number",false]],"String",false], [...]] : [methodName, inputParameterTypes, outputType, asyncRequired]
"XXXX" callExtension "GET_AVAILABLE_METHODS";

sleep 0.3;

// RETURNS EITHER: ["[""ASYNC_CANCEL_SUCCESS"",[]]",returnCode, errorCode] OR ["[""ASYNC_CANCEL_SUCCESS"",[]]", returnCode, errorCode]
"XXXX" callExtension "ASYNC_CANCEL|9999";

sleep 0.3;

// RETURNS EITHER: [["ASYNC_STATUS_RUNNING",[]],returnCode, errorCode] OR [["ASYNC_STATUS_NOT_FOUND",[]], returnCode, errorCode]
"XXXX" callExtension "ASYNC_STATUS|9999";

sleep 0.3;


// When using async key, data is returned to Arma using addMissionEventHandler ["ExtensionCallback",[]]
"XXXX" callExtension ["Array|0",[10,[123],5]];
sleep 0.3;
"XXXX" callExtension ["ArrayInner|1",[[10,[123],5,["test",[2]]]]];
sleep 0.3;
"XXXX" callExtension ["Numeric|2",[10,10,10]];
sleep 0.3;
"XXXX" callExtension ["Boolean|3",[true]];
sleep 0.3;
"XXXX" callExtension ["String|5",["asdasd"]];
sleep 0.3;
"XXXX" callExtension ["Null|4",[nil]];



sleep 0.3;
"XXXX" callExtension ["Numeric",[10,10]];

sleep 0.3;
"XXXX" callExtension "NoArgs";
sleep 0.3;
"XXXX" callExtension "NoArgs|3333";
sleep 0.3;
"XXXX" callExtension "Numeric";
sleep 0.3;
"XXXX" callExtension ["String",["asdasd"]];


"XXXX" callExtension ["AsyncReturnTest|2",[]]
sleep 0.3;

// This will throw error: Task with asyncKey 2 is already running
"XXXX" callExtension ["AsyncTest|2",[]]
sleep 0.3;

// This will throw error
"XXXX" callExtension ["AsyncTest",[]]

// This will throw: [["ERROR",["Invalid Method"]],1,0]
"XXXX" callExtension ["MethodThatDoesNotExist",[]]

sleep 0.3;
freeExtension "XXXX";