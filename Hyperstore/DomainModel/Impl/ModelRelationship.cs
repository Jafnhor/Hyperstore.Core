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

using System;
using Hyperstore.Modeling.Commands;

#endregion

namespace Hyperstore.Modeling
{
    ///-------------------------------------------------------------------------------------------------
    /// <summary>
    ///  A model relationship.
    /// </summary>
    /// <seealso cref="T:Hyperstore.Modeling.ModelElement"/>
    /// <seealso cref="T:Hyperstore.Modeling.IModelRelationship"/>
    ///-------------------------------------------------------------------------------------------------
    public class ModelRelationship : ModelElement, IModelRelationship
    {
        //  private IModelElement _end;
        private Identity _endId;
        //  private IModelElement _start;
        private Identity _startId;

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Specialised default constructor for use only by derived classes.
        /// </summary>
        ///-------------------------------------------------------------------------------------------------
        protected ModelRelationship()
        {
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <exception cref="TypeMismatchException">
        ///  Thrown when a Type Mismatch error condition occurs.
        /// </exception>
        /// <param name="domainModel">
        ///  The domain model.
        /// </param>
        /// <param name="start">
        ///  The start.
        /// </param>
        /// <param name="endId">
        ///  The end identifier.
        /// </param>
        /// <param name="endSchema">
        ///  The end schema.
        /// </param>
        /// <param name="schemaRelationship">
        ///  (Optional) the schema relationship.
        /// </param>
        ///-------------------------------------------------------------------------------------------------
        public ModelRelationship(IDomainModel domainModel, IModelElement start, Identity endId, ISchemaElement endSchema, ISchemaRelationship schemaRelationship = null)
        {
            Contract.Requires(domainModel, "domainModel");
            Contract.Requires(start, "start");
            Contract.Requires(endSchema, "endSchema");
            Contract.Requires(endId, "endId");

            _startId = start.Id;
            _endId = endId;

            // Appel du ctor hérité
            Super(domainModel, schemaRelationship, (dm, melId, mid) => new AddRelationshipCommand(mid as ISchemaRelationship, start, _endId, melId));

            if (((IModelRelationship)this).SchemaRelationship == null)
                throw new TypeMismatchException(ExceptionMessages.SchemaMismatch);
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <exception cref="Exception">
        ///  Thrown when an exception error condition occurs.
        /// </exception>
        /// <param name="start">
        ///  The start.
        /// </param>
        /// <param name="end">
        ///  The end.
        /// </param>
        /// <param name="schemaRelationship">
        ///  (Optional) the schema relationship.
        /// </param>
        ///-------------------------------------------------------------------------------------------------
        public ModelRelationship(IModelElement start, IModelElement end, ISchemaRelationship schemaRelationship = null)
        {
            Contract.Requires(start, "start");
            Contract.Requires(end, "end");

            _startId = start.Id;
            _endId = end.Id;
            // Appel du ctor hérité
            Super(start.DomainModel, schemaRelationship, (dm, melId, mid) => new AddRelationshipCommand(mid as ISchemaRelationship, start, end.Id, melId));

            if (((IModelRelationship)this).SchemaRelationship == null)
                throw new TypeMismatchException(ExceptionMessages.SchemaMismatch);
        }

        ISchemaRelationship IModelRelationship.SchemaRelationship
        {
            get { return ((IModelElement)this).SchemaInfo as ISchemaRelationship; }
        }

        IModelElement IModelRelationship.Start
        {
            get { return DomainModel.GetElement(this._startId); }
        }

        Identity IModelRelationship.EndId
        {
            get { return _endId; }
        }


        IModelElement IModelRelationship.End
        {
            get { return Store.GetElement(this._endId); }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Called when [deserializing].
        /// </summary>
        /// <param name="schemaElement">
        ///  The schema element.
        /// </param>
        /// <param name="domainModel">
        ///  The domain model.
        /// </param>
        /// <param name="key">
        ///  The key.
        /// </param>
        /// <param name="start">
        ///  The start.
        /// </param>
        /// <param name="end">
        ///  The end.
        /// </param>
        ///-------------------------------------------------------------------------------------------------
        protected override void OnDeserializing(ISchemaElement schemaElement, IDomainModel domainModel, string key, Identity start, Identity end)
        {
            DebugContract.Requires(start);
            DebugContract.Requires(end);

            base.OnDeserializing(schemaElement, domainModel, key, start, end);

            _startId = start;
            _endId = end;
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Removes this instance.
        /// </summary>
        ///-------------------------------------------------------------------------------------------------
        protected override void Remove()
        {
            using (var session = EnsuresRunInSession())
            {
                var cmd = new RemoveRelationshipCommand(this);
                Session.Current.Execute(cmd);
                if (session != null)
                    session.AcceptChanges();
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        ///  A string that represents the current object.
        /// </returns>
        ///-------------------------------------------------------------------------------------------------
        public override string ToString()
        {
            return String.Format("{0} : {1} ({2}->{3})", GetType().Name, ((IModelElement)this).Id, _startId, _endId);
        }
    }
}