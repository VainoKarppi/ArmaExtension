using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static ArmaExtension.Logger;

namespace ArmaExtension;
public static class Extension {
    public readonly static string AssemblyDirectory = GetAssemblyLocation();
    public readonly static string Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString()!;
    public readonly static string ExtensionName = GetAssemblyName();
    
    public enum ReturnCodes {
        Success = 0,
        Error = 1,
        InvalidMethod = 2,
        InvalidParameters = 3
    }
    
    public enum ResultCodes {
        SUCCESS,
        SUCCESS_VOID,
        ERROR,
        ASYNC_RESPONSE,
        ASYNC_SENT,
        ASYNC_SENT_VOID,
        ASYNC_FAILED,
        ASYNC_CANCEL_SUCCESS,
        ASYNC_CANCEL,
        ASYNC_CANCEL_FAILED,
        ASYNC_SUCCESS,
        CALLFUNCTION
    }

    

    
    
    /// <summary>
    /// Called only once when Arma 3 loads the extension.
    /// </summary>
    /// <param name="func">Pointer to Arma 3's callback function</param>
    [UnmanagedCallersOnly(EntryPoint = "RVExtensionRegisterCallback")]
    public static unsafe void RvExtensionRegisterCallback(delegate* unmanaged<string, string, string, int> callback) { 
        Callback = callback; 
    }
    internal static unsafe delegate* unmanaged<string, string, string, int> Callback;



    /// <summary>
    /// Called only once when Arma 3 loads the extension.
    /// The output will be written in the RPT logs.
    /// </summary>
    /// <param name="output">A pointer to the output buffer</param>
    /// <param name="outputSize">The maximum length of the buffer (always 32 for this particular method)</param>
    [UnmanagedCallersOnly(EntryPoint = "RVExtensionVersion")]
    public static void RVExtensionVersion(nint output, int outputSize) {
        Log($"\n==============================================================\nExtension ({ExtensionName}) Started | {AssemblyDirectory} | {Version} |\n==============================================================");
        WriteOutput(output, outputSize, "Version", Version);
    }



    /// <summary>
    /// The entry point for the default "callExtension" command.
    /// </summary>
    /// <param name="output">A pointer to the output buffer</param>
    /// <param name="outputSize">The maximum size of the buffer (20480 bytes)</param>
    /// <param name="function">The function identifier passed in "callExtension"</param>
    [UnmanagedCallersOnly(EntryPoint = "RVExtension")]
    public static int RVExtension(nint output, int outputSize, nint function) {
        string method = Marshal.PtrToStringAnsi(function) ?? string.Empty;

        return ExecuteArmaMethod(output, outputSize, method);
    }



    /// <summary>
    /// The entry point for the "callExtension" command with function arguments.
    /// </summary>
    /// <param name="output">A pointer to the output buffer</param>
    /// <param name="outputSize">The maximum size of the buffer (20480 bytes)</param>
    /// <param name="function">The function identifier passed in "callExtension"</param>
    /// <param name="argv">The args passed to "callExtension" as a string array</param>
    /// <param name="argc">Number of elements in "argv"</param>
    /// <returns>The return code</returns>
    [UnmanagedCallersOnly(EntryPoint = "RVExtensionArgs")]
    public static int RVExtensionArgs(nint output, int outputSize, nint function, nint args, int argsCnt) {
        
        // Get Method Name
        string method = Marshal.PtrToStringAnsi(function) ?? string.Empty;

        // Get Args
        string[] argArray = new string[argsCnt];
        for (int i = 0; i < argsCnt; i++) {
            nint argPtr = Marshal.ReadIntPtr(args, i * nint.Size);
            argArray[i] = Marshal.PtrToStringAnsi(argPtr) ?? string.Empty;
        }

        return ExecuteArmaMethod(output, outputSize, method, argArray);
    }







    public static void SendAsyncCallbackMessage(string method, object?[] data, int errorCode = 0, int asyncKey = -1) {
        if (string.IsNullOrEmpty(method)) Log("Empty function name in SendCallbackMessage.");

        method += $"|{asyncKey}|{errorCode}";

        string returnData = Serializer.PrintArray(data);

        Log(@$"CALLBACK TO ARMA >> [""{ExtensionName}"", ""{method}"", ""{returnData}""]");

        try {
            unsafe { Callback(ExtensionName, method, returnData); }
        } catch (Exception ex) {
            Log(ex.Message);
        }
    }
    

    private static int WriteOutput(nint output, int outputSize, string methodName, string message, int returnCode = 0) {
        Log(@$"RESPONSE FOR METHOD: ({methodName}) >> {message}");

        byte[] bytes = Encoding.ASCII.GetBytes(message);
        int length = Math.Min(bytes.Length, outputSize - 1);
        Marshal.Copy(bytes, 0, output, length);
        Marshal.WriteByte(output, length, 0);

        return returnCode;
    }

