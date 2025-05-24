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
        readonly List<Point> _globalItems = new List<Point>();
        int _nextIndex = 0;

        public PointQuadtree(Rect bounds, int maxDepth = 10, int maxItems = 8)
        {
            root = new Node(bounds, 0, maxDepth, maxItems, _globalItems);
        }

        public List<Point> Points => _globalItems;

        public class Point
        {
            public Vec2 Value { get; }
            public int Index { get; }

            public Point(Vec2 value, int index)
            {
                Value = value;
                Index = index;
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
            private readonly List<Point> Global;

            public Node(Rect bounds, int depth, int maxDepth, int maxItems, List<Point> globalList)
            {
                Bounds = bounds;
                Depth = depth;
                MaxDepth = maxDepth;
                MaxItems = maxItems;
                Global = globalList;
                Items = new List<Point>();
            }

            public void Insert(Point item)
            {
                Vec2 v = item.Value;
                if (!Bounds.Contains(v.x, v.y)) return;

                if (Children == null)
                {
                    Items.Add(item);
                    Global.Add(item);

                    if (Items.Count > MaxItems && Depth < MaxDepth)
                    {
                        Subdivide();
                        foreach (var i in Items)
                            GetChild(i.Value.x, i.Value.y).Items.Add(i);
                        Items.Clear();
                    }
                }
                else
                {
                    GetChild(v.x, v.y).Insert(item);
                }
            }

            private void Subdivide()
            {
                double cx = (Bounds.minX + Bounds.maxX) * 0.5;
                double cy = (Bounds.minY + Bounds.maxY) * 0.5;
                Children = new Node[4];
                Children[0] = new Node(new Rect(Bounds.minX, Bounds.minY, cx, cy), Depth + 1, MaxDepth, MaxItems, Global); // bottom-left
                Children[1] = new Node(new Rect(cx, Bounds.minY, Bounds.maxX, cy), Depth + 1, MaxDepth, MaxItems, Global); // bottom-right
                Children[2] = new Node(new Rect(Bounds.minX, cy, cx, Bounds.maxY), Depth + 1, MaxDepth, MaxItems, Global); // top-left
                Children[3] = new Node(new Rect(cx, cy, Bounds.maxX, Bounds.maxY), Depth + 1, MaxDepth, MaxItems, Global); // top-right
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

            public void Query(double x, double y, List<Point> results, double eps = 1e-10)
            {
                if (!Bounds.Contains(x, y)) return;

                foreach (var item in Items)
                    if (Math.Abs(item.Value.x - x) < eps && Math.Abs(item.Value.y - y) < eps)
                        results.Add(item);

                if (Children != null)
                    GetChild(x, y).Query(x, y, results, eps);
            }

            public void Query(Rect area, List<Vec2> results)
            {
                if (!Bounds.Intersects(area)) return;

                foreach (var item in Items)
                {
                    if (area.Contains(item.Value.x, item.Value.y))
                    {
                        results.Add(item.Value);
                    }
                       
                }

                if (Children != null)
                {
                    foreach (var child in Children)
                    {
                        child.Query(area, results);
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

        public int IndexOf(Vec2 point, double precision = 1e-10)
        {
            List<Point> found = Query(point.x, point.y, precision);
            if (found.Count > 0)
            {
                return found[0].Index;
            }
            return -1;
        }

        public bool Contains(Vec2 point, double precision = 1e-10)
        {
            return Query(point.x, point.y, precision).Count > 0;
        }

        public void Insert(Vec2 point)
        {
            var item = new Point(point, _nextIndex++);
            root.Insert(item);
        }

        public List<Point> Query(double x, double y, double eps)
        {
            List<Point> results = new List<Point>();
            root.Query(x, y, results, eps);
            return results;
        }

        public List<Vec2> Query(Rect area)
        {
            var results = new List<Vec2>();
            root.Query(area, results);
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
