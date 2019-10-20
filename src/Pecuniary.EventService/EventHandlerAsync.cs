using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.SimpleNotificationService;
using Logging.LambdaLogger;
using JsonSerializer = Amazon.Lambda.Serialization.Json.JsonSerializer;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Pecuniary.EventService
{
    public class Function
    {
        private static readonly JsonSerializer JsonSerializer = new JsonSerializer();

        public async Task EventHandlerAsync(DynamoDBEvent dynamoEvent)
        {
            var snsClient = new AmazonSimpleNotificationServiceClient(RegionEndpoint.USWest2);

            Logger.Log($"Beginning to process {dynamoEvent.Records.Count} records...");

            foreach (var record in dynamoEvent.Records)
            {
                Logger.Log($"Event ID: {record.EventID}");
                Logger.Log($"Event Name: {record.EventName}");

                string streamRecordJson = SerializeObject(record.Dynamodb);
                Logger.Log($"DynamoDB Record:");
                Logger.Log(streamRecordJson);

                // Determine SNS topic from EventName
                string eventName = null;
                string @event = null;
                Logger.Log($"Count: {record.Dynamodb.NewImage.Values.Count}");
                foreach (var s in record.Dynamodb.NewImage)
                {
                    if (s.Key == "Event")
                    {
                        Logger.Log($" Key: {s.Key}");
                        Logger.Log($" Value: {s.Value.S}");

                        @event = s.Value.S;
                    }

                    if (s.Key == "EventName")
                    {
                        Logger.Log($" Key: {s.Key}");
                        Logger.Log($" Value: {s.Value.S}");

                        eventName = s.Value.S.Replace(@"""", "");
                    }
                }

                // Publish event to SNS Topic
                try
                {
                    var eventCreatedTopic = Environment.GetEnvironmentVariable($"{eventName}Topic");

                    Logger.Log("Sending event to SNS for further processing");
                    await snsClient.PublishAsync(eventCreatedTopic, @event);
                    Logger.Log("Successfully sent event to SNS for further processing");
                }
                catch (Exception ex)
                {
                    Logger.Log($"ERROR: {ex.Message}");
                }
            }

            Logger.Log("Stream processing complete.");
        }

        private static string SerializeObject(object streamRecord)
        {
            using (var ms = new MemoryStream())
            {
                JsonSerializer.Serialize(streamRecord, ms);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
}
