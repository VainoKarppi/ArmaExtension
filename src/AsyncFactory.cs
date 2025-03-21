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
                    bool isVoid = IsVoidMethod(method.Name) || asyncKey == -1;

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

                    // Dont send a response if the method is void
                    if (isVoid) return;

                    // Await the task if the result is an asynchronous task
                    if (result is Task taskResult) {
                        await taskResult;
                        if (taskResult is Task<object> taskObjectResult) result = await taskObjectResult;
                    }

                    SendAsyncCallbackMessage(ASYNC_RESPONSE, [result], (int)ReturnCodes.Success, asyncKey);
                } catch (Exception ex) {
                    SendAsyncCallbackMessage(ASYNC_FAILED, [ex.Message], (int)ReturnCodes.Error, asyncKey);
                } finally {
                    lock (AsyncTasks) AsyncTasks.Remove(asyncKey);
                }
            });

            lock (AsyncTasks) AsyncTasks.Add(asyncKey, Task.CompletedTask);
        }
    }
}