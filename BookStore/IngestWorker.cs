using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BookStore
{
    internal class IngestWorker
    {
        private readonly DataStore _dataStore;
        private readonly LatencyLogger _latencyLogger;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();
        private const int Port = 12345;
        private TcpListener _listener;
        private Task _listenerTask;
        private Task _processorTask;
        private bool _isRunning = true;

        public IngestWorker(DataStore dataStore, LatencyLogger latencyLogger)
        {
            this._dataStore = dataStore;
            this._latencyLogger = latencyLogger;
        }

        public void Start()
        {
            this._listenerTask = Task.Run((Func<Task>)(() => this.ListenForConnection(this._cts.Token)));
            this._processorTask = Task.Run((Func<Task>)(() => this.ProcessMessages(this._cts.Token)));
        }

        public void Stop()
        {
            this._isRunning = false;
            this._cts.Cancel();
            this._listener?.Stop();
        }

        private async Task ListenForConnection(CancellationToken token)
        {
            try
            {
                this._listener = new TcpListener(IPAddress.Any, 12345);
                this._listener.Start();
                Console.WriteLine("Listening for simulator connection...");
                using (TcpClient client = await this._listener.AcceptTcpClientAsync(token))
                {
                    using (StreamReader reader = new StreamReader((Stream)client.GetStream(), Encoding.UTF8))
                    {
                        Console.WriteLine("Simulator connected. Starting ingest.");
                        string line;
                        string str;
                        while (true)
                        {
                            str = await reader.ReadLineAsync();
                            if ((line = str) != null && this._isRunning)
                            {
                                long receiveTime = Stopwatch.GetTimestamp();
                                this._messageQueue.Enqueue($"{line}|{receiveTime}");
                            }
                            else
                                break;
                        }
                        str = (string)null;
                        line = (string)null;
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine("Ingest worker stopped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ingest worker error: " + ex.Message);
            }
        }

        private async Task ProcessMessages(CancellationToken token)
        {
            try
            {
                while (this._isRunning || !this._messageQueue.IsEmpty)
                {
                    List<string> batch = new List<string>();
                    string message;
                    while (batch.Count < 1000 && this._messageQueue.TryDequeue(out message))
                        batch.Add(message);
                    if (batch.Count > 0)
                    {
                        long applyStartTime = Stopwatch.GetTimestamp();
                        foreach (string msg in batch)
                        {
                            string[] parts = msg.Split('|');
                            string[] dataParts = parts[0].Split(',');
                            long receiveTime = long.Parse(parts[1]);
                            MarketData data = new MarketData()
                            {
                                Action = dataParts[0],
                                Type = dataParts[1],
                                SendTimestamp = DateTime.Parse(dataParts[2], (IFormatProvider)CultureInfo.InvariantCulture),
                                Id = long.Parse(dataParts[3]),
                                Symbol = dataParts[4],
                                Side = dataParts[5],
                                Price = Decimal.Parse(dataParts[6], (IFormatProvider)CultureInfo.InvariantCulture),
                                Quantity = long.Parse(dataParts[7])
                            };
                            long queueTime = Stopwatch.GetTimestamp();
                            this._dataStore.AddOrUpdate(data);
                            long applyEndTime = Stopwatch.GetTimestamp();
                            this._latencyLogger.Log(data, receiveTime, queueTime, applyEndTime, 0L, 0L);
                            parts = (string[])null;
                            dataParts = (string[])null;
                            data = (MarketData)null;
                        }
                    }
                    await Task.Delay(10, token);
                    batch = (List<string>)null;
                }
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine("Message processor stopped.");
            }
        }
    }
}
