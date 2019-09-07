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
 * AspNet Core (https://github.com/aspnet/AspNetCore)
 * Copyright (c) .NET Foundation. All rights reserved.
 * Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
 * --------------------------------------------------------------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;

namespace AI4E.AspNetCore.Components.Routing
{
    /// <summary>
    /// A base type for custom component routers.
    /// </summary>
    public abstract class ExtensibleRouter : IComponent, IHandleAfterRender, IDisposable
    {
        private static readonly char[] QueryOrHashStartChar = new[] { '?', '#' };
        private static readonly ReadOnlyDictionary<string, object> EmptyParametersDictionary
                       = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

        private RenderHandle _renderHandle;
        private string _baseUri;
        private string _locationAbsolute;
        private bool _isInitialized;
        private bool _navigationInterceptionEnabled;
        private ILogger<ExtensibleRouter> _logger;

        [Inject] private NavigationManager NavigationManager { get; set; }

        [Inject] private INavigationInterception NavigationInterception { get; set; }

        [Inject] private ILoggerFactory LoggerFactory { get; set; }

        /// <summary>
        /// Gets or sets the content to display when no match is found for the requested route.
        /// </summary>
        [Parameter] public RenderFragment NotFound { get; set; }

        /// <summary>
        /// Gets or sets the content to display when a match is found for the requested route.
        /// </summary>
        [Parameter] public RenderFragment<RouteData> Found { get; set; }

        private RouteTable Routes { get; set; }

        /// <inheritdoc />
        public void Attach(RenderHandle renderHandle)
        {
            _logger = LoggerFactory.CreateLogger<ExtensibleRouter>();
            _renderHandle = renderHandle;
            _baseUri = NavigationManager.BaseUri;
            _locationAbsolute = NavigationManager.Uri;
            NavigationManager.LocationChanged += OnLocationChanged;
        }

        /// <summary>
        /// Called when the router initializes.
        /// </summary>
        protected virtual void OnInit() { }

        /// <inheritdoc />
        public Task SetParametersAsync(ParameterView parameters)
        {
            if (!_isInitialized)
            {
                OnInit();
                _isInitialized = true;
            }

            parameters.SetParameterProperties(this);

            // Found content is mandatory, because even though we could use something like <RouteView ...> as a
            // reasonable default, if it's not declared explicitly in the template then people will have no way
            // to discover how to customize this (e.g., to add authorization).
            if (Found == null)
            {
                throw new InvalidOperationException($"The {GetType().Name} component requires a value for the parameter {nameof(Found)}.");
            }

            // NotFound content is mandatory, because even though we could display a default message like "Not found",
            // it has to be specified explicitly so that it can also be wrapped in a specific layout
            if (NotFound == null)
            {
                throw new InvalidOperationException($"The {GetType().Name} component requires a value for the parameter {nameof(NotFound)}.");
            }

            UpdateRouteTable();
            Refresh(isNavigationIntercepted: false);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates the route table.
        /// </summary>
        protected void UpdateRouteTable()
        {
            if (!_isInitialized)
                return;

            var types = ResolveRoutableComponents();
            Routes = RouteTableFactory.Create(types);
        }

        /// <summary>
        /// Resolves the types of components that can be routed to.
        /// </summary>
        /// <returns>An enumerable of types of components that can be routed to.</returns>
        protected abstract IEnumerable<Type> ResolveRoutableComponents();

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Frees resources used by the component.
        /// </summary>
        /// <param name="disposing">A boolean value indicating whether this is a managed dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
        }

        private static string StringUntilAny(string str, char[] chars)
        {
            var firstIndex = str.IndexOfAny(chars);
            return firstIndex < 0
                ? str
                : str.Substring(0, firstIndex);
        }

        /// <summary>
        /// Called before refreshing the router.
        /// </summary>
        /// <param name="locationPath">The location the user navigated to.</param>
        protected virtual void OnBeforeRefresh(string locationPath) { }

        /// <summary>
        /// Called after refreshing the router.
        /// </summary>
        /// <param name="success">A boolean value indicating routing success.</param>
        protected virtual void OnAfterRefresh(bool success) { }

        /// <summary>
        /// Refreshes the router.
        /// </summary>
        protected void Refresh()
        {
            Refresh(isNavigationIntercepted: false);
        }

        private void Refresh(bool isNavigationIntercepted)
        {
            var locationPath = NavigationManager.ToBaseRelativePath(_locationAbsolute);
            locationPath = StringUntilAny(locationPath, QueryOrHashStartChar);

            OnBeforeRefresh(locationPath);

            var context = new RouteContext(locationPath);
            Routes.Route(context);

            var handlerFound = !(context.Handler is null);
            OnAfterRefresh(handlerFound);

            if (handlerFound)
            {
                if (!typeof(IComponent).IsAssignableFrom(context.Handler))
                {
                    throw new InvalidOperationException($"The type {context.Handler.FullName} " +
                        $"does not implement {typeof(IComponent).FullName}.");
                }

                Log.NavigatingToComponent(_logger, context.Handler, locationPath, _baseUri);

                var routeData = new RouteData(
                    context.Handler,
                    context.Parameters ?? EmptyParametersDictionary);
                _renderHandle.Render(Found(routeData));
            }
            else
            {
                if (!isNavigationIntercepted)
                {
                    Log.DisplayingNotFound(_logger, locationPath, _baseUri);

                    // We did not find a Component that matches the route.
                    // Only show the NotFound content if the application developer programatically got us here i.e we did not
                    // intercept the navigation. In all other cases, force a browser navigation since this could be non-Blazor content.
                    _renderHandle.Render(NotFound);
                }
                else
                {
                    Log.NavigatingToExternalUri(_logger, _locationAbsolute, locationPath, _baseUri);
                    NavigationManager.NavigateTo(_locationAbsolute, forceLoad: true);
                }
            }
        }

        private void OnLocationChanged(object sender, LocationChangedEventArgs args)
        {
            _locationAbsolute = args.Location;
            if (_renderHandle.IsInitialized && Routes != null)
            {
                Refresh(args.IsNavigationIntercepted);
            }
        }

        Task IHandleAfterRender.OnAfterRenderAsync()
        {
            if (!_navigationInterceptionEnabled)
            {
                _navigationInterceptionEnabled = true;
                return NavigationInterception.EnableNavigationInterceptionAsync();
            }

            return Task.CompletedTask;
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, string, Exception> DisplayingNotFoundLogMessage =
                LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(1, "DisplayingNotFound"), $"Displaying {nameof(NotFound)} because path '{{Path}}' with base URI '{{BaseUri}}' does not match any component route");

            private static readonly Action<ILogger, Type, string, string, Exception> NavigatingToComponentLogMessage =
                LoggerMessage.Define<Type, string, string>(LogLevel.Debug, new EventId(2, "NavigatingToComponent"), "Navigating to component {ComponentType} in response to path '{Path}' with base URI '{BaseUri}'");

            private static readonly Action<ILogger, string, string, string, Exception> NavigatingToExternalUriLogMessage =
                LoggerMessage.Define<string, string, string>(LogLevel.Debug, new EventId(3, "NavigatingToExternalUri"), "Navigating to non-component URI '{ExternalUri}' in response to path '{Path}' with base URI '{BaseUri}'");

            internal static void DisplayingNotFound(ILogger logger, string path, string baseUri)
            {
                DisplayingNotFoundLogMessage(logger, path, baseUri, null);
            }

            internal static void NavigatingToComponent(ILogger logger, Type componentType, string path, string baseUri)
            {
                NavigatingToComponentLogMessage(logger, componentType, path, baseUri, null);
            }

            internal static void NavigatingToExternalUri(ILogger logger, string externalUri, string path, string baseUri)
            {
                NavigatingToExternalUriLogMessage(logger, externalUri, path, baseUri, null);
            }
        }
    }
}
