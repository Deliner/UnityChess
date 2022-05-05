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
    /// <summary>
    /// Stream frames from a webcam over Internet or LAN.
    /// </summary>
    public class IPWebcamStreamer : MonoBehaviour
    {
        [SerializeField] private UnityCam cam;

        private HttpFrameServer frameServer = new HttpFrameServer();
        private bool isStreaming = false;

        private void StartServer()
        {
            frameServer.Start(2378, false);
            Application.runInBackground = true;
            StartStreaming();
        }


        private void StartStreaming()
        {
            if (!isStreaming && frameServer.Listening)
            {
                isStreaming = true;
                StartCoroutine(StreamVideo());
            }
        }

        private void StopServer()
        {
            if (frameServer.Listening)
            {
                frameServer.Stop();
            }

            isStreaming = false;
        }

        private IEnumerator StreamVideo()
        {
            while (isStreaming)
            {
                if (frameServer.Listening)
                {
                    var tex = cam.GetJPG();
                    var arr = tex.EncodeToJPG(50);
                    if (arr != null)
                        frameServer.UpdateImageBuffer(arr);
                }

                yield return null;
            }

            if (frameServer.Listening)
            {
                frameServer.UpdateImageBuffer(null);
            }

            print("End of video streaming.");
        }

        private void Start()
        {
            StartServer();
        }

        private void OnDisable()
        {
            Quit();
        }

        private void OnApplicationQuit()
        {
            Quit();
        }

        private void OnDestroy()
        {
            StopServer();
        }

        private void Quit()
        {
            StopServer();
        }
    }
}