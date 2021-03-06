﻿//	Copyright © 2013 - 2014, Alain Metge. All rights reserved.
//
//		This file is part of Hyperstore (http://www.hyperstore.org)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Hyperstore.Modeling.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hyperstore.Modeling.Container
{
    class ServicesContainer : IServicesContainer
    {
        private readonly ServicesContainer _parent;
        private readonly ThreadSafeLazyRef<ImmutableDictionary<Type, ServiceDescriptor>> _services = new ThreadSafeLazyRef<ImmutableDictionary<Type, ServiceDescriptor>>(() => ImmutableDictionary<Type, ServiceDescriptor>.Empty);
        private readonly ThreadSafeLazyRef<ImmutableDictionary<ServiceDescriptor, object>> _resolvedServices = new ThreadSafeLazyRef<ImmutableDictionary<ServiceDescriptor, object>>(() => ImmutableDictionary<ServiceDescriptor, object>.Empty);

        private int _nb;
        private static int _count;

        internal ServicesContainer(ServicesContainer parent = null)
        {
            _nb = _count++;

            _parent = parent;
            Register<IServicesContainer>(this);
        }

        public IServicesContainer NewScope()
        {
            var services = new ServicesContainer(this);
            return services;
        }

        private void Register(ServiceDescriptor descriptor)
        {
            _services.ExchangeValue(services =>
                {
                    ServiceDescriptor desc;
                    if (!services.TryGetValue(descriptor.ServiceType, out desc))
                    {
                        return services.Add(descriptor.ServiceType, descriptor);
                    }

                    descriptor.Next = desc;
                    return services.SetItem(descriptor.ServiceType, descriptor);
                });
        }

        public void RegisterSetting(string name, object value)
        {
            Contract.RequiresNotEmpty(name, "name");

            Register(new Setting(name, value));
        }

        public TSetting GetSettingValue<TSetting>(string name)
        {
            var setting = ResolveAll<Setting>().FirstOrDefault(s => String.Compare(s.Name, name, StringComparison.OrdinalIgnoreCase) == 0);
            return setting == null ? default(TSetting) : (TSetting)setting.Value;
        }

        public void Register<TService>(TService service) where TService : class
        {
            Register(new ServiceDescriptor(typeof(TService), ServiceLifecycle.Singleton, service, null));
        }

        public void Register<TService>(Func<IServicesContainer, TService> factory, ServiceLifecycle lifecyle = ServiceLifecycle.Scoped) where TService : class
        {
            Register(new ServiceDescriptor(typeof(TService), lifecyle, null, factory));
        }

        public TService Resolve<TService>() where TService : class
        {
            ServiceDescriptor desc;
            if (_services.HasValue && _services.Value.TryGetValue(typeof(TService), out desc))
            {
                return ResolveService<TService>(desc);
            }

            if (_parent != null)
            {
                desc = _parent.TryGetService<TService>();
                if (desc != null && desc.Lifecycle == ServiceLifecycle.Scoped)
                {
                    Register(desc);
                    return ResolveService<TService>(desc);
                }

                return _parent.Resolve<TService>();
            }

            return default(TService);
        }

        private ServiceDescriptor TryGetService<TService>() where TService : class
        {
            var parent = this;
            while (parent != null)
            {
                ServiceDescriptor desc;
                if (_services.HasValue && _services.Value.TryGetValue(typeof(TService), out desc))
                    return desc;
                parent = parent._parent;
            }
            return null;
        }

        private TService ResolveService<TService>(ServiceDescriptor desc) where TService : class
        {
            if (desc.Lifecycle == ServiceLifecycle.Singleton && _parent != null)
            {
                return _parent.ResolveService<TService>(desc);
            }
            else if (desc.Lifecycle == ServiceLifecycle.Transient)
            {
                return (TService)desc.Create(this); // TODO disposable
            }

            _resolvedServices.ExchangeValue(services =>
                {
                    if (!services.ContainsKey(desc))
                    {
                        return services.Add(desc, desc.Create(this));
                    }
                    return services;
                });

            return (TService)_resolvedServices.Value[desc];
        }

        public IEnumerable<TService> ResolveAll<TService>() where TService : class
        {
            ServiceDescriptor desc;
            if (_services.Value.TryGetValue(typeof(TService), out desc))
            {
                var next = desc;
                while (next != null)
                {
                    yield return ResolveService<TService>(next);
                    next = next.Next;
                }
            }

            if (_parent != null)
            {
                foreach (var service in _parent.ResolveAll<TService>())
                {
                    yield return service;
                }
            }
        }

        public async Task ComposeAsync(params System.Reflection.Assembly[] assemblies)
        {
            if (Resolve<ICompositionService>() != null)
                throw new HyperstoreException(ExceptionMessages.CompositionAlreadyDone);

            var container = Platform.PlatformServices.Current.CreateCompositionService();
            await Task.Run(() => container.Compose(assemblies)).ConfigureAwait(false);
            Register<ICompositionService>(container);
        }

        public void Dispose()
        {
            if (_services.HasValue)
                _services.Value.Clear();

            if (!_resolvedServices.HasValue)
                return;

            var list = _resolvedServices.Value;
            foreach (var disposable in list.Values.OfType<IDisposable>())
            {
                disposable.Dispose();
            }
            _resolvedServices.Value.Clear();

        }

        internal IServicesContainer Merge(IServicesContainer serviceContainer)
        {
            var container = serviceContainer as ServicesContainer;
            if (container == null || !container._services.HasValue)
                return this;

            _services.ExchangeValue(services =>
            {
                foreach (var descriptor in container._services.Value.Values)
                {
                    ServiceDescriptor desc;
                    if (!services.TryGetValue(descriptor.ServiceType, out desc))
                    {
                        return services.Add(descriptor.ServiceType, descriptor);
                    }

                    descriptor.Next = desc;
                    services = services.SetItem(descriptor.ServiceType, descriptor);
                }
                return services;
            });
            return this;
        }
    }
}
