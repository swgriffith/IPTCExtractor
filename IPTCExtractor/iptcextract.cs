using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace IPTCExtractor
{
    public static class iptcextract
    {
        [FunctionName("iptcextract")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("IPTC Extract Triggered");

            try
            {
                // Get request body
                reqMessage reqData = JsonConvert.DeserializeObject<reqMessage>(req.Content.ReadAsAsync<object>().Result.ToString());

                respMessage outputObj = new respMessage();

                //If the request body has data in the values array, start processing
                if (reqData.values.Count > 0)
                {                
                    outputObj.values = new List<OutputItem>();

                    //for each item in the array process each image
                    foreach (var item in reqData.values)
                    {
                        //convert from base64 encoding into byte array for further processing
                        byte[] imageBytes = Convert.FromBase64String(item.data.imagedata);
                        MemoryStream ms = new MemoryStream(imageBytes);

                        //get metadata from the image
                        JpegBitmapDecoder decoder = new JpegBitmapDecoder(ms, BitmapCreateOptions.None, BitmapCacheOption.None);
                        BitmapMetadata metadata = decoder.Frames[0].Metadata as BitmapMetadata;

                        //build output object for this image
                        if (metadata != null)
                        {
                            OutputItem outItem = new OutputItem();
                            outItem.recordId = item.recordId;
                            outItem.data = new OutputData();
                            if (metadata.Author != null)
                            {
                                outItem.data.author = metadata.Author.ToList<string>();
                            }
                            outItem.data.copyright = metadata.Copyright;
                            outItem.data.datetaken = ConvertDateFormat(metadata.DateTaken);
                            outItem.data.location = metadata.Location;
                            if (metadata.Keywords !=null)
                            {
                                outItem.data.keywords = metadata.Keywords.ToList<string>();
                            }
                            outputObj.values.Add(outItem);
                        }
                        else
                        {
                            //if no metadata found for an image throw an exception
                            //TODO: Does it make sense ot return an empty array element for an image with no metadata
                            throw new Exception("No metadata found for image");
                        }

                    }
                }
                //send response object
                return req.CreateResponse(HttpStatusCode.OK, outputObj);
            }
            catch (Exception ex)
            {
                //return error object for any exceptions caught
                return req.CreateResponse(HttpStatusCode.BadRequest, string.Format("Processing Error: %0", ex.Message));
            }

        }

        private static string ConvertDateFormat(string inputDate)
        {
            DateTime stage = DateTime.Parse(inputDate);
            return stage.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
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
