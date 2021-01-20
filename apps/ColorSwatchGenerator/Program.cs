using System;
using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace SwatchGenerator
{
    class Program
    {
        const byte DefaultSwatchStepSize = 25;
        const int MaxDimension = 1024;

        static void Main(string[] args)
        {
            Console.WriteLine("SwatchGenerator utility");

            if (args.Length == 0)
            {
                Console.WriteLine($"Usage: swatchgenerator outputfolder swatchStepSize");
                Console.WriteLine($"       'outputfolder' is the name of the folder in which to place the generated swatch images.");
                Console.WriteLine($"       'swatchStepSize' is how much to increase each color per channel per swatch. Defaults to {DefaultSwatchStepSize}.");
                return;
            }

            GenerateSwatchImages(args[0], args.Length > 1 ? byte.Parse(args[1]) : DefaultSwatchStepSize);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Generates a collection of color swatch images in the specified folder. The
        /// swatches start from pure black and then for each color channel, progressive
        /// swatches are generated stepping the channel up by the specified step size 
        /// until the channel reaches saturation for the chosen color depth (eg 255 for 
        /// an 8-bit image).
        /// </summary>
        /// <param name="outputFolder"></param>
        /// <param name="swatchStepSize"></param>
        static void GenerateSwatchImages(string outputFolder, byte swatchStepSize)
        {
            Console.WriteLine($"Generating swatches into {outputFolder} with step size {swatchStepSize}");

            var configuration = Configuration.Default;
            configuration.ImageFormatsManager.SetEncoder(JpegFormat.Instance, new JpegEncoder
            {
                Quality = 100
            });

            var generatedCount = 0;
            for (var red = 0; red < 255; red += swatchStepSize)
            {
                for (var green = 0; green < 255; green += swatchStepSize)
                {
                    for (var blue = 0; blue < 255; blue += swatchStepSize)
                    {
                        generatedCount++;
                        var rgb = new Rgba32((byte)red, (byte)green, (byte)blue);
                        using (var image = new Image<Rgba32>(configuration, MaxDimension, MaxDimension, rgb))
                        {
                            var filename = $"{rgb.ToHex()}.jpg";
                            Console.WriteLine($"Swatch {generatedCount}: {filename}");

                            image.Save(Path.Combine(outputFolder, filename));
                        }
                    }
                }
            }

            // make sure to capture pure white, in case our step size caused us to jump over it
            var white = new Rgba32(255, 255, 255);
            using (var image = new Image<Rgba32>(configuration, MaxDimension, MaxDimension, white))
            {
                var filename = $"{white.ToHex()}.jpg";
                Console.WriteLine($"Swatch {generatedCount+1}: {filename}");
                image.Save(Path.Combine(outputFolder, filename));
            }
        }
    }
}
