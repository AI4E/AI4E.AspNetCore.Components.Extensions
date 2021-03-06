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
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace AI4E.AspNetCore.Components.Forms
{
    /// <summary>
    /// Represents an edit-context that can be extended via form-extensions.
    /// </summary>
    public sealed class ExtensibleEditContext
    {
        private EditContext _rootEditContext;
        private readonly Dictionary<EditContext, FormExtension> _formExtensions;

        /// <summary>
        /// Creates a new instance of the <see cref="ExtensibleEditContext"/> type.
        /// </summary>
        /// <param name="rootEditContext">The root edit-context.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="rootEditContext"/> is <c>null</c>.</exception>
        public ExtensibleEditContext(EditContext rootEditContext)
        {
            if (rootEditContext is null)
                throw new ArgumentNullException(nameof(rootEditContext));

            _rootEditContext = rootEditContext;
            _formExtensions = new Dictionary<EditContext, FormExtension>();
        }

        /// <summary>
        /// Gets or sets the root edit-context.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown on setting if the value is <c>null</c>.</exception>
        public EditContext RootEditContext
        {
            get => _rootEditContext;
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));

                _rootEditContext = value;
            }
        }

        /// <summary>
        /// Gets a collection of registered form-extensions.
        /// </summary>
        public IReadOnlyCollection<FormExtension> FormExtensions
        {
            get
            {
                if (_formExtensions.Count == 0)
                    return ImmutableList<FormExtension>.Empty;

                return _formExtensions.Values.ToImmutableList();
            }
        }

        /// <summary>
        /// Fires when <see cref="FormExtensions"/> changed.
        /// </summary>
        public event EventHandler<FormExtensionsChangedEventArgs>? OnFormExtensionsChanged;

        /// <summary>
        /// Registeres a form extension and returns a handle for the extension.
        /// </summary>
        /// <param name="editContext">The edit-context of the form extension.</param>
        /// <param name="onInvalidSubmit">The event-callback that shall be invoked on invalid submits.</param>
        /// <param name="onValidSubmit">The event-callback that shall be invoked on valid submits.</param>
        /// <returns>A handle for the form extension.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="editContext"/> is <c>null</c>.</exception>
        public FormExtension RegisterEditFormExtension(
            EditContext editContext,
            EventCallback<EditContext> onInvalidSubmit,
            EventCallback<EditContext> onValidSubmit)
        {
            if (editContext is null)
                throw new ArgumentNullException(nameof(editContext));

            var seqNum = 0;

            if (_formExtensions.TryGetValue(editContext, out var current))
            {
                seqNum = current.SeqNum;
            }

            var registration = new FormExtension(this, editContext, onInvalidSubmit, onValidSubmit, seqNum + 1);

            _formExtensions[editContext] = registration;

            OnFormExtensionsChanged?.Invoke(
                this, FormExtensionsChangedEventArgs.Instance);

            return registration;
        }

        /// <summary>
        /// Unregsiters a form-extension.
        /// </summary>
        /// <param name="editFormExtension">The form-extension to unregister.</param>
        public void UnregisterEditFormExtension(in FormExtension editFormExtension)
        {
            if (editFormExtension.EditContext != null
                && _formExtensions.TryGetValue(editFormExtension.EditContext, out var value)
                && value.SeqNum == editFormExtension.SeqNum)
            {
                _formExtensions.Remove(editFormExtension.EditContext);

                OnFormExtensionsChanged?.Invoke(
                 this, FormExtensionsChangedEventArgs.Instance);
            }
        }

        /// <summary>
        /// Validates the edit-contex.
        /// </summary>
        /// <returns>A boolean valud indicating whether the edit-context is valid.</returns>
        public bool Validate()
        {
            var isValid = RootEditContext.Validate();

            foreach (var formExtension in _formExtensions.Values)
            {
                isValid &= formExtension.EditContext.Validate();
            }

            return isValid;
        }

        internal Task OnValidSubmit()
        {
            return Task.WhenAll(
                _formExtensions
                .Values
                .Where(p => p.OnValidSubmit.HasDelegate)
                .Select(p => p.OnValidSubmit.InvokeAsync(p.EditContext)));
        }

        internal Task OnInvalidSubmit()
        {
            return Task.WhenAll(
                _formExtensions
                .Values
                .Where(p => p.EditContext.GetValidationMessages().Any() && p.OnInvalidSubmit.HasDelegate)
                .Select(p => p.OnInvalidSubmit.InvokeAsync(p.EditContext)));
        }
    }
}
