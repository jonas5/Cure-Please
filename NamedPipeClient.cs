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

    public bool IsConnected => _pipeClient?.IsConnected ?? false;

    public NamedPipeClient(string pipeName)
    {
        _pipeName = pipeName;
    }

    public void Connect()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        Task.Run(() => ClientLoop(_cancellationTokenSource.Token));
    }

    public void Send(string message)
    {
        if (IsConnected && _pipeClient != null && _pipeClient.CanWrite)
        {
            try
            {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message + "\n");
                _pipeClient.Write(buffer, 0, buffer.Length);
                _pipeClient.Flush();
            }
            catch (IOException)
            {
                // Pipe may have been closed
                _pipeClient?.Dispose();
                Disconnected?.Invoke();
            }
        }
    }

    private async Task ClientLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut);
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
