using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace MedicalCustomerManagement.Tests.Fixtures
{
    /// <summary>
    /// Fixture that automatically starts the application server on localhost:5000
    /// before tests run and stops it afterward. Used for UI and integration tests.
    /// </summary>
    public class ServerFixture : IAsyncLifetime
    {
        private Process? _serverProcess;
        private const int ServerStartupDelayMs = 3000;
        private const string ServerUrl = "http://localhost:5000";

        public async Task InitializeAsync()
        {
            // Check if server is already running
            if (IsServerRunning())
            {
                Console.WriteLine("✓ Application server already running on localhost:5000");
                return;
            }

            try
            {
                Console.WriteLine("Starting application server...");
                _serverProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "run",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = Directory.GetCurrentDirectory()
                    }
                };

                _serverProcess.OutputDataReceived += (sender, args) => 
                {
                    if (!string.IsNullOrEmpty(args.Data))
                        Console.WriteLine($"[SERVER] {args.Data}");
                };

                _serverProcess.ErrorDataReceived += (sender, args) => 
                {
                    if (!string.IsNullOrEmpty(args.Data))
                        Console.WriteLine($"[SERVER ERROR] {args.Data}");
                };

                _serverProcess.Start();
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();

                Console.WriteLine($"Server process started (PID: {_serverProcess.Id})");
                Console.WriteLine($"Waiting {ServerStartupDelayMs}ms for server to be ready...");

                // Wait for server to start
                await Task.Delay(ServerStartupDelayMs);

                // Verify server is responding
                int retries = 5;
                while (!IsServerRunning() && retries > 0)
                {
                    Console.WriteLine($"Server not responding yet, retrying... ({retries} attempts left)");
                    await Task.Delay(1000);
                    retries--;
                }

                if (IsServerRunning())
                {
                    Console.WriteLine("✓ Application server started successfully on localhost:5000");
                }
                else
                {
                    Console.WriteLine("⚠ Server may not be responding properly, but continuing with tests...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Failed to start application server: {ex.Message}");
                Console.WriteLine("Tests will proceed, but UI tests may fail without a running server");
            }
        }

        public async Task DisposeAsync()
        {
            if (_serverProcess != null)
            {
                try
                {
                    Console.WriteLine("Stopping application server...");
                    
                    if (!_serverProcess.HasExited)
                    {
                        _serverProcess.Kill(entireProcessTree: true);
                        await Task.Delay(500);
                    }

                    _serverProcess.Dispose();
                    Console.WriteLine("✓ Application server stopped");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ Error stopping server: {ex.Message}");
                }
            }
        }

        private bool IsServerRunning()
        {
            try
            {
                using (var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2) })
                {
                    var response = client.GetAsync($"{ServerUrl}/health").Result;
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Collection definition for tests that require the server to be running.
    /// Apply this to test classes that need the application server.
    /// </summary>
    [CollectionDefinition("Server Collection")]
    public class ServerCollection : ICollectionFixture<ServerFixture>
    {
        // This class has no code, just used to define the collection
    }
}
