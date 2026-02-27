
using System.Threading.Tasks;
using static ArmaExtension.Logger;

// Do not change the namespace, it is updated automatically by the build system.
// You can create your own namespace, but it needs to be added to linker.xml file using: <type fullname="MyNamespace.*" preserve="all" />
namespace ArmaExtension; 


// This Attribute Prevents AoT trimming of this plugin class.
[ArmaExtensionPlugin]
public static class ArmaMethods {
    public static string Version() {
        return Extension.Version;
    }

    public static double Numeric(double first, double second) {
        Log($"Numeric Method Called: {first}+{second}");
        return first + second;
    }

    public static object[] Boolean(bool input) {
        Log($"Boolean Method Called: {input}");
        return [true, 1000];
    }
    
    
    public static string String(string input)
    {
        Log($"String Method Called: {input}");
        return "Hello Arma 3!";
    }
    public static void Null(bool input) {
        Log($"Null Method Called: {input}");
    }
    public static object[] Array(double first, object[] second, double third) {
        Log("Array Method Called");
        return [1, 2, 3, 4, 5];
    }
    public static object[] ArrayInner(object[] items) {
        Log("ArrayInner Method Called");
        return [1, 2, 3, 4, new object[] { 1 }, 5];
    }
    public static object[] NoArgs() {
        Log("NoArgs Method Called");
        return [1, 2, 3, 4, 5];
    }

    public static async Task AsyncTest(string input = "test") {
        Log($"AsyncTest Method Called with input: {input}");
        await Task.Delay(2000); // Simulate some async work
        Log("AsyncTest Method Completed");
    }

    public static async Task<bool> AsyncReturnTest(string input = "test") {
        Log($"AsyncReturnTest Method Called with input: {input}");
        await Task.Delay(2000); // Simulate some async work
        Log("AsyncReturnTest Method Completed");

        return true;
    }




    // ! INITIALIZED WHEN FIRST EXTENSION CALL IS MADE
    // If just public static void is used in Main(), it will block the Arma 3 until this method is finished
    // If its using public static async Task, this will not block the Arma 3, but events might not have been registered yet.
    public static void Main()
    {
        MethodSystem.RegisterMethods(typeof(ArmaMethods)); // Always register your methods

        // Subscribe to events
        Events.OnVersionCalled += version => Debug($"VersionCalled event triggered with version: {version}");

        Events.OnMethodCalled += methodName => Debug($"MethodCalled event triggered with method: {methodName}");
        Events.OnMethodCalledResponse += (methodName, response, success) => Debug($"MethodCalledResponse event: {methodName} with response count: {response?.Length ?? 0}, success: {success}");

        Events.OnMethodCalledWithArgs += (methodName, args) => Debug($"MethodCalledWithArgs event: {methodName} with args count: {args?.Length ?? 0}");
        Events.OnMethodCalledWithArgsResponse += (methodName, response, success) => Debug($"MethodCalledWithArgsResponse event: {methodName} with response count: {response?.Length ?? 0}, success: {success}");

        Events.OnAsyncTaskStarted += (method, asyncKey, args) => Debug($"AsyncTaskStarted event triggered with method: {method}, asyncKey: {asyncKey}, args count: {args?.Length ?? 0}");
        Events.OnAsyncTaskCompleted += (method, asyncKey, response, success) => Debug($"AsyncTaskCompleted event triggered with method: {method}, asyncKey: {asyncKey}, success: {success}, response count: {response?.Length ?? 0}");
        Events.OnAsyncTaskCancelled += (asyncKey, success) => Debug($"AsyncTaskCancelled event triggered with asyncKey: {asyncKey}, success: {success}");

        Events.OnSendToArma += (method, data) => Debug($"OnSendToArma event triggered with method: {method}, data count: {data?.Length ?? 0}");
        
        Events.OnErrorOccurred += ex => Debug($"ErrorOccurred event triggered: {ex.Message}");


        // Send data to arma
        Extension.SendToArma("test_method1", [1, 2, 3]);
        Extension.SendToArma("test_method2", [true]);
    }

}