    [RequiresAssemblyFiles()]
    private static string GetAssemblyLocation() {
        string? dir = Assembly.GetExecutingAssembly().Location;
        if (string.IsNullOrEmpty(dir)) dir = AppContext.BaseDirectory;
        if (string.IsNullOrEmpty(dir)) dir = Assembly.GetAssembly(typeof(Extension))?.Location;
        if (string.IsNullOrEmpty(dir)) dir = typeof(Extension).Assembly.Location;
        
        if (string.IsNullOrEmpty(dir)) dir = AppContext.BaseDirectory;

        if (string.IsNullOrEmpty(dir)) dir = Assembly.GetCallingAssembly().Location;
        

        // Fallback to Arma 3 Directory
        if (string.IsNullOrEmpty(dir)) dir = AppDomain.CurrentDomain.BaseDirectory;
        if (string.IsNullOrEmpty(dir)) throw new DirectoryNotFoundException("Unable to locate Assembly start Directory!");
        return dir;
    }

    [RequiresAssemblyFiles()]
    private static string GetAssemblyName() {
        string name = Assembly.GetExecutingAssembly().GetName().Name!;
        if (string.IsNullOrEmpty(name)) throw new DirectoryNotFoundException("Unable to locate Assembly Name!");
        return name.EndsWith("_x64") ? name[..^4] : name;
    }



    public static bool MethodExists(string method) {
        if (string.IsNullOrEmpty(method)) return false;

        // Check if the method exists in the Methods class
        var methodInfo = typeof(Methods).GetMethod(method, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
        return methodInfo != null;
    }
    public static bool IsVoidMethod(string method) {
        // Check if the method exists in the Methods class
        MethodInfo? methodInfo = typeof(Methods).GetMethod(method, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
        return methodInfo != null && methodInfo.ReturnType == typeof(void);
    }
    public static MethodInfo GetMethod(string method) {
        //--- USING SYNCRONOUS METHOD
        MethodInfo? methodInfo = typeof(Methods).GetMethod(method, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
        return methodInfo!;
    }

    private static int ExecuteArmaMethod(nint output, int outputSize, string method, string[]? argArray = null) {
        try {
            argArray ??= [];

            int pipeIndex = method.IndexOf('|');
            string originalMethod = pipeIndex >= 0 ? method[..pipeIndex] : method;
            if (string.IsNullOrEmpty(originalMethod)) throw new Exception("Invalid Method");

            if (originalMethod.Equals(ResultCodes.ASYNC_CANCEL.ToString(), StringComparison.OrdinalIgnoreCase)) {
                string taskKey = pipeIndex >= 0 ? method[(pipeIndex + 1)..] : string.Empty;
                bool success = AsyncFactory.CancelAsyncTask(taskKey);
                return WriteOutput(output, outputSize, originalMethod,
                    $@"[""{(success ? ResultCodes.ASYNC_CANCEL_SUCCESS : ResultCodes.ASYNC_CANCEL_FAILED)}"",[]]",
                    success ? (int)ReturnCodes.Success : (int)ReturnCodes.Error);
            }

            if (!MethodExists(originalMethod)) throw new Exception("Invalid Method");

            MethodInfo methodToInvoke = GetMethod(originalMethod);
            bool isVoid = IsVoidMethod(originalMethod);

            int asyncKey = 0;
            bool async = pipeIndex >= 0 && int.TryParse(method[(pipeIndex + 1)..], out asyncKey);

            if (async) {
                string cancelKey = AsyncFactory.ExecuteAsyncTask(methodToInvoke, argArray, asyncKey);
                string outputPayload = isVoid
                    ? $@"[""{ResultCodes.ASYNC_SENT_VOID}"",[]]"
                    : $@"[""{ResultCodes.ASYNC_SENT}"",[""{cancelKey}""]]";
                
                return WriteOutput(output, outputSize, originalMethod, outputPayload, (int)ReturnCodes.Success);
            }


            ParameterInfo[] parameters = methodToInvoke.GetParameters();
            if (parameters.Length > 0 && argArray.Length == 0)
                throw new Exception("Parameters missing!");

            object?[] unserializedData = Serializer.DeserializeJsonArray(argArray);

            if (isVoid) {
                Task.Run(() => methodToInvoke.Invoke(null, unserializedData));
                return WriteOutput(output, outputSize, originalMethod,
                    $@"[""{ResultCodes.SUCCESS_VOID}"",[]]",
                    (int)ReturnCodes.Success);
            }

            object? result = methodToInvoke.Invoke(null, unserializedData);
            if (result is Task task) task.Wait();
            object? returnValue = result is Task t && t.GetType().IsGenericType
                ? ((dynamic)t).Result
                : result;

            return WriteOutput(output, outputSize, originalMethod,
                $@"[""{ResultCodes.SUCCESS}"",{Serializer.PrintArray([returnValue])}]",
                (int)ReturnCodes.Success);
        } catch (Exception ex) {
            return WriteOutput(output, outputSize, method,
                $@"[""{ResultCodes.ERROR}"",[""{ex.Message}""]]",
                (int)ReturnCodes.Error);
        }
    }

}