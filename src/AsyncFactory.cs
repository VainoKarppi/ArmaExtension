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

        public static void ExecuteAsyncTask(MethodInfo method, string[] argArray, int asyncKey) {
            Task.Run(async () => {
                try {
                    bool isVoid = IsVoidMethod(method.Name);

                    // Unserialize the data
                    object?[] unserializedData = Serializer.DeserializeJsonArray(argArray);

                    Log(@$"ASYNC RESPONSE {(isVoid ? "(VOID)" : "")} >> [""{method.Name}|{asyncKey}"", {Serializer.PrintArray(unserializedData)}]");

                    // Check parameters
                    ParameterInfo[] parameters = method.GetParameters();

                    // If there are extra parameters, remove them
                    if (unserializedData.Length > parameters.Length) unserializedData = unserializedData.Take(parameters.Length).ToArray();

                    // If there are not enough parameters return error
                    if (unserializedData.Length != parameters.Length) {
                        throw new ArmaAsyncException(asyncKey, $"Parameter count mismatch for method {method.Name}. Expected {parameters.Length}, got {unserializedData.Length}.");
                    }
                    
                    // Invoke the method and get the result
                    object result = method.Invoke(null, unserializedData)!;

                    // Await the task if the result is an asynchronous task
                    if (result is Task taskResult) {
                        await taskResult;
                        if (taskResult is Task<object> taskObjectResult) result = await taskObjectResult;
                    }

                    // Dont send a response if the method is void
                    if (isVoid) return;

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