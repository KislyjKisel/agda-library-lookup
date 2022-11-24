using System;
using System.Threading.Tasks;

namespace AgdaLibraryLookup.Agda.Connection
{
    public interface ICommunicator
    {
        public delegate void ResponseEventHandler(object sender, Response arg);

        public event ResponseEventHandler? Response;
        public event EventHandler? IsBusyChanged;

        public void SendMessage(Message message);
        public Task<Either<string, Response>> GetResponseAsync(); // sync
        public Either<string, Response> ProcessResponse(string text);
        public Task<R> SendRequestAsync<R>(Request<R> request);

        public bool IsClosed { get; }
        public bool IsBusy { get; }

        public void Close();
    }
}
