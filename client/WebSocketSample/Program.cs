// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modified by Isak Pao, GranDen Corp.

using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.CancelKeyPress += Console_CancelKeyPress;

            RunWebSockets().GetAwaiter().GetResult();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("exiting program, press any key to exit...");
            Console.ReadKey();
            Environment.Exit(0);
        }

        private static async Task RunWebSockets()
        {
            const string serverUrl = "ws://localhost:50732/ws";
            Console.WriteLine($"Press any key to connect to {serverUrl}, press Ctrl + C to exit...");
            Console.ReadKey();

            var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri(serverUrl), CancellationToken.None);

            Console.WriteLine("Underlying WebSocket Connected, press enter to send SignalR HandshakeRequest");
            Console.ReadLine();
            var handshakeRequestStr = @"{""protocol"": ""json"", ""version"" : 1}";
            Console.WriteLine($"sending SignalR HandshakeRequest = {handshakeRequestStr} ...");

            await ws.SendAsync(new ArraySegment<byte>(PaddingJsonRecordSeparator(handshakeRequestStr)), WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None);

            var buffer = new byte[2048];
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                Console.WriteLine($"SingalR HandshakeResponse = {Encoding.UTF8.GetString(buffer, 0, result.Count - 1)} ");
            }

            var sending = Task.Run(async () =>
            {
                Console.WriteLine(
                    $"Press Enter to input \"EchoWithJsonFormat()\",{Environment.NewLine}Press Ctrl+Enter to input \"Reverse()\" stream,{Environment.NewLine}Press Ctrl+C to exit.");

                var keyInfo = Console.ReadKey();
                while (!(keyInfo.Key == ConsoleKey.C && keyInfo.Modifiers == ConsoleModifiers.Control))
                {
                    if (keyInfo.Key == ConsoleKey.Enter)
                    {
                        var modifiers = keyInfo.Modifiers;

                        if (modifiers != ConsoleModifiers.Alt && modifiers != ConsoleModifiers.Control &&
                            modifiers != ConsoleModifiers.Shift)
                        {
                            Console.Write("Input EchoWithJsonFormat():");
                            var line = Console.ReadLine();
                            {
                                if (string.IsNullOrEmpty(line))
                                {
                                    continue;
                                }

                                Console.WriteLine($"send Invocation of EchoWithJsonFormat() with input = {line}");
                                var bytes = CreateServerEchoInvocation(line);
                                await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text,
                                    endOfMessage: true, cancellationToken: CancellationToken.None);
                            }
                        }
                        else if (modifiers == ConsoleModifiers.Control)
                        {
                            Console.Write("Input Reverse():");
                            var line = Console.ReadLine();
                            {
                                if (string.IsNullOrEmpty(line))
                                {
                                    continue;
                                }

                                Console.WriteLine($"send Invocation of Reverse() stream with input = {line}");
                                var bytes = CreateServerReverseStreamInvocation(line);
                                await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text,
                                    endOfMessage: true, cancellationToken: CancellationToken.None);
                            }
                        }
                    }

                    keyInfo = Console.ReadKey();
                }

                await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "program exit", CancellationToken.None);
            });

            var receiving = Receiving(ws);

            await Task.WhenAll(sending, receiving);
        }

        private static byte[] CreateServerReverseStreamInvocation(string line)
        {
            var invocationId = DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss.ff");

            var invocationJsonStr = $"{{\"type\":4,\"invocationId\":\"{invocationId}\",\"target\":\"Reverse\",\"arguments\":[\"{line}\"] }}";

            return PaddingJsonRecordSeparator(invocationJsonStr);
        }

        private static byte[] CreateServerEchoInvocation(string line)
        {
            var invocationId = DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss.ff");

            var invocationJsonStr = $"{{\"type\":1,\"invocationId\":\"{invocationId}\",\"target\":\"EchoWithJsonFormat\",\"arguments\":[\"{line}\"] }}";

            return PaddingJsonRecordSeparator(invocationJsonStr);
        }

        private static byte[] PaddingJsonRecordSeparator(string jsonStr)
        {
            var rawBytes = Encoding.UTF8.GetBytes(jsonStr);
            var paddingBytes = new byte[rawBytes.Length + 1];
            Buffer.BlockCopy(rawBytes, 0, paddingBytes, 0, rawBytes.Length);
            paddingBytes[rawBytes.Length] = 0x1E;
            return paddingBytes;
        }

        private static async Task Receiving(ClientWebSocket ws)
        {
            var buffer = new byte[2048];

            while (true)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    //because we are using json protocol, the last record separate byte 0x1E should not print.
                    var recvStr = Encoding.UTF8.GetString(buffer, 0, result.Count - 1);
                    Console.WriteLine($"receiving = {recvStr}");

                    if (recvStr == @"{""type"":6}")
                    {
                        Console.WriteLine("ping back to server");
                        await ws.SendAsync(new ArraySegment<byte>(PaddingJsonRecordSeparator(recvStr)), WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None);
                    }

                }
                else if (result.MessageType == WebSocketMessageType.Binary) { }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }

            }
        }
    }
}