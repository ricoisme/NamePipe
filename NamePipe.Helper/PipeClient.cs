using System.IO.Pipes;
using System.Text;
using System.Threading;
using static NamePipe.Helper.InternalPipeServer;

namespace NamePipe.Helper
{
   
    public interface IPipeClient : IPipeChannel
    {
        /// <summary>
        /// This method sends the given message asynchronously over the communication channel
        /// </summary>
        /// <param name="message">Comunication Message</param>
        /// <returns>A task of TaskResult</returns>
        Task<string> SendMessageAsync(string message);

        event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;

    }

    public sealed class PipeClient : IPipeClient
    {
        private readonly InternalPipeClient _pipeClient;      

        public PipeClient(string pipeName)
        {
            _pipeClient = new InternalPipeClient(pipeName);
           
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;


        void IPipeChannel.Start()
        {     
            _pipeClient.MessageReceivedEvent += MessageReceivedHandler;
            _pipeClient.Start();
        }

        private void MessageReceivedHandler(object sender, MessageReceivedEventArgs eventArgs)
        {
            OnMessageReceived(eventArgs);
        }

        private void OnMessageReceived(MessageReceivedEventArgs eventArgs)
        {
            MessageReceivedEvent?.Invoke(this, eventArgs);
        }

        void IPipeChannel.Stop()
        {
            _pipeClient.Stop();
        }
       
        Task<string> IPipeClient.SendMessageAsync(string message)
        {
           return _pipeClient.SendMessageAsync(message);
        }
    }
}
