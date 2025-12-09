using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using LabResultProcessor.Core.Models;
using LabResultProcessor.Core.Parsing;
using System.Net;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LabResultProcessor.Function
{
    public class Function
    {

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input">The event for the Lambda function handler to process.</param>
        /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
        /// <returns></returns>
        /// 
        private readonly IAmazonS3 _s3Client;
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly Hl7LabResultParser _parser;

        private readonly string _tableName;
        public Function(): this( new AmazonS3Client(), new AmazonDynamoDBClient(), new Hl7LabResultParser(),Environment.GetEnvironmentVariable("LAB_RESULTS_TABLE") ?? "LabResults")
        {
        }
        public Function(IAmazonS3 s3Client, IAmazonDynamoDB dynamoDb, Hl7LabResultParser parser, string tableName)
        {
            _s3Client = s3Client;
            _dynamoDb = dynamoDb;
            _parser = parser;
            _tableName = tableName;
        }
        public async Task HandleAsync(S3Event evnt, ILambdaContext context)
        {
            foreach (var record in evnt.Records)
            {
                var s3 = record.S3;
                var bucketName = s3.Bucket.Name;
                var objectKey = WebUtility.UrlDecode(s3.Object.Key);

                context.Logger.LogInformation($"Processing file s3://{bucketName}/{objectKey}");

                string hl7Content = await ReadObjectAsync(bucketName, objectKey, context);
                var labResults = _parser.ParseLabResults(hl7Content);

                foreach (var result in labResults)
                {
                    await SaveResultAsync(result, context);
                }

                context.Logger.LogInformation(
                    $"Stored {labResults.Count} results from s3://{bucketName}/{objectKey}");
            }
        }

        private async Task<string> ReadObjectAsync(string bucket, string key, ILambdaContext context)
        {
            using var resp = await _s3Client.GetObjectAsync(bucket, key);
            using var reader = new StreamReader(resp.ResponseStream);
            var content = await reader.ReadToEndAsync();

            context.Logger.LogInformation($"Read {content.Length} bytes from {key}");
            return content;
        }

        private async Task SaveResultAsync(LabResult result, ILambdaContext context)
        {
            var item = new Dictionary<string, AttributeValue>
            {
                ["PatientId"] = new(result.PatientPk),
                ["ResultKey"] = new(result.ResultKey),
                ["OrderId"] = new(result.OrderId),
                ["TestCode"] = new(result.TestCode),
                ["TestDescription"] = new(result.TestDescription),
                ["ResultValue"] = new(result.ResultValue),
                ["Units"] = new(result.Units),
                ["ReferenceRange"] = new(result.ReferenceRange),
                ["AbnormalFlag"] = new(result.AbnormalFlag),
                ["ObservationDateTime"] = new(result.ObservationDateTime.ToString("o")),
                ["ResultStatus"] = new(result.ResultStatus),
                ["PatientLastName"] = new(result.LastName),
                ["PatientFirstName"] = new(result.FirstName),
            };

            var request = new PutItemRequest
            {
                TableName = _tableName,
                Item = item
            };

            await _dynamoDb.PutItemAsync(request);
            context.Logger.LogInformation($"Saved result {result.ResultKey} for patient {result.PatientId}");
        }

    }
}
