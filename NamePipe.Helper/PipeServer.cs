using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NamePipe.Helper
{
    public interface IPipeChannel
    {
        /// <summary>
        /// Starts the communication channel
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the communication channel
        /// </summary>
        void Stop();
    }

    public interface IPipeServer : IPipeChannel
    {
        /// <summary>
        /// This event is fired when a message is received 
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;

        /// <summary>
        /// This event is fired when a client connects 
        /// </summary>
        event EventHandler<ClientConnectedEventArgs> ClientConnectedEvent;

        /// <summary>
        /// This event is fired when a client disconnects 
        /// </summary>
        event EventHandler<ClientDisconnectedEventArgs> ClientDisconnectedEvent;

        /// <summary>
        /// Gets the server id
        /// </summary>
        /// <value>
        /// The server id
        /// </value>
        string ServerId { get; }

        Task<string> SendMessageAsync(string message);
       
    }   

    public sealed class PipeServer : IPipeServer
    {
        private const int MaxNumberOfServerInstances = 10;
        private readonly string _serverName;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly IDictionary<string, IPipeServer> _servers;        

        public PipeServer(string serverName)
        {
            _serverName = serverName;
            _synchronizationContext = AsyncOperationManager.SynchronizationContext;
            _servers = new ConcurrentDictionary<string, IPipeServer>();
        }

        string IPipeServer.ServerId => _serverName;

        public event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;

        public event EventHandler<ClientConnectedEventArgs> ClientConnectedEvent;

        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnectedEvent;

        void IPipeChannel.Start()
        {
            StartNamedPipeServer();
        }

        void IPipeChannel.Stop()
        {
            foreach (var server in _servers.Values)
            {
                try
                {
                    UnregisterFromServerEvents(server);
                    server.Stop();
                }
                catch (Exception)
                {                   
                }
            }

            _servers.Clear();
        }

        Task<string> IPipeServer.SendMessageAsync(string message)
        {
           foreach(var server in _servers)
           {             
              server.Value.SendMessageAsync(message);
           }
           return Task.FromResult("");
        }

        private void StartNamedPipeServer()
        {
            var server = new InternalPipeServer(_serverName, MaxNumberOfServerInstances);
            _servers[server._Id] = server;

            server.ClientConnectedEvent += ClientConnectedHandler;
            server.ClientDisconnectedEvent += ClientDisconnectedHandler;
            server.MessageReceivedEvent += MessageReceivedHandler;

            ((IPipeServer)server).Start();
        }

        private void StopNamedPipeServer(string id)
        {
            UnregisterFromServerEvents(_servers[id]);
            _servers[id].Stop();
            _servers.Remove(id);
        }

        private void UnregisterFromServerEvents(IPipeServer server)
        {
            server.ClientConnectedEvent -= ClientConnectedHandler;
            server.ClientDisconnectedEvent -= ClientDisconnectedHandler;
            server.MessageReceivedEvent -= MessageReceivedHandler;
        }
               

        private void OnMessageReceived(MessageReceivedEventArgs eventArgs)
        {
            _synchronizationContext.Post(e => MessageReceivedEvent?.Invoke(this, (MessageReceivedEventArgs)e), eventArgs);
        }

        private void OnClientConnected(ClientConnectedEventArgs eventArgs)
        {
            _synchronizationContext.Post(e => ClientConnectedEvent?.Invoke(this, (ClientConnectedEventArgs)e), eventArgs);
        }

        private void OnClientDisconnected(ClientDisconnectedEventArgs eventArgs)
        {
            _synchronizationContext.Post(e => ClientDisconnectedEvent?.Invoke(this, (ClientDisconnectedEventArgs)e), eventArgs);
        }

        private void ClientConnectedHandler(object sender, ClientConnectedEventArgs eventArgs)
        {
            OnClientConnected(eventArgs);

            // Create a additional server as a preparation for new connection
            StartNamedPipeServer();
        }

        private void ClientDisconnectedHandler(object sender, ClientDisconnectedEventArgs eventArgs)
        {
            OnClientDisconnected(eventArgs);

            StopNamedPipeServer(eventArgs.ClientId);
        }

        private void MessageReceivedHandler(object sender, MessageReceivedEventArgs eventArgs)
        {
            OnMessageReceived(eventArgs);
        }

        
    }


}
