using Codeuctivity.ImageSharpCompare;
using NUnit.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Dithering;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using System.Drawing;
using System.Text;
using Color = SixLabors.ImageSharp.Color;
namespace OctreeQuantization.Tests;

public class OctreeTests
{
    private const string TestRgbPath = "00034445";

    public record TestCaseData(int ColorsCount, Rgba32 Color);

    private static readonly int[] Counts = [2, 5, 10, 1024 * 1024];
    private static readonly Rgba32[] Colors = [new(14, 15, 16), new(1, 1, 1), new(15, 16, 17)];
    private static readonly Rgba32 TestRgbColor = new Rgba32(15, 16, 17);
    private static readonly Rgba32 TestRgbColorOne = new Rgba32(15, 16, 18);
    private static readonly Rgba32 TestRgbColorBig = new Rgba32(255, 205, 107);

    [SetUp]
    public void Setup()
    {
    }

    public static IEnumerable<TestCaseData> TestCases =>
        Counts.SelectMany(count => Colors.Select(color => new TestCaseData(count, color)));

    [Test]
    public void Should_have_leaf_parents_with_exactly_eight_children()
    {
        var octree = new Octree();

        octree.Insert(TestRgbColor);
        octree.Insert(TestRgbColor);

        var leafParents = octree.FindLeafParents(octree.Root);
        Assert.That(leafParents, Has.All.Property(nameof(OctreeNode.Children)).Count.EqualTo(8));
    }

    [Test]
    public void Should_contain_leaf_with_correct_reference_count_after_insert_of_two_colors()
    {
        var octree = new Octree();

        octree.Insert(TestRgbColor);
        octree.Insert(TestRgbColor);

        var leaves = octree.FindAllLeaves(octree.Root);
        Assert.That(leaves, Has.One.Property(nameof(OctreeNode.Reference)).EqualTo(2));
    }

    [TestCaseSource(nameof(TestCases))]
    public void Should_contain_exactly_one_leaf_with_correct_color_after_insert_of_multiple_repeated_colors(
        TestCaseData testCase)
    {
        var (insertionCount, color) = testCase;

        var octree = new Octree();
        for (int i = 0; i < insertionCount; i++)
            octree.Insert(color);

        var leaves = octree.FindAllLeaves(octree.Root);
        var l = leaves.Where(x => x.R != 0).ToList();
        float resR = l[0].R;
        float resG = l[0].G;
        float resB = l[0].B;
        Assert.That(leaves, Has.One.Items.Property(nameof(OctreeNode.R)).EqualTo(color.R * insertionCount)
            .And.Property(nameof(OctreeNode.G)).EqualTo(color.G * insertionCount)
            .And.Property(nameof(OctreeNode.B)).EqualTo(color.B * insertionCount));
    }

    [Test]
    public void Should_have_root_equal_to_rgb_zero_after_insert()
    {
        var octree = new Octree();

        octree.Insert(TestRgbColor);

        Assert.That(octree.Root,
            Has.Property(nameof(Octree.Root.R)).Zero
                .And.Property(nameof(Octree.Root.G)).Zero
                .And.Property(nameof(Octree.Root.B)).Zero);
    }

    [Test]
    public void Should_contain_children_of_root_node_after_insert()
    {
        var octree = new Octree();

        octree.Insert(TestRgbColor);

        Assert.That(octree.Root.Children, Is.Not.Null.And.Count.Not.Zero);
    }

    [Test]
    public void Should_contain_inserted_color_leaf_at_level_eight()
    {
        var octree = new Octree();

        octree.Insert(TestRgbColor);

        var node = GetNodeByPath(octree, TestRgbPath);

        Assert.That(node, Has.Property(nameof(node.Reference)).EqualTo(1));
    }

    [Test]
    public void Should_reduce_properly_to_one_color()
    {
        var octree = new Octree();
        octree.Insert(TestRgbColorBig);
        octree.Reduce(1);
        var leaves = octree.FindAllLeaves(octree.Root);
        Assert.That(leaves.Count == 1);
    }


    [Test]
    public void Should_return_right_color()
    {
        var octree = new Octree();
        octree.Insert(TestRgbColor);

        var outputColor = octree.FindProperColor(octree.Root, TestRgbColor);

        Assert.That(outputColor == TestRgbColor);
    }

