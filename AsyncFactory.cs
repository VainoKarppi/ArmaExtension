using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static ArmaExtension.Logger;
using static ArmaExtension.Extension;

namespace ArmaExtension {
    public static class AsyncFactory {
        private static readonly Dictionary<int, Task> AsyncTasks = [];

        public static void ExecuteAsyncTask(string method, string[] argArray, int asyncKey) {
            Task.Run(async () => {
                try {
                    // Get the method to invoke
                    MethodInfo methodInfo = typeof(Methods).GetMethod(method, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase)
                        ?? throw new ArmaAsyncException(asyncKey, $"Method {method} not found.");
                    

                    object?[] unserializedData = Serializer.DeserializeJsonArray(argArray);
                    Log(@$"ASYNC {(IsVoidMethod(method) ? "(VOID)" : "")} >> [""{method}|{asyncKey}"", {Serializer.PrintArray(unserializedData)}]");
                    

                    ParameterInfo[] parameters = methodInfo.GetParameters();
                    if (unserializedData.Length > parameters.Length)
                        unserializedData = unserializedData.Take(parameters.Length).ToArray(); // Trim excess parameters

                    if (unserializedData.Length != parameters.Length) {
                        throw new ArmaAsyncException(asyncKey, $"Parameter count mismatch for method {method}. Expected {parameters.Length}, got {unserializedData.Length}.");
                    }

                    object result = methodInfo.Invoke(null, unserializedData)!;

                    // Await the task if the result is an asynchronous task
                    if (result is Task taskResult) {
                        await taskResult;
                        if (taskResult is Task<object> taskObjectResult) result = await taskObjectResult;
                    }


                    if (IsVoidMethod(method)) return;

                    SendCallbackMessage(ASYNC_RESPONSE, [result], asyncKey);
                } catch (Exception ex) {
                    Log(ex.Message);
                    SendCallbackMessage(ASYNC_FAILED, [ex.Message], asyncKey);
                } finally {
                    lock (AsyncTasks) AsyncTasks.Remove(asyncKey);
                }
            });

            lock (AsyncTasks) AsyncTasks.Add(asyncKey, Task.CompletedTask);
        }
    }
}