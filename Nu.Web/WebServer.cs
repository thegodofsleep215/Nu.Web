using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;

namespace Nu.Web
{
    public class WebServer
    {

        #region Fields
        private TcpListener listener;

               private bool running;

        private Thread listenThread;
        #endregion

        #region Properties
        public virtual string Name { get { return "nsWebServer/1.0.*"; } }

        public bool IsAlive
        {
            get
            {
                return listenThread.IsAlive;
            }
        }

        #endregion

        public static bool ExcessiveLogging { get; set; }

        #region Constructors

        protected WebServer() : this(80)
        {
        }

        protected WebServer(int listeningPort) : this(listeningPort, false)
        {
            listener = new TcpListener(IPAddress.Any, listeningPort);
        }
     
        protected WebServer(int listeningPort, bool excessiveLogging)
        {
            
            Configure(listeningPort, excessiveLogging);
        }

        #endregion
    
        public void Listen()
        {
            listener.Start();


            while (running)
            {
                var client = listener.AcceptTcpClient();
                var thread = new Thread(HandleRequest) {Name = "HTTP Request"};
                thread.Start(client);
            }

        }

        public void Configure(int listeningPort, bool excessiveLogging)
        {
            if (running)
            {
                return;
            }
            ExcessiveLogging = excessiveLogging;
            listener = new TcpListener(IPAddress.Any, listeningPort);
        }

        public void Start()
        {
            running = true;
            listenThread = new Thread(Listen) {Name = "WebServer.Listen"};
            listenThread.Start();
        }

        public void Stop()
        {
            running = false;
            if(!listenThread.Join(1000))
            {
                listenThread.Abort();
            }
            listener.Stop();
        }

        public virtual void OnResponse(ref HttpRequest httpWebRequest, ref HttpResponse httpWebResponse)
        {
        }

        private void HandleRequest(object objClient)
        {
            var client = (TcpClient) objClient;

            //TODO: 
           // Logger.Info("Connection accepted. Buffer: {0}", client.ReceiveBufferSize);

            NetworkStream ns = client.GetStream();

            try
            {
                bool syntaxError;
                var httpRequest = WebHelper.GetRequest(ns, client.ReceiveBufferSize, out syntaxError);

                var httpResponse = new HttpResponse();
                httpResponse.Version = "HTTP/1.1";

                if (syntaxError)
                {
                    httpResponse.Status = (int)RespState.BadRequest;
                }
                else
                {
                    httpResponse.Status = (int) RespState.Ok;
                }

                httpResponse.Headers = new Dictionary<string, string>
                                           {
                                               {"Server", Name},
                                               {"Date", DateTime.Now.ToString("r")}
                                           };

                ProcessHttpRequest(ref httpRequest, ref httpResponse);

                httpResponse.Headers.Add("Content-Length", httpResponse.BodySize.ToString(CultureInfo.InvariantCulture));
                string headersString = httpResponse.Version + " " + WebHelper.ResponseCodes[httpResponse.Status] + "\n";

                headersString = httpResponse.Headers.Aggregate(headersString, (current, header) => current + (header.Key + ": " + header.Value + "\n"));

                headersString += "\n";
                byte[] bHeadersString = Encoding.ASCII.GetBytes(headersString);

                // Send headers	
                ns.Write(bHeadersString, 0, bHeadersString.Length);
                // Send body
                if (httpResponse.BodyData != null)
                {
                    ns.Write(httpResponse.BodyData, 0, httpResponse.BodyData.Length);
                }

            }
            catch (Exception e)
            {
                // TODO: Deal with trace logging
            }
            finally
            {
                ns.Close();
                client.Close();
            }
        }

        private void ProcessHttpRequest(ref HttpRequest httpRequest1, ref HttpResponse httpResponse1)
        {
            // Check to serve files.
            // Check for mvc request and return jsonnne.
        }

    }

