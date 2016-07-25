using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JRPC.Client.Extensions {

    internal static class TaskExtensions {
        /// <summary>
        /// Добавляет таймаут к таске
        /// </summary>
        public static async Task<TResult> WithTimeout<TResult>(this Task<TResult> task, int timeoutMs) {
            if (!await TryTimeout(task, timeoutMs).ConfigureAwait(false)) {
                throw new TimeoutException("Превышен таймаут ожидания таски (" + timeoutMs + " мс)");
            }
            return task.Result;
        }
        /// <summary>
        /// Возвращает true, если таска завершается меньше, чем за <paramref name="timeoutMs"/> мс, иначе - false.
        /// </summary>
        public static async Task<bool> TryTimeout(this Task task, int timeoutMs) {
            return await Task.WhenAny(task, Task.Delay(timeoutMs)).ConfigureAwait(false) == task;
        }
    }
}
