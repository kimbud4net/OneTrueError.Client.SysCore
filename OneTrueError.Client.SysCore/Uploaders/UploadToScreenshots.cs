using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using OneTrueError.Client.Contracts;
using OneTrueError.Client.Converters;
using OneTrueError.Client.Uploaders;
using System.Diagnostics;
using OneTrueError.Client.SysCore.Contracts;

namespace OneTrueError.Client.SysCore.Uploaders
{
    public class UploadToScreenshots : IDisposable
    {
        private readonly string _apiKey;
        private readonly Uri _reportUri;
        private readonly string _sharedSecret;
        private readonly Func<bool> _queueReportsAccessor = () => OneTrue.Configuration.QueueReports;
        private UploadQueue<UploadToScreenshotsDTO> _reportQueue;


        public UploadToScreenshots(Uri oneTrueHost, string apiKey, string sharedSecret)
        {
            if (string.IsNullOrEmpty(apiKey)) throw new ArgumentNullException("apiKey");
            if (string.IsNullOrEmpty(sharedSecret)) throw new ArgumentNullException("sharedSecret");

            if (oneTrueHost.AbsolutePath.Contains("/receiver/"))
                throw new ArgumentException(
                    "The OneTrueError URI should not contain the reporting area '/receiver/', but should point at the site root.");
            if (!oneTrueHost.AbsolutePath.EndsWith("/"))
                oneTrueHost = new Uri(oneTrueHost + "/");

            _reportUri = new Uri(oneTrueHost, "syscoreapi/screenshots/" + apiKey + "/");
            _apiKey = apiKey;
            _sharedSecret = sharedSecret;
            _reportQueue = new UploadQueue<UploadToScreenshotsDTO>(TryUploadReportNow);
            _reportQueue.UploadFailed += OnUploadFailed;
        }

        public UploadToScreenshots(Uri oneTrueHost, string apiKey, string sharedSecret, Func<bool> queueReportsAccessor)
            : this(oneTrueHost, apiKey, sharedSecret)
        {
            if (queueReportsAccessor == null) throw new ArgumentNullException("queueReportsAccessor");
            _queueReportsAccessor = queueReportsAccessor;
        }

   

   

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        ///     Upload the report to the web service.
        /// </summary>
        /// <param name="report">CreateReport to submit</param>
        public void UploadReport(UploadToScreenshotsDTO report)
        {
            if (report == null) throw new ArgumentNullException("report");

            if (!NetworkInterface.GetIsNetworkAvailable() || _queueReportsAccessor())
                _reportQueue.Add(report);
            else
                TryUploadReportNow(report);
        }

        /// <summary>
        ///     Failed to deliver DTO within the given parameters.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public event EventHandler<UploadReportFailedEventArgs> UploadFailed;


        /// <summary>
        ///     Try to upload a report directly
        /// </summary>
        /// <param name="report">Report to upload</param>
        /// <exception cref="WebException">No internet connection is available; Destination server did not accept the report.</exception>
        public void TryUploadReportNow(UploadToScreenshotsDTO report)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                throw new WebException("Not connected, try again later.", WebExceptionStatus.ConnectFailure);

            var buffer = CompressErrorReport(report);
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString(2);
            var hashAlgo = new HMACSHA256(Encoding.UTF8.GetBytes(_sharedSecret));
            var hash = hashAlgo.ComputeHash(buffer);
            var signature = Convert.ToBase64String(hash);

