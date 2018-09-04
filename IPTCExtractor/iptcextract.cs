using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Windows.Media.Imaging;
using System;

namespace IPTCExtractor
{
    public static class iptcextract
    {
        [FunctionName("iptcextract")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("IPTC Extract Triggered");

            try
            {
                // Get request body
                reqMessage reqData = JsonConvert.DeserializeObject<reqMessage>(req.Content.ReadAsAsync<object>().Result.ToString());

                respMessage outputObj = new respMessage();

                if (reqData.values.Count > 0)
                {                
                    outputObj.values = new List<OutputItem>();

                    foreach (var item in reqData.values)
                    {
                        byte[] imageBytes = Convert.FromBase64String(item.data.imagedata);
                        MemoryStream ms = new MemoryStream(imageBytes);

                        JpegBitmapDecoder decoder = new JpegBitmapDecoder(ms, BitmapCreateOptions.None, BitmapCacheOption.None);
                        BitmapMetadata metadata = decoder.Frames[0].Metadata as BitmapMetadata;

                        if (metadata != null)
                        {
                            outputObj.values.Add(new OutputItem
                            {
                                recordId = item.recordId,
                                data = new OutputData
                                {
                                    author = metadata.Author.ToList<string>(),
                                    copyright = metadata.Copyright,
                                    datetaken = metadata.DateTaken,
                                    location = metadata.Location,
                                    keywords = metadata.Keywords.ToList<string>()
                                }
                            });
                        }

                    }
                }

                string output = JsonConvert.SerializeObject(outputObj);

                return req.CreateResponse(HttpStatusCode.OK, output);
            }
            catch (Exception ex)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Processing Error");
            }

        }

        //private static respMessage getSampleData()
        //{

        //    respMessage respMessage = new respMessage();
        //    respMessage.values = new List<OutputItem>();

        //    respMessage.values.Add(new OutputItem
        //    {
        //        recordId = "a1",
        //        data = new OutputData
        //        {
        //            author = "author1",
        //            copyright = "2017",
        //            datetaken = "yyyy-MM-ddTHH:mm:ss.fffZ",
        //            keywords = "data set 1",
        //            location = "New York"
        //        }
        //    });

        //    respMessage.values.Add(new OutputItem
        //    {
        //        recordId = "a2",
        //        data = new OutputData
        //        {
        //            author = "author2",
        //            copyright = "2018",
        //            datetaken = "yyyy-MM-ddTHH:mm:ss.fffZ",
        //            keywords = "data set 2",
        //            location = "Philadelphia"
        //        }
        //    });

        //    return respMessage;
        //}
    }

    class reqMessage
    {
        public List<InputItem> values { get; set; }
    }

    class respMessage
    {
        public List<OutputItem> values { get; set; }
    }
    

    class InputItem
    {
        public string recordId { get; set; }
        public InputData data { get; set; }
    }

    class OutputItem
    {
        public string recordId { get; set; }
        public OutputData data { get; set; }
    }

    class InputData
    {
        public string imagedata { get; set; }
    }

    class OutputData
    {
        public List<string> author { get; set; }
        public string copyright { get; set; }
        public string datetaken { get; set; }
        public List<string> keywords { get; set; }
        public string location { get; set; }
    }



}
