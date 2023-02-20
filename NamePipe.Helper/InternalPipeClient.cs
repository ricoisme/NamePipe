using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NamePipe.Helper
{
    internal sealed class InternalPipeClient : IPipeClient
    {

        private readonly NamedPipeClientStream _pipeClient;
        private const int BufferSize = 2048;

        public InternalPipeClient(string pipeName)
        {
            /*
               \\ServerName\pipe\NameOfThePipe
               \\.\pipe\NameOfThePipe =>local
             */
            _pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);           
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;

        public void Start()
        {
            const int tryConnectTimeout = 5 * 60 * 1000;
            if (!_pipeClient.IsConnected)
            {
                _pipeClient.Connect(tryConnectTimeout);
                _pipeClient.ReadMode = PipeTransmissionMode.Message;
                Console.WriteLine("Connected with Server...\n");

                BeginRead(new Info());
            }
        }

        public void Stop()
        {
            try
            {
                _pipeClient.WaitForPipeDrain();
            }
            finally
            {
                _pipeClient.Close();
                _pipeClient.Dispose();
                Console.WriteLine("client closed \n");
            }
        }
        public Task<string> SendMessageAsync(string message)
        {
            var taskCompletionSource = new TaskCompletionSource<string>();

            if (_pipeClient.IsConnected)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                _pipeClient.BeginWrite(buffer, 0, buffer.Length, asyncResult =>
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
            else
            {
                throw new IOException("pipe is not connected");
            }

            return taskCompletionSource.Task;
        }

        private string EndWriteCallBack(IAsyncResult asyncResult)
        {
            _pipeClient.EndWrite(asyncResult);
            _pipeClient.Flush();

            return "success";
        }

        private void BeginRead(Info info)
        {
            try
            {
                _pipeClient.BeginRead(info.Buffer, 0, BufferSize, EndReadCallBack, info);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
       
        private void EndReadCallBack(IAsyncResult result)
        {          
            var readBytes = _pipeClient.EndRead(result);
            if (readBytes > 0)
            {
                var info = (Info)result.AsyncState;

                // Get the read bytes and append them
                info.StringBuilder.Append(Encoding.UTF8.GetString(info.Buffer, 0, readBytes));

                if (!_pipeClient.IsMessageComplete) // Message is not complete, continue reading
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
                //if (!_isStopping)
                //{
                //    lock (_lockingObject)
                //    {
                //        if (!_isStopping)
                //        {
                //            OnDisconnected();
                //            ((IPipeServer)this).Stop();
                //        }
                //    }
                //}
            }
        }

        private void OnMessageReceived(string message)
        {
            MessageReceivedEvent?.Invoke(this, new MessageReceivedEventArgs { Message = message });
        }

       
    }
}
