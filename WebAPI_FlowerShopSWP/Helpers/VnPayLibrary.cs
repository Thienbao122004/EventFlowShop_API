using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace WebAPI_FlowerShopSWP.Helpers
{
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
        private readonly SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());


        public SortedList<string, string> RequestData => _requestData;

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public IReadOnlyDictionary<string, string> GetRequestData()
        {
            return _requestData;
        }


        public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
        {
            var data = new StringBuilder();
            foreach (var kv in _requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value) + "&");
                }
            }

            string rawData = data.ToString().TrimEnd('&');
            string vnp_SecureHash = HmacSHA512(vnp_HashSecret, rawData);

            // Logging for debugging
            Console.WriteLine("Raw Data: " + rawData);
            Console.WriteLine("Secure Hash: " + vnp_SecureHash);

            return baseUrl + "?" + rawData + "&vnp_SecureHash=" + vnp_SecureHash;
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }


        public string GetResponseData(string key)
        {
            _responseData.TryGetValue(key, out string value);
            return value;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            var data = new StringBuilder();
            foreach (var kv in _responseData)
            {
                if (!string.IsNullOrEmpty(kv.Value) && kv.Key != "vnp_SecureHash")
                {
                    data.Append(Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value) + "&");
                }
            }

            string rawData = data.ToString().TrimEnd('&');
            string myChecksum = HmacSHA512(secretKey, rawData);

            // Logging for debugging
            Console.WriteLine("Response Raw Data: " + rawData);
            Console.WriteLine("Calculated Checksum: " + myChecksum);
            Console.WriteLine("Input Hash: " + inputHash);

            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

         static string HmacSHA512(string key, string inputData)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var inputBytes = Encoding.UTF8.GetBytes(inputData);
                var hashBytes = hmac.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return string.CompareOrdinal(x, y);
        }
    }
}
