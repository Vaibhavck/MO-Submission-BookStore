using System.Collections.Concurrent;
using System.Diagnostics;

namespace BookStore
{
    internal class LatencyLogger : IDisposable
    {
        private readonly string _filePath;
        private readonly ConcurrentQueue<long> _latencyQueue = new ConcurrentQueue<long>();
        private StreamWriter _writer;
        private System.Threading.Timer _summaryTimer;
        private Task _logTask;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();

        public LatencyLogger(string filePath)
        {
            this._filePath = filePath;
            this._writer = new StreamWriter(filePath, true);
            this._writer.WriteLine("MessageId,Type,Action,SendTimestamp,ReceiveTimestamp,QueueTimestamp,ApplyTimestamp,LatencyMs");
            this._writer.Flush();
            this._summaryTimer = new System.Threading.Timer(new TimerCallback(this.LogSummary), (object)null, 10000, 10000);
            this._logTask = Task.Run(new Action(this.ProcessLogQueue), this._cts.Token);
        }

        public void Log(
          MarketData data,
          long receiveTime,
          long queueTime,
          long applyTime,
          long renderStart,
          long renderEnd)
        {
            double num = (double)(applyTime - Stopwatch.GetTimestamp()) / (double)Stopwatch.Frequency * 1000.0;
            this._latencyQueue.Enqueue((long)num);
            this._logQueue.Enqueue($"{data.Id},{data.Type},{data.Action},{data.SendTimestamp:o},{Stopwatch.GetTimestamp()},{queueTime},{applyTime},{num}");
        }

        private void ProcessLogQueue()
        {
            try
            {
                while (!this._cts.Token.IsCancellationRequested || !this._logQueue.IsEmpty)
                {
                    string result;
                    while (this._logQueue.TryDequeue(out result))
                        this._writer.WriteLine(result);
                    Task.Delay(100, this._cts.Token).Wait();
                }
            }
            catch (OperationCanceledException ex)
            {
            }
        }

        private void LogSummary(object state)
        {
            List<long> source = new List<long>();
            long result;
            while (this._latencyQueue.TryDequeue(out result))
                source.Add(result);
            if (!source.Any<long>())
                return;
            source.Sort();
            long num1 = source[(int)((double)source.Count * 0.5)];
            long num2 = source[(int)((double)source.Count * 0.95)];
            long num3 = source[(int)((double)source.Count * 0.99)];
            double num4 = (double)source.Count / 10.0;
            this._writer.WriteLine("--- Summary ---");
            this._writer.WriteLine($"Throughput: {num4} msg/s");
            this._writer.WriteLine($"P50 Latency: {num1} ms");
            this._writer.WriteLine($"P95 Latency: {num2} ms");
            this._writer.WriteLine($"P99 Latency: {num3} ms");
            this._writer.WriteLine("---------------");
            this._writer.Flush();
        }

        public void Dispose()
        {
            this._summaryTimer?.Dispose();
            this._cts.Cancel();
            this._logTask.Wait();
            this._writer?.Dispose();
        }
    }
}