    [Test]
    public void Should_assign_color_to_rgba32()
    {
        long a = 15;
        long b = 16;
        long c = 17;
        var result = new Rgba32((byte)a, (byte)b, (byte)c);
        Assert.That(result, Is.Not.EqualTo(Rgba32.ParseHex("#00000000")));
        Assert.That(result, Is.Not.EqualTo(new Rgba32(255, 255, 255, 255)));
    }

    [TestCase(993 * 993)]
    [TestCase(950 * 950)]
    [TestCase(925 * 925)]
    [TestCase(750 * 750)]
    [TestCase(500 * 500)]
    [TestCase(100 * 100)]
    [TestCase(5 * 5)]
    public void Numbers_should_add(int count)
    {
        // 994*994 - 993*993
        float a = 0;
        long b = 0;

        for (int i = 0; i < count; i++)
        {
            a += 17;
            b += 17;
        }

        var c = (a == (float)b);
        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void Should_return_colorful_reduced_tree()
    {
        Octree tree = new Octree();
        for (int i = 0; i < 64; i++)
        {
            tree.Insert(TestRgbColor);
            tree.Insert(TestRgbColorOne);
        }
        tree.Reduce(1);
        Assert.That(tree.Root, Has.Property(nameof(tree.Root.R)).EqualTo(TestRgbColor.R).And
                                  .Property(nameof(tree.Root.G)).EqualTo(TestRgbColor.G).And
                                  .Property(nameof(tree.Root.B)).EqualTo(TestRgbColor.B));
    }

    [Test]
    public void Should_return_if_all_children_of_node_are_leaves()
    {
        Octree tree = new Octree();
        tree.Insert(TestRgbColor);

        var path = GetPathNodes(tree, TestRgbPath).ToList();
        path.Select((x, i) => $"index{i} R:{x.R}, G:{x.G}, B:{x.B}, Ref:{x.Reference}").ToList().ForEach(Console.WriteLine);
        Assert.Multiple(() =>
        {
            foreach (var node in path[..^2].Index())
            {
                Assert.That(tree.AreAllChildrenLeaves(node.Item), Is.False, $"index of a node that fails the test {node.Index}");
            }

            Assert.That(tree.AreAllChildrenLeaves(path[^2]), Is.True);
        });
    }

    [Test]
    public void Should_reduce_image()
    {
        Stream imageStream = File.OpenRead("Images\\VikingGirl.png");
        Image<Rgba32> initialImage = Image.Load<Rgba32>(imageStream);
        Octree octree = new Octree();

        Image<Rgba32> outputImage = octree.GenerateOuputImage(initialImage.ToArray(), initialImage, 60);

        var allColors = outputImage.Frames.RootFrame.PixelBuffer.MemoryGroup.ToArray();

        Assert.That(allColors.Single().ToArray(), Has.Some.Not.EqualTo(Rgba32.ParseHex("#00000000")));
        Assert.That(allColors.Single().ToArray(), Has.Some.Not.EqualTo(Rgba32.ParseHex("#000000")));
    }

    [Test]
    public void Should_return_same_image_when_no_reduction()
    {
        Stream imageStream = File.OpenRead("Images\\VikingGirl.png");
        Image<Rgba32> initialImage = Image.Load<Rgba32>(imageStream);
        Octree octree = new Octree();
        Image<Rgba32> outputImage = octree.GenerateOuputImage(initialImage.ToArray(), initialImage, initialImage.ToArray().Distinct().Count());
        var inputAllColors = initialImage.Frames.RootFrame.PixelBuffer.MemoryGroup.Single();
        var outputAllColors = outputImage.Frames.RootFrame.PixelBuffer.MemoryGroup.Single();

        Assert.That(outputAllColors.Span.SequenceEqual(inputAllColors.Span));
    }
    //add test who checks if desired number of colors is bigger than palette colors 
    [Test]
    public void Should_return_all_right_color()
    {
        Stream imageStream = File.OpenRead("Images\\VikingGirl.png");
        Image<Rgba32> initialImage = Image.Load<Rgba32>(imageStream);
        initialImage.Mutate(x => x.Resize(100, 100));
        var octree = new Octree();
        Assert.Multiple(() =>
        {
            for (int i = 0; i < initialImage.Height; i++)
            {
                for (int j = 0; j < initialImage.Width; j++)
                {
                    octree.Insert(initialImage[i, j]);
                    var insertedColor = octree.FindProperColor(octree.Root, initialImage[i, j]);
                    Assert.That(insertedColor, Is.EqualTo(initialImage[i, j]));
                }
            }
        });
    }

    [Test]
    public void Should_all_nodes_have_children_count_equal_to_zero_or_eight()
    {
        Stream imageStream = File.OpenRead("Images\\VikingGirl.png");
        Image<Rgba32> image = Image.Load<Rgba32>(imageStream);
        Octree octree = new Octree();
        octree.Reduce(60);
        var nodes = FindAllNodesOfTree(octree.Root);
        Assert.That(nodes.All(x => x.Children.Count == 0 || x.Children.Count == 8), Is.True);
    }

    [Test]
    public void Should_two_pictures_have_difference_with_palette()
    {
        Dictionary<string, Rgba32> palette = new Dictionary<string, Rgba32>();
        var lines = File.ReadLines("customColorPalette.csv");
        foreach (var line in lines)
        {
            var splittedLine = line.Split(',');
            palette.Add(splittedLine[0], new Rgba32(byte.Parse(splittedLine[1]), byte.Parse(splittedLine[2]), byte.Parse(splittedLine[3]), byte.Parse(splittedLine[4])));
        }
        var colorPalette = palette.Values.ToArray();

        Stream imageStream = File.OpenRead("Images\\VikingGirl.png");
        Image<Rgba32> image = Image.Load<Rgba32>(imageStream);
        Octree octree1 = new Octree();
        var outputImage = octree1.GenerateOuputImage(colorPalette, image, 64);
        outputImage.Save("Images\\test-my-alg-output.png");

        var lessColoredPalette = colorPalette.Where((_, i) => i % 2 != 0).ToArray();
        var imageSharpPalette = lessColoredPalette.Select(x => (Color)x).ToArray();
        using (Image imageSharp = Image.Load("Images\\VikingGirl.png"))
        {
            var quantizer = new PaletteQuantizer(imageSharpPalette, new QuantizerOptions
            {
                Dither = null,
                MaxColors = 64,
            });

            imageSharp.Mutate(x => x.Quantize(quantizer));
            imageSharp.Save("Images\\custom_palette-output-dithered.png",
                            new PngEncoder() { ColorType = PngColorType.Palette, BitDepth = PngBitDepth.Bit8 }
                            );

            //compare imnage vs imageSharp
            ICompareResult difference = ImageSharpCompare.CalcDiff(image, imageSharp);
            Assert.That(difference.PixelErrorPercentage > 0);
        }
    }

    [Test]
    public void Should_two_pictures_have_difference_without_palette()
    {

        Stream imageStream = File.OpenRead("Images\\VikingGirl.png");
        Image<Rgba32> image = Image.Load<Rgba32>(imageStream);
        Octree octree1 = new Octree();
        var outputImage = octree1.GenerateOuputImage(image.ToArray(), image, 64);
        outputImage.Save("Images\\test-my-alg-output.png");

        using (Image imageSharp = Image.Load("Images\\VikingGirl.png"))
        {
            var quantizer = new OctreeQuantizer(new QuantizerOptions
            {
                Dither = null,
                MaxColors = 64,
            });

            imageSharp.Mutate(x => x.Quantize(quantizer));
            imageSharp.Save("Images\\custom_palette-output-dithered.png",
                            new PngEncoder() { ColorType = PngColorType.Palette, BitDepth = PngBitDepth.Bit8 }
                            );

            //compare inage vs imageSharp
            ICompareResult difference = ImageSharpCompare.CalcDiff(image, imageSharp);
            Assert.That(difference.MeanError < 25);
        }
    }


    private IEnumerable<OctreeNode> GetPathNodes(Octree tree, string path)
    {
        var node = tree.Root;
        foreach (var segment in path.Select(x => x - '0'))
        {
            node = node.Children[segment];

            yield return node;
        }
    }

    private OctreeNode GetNodeByPath(Octree tree, string path)
    {
        return GetPathNodes(tree, path).Last();
    }

    private IEnumerable<OctreeNode> FindAllNodesOfTree(OctreeNode node)
    {
        //if(node.Children.Count == 0)
        yield return node;
        foreach (var child in node.Children)
        {
            foreach (var childNodes in FindAllNodesOfTree(child))
            {
                yield return childNodes;
            }
        }
    }
}