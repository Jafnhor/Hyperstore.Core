﻿// Copyright 2014 Zenasoft.  All rights reserved.
//
// This file is part of Hyperstore.
//
//    Hyperstore is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    Hyperstore is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with Hyperstore.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// from https://github.com/aspnet/EntityFramework/blob/dev/src/EntityFramework/Utilities/ThreadSafeLazyRef.cs
namespace Hyperstore.Modeling.Utils
{
    [DebuggerStepThrough]
    public sealed class ThreadSafeLazyRef<T>
    where T : class
    {
        private Func<T> _initializer;
        private object _syncLock;
        private T _value;
        public ThreadSafeLazyRef(Func<T> initializer)
        {
            Contract.Requires(initializer, "initializer");
            _initializer = initializer;
        }
        public T Value
        {
            get
            {
                if (_value == null)
                {
                    var syncLock = new object();
                    syncLock
                    = Interlocked.CompareExchange(ref _syncLock, syncLock, null)
                    ?? syncLock;
                    lock (syncLock)
                    {
                        if (_value == null)
                        {
                            _value = _initializer();
                            _syncLock = null;
                            _initializer = null;
                        }
                    }
                }
                return _value;
            }
        }
        public void ExchangeValue(Func<T, T> newValueCreator)
        {
            Contract.Requires(newValueCreator, "newValueCreator");
            T originalValue, newValue;
            do
            {
                originalValue = Value;
                newValue = newValueCreator(originalValue);
                if (ReferenceEquals(newValue, originalValue))
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(ref _value, newValue, originalValue) != originalValue);
        }
        public bool HasValue
        {
            get { return _value != null; }
        }
    }
}