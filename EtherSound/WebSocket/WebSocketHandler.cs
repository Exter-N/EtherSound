using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtherSound.WebSocket
{
    abstract class WebSocketHandler : IDisposable
    {
        readonly HttpListenerWebSocketContext ctx;
        readonly CancellationTokenSource cancel;
        readonly Dictionary<string, RemoteMethod> methods;
        readonly Dictionary<string, AsyncRemoteMethod> asyncMethods;
        Action<(ArraySegment<byte> buffer, WebSocketMessageType messageType)> enqueueSend;

        protected HttpListenerWebSocketContext Context => ctx;
        protected System.Net.WebSockets.WebSocket WebSocket => ctx.WebSocket;

        public WebSocketHandler(HttpListenerWebSocketContext ctx)
        {
            this.ctx = ctx;
            cancel = new CancellationTokenSource();
            methods = new Dictionary<string, RemoteMethod>();
            asyncMethods = new Dictionary<string, AsyncRemoteMethod>();
            enqueueSend = null;
            MainLoop();
        }

        protected void RegisterMethod(string name, RemoteMethod method)
        {
            if (asyncMethods.ContainsKey(name))
            {
                throw new ArgumentException();
            }

            methods.Add(name, method);
        }

        protected void RegisterMethod(string name, AsyncRemoteMethod method)
        {
            if (methods.ContainsKey(name))
            {
                throw new ArgumentException();
            }

            asyncMethods.Add(name, method);
        }

        void OnMessage(JToken message)
        {
            JObject request;
            try
            {
                request = (JObject)message;
            }
            catch
            {
                SendMessage(new JObject
                {
                    ["error"] = CreateError(RemoteException.InvalidRequest, "Invalid Request"),
                });

                return;
            }

            JToken result;
            bool binary;
            try
            {
                string method;
                try
                {
                    method = (string)request["method"];
                }
                catch (InvalidCastException)
                {
                    method = null;
                }
                if (null == method)
                {
                    throw new RemoteException(RemoteException.InvalidRequest, "Invalid Request");
                }

                if (methods.TryGetValue(method, out RemoteMethod impl))
                {
                    (result, binary) = impl(request["params"]);
                }
                else if (asyncMethods.TryGetValue(method, out AsyncRemoteMethod asyncImpl))
                {
                    OnMessageAsync(request, asyncImpl, request["params"]);

                    return;
                }
                else
                {
                    throw new RemoteException(RemoteException.MethodNotFound, "Method not found");
                }
            }
            catch (RemoteException e)
            {
                SendMessage(new JObject
                {
                    ["id"] = request["id"],
                    ["error"] = CreateError(e),
                });

                return;
            }
            catch
            {
                SendMessage(new JObject
                {
                    ["id"] = request["id"],
                    ["error"] = CreateError(RemoteException.InternalError, "Internal error"),
                });

                return;
            }

            SendMessage(new JObject
            {
                ["id"] = request["id"],
                ["result"] = result,
            }, binary);
        }

        async void OnMessageAsync(JObject request, AsyncRemoteMethod asyncImpl, JToken @params)
        {
            JToken result;
            bool binary;
            try
            {
                (result, binary) = await asyncImpl(@params);
            }
            catch (RemoteException e)
            {
                SendMessage(new JObject
                {
                    ["id"] = request["id"],
                    ["error"] = CreateError(e),
                });

                return;
            }
            catch
            {
                SendMessage(new JObject
                {
                    ["id"] = request["id"],
                    ["error"] = CreateError(RemoteException.InternalError, "Internal error"),
                });

                return;
            }

            SendMessage(new JObject
            {
                ["id"] = request["id"],
                ["result"] = result,
            }, binary);
        }

        async void MainLoop()
        {
            using (MemoryStream messageBuffer = new MemoryStream())
            {
                try
                {
                    byte[] buffer = new byte[Environment.SystemPageSize];
                    for (; ; )
                    {
                        WebSocketReceiveResult result = await ctx.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancel.Token);
                        messageBuffer.Write(buffer, 0, result.Count);
                        if (result.EndOfMessage)
                        {
                            messageBuffer.Seek(0L, SeekOrigin.Begin);
                            JToken message = null;
                            try
                            {
                                switch (result.MessageType)
                                {
                                    case WebSocketMessageType.Text:
                                        using (StreamReader sReader = new StreamReader(messageBuffer, Encoding.UTF8, false, Environment.SystemPageSize, true))
                                        {
                                            using (JsonTextReader reader = new JsonTextReader(sReader))
                                            {
                                                message = JToken.ReadFrom(reader);
                                            }
                                        }
                                        break;
                                    case WebSocketMessageType.Binary:
                                        using (BinaryReader bReader = new BinaryReader(messageBuffer, Encoding.UTF8, true))
                                        {
                                            using (BsonReader reader = new BsonReader(bReader))
                                            {
                                                message = JToken.ReadFrom(reader);
                                            }
                                        }
                                        break;
                                    case WebSocketMessageType.Close:
                                        await ctx.WebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancel.Token);
                                        Program.Queue(Dispose);
                                        return;
                                }
                            }
                            catch (JsonReaderException)
                            {
                                SendMessage(new JObject
                                {
                                    ["error"] = CreateError(RemoteException.ParseError, "Parse Error"),
                                });
                            }
                            messageBuffer.Seek(0L, SeekOrigin.Begin);
                            messageBuffer.SetLength(0L);
                            if (null != message)
                            {
                                Program.Queue(OnMessage, message);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (WebSocketException)
                {
                    Dispose();
                    return;
                }
            }
        }

        JObject CreateError(int code, string message)
        {
            return new JObject
            {
                ["code"] = code,
                ["message"] = message,
            };
        }

        JObject CreateError(RemoteException e)
        {
            return CreateError(e.Code, e.Message);
        }

        protected void Notify(string method, JToken @params, bool binary = false)
        {
            SendMessage(new JObject
            {
                ["method"] = method,
                ["params"] = @params,
            }, binary);
        }

        void SendMessage(JToken message, bool binary = false)
        {
            if (binary)
            {
                byte[] binaryMessage;
                using (MemoryStream buffer = new MemoryStream())
                {
                    using (BsonWriter writer = new BsonWriter(buffer))
                    {
                        message.WriteTo(writer);
                    }
                    binaryMessage = buffer.ToArray();
                }
                SendMessage(binaryMessage);
            }
            else
            {
                string textMessage;
                using (StringWriter buffer = new StringWriter())
                {
                    using (JsonTextWriter writer = new JsonTextWriter(buffer))
                    {
                        message.WriteTo(writer);
                    }
                    textMessage = buffer.ToString();
                }
                SendMessage(textMessage);
            }
        }

        void SendMessage(string message)
        {
            SendMessage(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text);
        }

        void SendMessage(byte[] message)
        {
            SendMessage(new ArraySegment<byte>(message), WebSocketMessageType.Binary);
        }

        async void SendMessage(ArraySegment<byte> buffer, WebSocketMessageType messageType)
        {
            Queue<(ArraySegment<byte> buffer, WebSocketMessageType messageType)> queue;
            Action<(ArraySegment<byte> buffer, WebSocketMessageType messageType)> enqueue;
            lock (this)
            {
                if (null != enqueueSend)
                {
                    enqueueSend((buffer, messageType));

                    return;
                }
                else
                {
                    queue = new Queue<(ArraySegment<byte> buffer, WebSocketMessageType messageType)>();
                    queue.Enqueue((buffer, messageType));
                    enqueue = queue.Enqueue;
                    enqueueSend = enqueue;
                }
            }
            for(; ; )
            {
                lock (this)
                {
                    if (queue.Count == 0)
                    {
                        if (enqueueSend == enqueue)
                        {
                            enqueueSend = null;
                        }

                        return;
                    }
                    (buffer, messageType) = queue.Dequeue();
                }
                try
                {
                    await ctx.WebSocket.SendAsync(buffer, messageType, true, cancel.Token);
                }
                catch (WebSocketException)
                {
                    Dispose();
                    return;
                }
            }
        }

        public void Dispose()
        {
            lock (this)
            {
                enqueueSend = delegate { };
            }
            cancel.Cancel();
            OnDispose();
            ctx.WebSocket.Dispose();
        }
        
        protected virtual void OnDispose()
        {
        }

        protected delegate (JToken result, bool binary) RemoteMethod(JToken @params);
        protected delegate Task<(JToken result, bool binary)> AsyncRemoteMethod(JToken @params);

        [Serializable]
        protected class RemoteException : Exception
        {
            public const int ParseError = -32700;
            public const int InvalidRequest = -32600;
            public const int MethodNotFound = -32601;
            public const int InvalidParams = -32602;
            public const int InternalError = -32603;
            public const int ServerErrorMax = -32000;
            public const int ServerErrorMin = -32099;

            readonly int code;

            public int Code => code;

            public RemoteException(int code, string message) : base(message)
            {
                this.code = code;
            }

            public RemoteException(int code, string message, Exception inner) : base(message, inner)
            {
                this.code = code;
            }

            protected RemoteException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
                code = info.GetInt32("Code");
            }

            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
                info.AddValue("Code", code);
            }
        }
    }
}
