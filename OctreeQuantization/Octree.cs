using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;
namespace OctreeQuantization;

internal class OctreeNode
{

    public OctreeNode()
    {
        r = 0;
        g = 0;
        b = 0;
    }
    private long reference;
    private long r;
    private long g;
    private long b;

    public List<OctreeNode> Children { get; set; } = [];

    public long Reference { get => reference; set => reference = value; }
    public long R { get => r; set => r = value; }
    public long G { get => g; set => g = value; }
    public long B { get => b; set => b = value; }
}
internal class Octree
{
    private readonly OctreeNode _root;
    private List<List<OctreeNode>> _levels;

    public Octree()
    {
        _root = new OctreeNode();
        _levels = new List<List<OctreeNode>>(9);
        for (int i = 0; i < 9; i++)
            _levels.Add(new List<OctreeNode>());
        _levels[0].Add(Root);
    }

    public OctreeNode Root => _root;
    public List<List<OctreeNode>> Levels => _levels;

    public void BuildTree(Image<Rgba32> image)
    {
        for (int i = 0; i < image.Height; i++)
        {
            for (int j = 0; j < image.Width; j++)
                Insert(image[j, i]);
        }
    }

    public Image<Rgba32> GenerateOuputImage(Rgba32[] palette, Image<Rgba32> image, int desiredColorsCount)
    {
        Image<Rgba32> outputImage = new Image<Rgba32>(image.Width, image.Height);
        BuildTree(image);
        Reduce(desiredColorsCount);
        outputImage = MapImage(image, palette);
        return outputImage;
    }

    public Image<Rgba32> MapImage(Image<Rgba32> source, Rgba32[] palette = null)
    {
        Image<Rgba32> output = new Image<Rgba32>(source.Width, source.Height);
        Dictionary<Rgba32, Rgba32> colorsCaching = new Dictionary<Rgba32, Rgba32>();
        for (int i = 0; i < source.Height; i++)
        {
            for (int j = 0; j < source.Width; j++)
            {
                Rgba32 octreeColor = FindProperColor(Root, source[j, i]);
                if (!colorsCaching.TryGetValue(octreeColor, out Rgba32 value))
                {
                    if (palette is null)
                        output[j, i] = octreeColor;
                    else
                        output[j, i] = FindClosestPaletteColor(octreeColor, palette);

                    colorsCaching.Add(octreeColor, output[j, i]);
                }
                else
                    output[j, i] = value;
            }
        }
        return output;
    }
    private Rgba32 FindClosestPaletteColor(Rgba32 color, Rgba32[] palette) => palette.Zip(palette.Select(x => EuclidianDistance(color, x))).MinBy(x => x.Second).First;
    private int EuclidianDistance(Rgba32 first, Rgba32 second)
    {
        return ((first.R - second.R) * (first.R - second.R)) +
               ((first.G - second.G) * (first.G - second.G)) +
               ((first.B - second.B) * (first.B - second.B));
    }
    public void Insert(Rgba32 color, OctreeNode? root = null, OctreeNode? parent = null, int significantBit = 7)
    {
        var levelIndex = 8 - significantBit;
        if (significantBit == -1)
        {
            root.Reference++;
            root.R += color.R;
            root.G += color.G;
            root.B += color.B;
            return;
        }
        root ??= _root;
        int R = ((color.R & (1 << significantBit)) > 0) ? 1 : 0;
        int G = ((color.G & (1 << significantBit)) > 0) ? 1 : 0;
        int B = ((color.B & (1 << significantBit)) > 0) ? 1 : 0;
        int bit = R << 2 | G << 1 | B;
        if (root.Children is [])
        {
            for (int i = 0; i < 8; i++)
            {
                var node = new OctreeNode();
                root.Children.Add(node);
                _levels[levelIndex].Add(node);
            }
        }

        Insert(color, root.Children[bit], root, --significantBit);
    }
    private long FindLeavesCount(OctreeNode root)
    {
        long result = 0;
        if (root.Children is []) return 1;
        if (AreAllChildrenLeaves(root)) return root.Children.Count;

        foreach (var node in root.Children)
            result += FindLeavesCount(node);

        return result;
    }
    public void Reduce(int reduceColorsTo)
    {
        Dictionary<OctreeNode, long> nodes = new Dictionary<OctreeNode, long>();
        long leavesCount = FindAllLeaves(Root).Count();
        var colors = FindAllLeaves(Root).Count(x => x.Reference > 0);
        var levelsCount = Levels.Count();
        var totalColorsCount = Levels[^1].Count(x => x.Reference > 0);
        for (int i = levelsCount - 2; i >= 0 && reduceColorsTo < totalColorsCount; i--)
        {
            if (totalColorsCount - reduceColorsTo < 0)
                throw new Exception("Wtf, dont reduce 50 apples to 75");
            PriorityQueue<OctreeNode, long> candidates = new PriorityQueue<OctreeNode, long>(
                Levels[i].Select(x => (x, x.Children.Sum(child => child.Reference))),
                Comparer<long>.Create((long xS, long yS) => (int)(xS - yS))
            );
            HashSet<OctreeNode> removedChildren = new HashSet<OctreeNode>();
            while (totalColorsCount > reduceColorsTo && candidates.Count > 0)
            {
                var removed = candidates.Dequeue();
                removed.Children.ForEach(x => removedChildren.Add(x));
                totalColorsCount -= ReduceNode(removed);
            }
            if (removedChildren.Count == Levels[i + 1].Count)
            {
                Levels[i + 1].Clear();
                Levels.RemoveAt(i + 1);
            }
            else
                Levels[i + 1].RemoveAll(x => removedChildren.Contains(x));
            // compute how many nodes to remove 
            // Select nodes from Level[i+1] up to the count of that level
            //Removes nodes from level
            totalColorsCount = Levels.TakeLast(2).Sum(level => level.Count(x => x.Reference > 0));
        }

    }
    private int ReduceNode(OctreeNode node)
    {
        int childrenCount = node.Children.Count;
        var childColors = node.Children.Count(x => x.Reference > 0) - 1;
        for (int i = childrenCount - 1; i >= 0; i--)
        {
            node.Reference += node.Children[i].Reference;
            node.R += node.Children[i].R;
            node.G += node.Children[i].G;
            node.B += node.Children[i].B;
            node.Children.RemoveAt(i);
            if (node.Reference != 0)
            {
                node.R /= node.Reference;
                node.G /= node.Reference;
                node.B /= node.Reference;
                node.Reference /= node.Reference;
            }
        }
        return Math.Max(0, childColors);
    }
    private void ReduceHelper(OctreeNode root, Dictionary<OctreeNode, long> nodes)
    {
        long sumChildrenReferences = 0;
        sumChildrenReferences = SumDecendentReferences(root);
        nodes.TryAdd(root, sumChildrenReferences);
        if (root.Children is []) return;
        foreach (var node in root.Children)
        {
            if (node.Children is not [])
                ReduceHelper(node, nodes);
        }
    }

