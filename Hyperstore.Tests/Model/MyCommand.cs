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
using System.Linq;
using System.Text;
using Hyperstore.Modeling;
using Hyperstore.Modeling.Commands;
using Hyperstore.Tests.Model;

namespace Hyperstore.Tests
{
    /// <summary>
    /// Creation d'une classe X dont le name est test
    /// </summary>
    class MyCommand : AbstractDomainCommand, ICommandHandler<MyCommand>
    {
        public XExtendsBaseClass Element { get; private set; }

        public MyCommand( IDomainModel domainModel ) : base(domainModel)
        {
        }

        public Modeling.Events.IEvent Handle( ExecutionCommandContext<MyCommand> context )
        {
            Element = new XExtendsBaseClass( DomainModel );
            Element.Name = "Test";
            return new MyEvent( DomainModel, context.CurrentSession.SessionId );
        }
    }

    public class MyEvent : Hyperstore.Modeling.Events.DomainEvent
    {
        public MyEvent( IDomainModel domainModel, Guid correlationId )
            : base( domainModel.Name, domainModel.ExtensionName, 1, correlationId )
        {

        }
    }
}