    public static class WebHelper
    {
        public static HttpRequest GetRequest(NetworkStream ns, int receiveBufferSize, out bool syntaxError)
        {
            var parserState = RState.Method;
            var readBuffer = new byte[receiveBufferSize];
            var httpRequest = new HttpRequest();
            string message = "";
            string hValue = "";
            string hKey = "";
            // binary data buffer index
            int bfndx = 0;

            // Incoming message may be larger than the buffer size.
            do
            {
                int numberOfBytesRead = ns.Read(readBuffer, 0, readBuffer.Length);
                message = String.Concat(message, Encoding.ASCII.GetString(readBuffer, 0, numberOfBytesRead));

                // read buffer index
                int ndx = 0;
                do
                {
                    switch (parserState)
                    {
                        case RState.Method:
                            if (readBuffer[ndx] != ' ')
                                httpRequest.Method += (char) readBuffer[ndx++];
                            else
                            {
                                ndx++;
                                parserState = RState.Url;
                            }
                            break;
                        case RState.Url:
                            if (readBuffer[ndx] == '?')
                            {
                                ndx++;
                                hKey = "";
                                httpRequest.Execute = true;
                                httpRequest.Args = new Dictionary<string, string>();
                                parserState = RState.Urlparm;
                            }
                            else if (readBuffer[ndx] != ' ')
                                httpRequest.Url += (char) readBuffer[ndx++];
                            else
                            {
                                ndx++;
                                httpRequest.Url = HttpUtility.UrlDecode(httpRequest.Url);
                                parserState = RState.Version;
                            }
                            break;
                        case RState.Urlparm:
                            if (readBuffer[ndx] == '=')
                            {
                                ndx++;
                                hValue = "";
                                parserState = RState.Urlvalue;
                            }
                            else if (readBuffer[ndx] == ' ')
                            {
                                ndx++;

                                httpRequest.Url = HttpUtility.UrlDecode(httpRequest.Url);
                                parserState = RState.Version;
                            }
                            else
                            {
                                hKey += (char) readBuffer[ndx++];
                            }
                            break;
                        case RState.Urlvalue:
                            if (readBuffer[ndx] == '&')
                            {
                                ndx++;
                                hKey = HttpUtility.UrlDecode(hKey);
                                hValue = HttpUtility.UrlDecode(hValue);
                                httpRequest.Args[hKey] = httpRequest.Args.ContainsKey(hKey)
                                                             ? httpRequest.Args[hKey] + ", " + hValue
                                                             : hValue;
                                hKey = "";
                                parserState = RState.Urlparm;
                            }
                            else if (readBuffer[ndx] == ' ')
                            {
                                ndx++;
                                hKey = HttpUtility.UrlDecode(hKey);
                                hValue = HttpUtility.UrlDecode(hValue);
                                httpRequest.Args[hKey] = httpRequest.Args.ContainsKey(hKey)
                                                             ? httpRequest.Args[hKey] + ", " + hValue
                                                             : hValue;

                                httpRequest.Url = HttpUtility.UrlDecode(httpRequest.Url);
                                parserState = RState.Version;
                            }
                            else
                            {
                                hValue += (char) readBuffer[ndx++];
                            }
                            break;
                        case RState.Version:
                            if (readBuffer[ndx] == '\r')
                                ndx++;
                            else if (readBuffer[ndx] != '\n')
                                httpRequest.Version += (char) readBuffer[ndx++];
                            else
                            {
                                ndx++;
                                hKey = "";
                                httpRequest.Headers = new Dictionary<string, string>();
                                parserState = RState.Headerkey;
                            }
                            break;
                        case RState.Headerkey:
                            if (readBuffer[ndx] == '\r')
                                ndx++;
                            else if (readBuffer[ndx] == '\n')
                            {
                                ndx++;
                                if (httpRequest.Headers.ContainsKey("Content-Length"))
                                {
                                    httpRequest.BodySize = Convert.ToInt32(httpRequest.Headers["Content-Length"]);
                                    httpRequest.BodyData = new byte[httpRequest.BodySize];
                                    parserState = RState.Body;
                                }
                                else
                                    parserState = RState.Ok;
                            }
                            else if (readBuffer[ndx] == ':')
                                ndx++;
                            else if (readBuffer[ndx] != ' ')
                                hKey += (char) readBuffer[ndx++];
                            else
                            {
                                ndx++;
                                hValue = "";
                                parserState = RState.Headervalue;
                            }
                            break;
                        case RState.Headervalue:
                            if (readBuffer[ndx] == '\r')
                                ndx++;
                            else if (readBuffer[ndx] != '\n')
                                hValue += (char) readBuffer[ndx++];
                            else
                            {
                                ndx++;
                                httpRequest.Headers.Add(hKey, hValue);
                                hKey = "";
                                parserState = RState.Headerkey;
                            }
                            break;
                        case RState.Body:
                            // Append to request BodyData
                            Array.Copy(readBuffer, ndx, httpRequest.BodyData, bfndx, numberOfBytesRead - ndx);
                            bfndx += numberOfBytesRead - ndx;
                            ndx = numberOfBytesRead;
                            if (httpRequest.BodySize <= bfndx)
                            {
                                parserState = RState.Ok;
                            }
                            break;
                    }
                } while (ndx < numberOfBytesRead);
            } while (ns.DataAvailable);

            if (!string.IsNullOrEmpty(httpRequest.Url))
            {
                if (httpRequest.Url.StartsWith("/"))
                {
                    httpRequest.Url = httpRequest.Url.Remove(0, 1);
                }

            }

            syntaxError = parserState != RState.Ok;
            return httpRequest;
        }
    
        public static Dictionary<int, string> ResponseCodes = new Dictionary<int, string>
        {
            {200, "200 Ok"},
            {201, "201 Created"},
            {202, "202 Accepted"},
            {204, "204 No Content"},

            {301, "301 Moved Permanently"},
            {302, "302 Redirection"},
            {304, "304 Not Modified"},

            {400, "400 Bad Request"},
            {401, "401 Unauthorized"},
            {403, "403 Forbidden"},
            {404, "404 Not Found"},

            {500, "500 Internal Server Error"},
            {501, "501 Not Implemented"},
            {502, "502 Bad Gateway"},
            {503, "503 Service Unavailable"},
        };
    }
}
