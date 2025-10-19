using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class NamedPipeClient
{
    private readonly string _pipeName;
    private NamedPipeClientStream _pipeClient;
    private CancellationTokenSource _cancellationTokenSource;

    public event Action<string> MessageReceived;
    public event Action Connected;
    public event Action Disconnected;

    public NamedPipeClient(string pipeName)
    {
        _pipeName = pipeName;
    }

    public void Connect()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        Task.Run(() => ClientLoop(_cancellationTokenSource.Token));
    }

    private async Task ClientLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.In);
                _pipeClient.Connect(5000);
                Connected?.Invoke();

                using (var reader = new StreamReader(_pipeClient, Encoding.UTF8))
                {
                    while (_pipeClient.IsConnected && !cancellationToken.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line != null)
                        {
                            MessageReceived?.Invoke(line);
                        }
                        else
                        {
                            break; // Pipe was closed
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception)
            {
                // Log or handle connection errors
            }
            finally
            {
                _pipeClient?.Dispose();
                Disconnected?.Invoke();
                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(5000, cancellationToken); // Wait before reconnecting
                }
            }
        }
    }

    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
        _pipeClient?.Dispose();
    }
}
