/* Copyright (c) 2008 Google Inc. All Rights Reserved
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace Google.GCalExchangeSync.Library.Util
{
    /// <summary>
    /// Implementation of a node in an interval tree
    /// </summary>
    class IntervalNode<U>
    {
        private readonly DateTimeRange interval;
        private DateTime max;
        private IntervalNode<U> left;
        private IntervalNode<U> right;
        private U data;

        public U Data
        {
            get { return data; }
        }

        public DateTimeRange Interval
        {
            get { return interval; }
        }

        public DateTime Max
        {
            get { return max; }
            set { max = value; }
        }

        public IntervalNode<U> Left
        {
            get { return left; }
            set { left = value; }
        }

        public IntervalNode<U> Right
        {
            get { return right; }
            set { right = value; }
        }

        public IntervalNode(DateTimeRange interval, U data)
        {
            this.interval = interval;
            this.max = this.interval.End;
            this.data = data;
        }

        public override string ToString()
        {
            return string.Format("Node: {0}", Interval);
        }
    }

    /// <summary>
    /// Type of IntervalTree matching to do
    /// </summary>
    public enum IntervalTreeMatch
    {
        /// <summary>
        /// Exact - start and end times must match the node
        /// </summary>
        Exact,

        /// <summary>
        /// Overlap - start or end time must overlap the node
        /// </summary>
        Overlap,

        /// <summary>
        /// Contained - start and end times must be contained.
        /// </summary>
        Contained,

        /// <summary>
        /// ContainedBy - start and end times must contain the node
        /// </summary>
        ContainedBy
    };

    /// <summary>
    /// Implementation of an interval tree that stores a set of datetime ranges
    /// and can return exact mathes or overlapping / contained intervals
    /// </summary>
    public class IntervalTree<T>
    {
        // TODO: Implement using a Red Black tree
        private IntervalNode<T> root = null;
        private int numNodes = 0;
        private int maxDepth = 0;
        
        /// <summary>
        /// Number of nodes in the tree
        /// </summary>
        public int NumNodes
        {
            get { return numNodes; }
        }

        /// <summary>
        /// Maximum depth of the tree
        /// </summary>
        public int MaxDepth
        {
            get { return maxDepth; }
        }

        /// <summary>
        /// Insert a new node int the tree
        /// </summary>
        /// <param name="range">Interval to insert</param>
        /// <param name="data">The node data</param>
        public void Insert(DateTimeRange range, T data)
        {
            Insert(new IntervalNode<T>(range, data));
        }

        private void Insert(IntervalNode<T> node)
        {
            InsertNode(root, node, 1);
            if (root == null)
            {
                root = node;
                numNodes = 1;
                maxDepth = 1;
            }
        }

        private void InsertNode(IntervalNode<T> parent, IntervalNode<T> node, int depth)
        {
            if (parent == null)
                return;

            if (parent.Max <= node.Max)
                parent.Max = node.Max;

            if (maxDepth < depth)
                maxDepth = depth;

            if(node.Interval.Start < parent.Interval.Start)
            {
                if (parent.Left == null)
                {
                    parent.Left = node;
                    numNodes++;
                }
                else
                {
                    InsertNode(parent.Left, node, depth + 1);
                }

            }
            else 
            {
                if (parent.Right == null)
                {
                    parent.Right = node;
                    numNodes++;
                }
                else
                {
                    InsertNode(parent.Right, node, depth + 1);
                }
            }
        }

        /// <summary>
        /// Find all the events contained within the date range
        /// </summary>
        /// <param name="range">Date range to return events in</param>
        /// <returns>All the events contained within the date range</returns>
        public List<T> FindAll(DateTimeRange range)
        {
            return FindAll(range, IntervalTreeMatch.ContainedBy);
        }
        
        /// <summary>
        /// Find all events for the range based on the match criteria
        /// - Exact: Return only elements with the same start / end time
        /// - Contained: Return only elements within the range
        /// - Overlap: Return only elements that overlap the range
        /// </summary>
        /// <param name="range">The event range to match</param>
        /// <param name="match">Type of match to do</param>
        /// <returns>The set of all matching elements</returns>
        public List<T> FindAll(DateTimeRange range, IntervalTreeMatch match)
        {
            List<T> result = new List<T>();

            FindAll(root, range, match, result);

            return result;
        }

        private void FindAll(IntervalNode<T> node, DateTimeRange range, IntervalTreeMatch match, List<T> result)
        {
            if (node == null)
            {
                return;
            }

            if (match == IntervalTreeMatch.Contained && range.Contains(node.Interval))
            {
                result.Add(node.Data);
            }
            else if (match == IntervalTreeMatch.ContainedBy && node.Interval.Contains(range))
            {
                result.Add(node.Data);
            }
            else if (match == IntervalTreeMatch.Overlap && range.Overlaps(node.Interval))
            {
                result.Add(node.Data);
            }
            else if (match == IntervalTreeMatch.Exact && range.Equals(node.Interval))
            {
                result.Add(node.Data);
            }

            if (node.Left != null && range.Start < node.Interval.Start && range.Start < node.Left.Max)
            {
                FindAll(node.Left, range, match, result);
            }
            
            if(node.Right != null && range.End >= node.Interval.Start && range.Start <= node.Right.Max)
            {
                FindAll(node.Right, range, match, result);
            }
        }

        /// <summary>
        /// Find an exact match for the date time range - the search ends after the first match
        /// </summary>
        /// <param name="range">The datetime range to find</param>
        /// <returns>The first node matching the range, or null if there is no match</returns>
        public T FindExact(DateTimeRange range)
        {
            IntervalNode<T> node = root;

            while (node != null)
            {
                if (node.Interval.Equals(range))
                {
                    return node.Data;
                }

                if (node.Left != null && range.Start < node.Interval.Start && range.Start < node.Left.Max)
                {
                    node = node.Left;
                }
                else if (node.Right != null && range.End >= node.Interval.Start && range.Start <= node.Right.Max)
                {
                    node = node.Right;
                }
                else
                {
                    node = null;
                }
            }

            return default(T);
        }

        /// <summary>
        /// Get the set of all nodes in the tree from an in-order walk
        /// </summary>
        /// <returns>List of nodes</returns>
        public List<T> GetNodeList()
        {
            List<T> result = new List<T>();
            GetNodeList(root, result);
            return result;
        }

        private void GetNodeList(IntervalNode<T> node, List<T> result)
        {
            if(node == null)
            {
                return;
            }
            GetNodeList(node.Left, result);
            result.Add(node.Data);
            GetNodeList(node.Right, result);
        }
    }
}
