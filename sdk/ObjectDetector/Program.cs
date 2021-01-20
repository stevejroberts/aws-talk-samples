using System;
using System.Threading.Tasks;
using System.IO;

using Amazon.Rekognition;
using Amazon.Rekognition.Model;

namespace ObjectDetector
{
    class Program
    {
        static void Main(string[] args)
        {
            string filename;

            if (args.Length >= 1)
            {
                filename = args[0];
            }
            else
            {
                Console.WriteLine("Enter the name of an image file to process");
                filename = Console.ReadLine();
            }

            if (!System.IO.File.Exists(filename))
            {
                Console.WriteLine($"Image {filename} does not exist!");
                return;
            }

            DetectAndListObjectLabels(filename).Wait();
        }

        /// <summary>
        /// Invokes Amazon Rekognition's DetectLabels API, sending the image to analyze as
        /// a bytestream, and echoes the labels representing the found objects to the console.
        // The default confidence level of 55% is assumed.
        /// </summary>
        static async Task DetectAndListObjectLabels(string imageFilename)
        {
            var rekognitionClient = new AmazonRekognitionClient(Amazon.RegionEndpoint.USWest2);
            var response = await rekognitionClient.DetectLabelsAsync(new DetectLabelsRequest
            {
                Image = new Image
                {
                    // Note - subject to 5MB limit. If the image is larger than this, upload
                    // to an Amazon S3 bucket and pass the bucket name/object key instead.
                    Bytes = new MemoryStream(File.ReadAllBytes(imageFilename))
                }
            });

            // output the found objects
            Console.WriteLine("Detected objects:");
            foreach (var label in response.Labels)
            {
                Console.WriteLine($"Found '{label.Name}', with confidence {label.Confidence}%");
                if (label.Instances.Count > 0)
                {
                    // echo any bounding boxes, which are returned as ratios of the image size
                    foreach (var instance in label.Instances)
                    {
                        Console.WriteLine($" - bounding box (l:{instance.BoundingBox.Left},t:{instance.BoundingBox.Top}),(w:{instance.BoundingBox.Width},h:{instance.BoundingBox.Height})");
                    }
                }
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
    }
}
