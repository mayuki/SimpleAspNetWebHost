using System;
using System.IO;
using System.Net;
using System.Web;
using Microsoft.Win32.SafeHandles;

namespace SimpleAspNetWebHost
{
    /// <summary>
    /// HttpListenerContextを利用するHttpWorkerRequestクラスです。
    /// </summary>
    public class HttpListenerWorkerRequest : HttpWorkerRequest
    {
        private HttpListenerContext _ctx;

        public HttpListenerWorkerRequest(HttpListenerContext ctx)
        {
            _ctx = ctx;

        }

        public override string GetUriPath()
        {
            return _ctx.Request.Url.AbsolutePath;
        }

        public override string GetQueryString()
        {
            return _ctx.Request.Url.Query.TrimStart('?');
        }

        public override string GetRawUrl()
        {
            return _ctx.Request.RawUrl;
        }

        public override string GetHttpVerbName()
        {
            return _ctx.Request.HttpMethod;
        }

        public override string GetHttpVersion()
        {
            return String.Format("HTTP/{0}.{1}", _ctx.Request.ProtocolVersion.Major, _ctx.Request.ProtocolVersion.Minor);
        }

        public override string GetKnownRequestHeader(int index)
        {
            var name = GetKnownRequestHeaderName(index);
            return _ctx.Request.Headers[name];
        }

        public override string GetRemoteAddress()
        {
            return _ctx.Request.RemoteEndPoint.Address.ToString();
        }

        public override int GetRemotePort()
        {
            return _ctx.Request.RemoteEndPoint.Port;
        }

        public override string GetLocalAddress()
        {
            return _ctx.Request.LocalEndPoint.Address.ToString();
        }

        public override int GetLocalPort()
        {
            return _ctx.Request.LocalEndPoint.Port;
        }

        public override void SendStatus(int statusCode, string statusDescription)
        {
            _ctx.Response.StatusCode = statusCode;
            _ctx.Response.StatusDescription = statusDescription;
        }

        public override void SendKnownResponseHeader(int index, string value)
        {
            var name = GetKnownResponseHeaderName(index);
            SendUnknownResponseHeader(name, value);
        }

        public override void SendUnknownResponseHeader(string name, string value)
        {
            _ctx.Response.Headers[name] = value;
        }

        public override void SendResponseFromMemory(byte[] data, int length)
        {
            _ctx.Response.OutputStream.Write(data, 0, length);
        }

        public override void SendResponseFromFile(string filename, long offset, long length)
        {
            // FIXME: Int64からInt32にしてたり、一度全部読み込んだりと手抜き
            try
            {
                using (var fileStream = new FileStream(filename, FileMode.Open))
                {
                    var data = new byte[length];
                    fileStream.Read(data, (Int32)offset, (Int32)length);
                    _ctx.Response.OutputStream.Write(data, 0, (Int32)length);
                }
            }
            catch (Exception)
            {
            }
        }

        public override void SendResponseFromFile(IntPtr handle, long offset, long length)
        {
            // FIXME: Int64からInt32にしてたり、一度全部読み込んだりと手抜き
            using (var safeHandle = new SafeFileHandle(handle, false))
            using (var fileStream = new FileStream(safeHandle, FileAccess.Read))
            {
                var data = new byte[length];
                fileStream.Read(data, (Int32)offset, (Int32)length);
                _ctx.Response.OutputStream.Write(data, 0, (Int32)length);
            }
        }

        public override void FlushResponse(bool finalFlush)
        {
            _ctx.Response.OutputStream.Flush();
        }

        public override void EndOfRequest()
        {
            _ctx.Response.Close();
        }
    }
}
