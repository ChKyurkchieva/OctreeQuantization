using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;
namespace OctreeQuantization.Tests;

static internal class ImageExtensions
{
    public static Rgba32[] ToArray(this Image<Rgba32> initialImage)
    {
        Rgba32[] dest = new Rgba32[initialImage.Height * initialImage.Width];
        initialImage.Frames.RootFrame.CopyPixelDataTo(dest);
        return dest;
    }
}
