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

public static partial class Extension
{
    public readonly static string AssemblyDirectory = GetAssemblyLocation();
    public readonly static string Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString()!;
    public readonly static string ExtensionName = GetAssemblyName();



    private static unsafe delegate* unmanaged<string, string, string, int> Callback;


    /// <summary>
    /// Called only once when Arma 3 loads the extension.
    /// </summary>
    /// <param name="func">Pointer to Arma 3's callback function</param>
    [UnmanagedCallersOnly(EntryPoint = "RVExtensionRegisterCallback")]
    public static unsafe void RvExtensionRegisterCallback(delegate* unmanaged<string, string, string, int> callback) => Callback = callback;
    



    /// <summary>
    /// Called only once when Arma 3 loads the extension.
    /// The output will be written in the RPT logs.
    /// </summary>
    /// <param name="output">A pointer to the output buffer</param>
    /// <param name="outputSize">The maximum length of the buffer (always 32 for this particular method)</param>
    [UnmanagedCallersOnly(EntryPoint = "RVExtensionVersion")]
    public static void RVExtensionVersion(nint output, int outputSize)
    {
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
    public static int RVExtension(nint output, int outputSize, nint function)
    {
        string request = Marshal.PtrToStringAnsi(function) ?? string.Empty;
        string method = GetMethodName(request);
        try
        {
            bool fireAndForget = TryGetAsyncKey(request, out int asyncKey);
            return ExecuteTask(output, outputSize, method);
        }
        catch (Exception ex){
            return HandleError(output, outputSize, method, ex);
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
    public static int RVExtensionArgs(nint output, int outputSize, nint function, nint args, int argsCnt)
    {
        // Get Method Name
        string request = Marshal.PtrToStringAnsi(function) ?? string.Empty;
        string method = GetMethodName(request);

        try {
            object?[] arguments = GetArguments(args, argsCnt);

            bool async = TryGetAsyncKey(request, out int asyncKey);

            // Cancel Async Task
            if (method == ResultMessages.ASYNC_CANCEL) return CancelAsyncTask(output, outputSize, method, asyncKey);

            // Excute Method in ASYNC Mode
            if (async) return ExecuteAsyncTask(output, outputSize, method, asyncKey, arguments);

            // Execute Method in SYNC Mode
            return ExecuteTask(output, outputSize, method, arguments);
        } catch (Exception ex) {
            return HandleError(output, outputSize, method, ex);
        }
    }
    private static string GetMethodName(string request)
    {
        string[] splitted = request.Split('|');
        return splitted.Length > 0 ? splitted[0] : string.Empty;
    }

    private static object?[] GetArguments(nint args, int count) {
        string[] rawArgs = new string[count];
        for (int i = 0; i < count; i++) {
            nint ptr = Marshal.ReadIntPtr(args, i * IntPtr.Size);
            rawArgs[i] = Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
        }
        return Serializer.DeserializeJsonArray(rawArgs);
    }

    private static bool TryGetAsyncKey(string request, out int asyncKey)
    {
        string[] splitted = request.Split('|');
        asyncKey = -1;
        return splitted.Length > 1 && int.TryParse(splitted[1], out asyncKey);
    }

    private static int HandleError(nint output, int outputSize, string method, Exception ex) {
        string message = $@"[""{ResultMessages.ERROR}"",[""{ex.Message}""]]";

        return WriteOutput(output, outputSize, method, message, (int)ReturnCodes.Error);
    }

    private static int CancelAsyncTask(nint outputAdress, int outputSize, string method, int asyncKey)
    {
        bool success = AsyncFactory.CancelAsyncTask(asyncKey);

        string message = success ? ResultMessages.ASYNC_CANCEL_SUCCESS : ResultMessages.ASYNC_CANCEL_FAILED;
        int returnCode = success ? (int)ReturnCodes.Success : (int)ReturnCodes.Error;

        return WriteOutput(outputAdress, outputSize, method, message, returnCode);
    }

    public static void SendAsyncCallbackMessage(string method, object?[] data, int errorCode = 0, int asyncKey = -1)
    {
        if (string.IsNullOrEmpty(method)) Log("Empty function name in SendCallbackMessage.");

        method = $"{method}|{asyncKey}|{errorCode}";

        string returnData = Serializer.PrintArray(data);

        Log(@$"CALLBACK TO ARMA >> [""{ExtensionName}"", ""{method}"", ""{returnData}""]");

        try {
            unsafe { Callback(ExtensionName, method, returnData); }
        } catch (Exception ex) {
            Log(ex.Message);
        }
    }


    private static int WriteOutput(nint output, int outputSize, string methodName, string message, int returnCode = (int)ReturnCodes.Success)
    {
        try
        {
            if (string.IsNullOrEmpty(message)) throw new Exception("Empty message!");
            if (string.IsNullOrEmpty(methodName)) throw new Exception("Empty method name!");

            // Make sure the message is in the correct format
            if (message[0] != '[' && message[^1] != ']') message = $@"[""{message}"",[]]";

            Log(@$"RESPONSE FOR METHOD: ({methodName}) >> {message}");

            byte[] bytes = Encoding.ASCII.GetBytes(message);
            int length = Math.Min(bytes.Length, outputSize - 1);
            Marshal.Copy(bytes, 0, output, length);
            Marshal.WriteByte(output, length, 0);

            return returnCode;
        }
        catch (Exception ex)
        {
            Log(ex.Message);
            return (int)ReturnCodes.Error;
        }

    }

    [RequiresAssemblyFiles]
    private static string GetAssemblyLocation()
    {
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
    private static string GetAssemblyName()
    {
        string name = Assembly.GetExecutingAssembly().GetName().Name!;
        if (string.IsNullOrEmpty(name)) throw new DirectoryNotFoundException("Unable to locate Assembly Name!");
        return name.EndsWith("_x64") ? name[..^4] : name;
    }



    
    public static MethodInfo GetMethod(string method, out bool isVoid) {
        if (string.IsNullOrEmpty(method)) throw new Exception("Invalid Method");

        //--- USING SYNCRONOUS METHOD
        MethodInfo? methodInfo = typeof(Methods).GetMethod(method, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
        if (methodInfo == null) throw new Exception("Method Not Found");

        isVoid = methodInfo.ReturnType == typeof(void);

        return methodInfo!;
    }
    public static MethodInfo GetMethod(string method) {
        return GetMethod(method, out _)!;
    }

    // Execute the method and return the result later using Callback
    private static int ExecuteAsyncTask(nint output, int outputSize, string method, int asyncKey, object?[] arguments)
    {
        MethodInfo methodToInvoke = GetMethod(method, out bool isVoid);
        isVoid = isVoid || asyncKey == 0;

        // Call in different thread to avoid blocking the main thread
        Task.Run(() => AsyncFactory.ExecuteAsyncTask(methodToInvoke, arguments, asyncKey, isVoid));

        string message = isVoid ? ResultMessages.ASYNC_SENT_VOID : ResultMessages.ASYNC_SENT;
        return WriteOutput(output, outputSize, method, message);
    }


    // Execute the code immedietly in synchronous and return the result
    private static int ExecuteTask(nint output, int outputSize, string method, object?[]? arguments = null) {
        arguments ??= [];

        MethodInfo methodToInvoke = GetMethod(method, out bool isVoid);
        

        // Validate arguments before invoking the method
        ValidateArguments(arguments, methodToInvoke);


        if (isVoid) {
            Task.Run(() => methodToInvoke.Invoke(null, arguments));
            return WriteOutput(output, outputSize, method, ResultMessages.SUCCESS_VOID);
        }

        object? result = methodToInvoke.Invoke(null, arguments);
        if (result is Task task) task.Wait();
        object? returnValue = result is Task type && type.GetType().IsGenericType
            ? ((dynamic)type).Result
            : result;

        string message = $@"[""{ResultMessages.SUCCESS}"",{Serializer.PrintArray([returnValue])}]";
        return WriteOutput(output, outputSize, method, message);
    }
    
    public static void ValidateArguments(object?[]? arguments, MethodInfo methodToInvoke) {
        ParameterInfo[] parameters = methodToInvoke.GetParameters();

        arguments ??= [];

        // Ensure that we don't try to provide more arguments than parameters
        if (arguments.Length > parameters.Length) arguments = arguments.Take(parameters.Length).ToArray();

        // Check for missing required arguments and type mismatches
        for (int i = 0; i < parameters.Length; i++) {
            var parameter = parameters[i];

            // If the parameter is optional and there's no argument provided, skip to the next one
            if (i >= arguments.Length && parameter.HasDefaultValue) continue;

            // If there's an argument and it's null, replace with default value if possible
            if (i < arguments.Length && arguments[i] == null && parameter.HasDefaultValue) arguments[i] = parameter.DefaultValue;

            // If there's an argument, ensure its type is compatible with the parameter type
            if (i < arguments.Length && arguments[i] != null && !parameter.ParameterType.IsAssignableFrom(arguments[i]?.GetType())) {
                throw new Exception($"Argument type mismatch for parameter {parameter.Name} (Index {i}) in method {methodToInvoke.Name}. Expected {parameter.ParameterType}, got {arguments[i]?.GetType()}.");
            }
        }

        // If there's still a mismatch in argument count and no optional parameters to account for, throw an exception
        if (arguments.Length < parameters.Length && parameters.Any(p => !p.HasDefaultValue)) {
            throw new Exception($"Parameter count mismatch for method {methodToInvoke.Name}. Expected {parameters.Length}, got {arguments.Length}.");
        }
    }

}