using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace SimpleAspNetWebHost
{
    /// <summary>
    /// HttpListenerを利用してASP.NETをホストするクラスです。
    /// </summary>
    public class HttpListenerHost : MarshalByRefObject
    {
        private String _prefix;
        private HttpListener _httpListener;

        public HttpListenerHost()
        {
        }

        /// <summary>
        /// HttpListenerで待ち受けを開始します。
        /// </summary>
        /// <param name="prefix">HttpListenerに登録するプレフィックス。</param>
        public void Start(String prefix)
        {
            if (_httpListener != null && _httpListener.IsListening)
                throw new InvalidOperationException("HttpListenerはすでに開始されています。");

            _prefix = prefix;

            // HTTPのLISTENを開始
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(_prefix);
            _httpListener.Start();

            // リクエストを受け付けるのを待つ
#if USE_APM
            _httpListener.BeginGetContext(OnRequestReceived, null);
#else
            WaitRequest();
#endif
        }

        /// <summary>
        /// HttpListenerの待ち受けを停止します。
        /// </summary>
        public void Stop()
        {
            if (_httpListener != null)
            {
                _httpListener.Abort();
                //_httpListener.Stop();
                _httpListener = null;
            }
        }

        /// <summary>
        /// ホストそのもののアプリケーションドメインを取得します。
        /// </summary>
        /// <returns></returns>
        public AppDomain AppDomain
        {
            get { return AppDomain.CurrentDomain; }
        }

        /// <summary>
        /// HttpListenerがリクエストを待ち受けます。
        /// </summary>
        private async void WaitRequest()
        {
            try
            {
                while (_httpListener != null && _httpListener.IsListening)
                {
                    // リクエストを受け付ける
                    var ctx = await _httpListener.GetContextAsync();
                    Console.WriteLine("{0} | {1} | {2}", DateTime.Now.ToString("u"), ctx.Request.RemoteEndPoint, ctx.Request.Url);
                    // ASP.NET のランタイムパイプラインに投げる
                    var workerRequest = new HttpListenerWorkerRequest(ctx);
                    HttpRuntime.ProcessRequest(workerRequest);
                }
            }
            catch (ObjectDisposedException)
            {
                // 待ち受けてるのがキャンセルされるとここに来る
            }
        }

#if USE_APM
        /// <summary>
        /// HttpListenerがリクエストを受け取った時のコールバックメソッドです。
        /// </summary>
        /// <param name="ar"></param>
        private void OnRequestReceived(IAsyncResult ar)
        {
            if (_httpListener == null) return;

            var ctx = _httpListener.EndGetContext(ar);
            Console.WriteLine("{0} | {1} | {2}", DateTime.Now.ToString("u"), ctx.Request.RemoteEndPoint, ctx.Request.Url);

            if (_httpListener.IsListening)
                _httpListener.BeginGetContext(OnRequestReceived, null);

            // ASP.NET のランタイムパイプラインに投げる
            var workerRequest = new HttpListenerWorkerRequest(ctx);
            HttpRuntime.ProcessRequest(workerRequest);
        }
#endif
    }
}
