using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AgdaLibraryLookup.Agda.Connection
{
    public class StreamCommunicator : ICommunicator
    {
        public StreamCommunicator(StreamWriter output, StreamReader input) 
        {
            (_output, _input, _responseHandlers) = (output, input, new());

            Response += (s, rr) =>
            {
                bool wasBusy = IsBusy;
                // the order of responses should be the same as the order of requests,
                // so - stop at first successful response handler (and remove it).
                for(int i = 0; i < _responseHandlers.Count; ++i)
                {
                    var (mr, tcs) = _responseHandlers[i](rr);
                    mr.Map(r => { 
                        tcs.SetResult(r); 
                        _responseHandlers.RemoveAt(i); 
                        i = int.MaxValue - 2; 
                    }); 
                }

                if(wasBusy ^ IsBusy) IsBusyChanged?.Invoke(this, new());
            };
        }

        // ICommunicator

        public event ICommunicator.ResponseEventHandler? Response;
        public event EventHandler? IsBusyChanged;

        public void SendMessage(Message request)
        {
            if (IsClosed) throw new InvalidOperationException();
            _output.WriteLine(request.Serialize());
        }

        public async Task<Either<string, Response>> GetResponseAsync()
        {
            if (IsClosed) throw new InvalidOperationException();
            string? responseRaw = await _input.ReadLineAsync();
            if (responseRaw is null) throw new EndOfStreamException();
            return ProcessResponse(responseRaw);
        }

        public Either<string, Response> ProcessResponse(string responseRaw)
        {
            var response = Connection.Response.Deserialize(responseRaw);
            response.MapRight(responseR => Response?.Invoke(this, responseR));
            return response;
        }

        public async Task<R> SendRequestAsync<R>(Request<R> request)
        {
            if(IsClosed) throw new InvalidOperationException();
            var tcs = new TaskCompletionSource<object>();
            _responseHandlers.Add(rr => (request.ProcessResponseObject(rr), tcs));
            SendMessage(request);
            return (R)await tcs.Task;
        }

        public bool IsClosed => _closed;
        public bool IsBusy => _responseHandlers.Count > 0;

        public void Close()
        {
            Response = null;
            _closed = true;
            SendMessage(new ExitMessage());
        }

        private readonly StreamWriter _output;
        private readonly StreamReader _input;
        private bool _closed = false;


        private delegate (Maybe<object>, TaskCompletionSource<object>) QResponseHandler(Response response);
        private readonly List<QResponseHandler> _responseHandlers;
    }
}
