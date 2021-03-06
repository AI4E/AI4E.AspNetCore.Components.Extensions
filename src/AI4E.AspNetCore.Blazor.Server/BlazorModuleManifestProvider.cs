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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AI4E.Modularity.Metadata;
using Newtonsoft.Json;

namespace AI4E.AspNetCore.Blazor.Server
{
    public sealed class BlazorModuleManifestProvider : IBlazorModuleManifestProvider
    {
        private readonly Assembly _appAssembly;
        private readonly IMetadataAccessor _metadataAccessor;

        public BlazorModuleManifestProvider(Assembly appAssembly, IMetadataAccessor metadataAccessor)
        {
            if (appAssembly == null)
                throw new ArgumentNullException(nameof(appAssembly));

            if (metadataAccessor == null)
                throw new ArgumentNullException(nameof(metadataAccessor));

            _appAssembly = appAssembly;
            _metadataAccessor = metadataAccessor;
        }

        public async ValueTask<BlazorModuleManifest> GetBlazorModuleManifestAsync(CancellationToken cancellation)
        {
            return new BlazorModuleManifest
            {
                Name = (await _metadataAccessor.GetMetadataAsync(cancellation)).Name,
                Assemblies = GetAppAssemblies()
            };
        }

        private List<BlazorModuleManifestAssemblyEntry> GetAppAssemblies()
        {
            var blazorConfig = BlazorConfig.Read(_appAssembly.Location);
            var distPath = blazorConfig.DistPath;
            var blazorBootPath = Path.Combine(distPath, "_framework", "blazor.boot.json");

            BlazorBoot blazorBoot;

            using (var fileStream = new FileStream(blazorBootPath, FileMode.Open))
            using (var streamReader = new StreamReader(fileStream))
            {
                blazorBoot = (BlazorBoot)JsonSerializer.CreateDefault().Deserialize(streamReader, typeof(BlazorBoot));
            }

            var binPath = Path.Combine(distPath, "_framework", "_bin");

            var result = new List<BlazorModuleManifestAssemblyEntry>(capacity: blazorBoot.AssemblyReferences.Count + 1);

            foreach (var assembly in blazorBoot.AssemblyReferences.Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)).Append(blazorBoot.Main))
            {
                var dllFile = Path.Combine(binPath, assembly!);

                if (File.Exists(dllFile))
                {
                    var dllFileRef = AssemblyName.GetAssemblyName(dllFile);

                    Debug.Assert(dllFileRef.Name != null);
                    Debug.Assert(dllFileRef.Version != null);

                    result.Add(new BlazorModuleManifestAssemblyEntry
                    {
                        AssemblyName = dllFileRef.Name!,
                        AssemblyVersion = dllFileRef.Version!,
                        IsComponentAssembly = assembly == blazorBoot.Main
                    });
                }
            }

            return result;

        }
#pragma warning disable CA1812
        private sealed class BlazorBoot
#pragma warning restore CA1812
        {
            [JsonProperty("main")]
            public string? Main { get; set; }

            [JsonProperty("assemblyReferences")]
            public List<string> AssemblyReferences { get; set; } = new List<string>();

            [JsonProperty("cssReferences")]
            public List<string> CssReferences { get; set; } = new List<string>();

            [JsonProperty("jsReferences")]
            public List<string> JsReferences { get; set; } = new List<string>();
        }
    }
}
