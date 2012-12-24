using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Web.Hosting;

namespace SimpleAspNetWebHost
{
    public class Program : MarshalByRefObject
    {
        private HttpListenerHost _applicationHost;
        private String _wwwRootDir;
        private String _prefix;

        static void Main(string[] args)
        {
            new Program().Start();
        }

        private void Start()
        {
            // 現在実行中のアセンブリ
            var currentExecAssemblyPath = Assembly.GetEntryAssembly().Location;

            // HTTPで公開するファイルのドキュメントルートディレクトリ
            _wwwRootDir = Path.Combine(Path.GetDirectoryName(currentExecAssemblyPath), "wwwroot");

            // binディレクトリに自分自身がないとアプリケーションのホストを生成できない(AppDomainが生成されるときにNotFoundになる)のでコピーする。
            var wwwBinDir = Path.Combine(_wwwRootDir, "bin");
            Directory.CreateDirectory(wwwBinDir);
            File.Copy(currentExecAssemblyPath, Path.Combine(wwwBinDir, Path.GetFileName(currentExecAssemblyPath)), true);

            // 一時ポートを探してみる
            // TcpListenerでポートを0にすると未使用のポートを自動で割り当ててくれる。
            var tcpListener = new TcpListener(IPAddress.Loopback, 0); // LoopbackにしておかないとFirewallが怒る
            tcpListener.Start();
            var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();

            // HttpListenerの待ち受け場所
            // http://+:8080/ や http://*:8080/ のようなパターンの場合にはAdministrator権限が必要。
            _prefix = String.Format("http://localhost:{0}/", port);

            Console.WriteLine("Start HTTP Server: {0}", _prefix);

            // HttpListenerを開く
            StartWebHost();

            // ブラウザ開く
            Process.Start(_prefix.Replace("*", "localhost"));

            Console.ReadLine();
        }

        /// <summary>
        /// ホストとHTTPサーバーを起動するメソッドです。
        /// </summary>
        private void StartWebHost()
        {
            // HttpListenerを使うアプリケーションのホストを生成する
            _applicationHost = (HttpListenerHost)ApplicationHost.CreateApplicationHost(typeof(HttpListenerHost), "/", _wwwRootDir);
            _applicationHost.AppDomain.DomainUnload += OnApplicationHostAppDomainUnload;

            // HttpListenerの待ち受け開始
            _applicationHost.Start(_prefix);
        }


        /// <summary>
        /// ホストのアプリケーションドメインがアンロードされるときに呼び出されるイベントです。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnApplicationHostAppDomainUnload(Object sender, EventArgs e)
        {
            // Web.configのようなものがリロードされるとAppDomainがUnloadされるのでホストごと再起動する
            _applicationHost.Stop();
            StartWebHost();
        }
    }
}