            try
            {
                ExecuteRetryer retryer = new ExecuteRetryer(20, 300);
                retryer.Execute(() =>
                {
                    var uri = _reportUri + "?sig=" + signature + "&v=" + version;
                    var request = (HttpWebRequest)WebRequest.Create(uri);
                    AddProxyIfRequired(request, uri);
                    request.Method = "POST";
                    request.ContentType = "application/octet-stream";
                    var evt = request.BeginGetRequestStream(null, null);
                    var stream = request.EndGetRequestStream(evt);
                    stream.Write(buffer, 0, buffer.Length);
                    var responseRes = request.BeginGetResponse(null, null);
                    var response = (HttpWebResponse)request.EndGetResponse(responseRes);
                    using (response)
                    {
                        //Console.WriteLine(response);
                    }
                });
            }
            catch (Exception err)
            {

                AnalyzeException(err);
                throw new WebException(
                    "The actual upload failed (probably network error). We'll try again later..", err);

            }
        }


        /// <summary>
        ///     Dispose pattern
        /// </summary>
        /// <param name="isDisposing">Invoked from the dispose method.</param>
        protected virtual void Dispose(bool isDisposing)
        {

            if (_reportQueue != null)
            {
                _reportQueue.Dispose();
                _reportQueue = null;
            }
        }

        /// <summary>
        ///     Compress an ErrorReport as JSON string
        /// </summary>
        /// <param name="errorReport">ErrorReport</param>
        /// <returns>Compressed JSON representation of the ErrorReport.</returns>
        internal byte[] CompressErrorReport(UploadToScreenshotsDTO errorReport)
        {
            var reportJson = JsonConvert.SerializeObject(errorReport, Formatting.None,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None,
                    //ContractResolver =
                    //    new IncludeNonPublicMembersContractResolver()
                });
            var buffer = Encoding.UTF8.GetBytes(reportJson);

            //collected by GZipStream
            var outMs = new MemoryStream();
            using (var zipStream = new GZipStream(outMs, CompressionMode.Compress))
            {
                zipStream.Write(buffer, 0, buffer.Length);

                //MUST close the stream, flush doesn't help and without close
                // the memory stream won't get its bytes
                zipStream.Close();

                var result = outMs.ToArray();
                return result;
            }
        }


        private static void AddProxyIfRequired(HttpWebRequest request, string uri)
        {
            var proxy = request.Proxy;
            if ((proxy != null) && !proxy.IsBypassed(new Uri(uri)))
            {
                var proxyuri = proxy.GetProxy(request.RequestUri).ToString();
                request.UseDefaultCredentials = true;
                request.Proxy = new WebProxy(proxyuri, false);
                request.Proxy.Credentials = CredentialCache.DefaultCredentials;
            }
        }

        private static void AnalyzeException(Exception err)
        {
            var exception = err as WebException;
            if (exception == null)
                return;

            if (exception.Response == null)
                return;

            var title = "Failed to execute";
            var description = "Did not get a response. Check your network connection.";

            var resp = (HttpWebResponse)exception.Response;
            var stream = exception.Response.GetResponseStream();
            if (stream != null)
            {
                var reader = new StreamReader(stream);
                description = reader.ReadToEnd();
                title = resp.StatusDescription;
            }
            try
            {
                exception.Response.Close();
            }
            catch
            {
                // ignored
            }

            switch (resp.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    throw new UnauthorizedAccessException(title + "\r\n" + description, err);
                case HttpStatusCode.NotFound:
                    //legacy handling of 404. Misconfigured web servers will report 404,
                    //so remove the usage to avoid ambiguity
                    if (title.IndexOf("key", StringComparison.OrdinalIgnoreCase) != -1)
                        throw new InvalidApplicationKeyException(title + "\r\n" + description, err);
                    throw new InvalidOperationException("Server returned an error.\r\n" + description, err);
                default:
                    if ((resp.StatusCode == HttpStatusCode.BadRequest) && title.Contains("APP_KEY"))
                        throw new InvalidApplicationKeyException(title + "\r\n" + description, err);

                    throw new InvalidOperationException("Server returned an error.\r\n" + description, err);
            }
        }

        private void OnUploadFailed(object sender, UploadReportFailedEventArgs args)
        {
            if (UploadFailed != null)
                UploadFailed(this, args);
        }
    }

}
