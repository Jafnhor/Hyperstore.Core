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
 
namespace Hyperstore.Modeling.Commands
{
    ///-------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Interface for command processor.
    /// </summary>
    ///-------------------------------------------------------------------------------------------------

    internal interface ICommandProcessor
    {
      ///-------------------------------------------------------------------------------------------------
      /// <summary>
      ///  Clears the interceptors.
      /// </summary>
      ///-------------------------------------------------------------------------------------------------
      void ClearInterceptors();

      ///-------------------------------------------------------------------------------------------------
      /// <summary>
      ///  Registers a command rule.
      /// </summary>
      /// <param name="rule">
      ///  The rule.
      /// </param>
      /// <param name="priority">
      ///  The execution priority rule. Lower first.
      /// </param>
      ///-------------------------------------------------------------------------------------------------
      void RegisterInterceptor(ICommandInterceptor rule, int priority);

      ///-------------------------------------------------------------------------------------------------
      /// <summary>
      ///  Set the handler.
      /// </summary>
      /// <param name="handler">
      ///  The handler. Null to use the defaut handler.
      /// </param>
      ///-------------------------------------------------------------------------------------------------
      void SetHandler(ICommandHandler handler);
    }

    /// <summary>
    ///     Command processor
    /// </summary>
    /// <typeparam name="TCommand">Command type</typeparam>
    internal interface ICommandProcessor<TCommand> : ICommandProcessor where TCommand : IDomainCommand
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Processes the command with the specified context.
        /// </summary>
        /// <param name="context">
        ///  The command context.
        /// </param>
        /// <returns>
        ///  The ContinuationStatus.
        /// </returns>
        ///-------------------------------------------------------------------------------------------------
        ContinuationStatus Process(ExecutionCommandContext<TCommand> context);
    }
}