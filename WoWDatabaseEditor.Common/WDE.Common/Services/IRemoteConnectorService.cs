﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    [UniqueProvider]
    public interface IRemoteConnectorService
    {
        bool IsConnected { get; }
        
        Task<string> ExecuteCommand(IRemoteCommand command);
        Task ExecuteCommands(IList<IRemoteCommand> commands);

        IList<IRemoteCommand> Merge(IList<IRemoteCommand> commands);
    }

    public class CouldNotConnectToRemoteServer : Exception
    {
        public CouldNotConnectToRemoteServer(Exception inner) : base("Couldn't connect to the remote server", inner)
        {
        }
    }
    
    public interface IRemoteCommand
    {
        RemoteCommandPriority Priority { get; }
        string GenerateCommand();
        bool TryMerge(IRemoteCommand other, out IRemoteCommand? mergedCommand);
    }

    public enum RemoteCommandPriority
    {
        VeryFirst = 0,
        First,
        Middle,
        Last,
        VeryLast
    }
}