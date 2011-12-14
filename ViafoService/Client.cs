using Microsoft.Phone.Info;
using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using RestSharp;
using System.Threading;
using ViafoService.Models;
using System.Text;
using Microsoft.Phone.Tasks;


namespace ViafoService
{
    public class Client
    {
        public static readonly string VIAFO_RETURN_URL = "http://example.com";

        private static readonly string VIAFO_SERVER = "https://vsg-live.appspot.com/client/1/";

        private static readonly int ANIDLength = 32;
        private static readonly int ANIDOffset = 2;

        private RestClient client = new RestClient(VIAFO_SERVER);
        private int failureCount = 0;

        public bool RefreshServices { get; set; }

        public string AccessToken { get; set; }

        public ServicesResponse Services { get; set; }


        public event Action<string> AuthenticationRequired;
        public event Action<ServicesResponse> ServicesReceived;
        public event Action<string> AccessTokenReceived;

        public Client(string id, string secret)
        {
            System.Diagnostics.Debug.WriteLine("Client created");
            this.clientId = id;
            this.clientSecret = secret;
        }

        // NOTE: to get a result requires ID_CAP_IDENTITY_USER  
        // to be added to the capabilities of the WMAppManifest  
        // this will then warn users in marketplace  
        public static string GetWindowsLiveAnonymousID()
        {
            string result = string.Empty;
            object anid;

            if (UserExtendedProperties.TryGetValue("ANID", out anid))
            {
                if (anid != null && anid.ToString().Length >= (ANIDLength + ANIDOffset))
                {
                    result = anid.ToString().Substring(ANIDOffset, ANIDLength);
                }
            }

            if (result == string.Empty)
            {
                object uniqueId;
                if (DeviceExtendedProperties.TryGetValue("DeviceUniqueId", out uniqueId))
                {
                    byte[] buf = uniqueId as byte[];
                    StringBuilder hex = new StringBuilder(buf.Length * 2);

                    for (int i = 0; i < buf.Length; i++)     // <-- use for loop is faster than foreach   
                        hex.Append(buf[i].ToString("X2"));   // <-- ToString is faster than AppendFormat   

                    result = hex.ToString();
                }
            }

            return result;
        }

        public void Refresh()
        {
            if (string.IsNullOrEmpty(this.AccessToken))
            {
                this.CallRegister(this.clientId, this.clientSecret,
                    (resp) =>
                    {
                        System.Diagnostics.Debug.WriteLine("Success: " + resp.AccessToken);
                        this.AccessToken = resp.AccessToken;

                        if (this.AccessTokenReceived != null)
                        {
                            this.AccessTokenReceived(this.AccessToken);
                        }

                        this.CallGetServices(
                            (res) =>
                            {
                                this.Services = res;
                                this.RefreshServices = false;

                                if (this.ServicesReceived != null)
                                {
                                    this.ServicesReceived(res);
                                }
                            },
                            (err) =>
                            {
                                System.Diagnostics.Debug.WriteLine("Failure: " + err);
                            });
                    },
                    (err) =>
                    {
                        System.Diagnostics.Debug.WriteLine("Failure: " + err);
                    });
            }
            else if (this.RefreshServices || this.Services == null)
            {
                this.CallGetServices(
                    (res) =>
                    {
                        this.Services = res;
                        this.RefreshServices = false;

                        if (this.ServicesReceived != null)
                        {
                            this.ServicesReceived(res);
                        }
                    },
                    (err) =>
                    {
                        System.Diagnostics.Debug.WriteLine("Failure: " + err);
                    });
            }
        }


        public void CallRegister(string clientId, string clientSecret, Action<RegisterResponse> successCallback, Action<string> failureCallback)
        {
            var request = new RestRequest("register.json", Method.POST);

            request.AddParameter("client_id", clientId);
            request.AddParameter("client_secret", clientSecret);
            request.AddParameter("uuid", GetWindowsLiveAnonymousID());
            request.AddParameter("os", "Windows Phone 7 firm:" + DeviceStatus.DeviceFirmwareVersion + " hard:" + DeviceStatus.DeviceHardwareVersion);
            request.AddParameter("make", DeviceStatus.DeviceName);
            request.AddParameter("manufacturer", DeviceStatus.DeviceManufacturer);
            request.AddParameter("language", Thread.CurrentThread.CurrentUICulture.Name);
            request.AddParameter("screen_x", Application.Current.Host.Content.ActualWidth.ToString());
            request.AddParameter("screen_y", Application.Current.Host.Content.ActualHeight.ToString());

            this.client.ExecuteAsync<RegisterResponse>(request,
                (response) =>
                {
                    bool tryAgain = true;

                    if (response.ResponseStatus == ResponseStatus.Completed &&
                        response.StatusCode == HttpStatusCode.OK)
                    {
                        tryAgain = false;
                        this.failureCount = 0;
                        successCallback(response.Data);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Failure: " + response.ResponseStatus + " " + response.StatusCode);
                        if (response.Data != null && response.Data.Code != 0 && response.Data.Message != null)
                        {
                            System.Diagnostics.Debug.WriteLine("... " + response.Data.Code + " " + response.Data.Message);
                        }
                        this.failureCount++;
                    }

                    if (tryAgain)
                    {
                        if (this.failureCount < 3)
                        {
                            this.CallRegister(clientId, clientSecret, successCallback, failureCallback);
                        }
                        else
                        {
                            if (response.Data != null)
                            {
                                failureCallback(response.Data.Code + " " + response.Data.Message);
                            }
                            else
                            {
                                failureCallback("Connection error");
                            }
                        }
                    }
                });
        }

