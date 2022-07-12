using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Newtonsoft.Json;

namespace CalendarSyncCommons
{
    public class ServerInterface : IDisposable
    {
        private readonly string Url;
        private readonly string OwnerName;
        private readonly string PassphraseHash;

        private readonly HttpClient Client;

        public ServerInterface(string url, string ownerName, string passphrase, bool hashOwnerName = false,
            string customCAPath = null, string customCertificatePath = null)
        {
            this.Url = url.TrimEnd('/') + "/calendar";
            this.OwnerName = hashOwnerName ? Utils.HashString(ownerName) : ownerName;
            this.PassphraseHash = Utils.HashString(passphrase);

            if (customCAPath != null)
            {
                var handler = new HttpClientHandler();

                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                handler.ServerCertificateCustomValidationCallback = (message, certificate2, chain, _) =>
                {
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                    chain.ChainPolicy.CustomTrustStore.Add(new X509Certificate2(customCAPath));
                    if (customCertificatePath != null)
                    {
                        chain.ChainPolicy.ExtraStore.Add(new X509Certificate2(customCertificatePath));
                    }

                    return chain.Build(certificate2);
                };

                this.Client = new HttpClient(handler, true);
            }
            else
            {
                this.Client = new HttpClient();
            }
        }

        private string GetQueryUrl(string action, params (string key, object value)[] queryParameters)
        {
            StringBuilder sb = new StringBuilder(this.Url);
            sb.Append("/");
            sb.Append(action);

            if (queryParameters.Length > 0)
            {
                bool passedFirst = false;

                sb.Append("?");
                foreach (var (key, value) in queryParameters)
                {
                    if (passedFirst)
                    {
                        sb.Append("&");
                    }
                    else
                    {
                        passedFirst = true;
                    }

                    sb.Append(Uri.EscapeDataString(key));
                    sb.Append("=");
                    sb.Append(Uri.EscapeDataString(value.ToString()));
                }
            }

            return sb.ToString();
        }

        public bool Authenticate()
        {
            string url = this.GetQueryUrl("Authenticate",
                ("ownerName", this.OwnerName),
                ("passphraseHash", this.PassphraseHash)
            );
            var response = this.Client.GetAsync(url).Result;

            return response.IsSuccessStatusCode;
        }

        public bool AuthenticateOrCreate()
        {
            string url = this.GetQueryUrl("ReserveName",
                ("ownerName", this.OwnerName),
                ("passphraseHash", this.PassphraseHash)
            );
            var response = this.Client.PostAsync(url, null).Result;

            return response.IsSuccessStatusCode;
        }

        public bool UploadPartialSnapshot(DateTime from, DateTime to, Stream content)
        {
            string url = this.GetQueryUrl("AddPartialSnapshot",
                ("ownerName", this.OwnerName),
                ("passphraseHash", this.PassphraseHash),
                ("modifiedInterval_Start", from.ToUniversalTime().ToString("O")),
                ("modifiedInterval_End", to.ToUniversalTime().ToString("O"))
            );
            var sc = new StreamContent(content);
            sc.Headers.Add("Content-Type", "application/octet-stream");
            var response = this.Client.PostAsync(url, sc).Result;

            return response.IsSuccessStatusCode;
        }

        public bool UploadFullSnapshot(Stream content)
        {
            string url = this.GetQueryUrl("AddFullSnapshot",
                ("ownerName", this.OwnerName),
                ("passphraseHash", this.PassphraseHash)
            );
            var sc = new StreamContent(content);
            sc.Headers.Add("Content-Type", "application/octet-stream");
            var response = this.Client.PostAsync(url, sc).Result;

            return response.IsSuccessStatusCode;
        }

        public List<ServerModels.AvailableSnapshot> GetAvailableSnapshots()
        {
            string url = this.GetQueryUrl("GetAvaliableSnapshots",
                ("ownerName", this.OwnerName),
                ("passphraseHash", this.PassphraseHash)
            );
            var response = this.Client.GetAsync(url).Result;

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return JsonConvert.DeserializeObject<List<ServerModels.AvailableSnapshot>>(
                response.Content.ReadAsStringAsync().Result);
        }

        public Stream GetSnapshotData(int id)
        {
            string url = this.GetQueryUrl("GetSnapshotData",
                ("ownerName", this.OwnerName),
                ("passphraseHash", this.PassphraseHash),
                ("id", id)
            );
            var response = this.Client.GetAsync(url).Result;

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var ms = new MemoryStream();
            response.Content.CopyToAsync(ms).Wait();

            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        public void Dispose()
        {
            Client?.Dispose();
        }
    }
}
