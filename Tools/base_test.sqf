"XXXX" callExtension "version";


"XXXX" callExtension ["Array|0",[10,[123],5]];
sleep 0.3;
"XXXX" callExtension ["ArrayInner|1",[[10,[123],5,["test",[2]]]]];
sleep 0.3;
"XXXX" callExtension ["Numeric|2",[10,10,10]];
sleep 0.3;
"XXXX" callExtension ["Boolean|3",[true]];
sleep 0.3;
"XXXX" callExtension ["Null|4",[nil]];

freeExtension "XXXX";