using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using OpenTelemetry.Internal;
using OpenTelemetry.Metrics;

namespace OpenTelemetry.Exporter.Prometheus.HttpListener
{
    internal sealed class PrometheusHttpListener : IDisposable
    {
        internal readonly PrometheusHttpListenerOptions Options;

        private readonly PrometheusExporter exporter;
        private readonly System.Net.HttpListener httpListener = new();
        private readonly object syncObject = new();

        private CancellationTokenSource tokenSource;
        private Task workerThread;

        public PrometheusHttpListener(MeterProvider meterProvider, PrometheusHttpListenerOptions options)
        {
            Guard.ThrowIfNull(meterProvider);

            if ((options.HttpListenerPrefixes?.Count ?? 0) <= 0)
            {
                throw new ArgumentException("No HttpListenerPrefixes were specified on PrometheusHttpListenerOptions.");
            }

            this.Options = options;

            if (!meterProvider.TryFindExporter(out PrometheusExporter exporter))
            {
                throw new ArgumentException("A PrometheusExporter could not be found configured on the provided MeterProvider.");
            }

            this.exporter = exporter;
            PrometheusExporterOptions exporterOptions = this.exporter.Options;
            string path = exporterOptions.ScrapeEndpointPath ?? PrometheusExporterOptions.DefaultScrapeEndpointPath;

        }

        /// <summary>
        /// Start Http Server.
        /// </summary>
        /// <param name="token">An optional <see cref="CancellationToken"/> that can be used to stop the HTTP server.</param>
        public void Start(CancellationToken token = default)
        {
            lock (this.syncObject)
            {
                if (this.tokenSource != null)
                {
                    return;
                }

                // link the passed in token if not null
                this.tokenSource = token == default ?
                    new CancellationTokenSource() :
                    CancellationTokenSource.CreateLinkedTokenSource(token);

                this.workerThread = Task.Factory.StartNew(this.WorkerProc, default, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }

        /// <summary>
        /// Stop exporter.
        /// </summary>
        public void Stop()
        {
            lock (this.syncObject)
            {
                if (this.tokenSource == null)
                {
                    return;
                }

                this.tokenSource.Cancel();
                this.workerThread.Wait();
                this.tokenSource = null;
            }
        }

        public void Dispose()
        {
            if (this.httpListener != null && this.httpListener.IsListening)
            {
                this.Stop();
                this.httpListener.Close();
            }
        }

        private void WorkerProc()
        {
            this.httpListener.Start();

            try
            {
                using var scope = SuppressInstrumentationScope.Begin();
                while (!this.tokenSource.IsCancellationRequested)
                {
                    var ctxTask = this.httpListener.GetContextAsync();
                    ctxTask.Wait(this.tokenSource.Token);
                    var ctx = ctxTask.Result;

                    Task.Run(() => this.ProcessRequestAsync(ctx));
                }
            }
            catch (OperationCanceledException ex)
            {
                PrometheusExporterEventSource.Log.CanceledExport(ex);
            }
            finally
            {
                try
                {
                    this.httpListener.Stop();
                    this.httpListener.Close();
                }
                catch (Exception exFromFinally)
                {
                    PrometheusExporterEventSource.Log.FailedShutdown(exFromFinally);
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            try
            {
                var collectionResponse = await this.exporter.CollectionManager.EnterCollect().ConfigureAwait(false);
                try
                {
                    context.Response.Headers.Add("Server", string.Empty);
                    if (collectionResponse.View.Count > 0)
                    {
                        context.Response.StatusCode = 200;
                        context.Response.Headers.Add("Last-Modified", collectionResponse.GeneratedAtUtc.ToString("R"));
                        context.Response.ContentType = "text/plain; charset=utf-8; version=0.0.4";

                        await context.Response.OutputStream.WriteAsync(collectionResponse.View.Array, 0, collectionResponse.View.Count).ConfigureAwait(false);
                    }
                    else
                    {
                        // It's not expected to have no metrics to collect, but it's not necessarily a failure, either.
                        context.Response.StatusCode = 204;
                        PrometheusExporterEventSource.Log.NoMetrics();
                    }
                }
                finally
                {
                    this.exporter.CollectionManager.ExitCollect();
                }
            }
            catch (Exception ex)
            {
                PrometheusExporterEventSource.Log.FailedExport(ex);

                context.Response.StatusCode = 500;
            }

            try
            {
                context.Response.Close();
            }
            catch
            {
            }
        }
    }
}
