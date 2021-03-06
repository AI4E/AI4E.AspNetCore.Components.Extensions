/* License
 * --------------------------------------------------------------------------------------------------------------------
 * This file is part of the AI4E distribution.
 *   (https://github.com/AI4E/AI4E.AspNetCore.Components.Extensions)
 *
 * MIT License
 *
 * Copyright (c) 2019 Andreas Truetschel and contributors.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * --------------------------------------------------------------------------------------------------------------------
 */

/* Based on
 * --------------------------------------------------------------------------------------------------------------------
 * BlazorSignalR (https://github.com/csnewman/BlazorSignalR)
 *
 * MIT License
 *
 * Copyright (c) 2018 csnewman
 * --------------------------------------------------------------------------------------------------------------------
 */

using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace AI4E.AspNetCore.Blazor.SignalR
{
    public static class DuplexPipeExtensions
    {
        public static Task StartAsync(this IDuplexPipe pipe, Uri uri, TransferFormat transferFormat)
        {
#pragma warning disable CA1062
            var method = pipe.GetType().GetMethod(
                nameof(StartAsync), new Type[] { typeof(Uri), typeof(TransferFormat), typeof(CancellationToken) });
#pragma warning restore CA1062
            Debug.Assert(method != null);
            var result = method!.Invoke(pipe, new object[] { uri, transferFormat, default(CancellationToken) }) as Task;
            Debug.Assert(result != null);
            return result!;
        }

        public static Task StopAsync(this IDuplexPipe pipe)
        {
#pragma warning disable CA1062
            var method = pipe.GetType().GetMethod(nameof(StopAsync), Array.Empty<Type>());
#pragma warning restore CA1062
            Debug.Assert(method != null);
            var result = method!.Invoke(pipe, Array.Empty<object>()) as Task;
            Debug.Assert(result != null);
            return result!;
        }
    }
}
