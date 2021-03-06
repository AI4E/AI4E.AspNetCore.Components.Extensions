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
using System.Threading.Tasks;
using AI4E.Utils;

namespace AI4E.AspNetCore.Components.Notifications
{
    public sealed class NotificationManager : INotificationManager<Notification>
    {
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly LinkedList<ManagedNotificationMessage> _notificationMessages = new LinkedList<ManagedNotificationMessage>();
        private readonly object _mutex = new object();
        private bool _isDisposed = false;

        public NotificationManager(IDateTimeProvider dateTimeProvider)
        {
            if (dateTimeProvider is null)
                throw new ArgumentNullException(nameof(dateTimeProvider));

            _dateTimeProvider = dateTimeProvider;
        }

        /// <inheritdoc />
        public event EventHandler? NotificationsChanged;

        private void OnNotificationsChanged()
        {
            NotificationsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        public void Dismiss(Notification notification)
        {
            var node = notification.NotificationRef;

            if (node is null)
                return;

            lock (_mutex)
            {
                CheckDisposed();
                _notificationMessages.Remove(node);
            }

            OnNotificationsChanged();
        }

        /// <inheritdoc />
        public IEnumerable<Notification> GetNotifications()
        {
            return GetNotificationsInternal(key: null, uri: null);
        }

        /// <inheritdoc />
        public IEnumerable<Notification> GetNotifications(string key)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            return GetNotificationsInternal(key, uri: null);
        }

        /// <inheritdoc />
        public IEnumerable<Notification> GetNotifications(string key, string uri)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (uri is null)
                throw new ArgumentNullException(nameof(uri));

            return GetNotificationsInternal(key, uri);
        }

        public IEnumerable<Notification> GetNotifications(string key, Uri uri)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (uri is null)
                throw new ArgumentNullException(nameof(uri));

            return GetNotificationsInternal(key, uri.ToString());
        }

        private IEnumerable<Notification> GetNotificationsInternal(string? key, string? uri)
        {
            lock (_mutex)
            {
                CheckDisposed();

                if (_notificationMessages.Count == 0)
                {
                    return ImmutableList<Notification>.Empty;
                }

                if (_notificationMessages.Count == 1)
                {
                    return ImmutableList.Create(new Notification(_notificationMessages.First!));
                }

                var builder = ImmutableList.CreateBuilder<Notification>();

                for (var current = _notificationMessages.Last; current != null; current = current.Previous)
                {
#pragma warning disable CA2234
                    if (uri != null && !current.Value.UriFilter.IsMatch(uri))
#pragma warning restore CA2234
                    {
                        continue;
                    }

                    if (key != null && current.Value.Key != key)
                    {
                        continue;
                    }

                    builder.Add(new Notification(current));
                }

                return builder.ToImmutable();
            }
        }

        private void PlaceNotification(LinkedListNode<ManagedNotificationMessage> node)
        {
            //Debug.Assert(node != null);

            if (node.Value.Expiration == null)
            {
                lock (_mutex)
                {
                    CheckDisposed();
                    _notificationMessages.AddLast(node);
                }

                OnNotificationsChanged();

                return;
            }

            var now = _dateTimeProvider.GetCurrentTime();
            var delay = (DateTime)node.Value.Expiration - now;

            // The message is expired already, do not add it.
            if (delay <= TimeSpan.Zero)
            {
                return;
            }

            // We have to add the message before scheduling the continuation
            // to prevent a race when delay is small and the continuation is
            // invoked before the message is added actually.
            lock (_mutex)
            {
                CheckDisposed();
                _notificationMessages.AddLast(node);
            }

            OnNotificationsChanged();

            async Task RemoveNotificationAfterDelayAsync()
            {
                await Task.Delay(delay).ConfigureAwait(false);

                lock (_mutex)
                {
                    if (!_isDisposed)
                    {
                        _notificationMessages.Remove(node);
                    }
                }

                OnNotificationsChanged();
            }

            RemoveNotificationAfterDelayAsync().HandleExceptions();
        }

        /// <inheritdoc />
        public NotificationPlacement PlaceNotification(NotificationMessage notificationMessage)
        {
            if (notificationMessage is null)
                throw new ArgumentNullException(nameof(notificationMessage));

            if (!notificationMessage.NotificationType.IsValid())
            {
                throw new ArgumentException($"The alert type must be one of the values defined in {typeof(NotificationType)}.", nameof(notificationMessage));
            }

            if (notificationMessage.NotificationType == NotificationType.None)
            {
                return default;
            }

            var managedMessage = new ManagedNotificationMessage(notificationMessage, this, _dateTimeProvider);
            var node = new LinkedListNode<ManagedNotificationMessage>(managedMessage);
            PlaceNotification(node);
            return new NotificationPlacement(this, node);
        }

        /// <inheritdoc />
        public void CancelNotification(in NotificationPlacement notificationPlacement)
        {
            if (notificationPlacement.NotificationManager != this)
            {
                if (notificationPlacement.IsOfScopedNotificationManager(this))
                {
                    notificationPlacement.NotificationManager.CancelNotification(notificationPlacement);
                }
            }
            else if (notificationPlacement.NotificationRef is LinkedListNode<ManagedNotificationMessage> node)
            {
                lock (_mutex)
                {
                    CheckDisposed();

                    _notificationMessages.Remove(node);
                }

                OnNotificationsChanged();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            lock (_mutex)
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;

                _notificationMessages.Clear();
                OnNotificationsChanged();
            }
        }

        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        INotificationManagerScope INotificationManager.CreateScope()
        {
            return CreateScope();
        }

        INotificationRecorder INotificationManager.CreateRecorder()
        {
            return CreateRecorder();
        }

        public NotificationManagerScope CreateScope()
        {
            return new NotificationManagerScope(this);
        }

        public NotificationRecorder CreateRecorder()
        {
            return new NotificationRecorder(this);
        }
    }
}
