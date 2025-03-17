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
    
    enum ReturnCodes {
        Success = 0,
        Error = 1,
        InvalidMethod = 2,
        InvalidParameters = 3
    }
    
    public const string SUCCESS = "SUCCESS";
    public const string ERROR = "ERROR";
    public const string ASYNC_RESPONSE = "ASYNC_RESPONSE";
    public const string ASYNC_SENT = "ASYNC_SENT";
    public const string ASYNC_SENT_VOID = "ASYNC_SENT_VOID";
    public const string ASYNC_FAILED = "ASYNC_FAILED";
    public const string ASYNC_CANCELLED = "ASYNC_CANCELLED";
    public const string ASYNC_SUCCESS = "ASYNC_SUCCESS";
    public const string CALLFUNCTION = "CALLFUNCTION";
    

    
    
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
    public static void RVExtension(nint output, int outputSize, nint function) {
        string method = Marshal.PtrToStringAnsi(function) ?? string.Empty;
        string[] methodData = method.Split("|");
        method = methodData.First();

        try {
            // Make sure the method is not empty and exists
            if (string.IsNullOrEmpty(method) || !MethodExists(method)) throw new Exception("Invalid Method");
            
            
            // Get method info
            MethodInfo methodToInvoke = GetMethod(method);

            bool isVoid = IsVoidMethod(method);

            //--- USE ASYNC METHOD
            int asyncKey = 0;
            bool async = methodData.Length > 1 && int.TryParse(methodData.Last(), out asyncKey);
            if (async) {
                AsyncFactory.ExecuteAsyncTask(methodToInvoke, [], asyncKey);

                WriteOutput(output, outputSize, method, $@"[""{(isVoid ? ASYNC_SENT_VOID : ASYNC_SENT)}"",[]]", (int)ReturnCodes.Success);
                return;
            }


            // Make sure there are no parameters
            ParameterInfo[] parameters = methodToInvoke.GetParameters();
            if (parameters.Length > 0) throw new Exception("This method requires parameters! Use Args method.");

            // Invoke the method and get the result
            dynamic? invocationResult = methodToInvoke.Invoke(null, null);
            object result = invocationResult is Task ? invocationResult.Result : invocationResult!;

            WriteOutput(output, outputSize, method, @$"[""{SUCCESS}"",{Serializer.PrintArray([result])}]", (int)ReturnCodes.Success);
        } catch (Exception ex) {
            WriteOutput(output, outputSize, method, @$"[""{ERROR}"",[""{ex.Message}""]]", (int)ReturnCodes.Error);
        }
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
        
        // Get Method Name and possible Async key
        string method = Marshal.PtrToStringAnsi(function) ?? string.Empty;
        string[] methodData = method.Split("|");
        method = methodData.First();

        try {
            if (!MethodExists(method)) throw new Exception("Invalid Method");

            string[] argArray = new string[argsCnt];

            for (int i = 0; i < argsCnt; i++) {
                nint argPtr = Marshal.ReadIntPtr(args, i * nint.Size);
                argArray[i] = Marshal.PtrToStringAnsi(argPtr) ?? string.Empty;
            }

            bool isVoid = IsVoidMethod(method);
            MethodInfo methodToInvoke = GetMethod(method);

            //--- USE ASYNC METHOD
            int asyncKey = 0;
            bool async = methodData.Length > 1 && int.TryParse(methodData.Last(), out asyncKey);
            if (async) {
                // Execute in Async
                AsyncFactory.ExecuteAsyncTask(methodToInvoke, argArray, asyncKey);

                return WriteOutput(output, outputSize, method, $@"[""{(isVoid ? ASYNC_SENT_VOID : ASYNC_SENT)}"",[]]", (int)ReturnCodes.Success);
            }

            //--- USE SYNCRONOUS METHOD

            // Unserialize the data
            object?[] unserializedData = Serializer.DeserializeJsonArray(argArray);
            
            // Invoke the method and get the result
            dynamic? invocationResult = methodToInvoke.Invoke(null, unserializedData);
            object result = invocationResult is Task ? invocationResult.Result : invocationResult!;

            return WriteOutput(output, outputSize, method, @$"[""{SUCCESS}"",{Serializer.PrintArray([result])}]", (int)ReturnCodes.Success);
        } catch (Exception ex) {
            return WriteOutput(output, outputSize, method, @$"[""{ERROR}"",[""{ex.Message}""]]", (int)ReturnCodes.Error);
        }
    }







    public static void SendCallbackMessage(string method, object[] data, int? asyncKey = null) {
        if (string.IsNullOrEmpty(method)) Log("Empty function name in SendCallbackMessage.");

        if (asyncKey != null) method += $"|{asyncKey}"; // Check if ASYNC answer
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
        string dir = Assembly.GetExecutingAssembly().Location!;
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
}