using OctreeQuantization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Dithering;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("OctreeQuantization.Tests")]

var watch = System.Diagnostics.Stopwatch.StartNew();
Dictionary<string, Rgba32> palette = new Dictionary<string, Rgba32>();
var lines = File.ReadLines("customColorPalette.csv");
foreach (var line in lines)
{
    var splittedLine = line.Split(',');
    palette.Add(splittedLine[0], new Rgba32(byte.Parse(splittedLine[1]), byte.Parse(splittedLine[2]), byte.Parse(splittedLine[3]), byte.Parse(splittedLine[4])));
}
var colorPalette = palette.Values.ToArray();
Stream imageStream = File.OpenRead("Images\\VikingGirl.png");
using Image<Rgba32> initialImage = Image.Load<Rgba32>(imageStream);
Octree octree = new Octree();
Image<Rgba32> outputImage = octree.GenerateOuputImage(colorPalette, initialImage, 64);
outputImage.Save("Images\\VikingGirlMyOctree.png");
watch.Stop();
var elapsedMs = watch.ElapsedMilliseconds;
Console.WriteLine(octree.FindAllLeaves(octree.Root).Count);
Console.WriteLine($"MyQuantize: {elapsedMs}ms");

//var watch1 = System.Diagnostics.Stopwatch.StartNew();
//var lines = File.ReadLines("customColorPalette.csv");
//using (Image image = Image.Load("Images\\VikingGirl.png"))
//{
//    //var quantizer = new WuQuantizer(new QuantizerOptions
//    //{
//    //    Dither = ErrorDither.Burkes,
//    //    MaxColors = 70
//    //});
//    var quantizer = new OctreeQuantizer(new QuantizerOptions
//    {
//        Dither = null,//ErrorDither.Burkes,
//        MaxColors = 64,
//        ColorMatchingMode = ColorMatchingMode.Exact,
//    });

//    image.Mutate(x => x.Quantize(quantizer));
//    image.Save("Images\\ImageSharpPaletteQuantizer.png",
//        new PngEncoder() { ColorType = PngColorType.Palette, BitDepth = PngBitDepth.Bit8 });

//}
//Dictionary<string, Rgba32> palette1 = new Dictionary<string, Rgba32>();
//foreach (var line in lines)
//{
//    var splittedLine = line.Split(',');
//    palette1.Add(splittedLine[0], new Rgba32(byte.Parse(splittedLine[1]), byte.Parse(splittedLine[2]), byte.Parse(splittedLine[3]), byte.Parse(splittedLine[4])));
//}
//var colorPalette1 = palette1.Values.ToArray();
//Stream imageStream = File.OpenRead("Images\\ImageSharpPaletteQuantizer.png");
//using Image<Rgba32> initialImage = Image.Load<Rgba32>(imageStream);
//Octree octree = new Octree();
//Image<Rgba32> outputImage = octree.GenerateOuputImage(colorPalette1, initialImage, 128);
//outputImage.Save("Images\\ImageSharpPaletteQuantizerCustomPaletteMAPPING.png");
//watch1.Stop();
//var elapsedMs = watch1.ElapsedMilliseconds;
//Console.WriteLine($"ImageSharp: {elapsedMs}ms");
