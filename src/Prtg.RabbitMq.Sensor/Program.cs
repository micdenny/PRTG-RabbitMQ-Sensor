using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using System.Collections;

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

            var response = new
            {
                prtg = await Read(requestParams)
            };

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

            if (request.Type.Equals("overview", StringComparison.OrdinalIgnoreCase))
            {
                dynamic overviewData;
                using (var result = await client.GetAsync("overview"))
                {
                    result.EnsureSuccessStatusCode();
                    var serializer = new JsonSerializer();
                    using (var s = await result.Content.ReadAsStreamAsync())
                    {
                        var reader = new JsonTextReader(new StreamReader(s));
                        overviewData = serializer.Deserialize(reader);
                    }
                }
                PrtgResponse overview = ReadOverview(overviewData);

                dynamic nodesData;
                using (var result = await client.GetAsync("nodes"))
                {
                    result.EnsureSuccessStatusCode();
                    var serializer = new JsonSerializer();
                    using (var s = await result.Content.ReadAsStreamAsync())
                    {
                        var reader = new JsonTextReader(new StreamReader(s));
                        nodesData = serializer.Deserialize(reader);
                    }
                }
                string nodeName = overviewData.node;
                PrtgResponse nodes = ReadNode(nodesData, nodeName);
                
                return new PrtgResponse
                {
                    result = overview.result.Concat(nodes.result).ToList()
                };
            }
            else if (request.Type.Equals("queues", StringComparison.OrdinalIgnoreCase))
            {
                string url = request.Type;
                if (request.Host != null)
                {
                    url += "/" + request.Host;
                }
                if (request.Name != null)
                {
                    url += "/" + request.Name;
                }
                object data;
                using (var result = await client.GetAsync(url))
                {
                    result.EnsureSuccessStatusCode();
                    var serializer = new JsonSerializer();
                    using (var s = await result.Content.ReadAsStreamAsync())
                    {
                        var reader = new JsonTextReader(new StreamReader(s));
                        data = serializer.Deserialize(reader);
                    }
                }
                return ReadQueue(data);
            }

            throw new ArgumentOutOfRangeException("request", request.Type + " is an unsupported object type");
        }

        public PrtgResponse ReadNode(dynamic data, string nodeName)
        {
            var response = new PrtgResponse();

            var nodes = data as IEnumerable;
            if (nodes == null)
            {
                return new PrtgResponse();
            }

            dynamic node = nodes.Cast<dynamic>().FirstOrDefault(x => x.name == nodeName);
            if (node == null)
            {
                return new PrtgResponse();
            }

            response.result.Add(new PrtgResult
            {
                channel = "Used Memory",
                unit = PrtgUnit.BytesMemory,
                value = node.mem_used
            });

            response.result.Add(new PrtgResult
            {
                channel = "Disk Free",
                unit = PrtgUnit.BytesDisk,
                value = node.disk_free
            });

            response.result.Add(new PrtgResult
            {
                channel = "Used File Descriptors",
                unit = PrtgUnit.Count,
                value = node.fd_used
            });

            response.result.Add(new PrtgResult
            {
                channel = "Used Socket Descriptors",
                unit = PrtgUnit.Count,
                value = node.sockets_used
            });

            response.result.Add(new PrtgResult
            {
                channel = "Used Erlang Processes",
                unit = PrtgUnit.Count,
                value = node.proc_used
            });

            response.result.Add(new PrtgResult
            {
                channel = "Memory Alarm",
                unit = PrtgUnit.Count,
                value = (bool)node.mem_alarm ? "1" : "0"
            });

            response.result.Add(new PrtgResult
            {
                channel = "Disk Free Alarm",
                unit = PrtgUnit.Count,
                value = (bool)node.disk_free_alarm ? "1" : "0"
            });

            response.result.Add(new PrtgResult
            {
                channel = "Used Memory per Second",
                unit = PrtgUnit.BytesMemory,
                value = ReadDecimal(() => node.mem_used_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "GC per Second",
                unit = PrtgUnit.Count,
                value = ReadDecimal(() =>node.gc_num_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "GC Bytes Reclaimed per Second",
                unit = PrtgUnit.BytesMemory,
                value = ReadDecimal(() =>node.gc_bytes_reclaimed_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "I/O Reads per Second",
                unit = PrtgUnit.Count,
                value = ReadDecimal(() =>node.io_read_count_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "I/O Writes per Second",
                unit = PrtgUnit.Count,
                value = ReadDecimal(() =>node.io_write_count_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "I/O Syncs per Second",
                unit = PrtgUnit.Count,
                value = ReadDecimal(() =>node.io_sync_count_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "I/O Seeks per Second",
                unit = PrtgUnit.Count,
                value = ReadDecimal(() =>node.io_seek_count_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "Reopened File Handle",
                unit = PrtgUnit.Count,
                value = node.io_reopen_count
            });

            response.result.Add(new PrtgResult
            {
                channel = "Reopened File Handle per Second",
                unit = PrtgUnit.Count,
                value = ReadDecimal(() =>node.io_reopen_count_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "Opened File Handle per Second",
                unit = PrtgUnit.Count,
                value = ReadDecimal(() =>node.io_file_handle_open_attempt_count_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "I/O Read Time (ms)",
                unit = PrtgUnit.Count,
                value = node.io_read_avg_time
            });

            response.result.Add(new PrtgResult
            {
                channel = "I/O Write Time (ms)",
                unit = PrtgUnit.Count,
                value = node.io_write_avg_time
            });

            response.result.Add(new PrtgResult
            {
                channel = "I/O Sync Time (ms)",
                unit = PrtgUnit.Count,
                value = node.io_sync_avg_time
            });

            response.result.Add(new PrtgResult
            {
                channel = "I/O Seek Time (ms)",
                unit = PrtgUnit.Count,
                value = node.io_seek_avg_time
            });

            response.result.Add(new PrtgResult
            {
                channel = "File Handle Open Time (ms)",
                unit = PrtgUnit.Count,
                value = node.io_file_handle_open_attempt_avg_time
            });

            return response;
        }

        public PrtgResponse ReadOverview(dynamic data)
        {
            var response = new PrtgResponse();
            
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
                channel = "Publish per Second",
                unit = PrtgUnit.Custom,
                Float = 1,
                value = ReadDecimal(() => data.message_stats.publish_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "Deliver per Second",
                unit = PrtgUnit.Custom,
                Float = 1,
                value = ReadDecimal(() => data.message_stats.deliver_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "Ack per Second",
                unit = PrtgUnit.Custom,
                Float = 1,
                value = ReadDecimal(() => data.message_stats.ack_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "Confirm per Second",
                unit = PrtgUnit.Custom,
                Float = 1,
                value = ReadDecimal(() => data.message_stats.confirm_details.rate)
            });

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
                channel = "Messages per Second",
                unit = PrtgUnit.Custom,
                Float = 1,
                value = ReadDecimal(() => data.messages_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "Publish per Second",
                unit = PrtgUnit.Custom,
                Float = 1,
                value = ReadDecimal(() => data.message_stats.publish_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "Ack per Second",
                unit = PrtgUnit.Custom,
                Float = 1,
                value = ReadDecimal(() => data.message_stats.ack_details.rate)
            });

            response.result.Add(new PrtgResult
            {
                channel = "Consumers",
                unit = PrtgUnit.Count,
                value = data.consumers
            });

            response.result.Add(new PrtgResult
            {
                channel = "Used Memory",
                unit = PrtgUnit.BytesMemory,
                value = data.memory
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
