﻿using Advanced.Algorithms.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Advanced.Algorithms.DataStructures
{
    /// <summary>
    ///     Minimum bounded rectangle (MBR).
    /// </summary>
    internal class MBRectangle : Rectangle
    {
        public MBRectangle() { }
        public MBRectangle(Rectangle rectangle)
        {
            LeftTopCorner = rectangle.LeftTopCorner;
            RightBottomCorner = rectangle.RightBottomCorner;
        }
        /// <summary>
        ///     The actual polygon if this MBR is a leaf.
        /// </summary>
        internal Polygon Polygon { get; set; }

        /// <summary>
        ///     returns the required enlargement area to fit the given rectangle inside this minimum bounded rectangle.
        /// </summary>
        /// <param name="polygonToFit">The rectangle to fit inside current MBR.</param>
        internal double GetEnlargementArea(MBRectangle rectangleToFit)
        {
            return Math.Abs(getMergedRectangle(rectangleToFit).Area() - Area());
        }

        /// <summary>
        ///     set current rectangle with the merge of given rectangle.
        /// </summary>
        internal void Merge(MBRectangle rectangleToMerge)
        {
            var merged = getMergedRectangle(rectangleToMerge);

            LeftTopCorner = merged.LeftTopCorner;
            RightBottomCorner = merged.RightBottomCorner;
        }

        /// <summary>
        ///     Merge the current rectangle with given rectangle. 
        /// </summary>
        /// <param name="rectangleToMerge">The new rectangle.</param>
        private Rectangle getMergedRectangle(MBRectangle rectangleToMerge)
        {
            var mergedRectangle = new MBRectangle();

            mergedRectangle.LeftTopCorner = new Point(LeftTopCorner.X > rectangleToMerge.LeftTopCorner.X ? rectangleToMerge.LeftTopCorner.X : LeftTopCorner.X,
             LeftTopCorner.Y < rectangleToMerge.LeftTopCorner.Y ? rectangleToMerge.LeftTopCorner.Y : LeftTopCorner.Y);

            mergedRectangle.RightBottomCorner = new Point(RightBottomCorner.X < rectangleToMerge.RightBottomCorner.X ? rectangleToMerge.RightBottomCorner.X : RightBottomCorner.X,
                RightBottomCorner.Y > rectangleToMerge.RightBottomCorner.Y ? rectangleToMerge.RightBottomCorner.Y : RightBottomCorner.Y);

            return mergedRectangle;
        }
       
    }

    internal class RTreeNode
    {
        /// <summary>
        /// Array Index of this node in parent's Children array
        /// </summary>
        internal int Index;

        internal MBRectangle MBRectangle { get; set; }
        internal int KeyCount;

        internal RTreeNode Parent { get; set; }
        internal RTreeNode[] Children { get; set; }

        //leafs will hold the actual polygon
        internal bool IsLeaf => Children[0] == null || Children[0].MBRectangle.Polygon != null;

        internal RTreeNode(int maxKeysPerNode, RTreeNode parent)
        {
            Parent = parent;
            Children = new RTreeNode[maxKeysPerNode];
        }

        internal void AddChild(RTreeNode child)
        {
            if (KeyCount == Children.Length)
            {
                throw new Exception("No space to add child.");
            }

            SetChild(KeyCount, child);
            KeyCount++;
        }

        /// <summary>
        ///     Set the child at specifid index.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="child"></param>
        internal void SetChild(int index, RTreeNode child)
        {
            Children[index] = child;
            Children[index].Parent = this;
            Children[index].Index = index;

            if(MBRectangle == null)
            {
                MBRectangle = new MBRectangle(child.MBRectangle);
            }
            else
            {
                MBRectangle.Merge(child.MBRectangle);
            }      
        }

        /// <summary>
        /// Select the node whose MBR will require the minimum area enlargement
        /// to cover the new polygon to insert.
        /// </summary>
        /// <param name="newPolygon"></param>
        /// <returns></returns>
        internal RTreeNode GetMinimumEnlargementAreaMBR(MBRectangle newPolygon)
        {
            if(Children.Length == 0)
            {
                throw new Exception("Empty node.");
            }

            //order by enlargement area
            //then by minimum area
            return Children[Children.Take(KeyCount)
                              .Select((node, index) => new { node, index })
                              .OrderBy(x => x.node.MBRectangle.GetEnlargementArea(newPolygon))
                              .ThenBy(x => x.node.MBRectangle.Area())
                              .First().index];
        }

    }

    /// <summary>
    /// A RTree implementation
    /// TODO support initial  bulk loading
    /// TODO: make sure duplicates are handled correctly if its not already
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RTree
    {
        private readonly int maxKeysPerNode;
        private readonly int minKeysPerNode;

        public int Count { get; private set; }

        internal RTreeNode Root;

        public RTree(int maxKeysPerNode)
        {
            if (maxKeysPerNode < 3)
            {
                throw new Exception("Max keys per node should be atleast 3.");
            }

            this.maxKeysPerNode = maxKeysPerNode;
            this.minKeysPerNode = maxKeysPerNode / 2;
        }

        /// <summary>
        /// Inserts  to R-Tree
        /// </summary>
        /// <param name="newPolygon"></param>
        public void Insert(Polygon newPolygon)
        {
            var newNode = new RTreeNode(maxKeysPerNode, null)
            {
                MBRectangle = newPolygon.GetContainingRectangle()
            };

            if (Root == null)
            {
                Root = new RTreeNode(maxKeysPerNode, null);
                Root.AddChild(newNode);
                Count++;
                return;
            }

            var leafToInsert = findInsertionLeaf(Root, newNode);

            insertAndSplit(ref leafToInsert, newNode);
            Count++;
        }


        /// <summary>
        ///     Find the leaf node to start initial insertion
        /// </summary>
        /// <param name="node"></param>
        /// <param name="newPolygon"></param>
        /// <returns></returns>
        private RTreeNode findInsertionLeaf(RTreeNode node, RTreeNode newNode)
        {
            //if leaf then its time to insert
            if (node.IsLeaf)
            {
                return node;
            }

            return findInsertionLeaf(node.GetMinimumEnlargementAreaMBR(newNode.MBRectangle), newNode);
        }

        /// <summary>
        ///     Insert and split recursively up until no split is required
        /// </summary>
        /// <param name="node"></param>
        /// <param name="newValue"></param>
        private void insertAndSplit(ref RTreeNode node, RTreeNode newValue)
        {
            //newValue have room to fit in this node
            if (node.KeyCount < maxKeysPerNode)
            {
                node.AddChild(newValue);
                return;
            }

            var e = new List<RTreeNode>(new RTreeNode[] { newValue });
            e.AddRange(node.Children);

            var distantPairs = getDistantPairs(e);

            //Let E be the set consisting of all current entries and new entry.
            //Select as seeds two entries e1, e2 ∈ E, where the distance between
            //left and right is the maximum among all other pairs of entries from E
            var e1 = new RTreeNode(maxKeysPerNode, null);
            var e2 = new RTreeNode(maxKeysPerNode, null);

            e1.AddChild(distantPairs.Item1);
            e2.AddChild(distantPairs.Item2);

            e = e.Where(x => x != distantPairs.Item1 && x != distantPairs.Item2)
                             .ToList();

            /*Examine the remaining members of E one by one and assign them
            to e1 or e2, depending on which of the MBRs of these nodes
            will require the minimum area enlargement so as to cover this entry.
            If a tie occurs, assign the entry to the node whose MBR has the smaller area.
            If a tie occurs again, assign the entry to the node that contains the smaller number of entries*/
            while (e.Count > 0)
            {
                var current = e[e.Count - 1];

                var leftEnlargementArea = e1.MBRectangle.GetEnlargementArea(current.MBRectangle);
                var rightEnlargementArea = e2.MBRectangle.GetEnlargementArea(current.MBRectangle);

                if (leftEnlargementArea == rightEnlargementArea)
                {
                    var leftArea = e1.MBRectangle.Area();
                    var rightArea = e2.MBRectangle.Area();

                    if (leftArea == rightArea)
                    {
                        if (e1.KeyCount < e2.KeyCount)
                        {
                            e1.AddChild(current);
                        }
                        else
                        {
                            e2.AddChild(current);
                        }
                    }
                    else if (leftArea < rightArea)
                    {
                        e1.AddChild(current);
                    }
                    else
                    {
                        e2.AddChild(current);
                    }
                }
                else if (leftEnlargementArea < rightEnlargementArea)
                {
                    e1.AddChild(current);
                }
                else
                {
                    e2.AddChild(current);
                }

                e.RemoveAt(e.Count - 1);

                var remaining = e.Count;

                /*if during the assignment of entries, there remain λ entries to be assigned
                and the one node contains minKeysPerNode − λ entries then
                assign all the remaining entries to this node without considering
                the aforementioned criteria
                so that the node will contain at least minKeysPerNode entries */
                if (e1.KeyCount == minKeysPerNode - remaining)
                {
                    foreach (var entry in e)
                    {
                        e1.AddChild(entry);
                    }
                    e.Clear();
                }
                else if (e2.KeyCount == minKeysPerNode - remaining)
                {
                    foreach (var entry in e)
                    {
                        e2.AddChild(entry);
                    }
                    e.Clear();
                }
            }

            //insert overflow element to parent
            var parent = node.Parent;
            if (parent != null)
            {
                //replace current node with e1
                parent.SetChild(node.Index, e1);
                //insert e2
                insertAndSplit(ref parent, e2);
            }
            else
            {
                //node is the root.
                //increase the height of RTree by one by adding a new root.
                Root = new RTreeNode(maxKeysPerNode, null);
                Root.AddChild(e1);
                Root.AddChild(e2);
            }

        }

        /// <summary>
        ///     Get the pairs of rectangles farther apart by comparing enlargement areas.
        /// </summary>
        /// <param name="allEntries"></param>
        /// <returns></returns>
        private Tuple<RTreeNode, RTreeNode> getDistantPairs(List<RTreeNode> allEntries)
        {
            Tuple<RTreeNode, RTreeNode> result = null;

            double maxArea = Double.MinValue;
            for (int i = 0; i < allEntries.Count; i++)
            {
                for (int j = i + 1; j < allEntries.Count; j++)
                {
                    var currentArea = allEntries[i].MBRectangle.GetEnlargementArea(allEntries[j].MBRectangle);
                    if (currentArea > maxArea)
                    {
                        result = new Tuple<RTreeNode, RTreeNode>(allEntries[i], allEntries[j]);
                        maxArea = currentArea;
                    }
                }
            }

            return result;
        }

    }

    internal static class PolygonExtensions
    {
        /// <summary>
        ///     Gets the imaginary rectangle that contains the polygon.
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        internal static MBRectangle GetContainingRectangle(this Polygon polygon)
        {
            var x = polygon.Edges.SelectMany(z => new double[] { z.Start.X, z.End.X })
                .Aggregate(new
                {
                    Max = double.MinValue,
                    Min = double.MaxValue
                }, (accumulator, o) => new
                {
                    Max = Math.Max(o, accumulator.Max),
                    Min = Math.Min(o, accumulator.Min),
                });


            var y = polygon.Edges.SelectMany(z => new double[] { z.Start.Y, z.End.Y })
                   .Aggregate(new
                   {
                       Max = double.MinValue,
                       Min = double.MaxValue
                   }, (accumulator, o) => new
                   {
                       Max = Math.Max(o, accumulator.Max),
                       Min = Math.Min(o, accumulator.Min),
                   });

            return new MBRectangle()
            {
                LeftTopCorner = new Point(x.Min, y.Max),
                RightBottomCorner = new Point(x.Max, y.Min),
                Polygon = polygon
            };
        }
    }

}