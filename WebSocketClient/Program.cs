using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketClient
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Press enter to connect .................");
            Console.ReadLine();

            using var client = new ClientWebSocket();

            var serviceUri = new Uri("ws://localhost:5000/send");

            var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(120));

            try
            {
                await client.ConnectAsync(serviceUri, cancellationTokenSource.Token);
                while (client.State == WebSocketState.Open)
                {
                    Console.WriteLine("Enter message to send");
                    var message = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(message))
                    {
                        continue;
                    }

                    if (message.TrimEnd().ToLower() == "close")
                    {
                      await  client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed by client",
                            cancellationTokenSource.Token);
                      break;
                    }

                    var byteToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
                    await client.SendAsync(byteToSend, WebSocketMessageType.Text, true, cancellationTokenSource.Token);


                    var responseBuffer = new byte[1024];
                    while (true)
                    {
                        var byteReceived = new ArraySegment<byte>(responseBuffer, 0, 1024);
                        var wsReceivedResult = await client.ReceiveAsync(byteReceived, cancellationTokenSource.Token);

                        if (wsReceivedResult.MessageType == WebSocketMessageType.Close)
                        {
                            var status = wsReceivedResult.CloseStatus.HasValue
                                ? wsReceivedResult.CloseStatus.Value.ToString()
                                : "unknown";

                            Console.WriteLine($"Connection closed: Status: {status} , Description: {wsReceivedResult.CloseStatusDescription}");
                        }

                        var responseMessage = Encoding.UTF8.GetString(responseBuffer, 0, wsReceivedResult.Count);

                        Console.WriteLine(responseMessage);

                        if (wsReceivedResult.EndOfMessage)
                        {
                            break;
                        }
                    }
                }

                if (client.State == WebSocketState.CloseReceived)
                {
                    Console.WriteLine("Closed received from the server");
                }

            }
            catch (WebSocketException e)
            {
                Console.WriteLine(e.Message);
            }

            Console.ReadKey();
        }
    }
}
