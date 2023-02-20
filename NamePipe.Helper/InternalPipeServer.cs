using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NamePipe.Helper
{
   

    internal sealed class InternalPipeServer : IPipeServer
    {
        public readonly string _Id;      
        private readonly NamedPipeServerStream _pipeServer;
        private readonly object _lockingObject = new object();
        private bool _isStopping;
       

        public InternalPipeServer(string pipeName, int maxNumberOfServerInstances)
        {
            //InOut:接收和發送
            _pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxNumberOfServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            _Id = Guid.NewGuid().ToString();
        }
               

        string IPipeServer.ServerId => _Id;

        public event EventHandler<ClientConnectedEventArgs> ClientConnectedEvent;

        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnectedEvent;

        public event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;

        void IPipeChannel.Start()
        {
            try
            {
                //等待client連接
                _pipeServer.BeginWaitForConnection(WaitForConnectionCallBack, null);
            }
            catch (Exception ex)
            {                
                throw;
            }
        }

        void IPipeChannel.Stop()
        {
            _isStopping = true;

            try
            {
                if (_pipeServer.IsConnected)
                {
                    _pipeServer.Disconnect();
                }
            }
            catch (Exception ex)
            {                
                throw;
            }
            finally
            {
                _pipeServer.Close();
                _pipeServer.Dispose();
            }
        }

        public Task<string> SendMessageAsync(string message)
        {
            var taskCompletionSource = new TaskCompletionSource<string>();
            var buffer = Encoding.UTF8.GetBytes(message);         

            if (_pipeServer.IsConnected)
            {                
                _pipeServer.BeginWrite(buffer, 0, buffer.Length, asyncResult =>
                {
                    try
                    {
                        taskCompletionSource.SetResult(EndWriteCallBack(asyncResult));
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.SetException(ex);
                    }
                }, null);
            }
            
            return taskCompletionSource.Task;
        }

        private void BeginRead(Info info)
        {
            try
            {
                _pipeServer.BeginRead(info.Buffer, 0, Info.BufferSize, EndReadCallBack, info);
            }
            catch (Exception ex)            {
                
                throw;
            }
        }

        private void WaitForConnectionCallBack(IAsyncResult result)
        {
            if (!_isStopping)
            {
                lock (_lockingObject)
                {
                    if (!_isStopping)
                    {
                        // Call EndWaitForConnection to complete the connection operation
                        _pipeServer.EndWaitForConnection(result);

                        OnConnected();

                        BeginRead(new Info());
                    }
                }
            }
        }

        private void EndReadCallBack(IAsyncResult result)
        {
            var readBytes = _pipeServer.EndRead(result);
            if (readBytes > 0)
            {
                var info = (Info)result.AsyncState;

                // Get the read bytes and append them
                info.StringBuilder.Append(Encoding.UTF8.GetString(info.Buffer, 0, readBytes));

                if (!_pipeServer.IsMessageComplete) // Message is not complete, continue reading
                {
                    BeginRead(info);
                }
                else // Message is completed
                {
                    // Finalize the received string and fire MessageReceivedEvent
                    var message = info.StringBuilder.ToString().TrimEnd('\0');

                    OnMessageReceived(message);

                    // Begin a new reading operation
                    BeginRead(new Info());
                }
            }
            else // When no bytes were read, it can mean that the client have been disconnected
            {
                if (!_isStopping)
                {
                    lock (_lockingObject)
                    {
                        if (!_isStopping)
                        {
                            OnDisconnected();
                            ((IPipeServer)this).Stop();
                        }
                    }
                }
            }
        }

        private string EndWriteCallBack(IAsyncResult asyncResult)
        {
            _pipeServer.EndWrite(asyncResult);
            _pipeServer.Flush();

            return "success";
        }

        private void OnMessageReceived(string message)
        {
            MessageReceivedEvent?.Invoke(this, new MessageReceivedEventArgs { Message = message });
        }

        private void OnConnected()
        {
            ClientConnectedEvent?.Invoke(this, new ClientConnectedEventArgs { ClientId = _Id });
        }

        private void OnDisconnected()
        {
            ClientDisconnectedEvent?.Invoke(this, new ClientDisconnectedEventArgs { ClientId = _Id });
        }

       
    }
}
