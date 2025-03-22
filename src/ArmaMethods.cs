using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static ArmaExtension.Logger;

namespace ArmaExtension;

public static class Methods {
    public static string Version() {
        return Extension.Version;
    }
    public static double Numeric(double first, double second) {
        Log($"Numeric Method Called: {first}+{second}");
        return first+second;
    }
    public static object[] Boolean(bool input) {
        Log($"Boolean Method Called: {input}");
        return [true,1000];
    }
    public static string String(string input) {
        Log($"String Method Called: {input}");
        return "IS THIS WORKINGgg";
    }
    public static void Null(bool input) {
        Log($"Null Method Called: {input}");
    }
    public static object[] Array(double first, object[] second, double third) {
        Console.WriteLine("Array Method Called");
        return [1, 2, 3, 4, 5];
    }
    public static object[] ArrayInner(object[] items) {
        Console.WriteLine("ArrayInner Method Called");
        return [1, 2, 3, 4,new object[] {1}, 5];
    }
    public static object[] NoArgs() {
        Console.WriteLine("NoArgs Method Called");
        return [1, 2, 3, 4, 5];
    }
}