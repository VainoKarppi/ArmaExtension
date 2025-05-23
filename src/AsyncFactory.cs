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
        private static readonly ConcurrentDictionary<int, CancellationTokenSource> CancelTokens = [];


        public static bool CancelAsyncTask(int asyncKey) {
            if (CancelTokens.TryGetValue(asyncKey, out CancellationTokenSource? source)) {
                Task.Run(source.Cancel); // We dont want to wait for any expections here
                return CancelTokens.TryRemove(asyncKey, out _);
            }
            return false;
        }

        public static async Task ExecuteAsyncTask(MethodInfo method, object?[] arguments, int asyncKey, bool isVoid) {

            // Only create cancel token if we’ll actually use it
            CancellationTokenSource? source = null;

            if (!isVoid) {
                source = new CancellationTokenSource();
                CancelTokens[asyncKey] = source;
            }

            await Task.Run(async () => {
                try {

                    Log(@$"ASYNC RESPONSE {(isVoid ? "(VOID)" : "")} >> [""{method.Name}|{asyncKey}"", {Serializer.PrintArray(arguments)}]");

                    ValidateArguments(arguments, method);

                    object? result = method.Invoke(null, arguments);

                    if (isVoid) return;

                    if (result is Task taskResult) {
                        await taskResult;
                        if (taskResult.GetType().IsGenericType) result = ((dynamic)taskResult).Result;
                    }

                    SendAsyncCallbackMessage(ResultMessages.ASYNC_RESPONSE, [result], (int)ReturnCodes.Success, asyncKey);
                } catch (Exception ex) {
                    SendAsyncCallbackMessage(ResultMessages.ASYNC_RESPONSE, [ex.Message], (int)ReturnCodes.Error, asyncKey);
                } finally {
                    CancelTokens.TryRemove(asyncKey, out _);
                }
            }, source?.Token ?? CancellationToken.None);
        }
    }
}