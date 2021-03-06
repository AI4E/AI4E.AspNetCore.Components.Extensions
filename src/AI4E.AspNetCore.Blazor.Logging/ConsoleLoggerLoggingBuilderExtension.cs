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

using System;
using AI4E.AspNetCore.Blazor.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Contains extensions for the <see cref="ILoggingBuilder"/> type.
    /// </summary>
    public static class ConsoleLoggerLoggingBuilderExtension
    {
        /// <summary>
        /// Adds browser console logging to the <see cref="ILoggingBuilder"/>.
        /// </summary>
        /// <param name="builder">The logging builder.</param>
        /// <returns>The logging builder with the browser console logging added.</returns>
        public static ILoggingBuilder AddBrowserConsole(this ILoggingBuilder builder)
        {
#pragma warning disable CA1062
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ConsoleLoggerProvider>());
#pragma warning restore CA1062
            return builder;
        }

        /// <summary>
        /// Adds browser console logging to the <see cref="ILoggingBuilder"/>.
        /// </summary>
        /// <param name="builder">The logging builder.</param>
        /// <param name="configure">A configuration for the <see cref="ConsoleLoggerOptions"/>.</param>
        /// <returns>The logging builder with the browser console logging added.</returns>
        public static ILoggingBuilder AddBrowserConsole(
            this ILoggingBuilder builder, Action<ConsoleLoggerOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.AddBrowserConsole();
#pragma warning disable CA1062
            builder.Services.Configure(configure);
#pragma warning restore CA1062

            return builder;
        }
    }
}
