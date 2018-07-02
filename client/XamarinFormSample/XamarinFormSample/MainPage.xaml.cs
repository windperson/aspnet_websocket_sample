using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace XamarinFormSample
{
    public partial class MainPage : ContentPage
    {
        private HubConnection _connection;

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
                    await _connection.StopAsync();
                }
                else
                {
                    _connection = CreateHubConnection(urlStr);
                }

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
            var inputStr = ReverseInputEntry.Text.Trim();
            if (string.IsNullOrEmpty(inputStr))
            {
                await DisplayAlert("no input", "no data in Reverse() input field", "OK");
                return;
            }

            try
            {
                var channel = await _connection.StreamAsChannelAsync<char>("Reverse", inputStr);

                while (await channel.WaitToReadAsync())
                {
                    while (channel.TryRead(out char streamResult))
                    {
                        ReverseLabel.Text += $"{streamResult}\n";
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("call Reverse() fail", $"ex=\n{ex}", "OK");
            }
        }

        private HubConnection CreateHubConnection(string url)
        {
            var conn = (new HubConnectionBuilder()).WithUrl(url).Build();

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
    }
}
