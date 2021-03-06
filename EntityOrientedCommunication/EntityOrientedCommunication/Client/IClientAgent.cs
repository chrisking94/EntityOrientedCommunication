﻿/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：5/15/2019 5:42:39 PM
 * ============================================================================================*/
using System;
using System.Net;

namespace EntityOrientedCommunication.Client
{
    /// <summary>
    /// a kind of mail dispatcher
    /// </summary>
    public interface IClientAgent
    {
        /// <summary>
        /// client name
        /// </summary>
        string ClientName { get; }

        /// <summary>
        /// server name
        /// </summary>
        string TeleClientName { get; }

        /// <summary>
        /// some events about connection affair
        /// </summary>
        event ClientAgentEventHandler ClientAgentEvent;

        /// <summary>
        /// point to the server
        /// </summary>
        EndPoint EndPoint { get; }

        /// <summary>
        /// denote this agent has logged in or not
        /// </summary>
        bool LoggedIn { get; }

        /// <summary>
        /// server time
        /// </summary>
        DateTime Now { get; }

        /// <summary>
        /// login to server with specified username and password
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="timeout">the maximum time for waiting, in milliseconds</param>
        void Login(string username, string password, int timeout);

        /// <summary>
        /// logout from server
        /// </summary>
        void Logout();
    }
}