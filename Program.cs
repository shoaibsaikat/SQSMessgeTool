using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThirdParty.Json.LitJson;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace CrServiceSQSMessgeTool
{
    class Program
    {
        private readonly SQSConfiguration _sqsConfig;
        private readonly string _queueUrl;

        private class CrData
        {
            public string SessionId { get; set; }
            public string FileName { get; set; }
            public string RecordingId { get; set; }
        }

        private class SQSConfiguration
        {
            public string QueueName { get; set; }
            public string AccessKey { get; set; }
            public string SecretAccessKey { get; set; }
            public RegionEndpoint RegionEndpoint { get; set; }
        }

        public void SendMessage(string clientId, string message)
        {
            AmazonSQSConfig amazonSqsConfig = new AmazonSQSConfig();
            amazonSqsConfig.RegionEndpoint = _sqsConfig.RegionEndpoint;

            using (AmazonSQSClient sqsClient = new AmazonSQSClient(_sqsConfig.AccessKey, _sqsConfig.SecretAccessKey, amazonSqsConfig))
            {
                var sqsRequest = new GetQueueUrlRequest();
                sqsRequest.QueueName = _sqsConfig.QueueName;

                var attrs = new Dictionary<string, MessageAttributeValue>();
                attrs.Add("ClientId", new MessageAttributeValue()
                {
                    DataType = "String",
                    StringValue = clientId
                });

                var sendMessageRequest = new SendMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MessageBody = message,
                    MessageAttributes = attrs,
                };

                //Console.WriteLine("QueueUrl : " + _queueUrl);

                var response = sqsClient.SendMessage(sendMessageRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                    Console.WriteLine("sqs msg sent.");
                else
                    Console.WriteLine("sqs msg not sent. Response code = " + response.HttpStatusCode.ToString());
            }
        }

        public Program()
        {
            _sqsConfig = new SQSConfiguration()
            {
                QueueName = ConfigurationManager.AppSettings["QueueName"],
                RegionEndpoint = RegionEndpoint.USEast1,
                AccessKey = ConfigurationManager.AppSettings["AccessKeySQS"],
                SecretAccessKey = ConfigurationManager.AppSettings["SecretAccessKeySQS"]
            };

            Console.WriteLine("QueueName : " + _sqsConfig.QueueName);
            Console.WriteLine("AccessKey : " + _sqsConfig.AccessKey);
            Console.WriteLine("SecretAccessKey : " + _sqsConfig.SecretAccessKey);

            AmazonSQSConfig amazonSqsConfig = new AmazonSQSConfig();
            amazonSqsConfig.RegionEndpoint = _sqsConfig.RegionEndpoint;
            var  sqsClient = AWSClientFactory.CreateAmazonSQSClient(_sqsConfig.AccessKey, _sqsConfig.SecretAccessKey, amazonSqsConfig);
            var sqsRequest = new GetQueueUrlRequest();
            sqsRequest.QueueName = _sqsConfig.QueueName;
            var sqsResponse = sqsClient.GetQueueUrl(sqsRequest);

            _queueUrl = sqsResponse.QueueUrl;
        }

        public string CreateMessage(string sessionId, string fileName, string recordingId)
        {
            CrData data = new CrData()
            {
                SessionId = sessionId,
                FileName = fileName,
                RecordingId = recordingId
            };
            var serializer = new JavaScriptSerializer();
            var json = serializer.Serialize(data);
            Console.WriteLine(json);
            return json;
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            var sessionId = "233411";
            var fileName = "233411_07172017124335_12345_Testy Testguy_07172017163000.mp4";
            var recordingId = "12345";
            string message;

            Console.WriteLine("File Name example: {0}", fileName);

            do
            {
                Console.WriteLine("Please enter file name");

                var tmpInput = Console.ReadLine();
                if (tmpInput != string.Empty)
                {
                    Console.WriteLine("Setting File Name");
                    fileName = tmpInput;
                }

                sessionId = fileName != null ? fileName.Split('_')[0] : string.Empty;
                message = p.CreateMessage(sessionId, fileName, recordingId);
                p.SendMessage("SecureVideo", message);
            } while (true);
        }
    }
}
