using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    public class PointQuadtree<T>
    {
        readonly Node root;
        readonly List<PointItem> _globalItems = new List<PointItem>();
        int _nextIndex = 0;

        public PointQuadtree(Rect bounds, int maxDepth = 10, int maxItems = 8)
        {
            root = new Node(bounds, 0, maxDepth, maxItems, _globalItems);
        }

        public List<PointItem> Items => _globalItems;

        public class PointItem
        {
            public double X { get; }
            public double Y { get; }
            public T Value { get; }
            public int Index { get; }

            public PointItem(double x, double y, T value, int index)
            {
                X = x;
                Y = y;
                Value = value;
                Index = index;
            }
        }

        private class Node
        {
            public Rect Bounds;
            public List<PointItem> Items;
            public Node[]? Children;
            public int Depth;
            readonly int MaxDepth;
            readonly int MaxItems;
            readonly List<PointItem> Global;

            public Node(Rect bounds, int depth, int maxDepth, int maxItems, List<PointItem> globalList)
            {
                Bounds = bounds;
                Depth = depth;
                MaxDepth = maxDepth;
                MaxItems = maxItems;
                Global = globalList;
                Items = new List<PointItem>();
            }

            public void Insert(PointItem item)
            {
                if (!Bounds.Contains(item.X, item.Y)) return;

                if (Children == null)
                {
                    Items.Add(item);
                    Global.Add(item);

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
                    GetChild(item.X, item.Y).Insert(item);
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

            Node GetChild(double x, double y)
            {
                double cx = (Bounds.minX + Bounds.maxX) * 0.5;
                double cy = (Bounds.minY + Bounds.maxY) * 0.5;
                if (y < cy)
                    return x < cx ? Children[0] : Children[1];
                else
                    return x < cx ? Children[2] : Children[3];
            }

            public void Query(double x, double y, List<T> results)
            {
                if (!Bounds.Contains(x, y)) return;

                foreach (PointItem item in Items)
                    if (Math.Abs(item.X - x) < 1e-10 && Math.Abs(item.Y - y) < 1e-10)
                        results.Add(item.Value);

                if (Children != null)
                    GetChild(x, y).Query(x, y, results);
            }

            public void Query(Rect area, List<T> results)
            {
                if (!Bounds.Intersects(area)) return;

                foreach (var item in Items)
                    if (area.Contains(item.X, item.Y))
                        results.Add(item.Value);

                if (Children != null)
                    foreach (var child in Children)
                        child.Query(area, results);
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

        public int AddIfUnique(double x, double y, T item, double precision = 1e-10)
        {
            foreach (var existing in _globalItems)
            {
                if (Math.Abs(existing.X - x) <= precision && Math.Abs(existing.Y - y) <= precision)
                {
                    return existing.Index;
                }
            }

            var point = new PointItem(x, y, item, _nextIndex++);
            root.Insert(point);
            return point.Index;
        }

        public void Insert(double x, double y, T item)
        {
            var point = new PointItem(x, y, item, _nextIndex++);
            root.Insert(point);
        }

        public List<T> Query(double x, double y)
        {
            var results = new List<T>();
            root.Query(x, y, results);
            return results;
        }

        public List<T> Query(Rect area)
        {
            var results = new List<T>();
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
