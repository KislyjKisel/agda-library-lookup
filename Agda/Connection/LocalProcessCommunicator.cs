using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AgdaLibraryLookup.Agda.Connection
{
    public class LocalProcessCommunicator : ICommunicator
    {
        public LocalProcessCommunicator(string executablePath, bool callback_based = false)
        {
            _process = new Process();
            _process.StartInfo.FileName = executablePath;
            _process.StartInfo.Arguments = "--interaction";
            _process.StartInfo.RedirectStandardInput = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.UseShellExecute = false;
            _process.Start();
            _streamCommunicator = new StreamCommunicator(_process.StandardInput, callback_based ? null : _process.StandardOutput);
            _streamCommunicator.Response += (_, r) => Response?.Invoke(this, r);
            _streamCommunicator.IsBusyChanged += (_, r) => IsBusyChanged?.Invoke(this, r);

            if(callback_based) { 
                _process.OutputDataReceived += (s, e) => { ProcessResponse(e.Data!); }; 
                _process.EnableRaisingEvents = true;
                _process.BeginOutputReadLine();
            }
        }
        
        // ICommunicator

        public event ICommunicator.ResponseEventHandler? Response;
        public event EventHandler? IsBusyChanged;

        public void SendMessage(Message request)
            => _streamCommunicator.SendMessage(request);

        public Task<Either<string, Response>> GetResponseAsync() 
            => _streamCommunicator.GetResponseAsync();

        public Task<R> SendRequestAsync<R>(Request<R> req)
            => _streamCommunicator.SendRequestAsync(req);

        public Either<string, Response> ProcessResponse(string responseRaw)
            => _streamCommunicator.ProcessResponse(responseRaw);

        public void Close() 
        {
            Response = null;
            _streamCommunicator.Close();
        }

        public bool IsClosed => _streamCommunicator.IsClosed || _process.HasExited;
        public bool IsBusy => _streamCommunicator.IsBusy;

        private readonly StreamCommunicator _streamCommunicator;
        private readonly Process _process;
    }
}
