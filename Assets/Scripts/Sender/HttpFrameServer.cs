using System.Collections;
using System;
using System.Collections.Generic;
using System.Text;
#if UNITY_WSA_10_0 && WINDOWS_UWP
using System.Net.Http;
#else
using System.Net;
#endif
using System.Net.Sockets;
using System.IO;
using System.Threading;
using UnityEngine;


namespace VidStreamComp
{
    public class HttpFrameServer
    {
        private HttpListener httpListener = null;
        private HttpListenerContext context = null;
        private Thread serverThread = null;
        private readonly object bufferLock = new object();
        private byte[] imageBuffer = null;
        private bool singleFrame = false;
        private bool isListening = false;

        public int ServerPort { get; private set; }

        public bool Listening => isListening;
        public long counter;

        public void Start(int port, bool singleFrameMode)
        {
            if (serverThread == null || serverThread.ThreadState != ThreadState.Running)
            {
                singleFrame = singleFrameMode;
                StartListening(port);
            }
        }

        public void UpdateImageBuffer(byte[] buffer)
        {
            lock (bufferLock)
            {
                imageBuffer = buffer;
            }
        }

        public void Stop()
        {
            if (isListening && httpListener != null)
            {
                UpdateImageBuffer(null);
                TerminateListener();
                MonoBehaviour.print("Frame server stopped.");
            }
        }

        private void Listen()
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://192.168.1.72:2378/");
            httpListener.Start();
            MonoBehaviour.print("Listening...");
            try
            {
                while (httpListener != null && httpListener.IsListening)
                {
                    context = httpListener.GetContext();
                    MonoBehaviour.print("Request from " + context.Request.Url);
                    try
                    {
                        Process(context);
                    }
                    catch (IOException e)
                    {
                        MonoBehaviour.print(e.Message);
                        httpListener.Stop();
                        httpListener.Start();
                    }
                }
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e);
            }
            finally
            {
                TerminateListener();
            }
        }

        private void TerminateListener()
        {
            if (httpListener != null)
            {
                if (context != null)
                {
                    context.Response.OutputStream.Close();
                    context.Response.Close();
                }

                httpListener.Close();
                httpListener = null;
                isListening = false;
                MonoBehaviour.print("Stopped listening.");
            }
        }

        private void WriteToStream(Stream stream, string msg)
        {
            byte[] msgBytes = Encoding.UTF8.GetBytes(msg);
            stream.Write(msgBytes, 0, msgBytes.Length);
        }

        private void Process(HttpListenerContext context)
        {
            byte[] localBuffer = null;

            // wait until a frame is available
            do
            {
                if (!isListening)
                {
                    // quit if the server has been stopped
                    return;
                }

                Thread.Sleep(0);
                lock (bufferLock)
                {
                    localBuffer = imageBuffer;
                }
            } while (localBuffer == null);

            try
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = "multipart/x-mixed-replace; boundary=unity_gpvudk";
                do
                {
                    if (!CheckTime())
                    {
                        Thread.Sleep(0);
                        continue;
                    }

                    var flag = false;
                    lock (bufferLock)
                    {
                        if (localBuffer != imageBuffer && imageBuffer != null)
                        {
                            flag = true;
                            localBuffer = imageBuffer;
                        }
                    }

                    if (flag)
                    {
                        WriteToStream(context.Response.OutputStream, "--unity_gpvudk\r\n");
                        WriteToStream(context.Response.OutputStream, "Content-Type: image/jpeg\r\n");
                        int nbytes = localBuffer.Length;
                        WriteToStream(context.Response.OutputStream, "Content-Length: " + nbytes + "\r\n\r\n");
                        context.Response.OutputStream.Write(localBuffer, 0, nbytes);
                        WriteToStream(context.Response.OutputStream, "\r\n");
                        context.Response.OutputStream.Flush();
                    }

                    Thread.Sleep(0);
                } while (localBuffer != null);
            }
            finally
            {
                context.Response.OutputStream.Close();
                if (!singleFrame)
                {
                    MonoBehaviour.print("Streaming stopped.");
                }
            }
        }

        private bool CheckTime()
        {
            var time = GetTime();
            if (time - counter > 100)
            {
                counter = time;
                return true;
            }

            return false;
        }

        private long GetTime()
        {
            DateTime unixEpoch = new DateTime(1970, 1, 1);

            DateTime currentTime = DateTime.UtcNow;
            TimeSpan elapsedTime = unixEpoch.Subtract(currentTime);

            return (long)elapsedTime.TotalMilliseconds * -1; // <--- unix timstamp in milliseconds
        }

        private void StartListening(int port)
        {
            ServerPort = port;
            serverThread = new Thread(Listen);
            serverThread.Start();
            isListening = true;
            MonoBehaviour.print("Frame server started.");
        }
    }
}