    private long SumDecendentReferences(OctreeNode node)
    {
        long sum = 0;
        if (node.Children is []) return 0;
        if (AreAllChildrenLeaves(node))
            foreach (var child in node.Children)
                sum += child.Reference;
        else
        {
            foreach (var child in node.Children)
                sum += SumDecendentReferences(child);
        }
        return sum;
    }

    internal bool AreAllChildrenLeaves(OctreeNode node)
    {
        if (node.Children is []) return false;
        return node.Children.All(x => x.Children is []);
    }

    public List<OctreeNode> FindLeafParents(OctreeNode node)
    {
        List<OctreeNode>? result = new List<OctreeNode>();
        FindLeaves(node, result, x => x.Children is not [] && x.Children.FirstOrDefault() is { Children: [] });
        return result;
    }

    public List<OctreeNode> FindAllLeaves(OctreeNode node)
    {
        List<OctreeNode>? result = new List<OctreeNode>();
        FindLeaves(node, result, x => x is { Children: [], Reference: > 0 });
        return result;
    }
    private void FindLeaves(OctreeNode node, List<OctreeNode> found, Func<OctreeNode, bool> nodePredicate)
    {
        if (node is null) { return; }
        if (nodePredicate(node))
        {
            found.Add(node);
        }
        else if (node.Children is not [])
        {
            foreach (var child in node.Children)
                FindLeaves(child, found, nodePredicate);
        }
    }
    internal Rgba32 FindProperColor(OctreeNode node, Rgba32 color)
    {
        return FindProperColorHelper(node, color, 7);
    }
    private Rgba32 FindProperColorHelper(OctreeNode node, Rgba32 color, int significantBit)
    {
        if (node.Children is [] || significantBit == -1)
        {
            long reference = Math.Max(node.Reference, 1);
            return new Rgba32((byte)(node.R / reference), (byte)(node.G / reference), (byte)(node.B / reference));
        }
        int R = ((color.R & (1 << significantBit)) > 0) ? 1 : 0;
        int G = ((color.G & (1 << significantBit)) > 0) ? 1 : 0;
        int B = ((color.B & (1 << significantBit)) > 0) ? 1 : 0;
        int bit = R << 2 | G << 1 | B;
        return FindProperColorHelper(node.Children[bit], color, --significantBit);
    }

}

