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
 
#region Imports

using System;

#endregion

namespace Hyperstore.Modeling.Events
{
    ///-------------------------------------------------------------------------------------------------
    /// <summary>
    ///  Interface for event.
    /// </summary>
    ///-------------------------------------------------------------------------------------------------
    public interface IEvent
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the version.
        /// </summary>
        /// <value>
        ///  The version.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        long Version { get; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the correlation identifier.
        /// </summary>
        /// <value>
        ///  The correlation identifier.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        Guid CorrelationId { get; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the domain model.
        /// </summary>
        /// <value>
        ///  The domain model.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        string DomainModel { get; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the name of the extension.
        /// </summary>
        /// <value>
        ///  The name of the extension.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        string ExtensionName { get; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets or sets a value indicating whether [is top level event].
        /// </summary>
        /// <value>
        ///  <c>true</c> if [is top level event]; otherwise, <c>false</c>.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        bool IsTopLevelEvent { get; set; }
    }
}