        public void CallGetServices(Action<ServicesResponse> successCallback, Action<string> failureCallback)
        {
            var request = new RestRequest("get_services.json", Method.POST);

            request.AddParameter("verbs", "share");
            request.AddParameter("access_token", this.AccessToken);
            request.AddParameter("return_url", VIAFO_RETURN_URL);

            this.client.ExecuteAsync<ServicesResponse>(request,
            (response) =>
            {
                bool tryAgain = true;

                if (response.ResponseStatus == ResponseStatus.Completed)
                {
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            tryAgain = false;
                            this.failureCount = 0;
                            //System.Diagnostics.Debug.WriteLine(response.Content);
                            successCallback(response.Data);
                            break;

                        case HttpStatusCode.Unauthorized:
                            this.failureCount = 0;
                            this.AccessToken = null;
                            this.Refresh();
                            failureCallback("Unauthorized");
                            tryAgain = false;
                            break;

                        default:
                            this.failureCount++;
                            break;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failure: " + response.ResponseStatus + " " + response.StatusCode);
                    this.failureCount++;
                }

                if (tryAgain)
                {
                    if (this.failureCount < 3)
                    {
                        this.CallGetServices(successCallback, failureCallback);
                    }
                    else
                    {
                        failureCallback("Failure: " + response.ResponseStatus + " " + response.StatusCode);
                    }
                }
            });
        }

        public void CallShare(Share share, Action successCallback, Action failureCallback)
        {
            var request = new RestRequest("{service}/{verb}.json", Method.POST);

            request.AddUrlSegment("service", share.Service);
            request.AddUrlSegment("verb", "share");

            request.AddParameter("access_token", this.AccessToken);
            request.AddParameter("text", share.Text);
            request.AddParameter("link", share.Link);
            if (!string.IsNullOrWhiteSpace(share.Id))
            {
                request.AddParameter("id", share.Id);
            }

            this.client.ExecuteAsync(request,
                (response) =>
                {
                    if (response.ResponseStatus == ResponseStatus.Completed &&
                        response.StatusCode == HttpStatusCode.OK)
                    {
                        System.Diagnostics.Debug.WriteLine(response.Content);
                        successCallback();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Failure: " + response.ResponseStatus);
                        failureCallback();
                    }
                });
        }

        public void CallRetweet(Service service, string id, Action successCallback, Action failureCallback)
        {
            var request = new RestRequest("{service}/{verb}.json", Method.POST);

            request.AddUrlSegment("service", service.Name);
            request.AddUrlSegment("verb", "retweet");

            request.AddParameter("access_token", this.AccessToken);
            request.AddParameter("id", id);

            this.client.ExecuteAsync(request,
                (response) =>
                {
                    if (response.ResponseStatus == ResponseStatus.Completed &&
                        response.StatusCode == HttpStatusCode.OK)
                    {
                        System.Diagnostics.Debug.WriteLine(response.Content);
                        successCallback();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Failure: " + response.ResponseStatus);
                        failureCallback();
                    }
                });
        }

        public void CallFollow(Service service, string id, Action successCallback, Action failureCallback)
        {
            var request = new RestRequest("{service}/{verb}.json", Method.POST);

            request.AddUrlSegment("service", service.Name);
            request.AddUrlSegment("verb", "follow");

            request.AddParameter("access_token", this.AccessToken);
            request.AddParameter("id", id);

            this.client.ExecuteAsync(request,
                (response) =>
                {
                    if (response.ResponseStatus == ResponseStatus.Completed &&
                        response.StatusCode == HttpStatusCode.OK)
                    {
                        System.Diagnostics.Debug.WriteLine(response.Content);
                        successCallback();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Failure: " + response.ResponseStatus);
                        failureCallback();
                    }
                });
        }

