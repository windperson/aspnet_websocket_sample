using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace XamarinFormSample
{
    public partial class MainPage : ContentPage
    {
        private HubConnection _connection;

        private CancellationTokenSource _streamingCancellationTokenSource;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void ConnectHubButton_OnClicked(object sender, EventArgs e)
        {
            ConnectHubButton.IsEnabled = false;
            var urlStr = UrlEntry.Text.Trim();
            if (string.IsNullOrEmpty(urlStr))
            {
                await DisplayAlert("no url", "server url is empty", "OK");
                ResetUIStatus();
                return;
            }

            try
            {
                if (_connection != null)
                {
                    ConnectStatusLabel.Text = "disconnecting...";
                    await _connection.StopAsync();
                    ConnectStatusLabel.Text = "disconnected";
                }
                else
                {
                    //Note: HubConnection cannot be reused
                    _connection = CreateHubConnection(urlStr);
                }
                ConnectStatusLabel.Text = "connecting...";
                await _connection.StartAsync();
                SetDemoUIStatus();
            }
            catch (Exception ex)
            {
                await DisplayAlert("connect fail", $"ex=\n{ex}", "OK");
                ResetUIStatus();
            }
        }

        private async void CallEchoButton_OnClicked(object sender, EventArgs e)
        {
            var inputStr = EchoInputEntry.Text.Trim();
            if (string.IsNullOrEmpty(inputStr))
            {
                await DisplayAlert("no input", "no data in Echo() input field", "OK");
                return;
            }

            try
            {
                var result = await _connection.InvokeAsync<string>("EchoWithJsonFormat", inputStr);
                await DisplayAlert("call Echo()", $"result=\n{result}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("call Echo() fail", $"ex=\n{ex}", "OK");
            }
        }

        private async void CallReverseButton_OnClicked(object sender, EventArgs e)
        {
            const string streamingBtnTxt = "Cancel Streaming...";
            const string defaultBtnTxt = "Call Stream Reverse()";

            if (CallReverseButton.Text == streamingBtnTxt && _streamingCancellationTokenSource != null)
            {

                _streamingCancellationTokenSource.Cancel();
                CallReverseButton.Text = defaultBtnTxt;
                return;
            }

            var inputStr = ReverseInputEntry.Text.Trim();
            if (string.IsNullOrEmpty(inputStr))
            {
                await DisplayAlert("no input", "no data in Reverse() input field", "OK");
                return;
            }

            ReverseLabel.Text = string.Empty;
            CallReverseButton.Text = streamingBtnTxt;

            _streamingCancellationTokenSource = new CancellationTokenSource();
            try
            {
                var channelReader = await _connection.StreamAsChannelAsync<char>("Reverse", inputStr,
                    _streamingCancellationTokenSource.Token);
                while (!_streamingCancellationTokenSource.IsCancellationRequested &&
                       await channelReader.WaitToReadAsync(_streamingCancellationTokenSource.Token))
                {
                    while (channelReader.TryRead(out char streamResult))
                    {
                        ReverseLabel.Text += $"{streamResult}\n";
                    }
                }
            }
            catch (OperationCanceledException)
            {
                await DisplayAlert("cancel operation", "Streaming Reverse() has cancelled.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("call Reverse() fail", $"ex=\n{ex}", "OK");
            }
            finally
            {
                CallReverseButton.Text = defaultBtnTxt;
            }
        }

        #region UI Status
        private void ResetUIStatus()
        {
            ConnectStatusLabel.Text = "disconnected";
            ConnectHubButton.IsEnabled = true;
            CallEchoButton.IsEnabled = false;
            CallReverseButton.IsEnabled = false;
        }

        private void SetDemoUIStatus()
        {
            ConnectStatusLabel.Text = "connected";
            ConnectHubButton.IsEnabled = true;

            EchoInputEntry.Text = string.Empty;
            CallEchoButton.IsEnabled = true;

            ReverseInputEntry.Text = string.Empty;
            ReverseLabel.Text = string.Empty;
            CallReverseButton.IsEnabled = true;

            ServerSendLabel.Text = string.Empty;
        }
        #endregion

        #region ASP.NET Core SignalR related
        private HubConnection CreateHubConnection(string url)
        {
            HubConnection conn =
                new HubConnectionBuilder()
                .WithUrl(url)
                .ConfigureLogging(loggingBuilder =>
                {
#if DEBUG
                    loggingBuilder.SetMinimumLevel(LogLevel.Debug);
#else
                    loggingBuilder.SetMinimumLevel(LogLevel.Information);
#endif
                    if (DeviceInfo.Platform != DeviceInfo.Platforms.UWP)
                    {
                        loggingBuilder.AddConsole(options =>
                        {
                            options.DisableColors = true;
                        });
                    }
                    else
                    {
                        loggingBuilder.AddDebug();
                    }
                })
                .Build();

            conn.On<string>("OnBroadcast", recv =>
            {
                Device.BeginInvokeOnMainThread(async () => await DoThingsOnServerCall(recv));
            });

            return conn;
        }

        private async Task DoThingsOnServerCall(string recv)
        {
            await DisplayAlert("server call client", $"recv={recv}", "OK");
            ServerSendLabel.Text += $"{recv}\n";
        }

        #endregion
    }
}
