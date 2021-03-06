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
 
#region Imports

using Hyperstore.Modeling.HyperGraph;
using System.Collections.Generic;

#endregion

namespace Hyperstore.Modeling.Traversal
{
    ///-------------------------------------------------------------------------------------------------
    /// <summary>
    ///  Interface for graph path traverser.
    /// </summary>
    ///-------------------------------------------------------------------------------------------------
    public interface IGraphPathTraverser
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Initializes this instance.
        /// </summary>
        /// <param name="query">
        ///  The query.
        /// </param>
        ///-------------------------------------------------------------------------------------------------
        void Initialize(ITraversalQuery query);

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Enumerates the items in this collection that meet given criteria.
        /// </summary>
        /// <param name="node">
        ///  The start.
        /// </param>
        /// <returns>
        ///  An enumerator that allows foreach to be used to process the matched items.
        /// </returns>
        ///-------------------------------------------------------------------------------------------------
        IEnumerable<GraphPath> Traverse(NodeInfo node);
    }
}