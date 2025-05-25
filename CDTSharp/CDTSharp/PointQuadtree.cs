using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    public class PointQuadtree
    {
        readonly Node root;
        int _nextIndex = 0;

        public PointQuadtree(Rect bounds, int maxDepth = 10, int maxItems = 8)
        {
            root = new Node(bounds, 0, maxDepth, maxItems);
        }

        public class Point
        {
            public double X { get; }
            public double Y { get; }
            public int Index { get; }

            public Point(int index, double x, double y)
            {
                Index = index + 3;
                X = x; Y = y;
            }
        }

        private class Node
        {
            public Rect Bounds;
            public List<Point> Items;
            public Node[]? Children;
            public int Depth;
            private readonly int MaxDepth;
            private readonly int MaxItems;

            public Node(Rect bounds, int depth, int maxDepth, int maxItems)
            {
                Bounds = bounds;
                Depth = depth;
                MaxDepth = maxDepth;
                MaxItems = maxItems;
                Items = new List<Point>();
            }

            public void Insert(Point item)
            {
                double x = item.X, y = item.Y;
                if (!Bounds.Contains(x, y)) return;

                if (Children == null)
                {
                    Items.Add(item);
                    if (Items.Count > MaxItems && Depth < MaxDepth)
                    {
                        Subdivide();
                        foreach (var i in Items)
                            GetChild(i.X, i.Y).Items.Add(i);
                        Items.Clear();
                    }
                }
                else
                {
                    GetChild(x, y).Insert(item);
                }
            }

            private void Subdivide()
            {
                double cx = (Bounds.minX + Bounds.maxX) * 0.5;
                double cy = (Bounds.minY + Bounds.maxY) * 0.5;
                Children = new Node[4];
                Children[0] = new Node(new Rect(Bounds.minX, Bounds.minY, cx, cy), Depth + 1, MaxDepth, MaxItems); // bottom-left
                Children[1] = new Node(new Rect(cx, Bounds.minY, Bounds.maxX, cy), Depth + 1, MaxDepth, MaxItems); // bottom-right
                Children[2] = new Node(new Rect(Bounds.minX, cy, cx, Bounds.maxY), Depth + 1, MaxDepth, MaxItems); // top-left
                Children[3] = new Node(new Rect(cx, cy, Bounds.maxX, Bounds.maxY), Depth + 1, MaxDepth, MaxItems); // top-right
            }

            private Node GetChild(double x, double y)
            {
                double cx = (Bounds.minX + Bounds.maxX) * 0.5;
                double cy = (Bounds.minY + Bounds.maxY) * 0.5;
                if (y < cy)
                    return x < cx ? Children[0] : Children[1];
                else
                    return x < cx ? Children[2] : Children[3];
            }

            public void Query(double x, double y, List<Point> results, ref int steps, double eps = 1e-10)
            {
                steps++;
                if (!Bounds.Contains(x, y)) return;

                foreach (var item in Items)
                {
                    if (Math.Abs(item.X - x) < eps && Math.Abs(item.Y - y) < eps)
                        results.Add(item);
                }

                if (Children != null)
                {
                    GetChild(x, y).Query(x, y, results, ref steps, eps);
                }
            }

            public void Query(Rect area, List<Point> results, ref int steps, double eps)
            {
                steps++;

                if (!Bounds.Intersects(area)) return;
                var (cx, cy) = area.Center();
                foreach (var item in Items)
                {
                    double dx = item.X - cx;
                    double dy = item.Y - cy;
                    if (Math.Abs(dx) <= eps && Math.Abs(dy) <= eps)
                    {
                        results.Add(item);
                    }
                }

                if (Children != null)
                {
                    foreach (var child in Children)
                    {
                        child.Query(area, results, ref steps, eps);
                    }
                }
            }

            public void CollectDebugInfo(ref int nodeCount, ref int itemCount, ref int maxDepth, Dictionary<int, int> histogram)
            {
                nodeCount++;
                itemCount += Items.Count;
                if (Depth > maxDepth) maxDepth = Depth;

                int count = Items.Count;
                if (histogram.TryGetValue(count, out int current))
                    histogram[count] = current + 1;
                else
                    histogram[count] = 1;

                if (Children != null)
                {
                    foreach (var child in Children)
                        child.CollectDebugInfo(ref nodeCount, ref itemCount, ref maxDepth, histogram);
                }
            }
        }

        public int IndexOf(double x, double y, double precision = 1e-10)
        {
            List<Point> found = Query(x, y, precision);
            if (found.Count > 0)
            {
                return found[0].Index;
            }
            return -1;
        }

        public bool Contains(double x, double y, double precision = 1e-10)
        {
            return Query(x, y, precision).Count > 0;
        }

        public void Insert(double x, double y)
        {
            var item = new Point(_nextIndex++, x, y);
            root.Insert(item);
        }

        public List<Point> Query(double x, double y, double eps)
        {
            List<Point> results = new List<Point>();
            int steps = 0;
            root.Query(x, y, results, ref steps, eps);
            return results;
        }

        public List<Point> Query(Rect area, double eps = 1e-10)
        {
            List<Point> results = new List<Point>();
            int steps = 0;
            root.Query(area, results, ref steps, eps);
            return results;
        }

        public QuadtreeDebugInfo GetDebugInfo()
        {
            int totalNodes = 0;
            int totalElements = 0;
            int maxDepth = 0;
            var histogram = new Dictionary<int, int>();

            root.CollectDebugInfo(ref totalNodes, ref totalElements, ref maxDepth, histogram);

            return new QuadtreeDebugInfo
            {
                TotalNodes = totalNodes,
                TotalElements = totalElements,
                MaxDepthReached = maxDepth,
                ElementCountHistogram = histogram
            };
        }
    }


    public struct QuadtreeDebugInfo
    {
        public int TotalElements;
        public int TotalNodes;
        public int MaxDepthReached;
        public Dictionary<int, int> ElementCountHistogram;

        public override string ToString()
        {
            var sb = new StringBuilder(256);
            sb.AppendLine($"Total Elements: {TotalElements}");
            sb.AppendLine($"Total Nodes: {TotalNodes}");
            sb.AppendLine($"Max Depth Reached: {MaxDepthReached}");
            sb.AppendLine("Elements per Node:");
            foreach (var kv in ElementCountHistogram.OrderBy(kv => kv.Key))
                sb.AppendLine($"  {kv.Key} elements: {kv.Value} nodes");
            return sb.ToString();
        }
    }


}
