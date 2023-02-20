using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NamePipe.Helper
{
    public sealed class ClientConnectedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
    }

    public sealed class ClientDisconnectedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
    }

    public sealed class MessageReceivedEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}