        /*
        request.AddParameter("name", "value"); // adds to POST or URL querystring based on Method
        //request.AddUrlSegment("id", 123); // replaces matching token in request.Resource

        // add parameters for all properties on an object
        request.AddObject(object);

        // or just whitelisted properties
        //request.AddObject(object, "PersonId", "Name", ...);

        // easily add HTTP Headers
        request.AddHeader("header", "value");

        // add files to upload (works with compatible verbs)
        //request.AddFile(path);

        // execute the request
        RestResponse response = client.Execute(request);
        var content = response.Content; // raw content as string

        // or automatically deserialize result
        // return content type is sniffed but can be explicitly set via RestClient.AddHandler();
        RestResponse<Person> response2 = client.Execute<Person>(request);
        var name = response2.Data.Name;

        // or download and save file to disk
        client.DownloadData(request).SaveAs(path);

        // easy async support
        client.ExecuteAsync(request, response => {
            Console.WriteLine(response.Content);
        });

        // async with deserialization
        var asyncHandle = client.ExecuteAsync<Person>(request, response => {
            Console.WriteLine(response.Data.Name);
        });
        */

        private Service GetService(string serviceName, Action retryAction)
        {
            Service retVal = null;

            System.Diagnostics.Debug.WriteLine("Get Service: {0} refresh: {1}", serviceName, this.RefreshServices);

            if (this.RefreshServices)
            {
                this.CallGetServices((res) =>
                {
                    this.Services = res;
                    this.RefreshServices = false;

                    if (this.ServicesReceived != null)
                    {
                        this.ServicesReceived(res);
                    }

                    retryAction();
                },
                (err) =>
                { });

                return retVal;
            }

            if (this.Services == null || this.Services.Services == null)
            {
                //MessageBox.Show("Please wait", "Viafo", MessageBoxButton.OK);
                return retVal;
            }

            foreach (var service in this.Services.Services)
            {
                if (service.Name == serviceName)
                {
                    retVal = service;
                    break;
                }
            }

            return retVal;
        }


        public void AttemptShare(Share share)
        {
            Service service = this.GetService(share.Service, () => { this.AttemptShare(share); });

            if (service != null)
            {
                if (!service.Authenticated)
                {
                    share.CallSilentFail();
                    this.Authenticate(service, () => { });
                }
                else
                {
                    this.CallShare(
                        share,
                        () =>
                        {
                            share.CallSuccess();
                        },
                        () =>
                        {
                            share.CallFailure();
                        });
                }
            }
        }

        public void AttemptRetweet(string id, Action success, Action<bool> failure)
        {
            Service service = this.GetService("twitter", () => { this.AttemptRetweet(id, success, failure); });

            if (service != null)
            {
                if (!service.Authenticated)
                {
                    failure(false);
                    this.Authenticate(service, () => { });
                }
                else
                {
                    this.CallRetweet(
                        service,
                        id,
                        () =>
                        {
                            success();
                        },
                        () =>
                        {
                            failure(true);
                        });
                }
            }
        }

        public void AttemptFollow(string id, Action success, Action<bool> failure)
        {
            Service service = this.GetService("twitter", () => { this.AttemptFollow(id, success, failure); });

            if (service != null)
            {
                if (!service.Authenticated)
                {
                    failure(false);
                    this.Authenticate(service, () => { });
                }
                else
                {
                    this.CallFollow(
                        service,
                        id,
                        () =>
                        {
                            success();
                        },
                        () =>
                        {
                            failure(true);
                        });
                }
            }
        }

        public void Authenticate(string serviceName, Action onComplete)
        {
            Service service = this.GetService(serviceName, () => { this.Authenticate(serviceName, onComplete); });
            if (service != null)
            {
                Authenticate(service, onComplete);
            }
        }

        public void Authenticate(Service service, Action onComplete)
        {
            // Show authentication
            var res = MessageBox.Show("You need to sign in to use this service.", service.DisplayName, MessageBoxButton.OK);

            if (res == MessageBoxResult.OK)
            {
                this.RefreshServices = true;

                if (this.AuthenticationRequired != null)
                {
                    this.AuthenticationRequired(service.Url);
                }
                else
                {
                    var webBrowserTask = new WebBrowserTask
                    {
                        Uri = new Uri(service.Url)
                    };

                    webBrowserTask.Show();
                }
            }
        }

        public void Deauth(string serviceName, Action onComplete)
        {
            Service service = this.GetService(serviceName, () => { this.Deauth(serviceName, onComplete); });
            if (service != null)
            {
                Deauth(service, onComplete);
            }
        }

        public void Deauth(Service service, Action onComplete)
        {
            // Show authentication
            MessageBox.Show("This will disable this service.", service.DisplayName, MessageBoxButton.OK);

            this.RefreshServices = true;

            var client = new RestClient(service.Url);
            client.ExecuteAsync(new RestRequest(), (resp) => { onComplete(); });
        }

        private string clientId = string.Empty;
        private string clientSecret = string.Empty;
    }
}
