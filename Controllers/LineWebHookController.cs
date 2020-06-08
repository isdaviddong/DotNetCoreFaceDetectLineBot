using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Drawing;

namespace isRock.Template
{
    public class LineFaceDetectController : isRock.LineBot.LineWebHookControllerBase
    {
        //FaceAPI Endpoint
        static string endpoint = "https://___Face_API____.cognitiveservices.azure.com/";
        //FaceAPI key
        static string key = "_______c4a3a8225eec________";
        //Imgur Client-ID
        static string ImgurClientID = "____cb4ff2____";

        [Route("api/LineFaceDetect")]
        [HttpPost]
        public IActionResult POST()
        {
            //Line Bot AdminUserId
            var AdminUserId = "_________d822559502_______";

            try
            {
                //設定LINE Bot ChannelAccessToken
                this.ChannelAccessToken = "___________v0MIF9FseVKc2WKqi9Tc2ck0LiL_____";
                //取得Line Event
                var LineEvent = this.ReceivedMessage.events.FirstOrDefault();
                //配合Line verify 
                if (LineEvent.replyToken == "00000000000000000000000000000000") return Ok();
                var responseMsg = "";
                //準備回覆訊息
                if (LineEvent.type.ToLower() == "message" && LineEvent.message.type == "text")
                    responseMsg = $"你說了: {LineEvent.message.text}";
                else if (LineEvent.type.ToLower() == "message" && LineEvent.message.type == "image")
                {
                    //取得用戶上傳給bot的照片
                    var bytes = this.GetUserUploadedContent(LineEvent.message.id);
                    var ret = FaceDetect(bytes);
                    if (ret == null || ret.Count <= 0)
                    {
                        this.ReplyMessage(LineEvent.replyToken, ret.ToString());
                        return Ok();
                    }
                    //drawing
                    System.Drawing.Bitmap bmp;
                    using (var ms = new System.IO.MemoryStream(bytes))
                    {
                        bmp = new System.Drawing.Bitmap(ms);
                    }
                    //建立繪圖物件
                    Graphics g = Graphics.FromImage(bmp);
                    foreach (var item in ret)
                    {
                        var faceRect = item.faceRectangle;
                        //用抓到的人臉座標位置畫框
                        g.DrawRectangle(
                                    new Pen(Brushes.Red, 3),
                                    new System.Drawing.Rectangle(
                                        (int)faceRect.left, (int)faceRect.top,
                                        (int)faceRect.width, (int)faceRect.height));
                    }
                    //get new image bytes 
                    var BmpBytes = ImageToByte2(bmp);
                    //upload image to Imgur
                    var ImgurRet = UploadImage2Imgur(BmpBytes);
                    //回覆訊息(Imgur上的圖片)
                    var msgs = new List<isRock.LineBot.MessageBase>();
                    msgs.Add(new isRock.LineBot.TextMessage($"JSON: {ret.ToString()} "));
                    msgs.Add(new isRock.LineBot.TextMessage($"共找到 {ret.Count} 張臉"));
                    msgs.Add(new isRock.LineBot.ImageMessage(new Uri("" + ImgurRet.data.link), new Uri("" + ImgurRet.data.link)));
                    this.ReplyMessage(LineEvent.replyToken, msgs);
                    return Ok();
                }
                else if (LineEvent.type.ToLower() == "message")
                    responseMsg = $"收到 event : {LineEvent.type} type: {LineEvent.message.type} ";
                else
                    responseMsg = $"收到 event : {LineEvent.type} ";
                //回覆訊息
                this.ReplyMessage(LineEvent.replyToken, responseMsg);
                //response OK
                return Ok();
            }
            catch (Exception ex)
            {
                //回覆訊息
                this.PushMessage(AdminUserId, "發生錯誤:\n" + ex.Message);
                //response OK
                return Ok();
            }
        }

        static dynamic UploadImage2Imgur(byte[] image)
        {
            HttpClient client = new HttpClient();
            string uriBase = "https://api.imgur.com/3/upload";

            // Request headers.
            client.DefaultRequestHeaders.Add(
                "Authorization", $"Client-ID {ImgurClientID}");

            string uri = uriBase;

            HttpResponseMessage response;

            // Add the byte array as an octet stream to the request body.
            using (ByteArrayContent content = new ByteArrayContent(image))
            {
                // and "multipart/form-data".
                content.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                // Asynchronously call the REST API method.
                response = client.PostAsync(uri, content).Result;
            }

            // Asynchronously get the JSON response.
            string JSON = response.Content.ReadAsStringAsync().Result;

            return Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(JSON);
        }

        static byte[] ImageToByte2(Image img)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }

        //call MS Cognitive Services to Detect Face
        static dynamic FaceDetect(byte[] byteData)
        {
            HttpClient client = new HttpClient();
            string uriBase = endpoint + "face/v1.0/detect";

            // Request headers.
            client.DefaultRequestHeaders.Add(
                "Ocp-Apim-Subscription-Key", key);

            string requestParameters =
                "returnFaceAttributes=age,gender";

            // Assemble the URI for the REST API method.
            string uri = uriBase + "?" + requestParameters;

            HttpResponseMessage response;

            // Add the byte array as an octet stream to the request body.
            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                // This example uses the "application/octet-stream" content type.
                // The other content types you can use are "application/json"
                // and "multipart/form-data".
                content.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                // Asynchronously call the REST API method.
                response = client.PostAsync(uri, content).Result;
            }

            // Asynchronously get the JSON response.
            string JSON = response.Content.ReadAsStringAsync().Result;

            return Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(JSON);
        }
    }
}