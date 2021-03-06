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

using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AI4E.AspNetCore.Blazor.SignalR
{
    internal static class StreamExtensions
    {
        public static ValueTask WriteAsync(this Stream stream, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (buffer.IsSingleSegment)
            {
                return stream.WriteAsync(buffer.First, cancellationToken);
            }

            return WriteMultiSegmentAsync(stream, buffer, cancellationToken);
        }

        private static async ValueTask WriteMultiSegmentAsync(Stream stream, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            var position = buffer.Start;
            while (buffer.TryGet(ref position, out var segment))
            {
                await stream.WriteAsync(segment, cancellationToken);
            }
        }
    }
}
