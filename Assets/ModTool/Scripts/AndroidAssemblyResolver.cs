using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using UnityEngine.Networking;
using ModTool.Shared;
using Mono.Cecil;

namespace ModTool
{
    internal class AndroidAssemblyResolver : AssemblyResolver
    {
        private List<Stream> streams;

        public AndroidAssemblyResolver()
        {
            streams = new List<Stream>();
        }

        protected override AssemblyDefinition GetAssembly(string location, ReaderParameters parameters)
        {
            location = "file://" + location;

            var webRequest = new WebRequest(location);

            while (!webRequest.isDone)
                Thread.Sleep(16);

            var stream = new MemoryStream(webRequest.data);
            streams.Add(stream);

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(stream, parameters);

            return assemblyDefinition;
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var stream in streams)
                stream.Dispose();

            streams.Clear();

            base.Dispose(disposing);
        }

        private class WebRequest
        {
            public bool isDone
            {
                get
                {
                    lock (_lock)
                        return _isDone;
                }
            }

            public byte[] data { get; private set; }

            private string uri;

            private bool _isDone;

            private object _lock = new object();

            public WebRequest(string uri)
            {
                this.uri = uri;

                Dispatcher.Enqueue(() => Dispatcher.StartCoroutine(ProcessRequest()));
            }

            IEnumerator ProcessRequest()
            {
                using (var webRequest = UnityWebRequest.Get(uri))
                {
                    yield return webRequest.SendWebRequest();

                    if (webRequest.isHttpError || webRequest.isNetworkError)
                        LogUtility.LogError(webRequest.error);

                    using (var downloadHandler = webRequest.downloadHandler)
                        data = downloadHandler.data;
                }

                lock (_lock)
                    _isDone = true;
            }
        }
    }
}
