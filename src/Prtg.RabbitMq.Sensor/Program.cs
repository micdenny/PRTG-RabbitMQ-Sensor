using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Prtg.RabbitMq.Sensor
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new Application();
            app.Run(args).Wait();
        }
    }

    public class Application
    {
        public async Task Run(string[] args)
        {
            var requestParams = new RequestParams
            {
                ServerAndPort = args[0],
                User = args[1],
                Password = args[2],
                Type = args[3],
                Host = args.Length >= 5 ? args[4] : null,
                Name = args.Length >= 6 ? args[5] : null
            };

            PrtgResponse response = await Read(requestParams);

            Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            }));
        }

        public async Task<PrtgResponse> Read(RequestParams request)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri("http://" + request.ServerAndPort + "/api/")
            };

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(request.User + ":" + request.Password)));
            string url = request.Type;
            if (request.Host != null)
            {
                url += "/" + request.Host;
            }
            if (request.Name != null)
            {
                url += "/" + request.Name;
            }
            using (var result = await client.GetAsync(url))
            {
                result.EnsureSuccessStatusCode();
                var serializer = new JsonSerializer();
                using (var s = await result.Content.ReadAsStreamAsync())
                {
                    var reader = new JsonTextReader(new StreamReader(s));
                    var data = serializer.Deserialize(reader);

                    if (request.Type.Equals("overview", StringComparison.OrdinalIgnoreCase))
                    {
                        return ReadOverview(data);
                    }
                    else if (request.Type.Equals("queues", StringComparison.OrdinalIgnoreCase))
                    {
                        return ReadQueue(data);
                    }

                    throw new ArgumentOutOfRangeException("request", request.Type + " is an unsupported object type");
                }
            }
        }

        public PrtgResponse ReadOverview(dynamic data)
        {
            var response = new PrtgResponse();

            // object_totals

            response.result.Add(new PrtgResult
            {
                channel = "Consumers",
                unit = PrtgUnit.Count,
                value = data.object_totals.consumers
            });

            response.result.Add(new PrtgResult
            {
                channel = "Queues",
                unit = PrtgUnit.Count,
                value = data.object_totals.queues
            });

            response.result.Add(new PrtgResult
            {
                channel = "Exchanges",
                unit = PrtgUnit.Count,
                value = data.object_totals.exchanges
            });

            response.result.Add(new PrtgResult
            {
                channel = "Connections",
                unit = PrtgUnit.Count,
                value = data.object_totals.connections
            });

            response.result.Add(new PrtgResult
            {
                channel = "Channels",
                unit = PrtgUnit.Count,
                value = data.object_totals.channels
            });

            // queue_totals

            response.result.Add(new PrtgResult
            {
                channel = "Total",
                unit = PrtgUnit.Count,
                value = data.queue_totals.messages
            });

            response.result.Add(new PrtgResult
            {
                channel = "Ready",
                unit = PrtgUnit.Count,
                value = data.queue_totals.messages_ready
            });

            response.result.Add(new PrtgResult
            {
                channel = "Unacked",
                unit = PrtgUnit.Count,
                value = data.queue_totals.messages_unacknowledged
            });

            // message_stats

            response.result.Add(new PrtgResult
            {
                channel = "Publish Rate",
                unit = PrtgUnit.Custom,
                Float = 1,
                value = ReadDecimal(() => data.message_stats.publish_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "Deliver Rate",
                unit = PrtgUnit.Custom,
                Float = 1,
                value = ReadDecimal(() => data.message_stats.deliver_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "Ack Rate",
                unit = PrtgUnit.Custom,
                Float = 1,
                value = ReadDecimal(() => data.message_stats.ack_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "Confirm Rate",
                unit = PrtgUnit.Custom,
                Float = 1,
                value = ReadDecimal(() => data.message_stats.confirm_details.rate)
            });

            return response;
        }

        public static PrtgResponse ReadQueue(dynamic data)
        {
            var response = new PrtgResponse();
            
            response.result.Add(new PrtgResult
            {
                channel = "Total",
                unit = PrtgUnit.Count,
                value = data.messages
            });

            response.result.Add(new PrtgResult
            {
                channel = "Ready",
                unit = PrtgUnit.Count,
                value = data.messages_ready
            });

            response.result.Add(new PrtgResult
            {
                channel = "Unacked",
                unit = PrtgUnit.Count,
                value = data.messages_unacknowledged
            });

            response.result.Add(new PrtgResult
            {
                channel = "Rate",
                unit = PrtgUnit.Custom,
                Float = 1,
                value = ReadDecimal(() => data.messages_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "Publish Rate",
                unit = PrtgUnit.Custom,
                Float = 1,
                value = ReadDecimal(() => data.message_stats.publish_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "Consumers",
                unit = PrtgUnit.Count,
                value = data.consumers
            });

            response.result.Add(new PrtgResult
            {
                channel = "Memory",
                unit = PrtgUnit.BytesMemory,
                value = data.memory
            });

            response.result.Add(new PrtgResult
            {
                channel = "Ack Rate",
                unit = PrtgUnit.Custom,
                Float = 1,
                value = ReadDecimal(() => data.message_stats.ack_details.rate)
            });

            return response;
        }

        private static string ReadDecimal(Func<decimal> func)
        {
            try
            {
                return func().ToString(CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return "0";
            }
        }
    }

    public class RequestParams
    {
        public string ServerAndPort { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Type { get; set; }
        public string Host { get; set; }
        public string Name { get; set; }
    }
}
