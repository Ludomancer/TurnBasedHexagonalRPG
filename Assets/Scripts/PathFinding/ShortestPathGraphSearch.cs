using System;
using System.Collections.Generic;

/// <summary>
/// Based on uniform-cost-search/A* from the book
/// Artificial Intelligence: A Modern Approach 3rd Ed by Russell/Norvig
/// </summary>
public class ShortestPathGraphSearch<TState, TAction>
{
    #region Fields

    private readonly IShortestPath<TState, TAction> _info;

    #endregion

    #region Other Members

    public ShortestPathGraphSearch(IShortestPath<TState, TAction> info)
    {
        _info = info;
    }

    public List<TAction> GetShortestPath(TState fromState, TState toState)
    {
        PriorityQueue<float, SearchNode<TState, TAction>> frontier =
            new PriorityQueue<float, SearchNode<TState, TAction>>();
        HashSet<TState> exploredSet = new HashSet<TState>();
        Dictionary<TState, SearchNode<TState, TAction>> frontierMap =
            new Dictionary<TState, SearchNode<TState, TAction>>();
        SearchNode<TState, TAction> startNode = new SearchNode<TState, TAction>(null, 0, 0, fromState,
            _info.DefaultValue());
        frontier.Enqueue(startNode, 0);
        frontierMap.Add(fromState, startNode);
        while (true)
        {
            if (frontier.IsEmpty) return null;
            SearchNode<TState, TAction> node = frontier.Dequeue();
            if (_info.Comparison(node.state, toState)) return BuildSolution(node);
            exploredSet.Add(node.state);
            // expand node and add to frontier
            foreach (TAction action in _info.Expand(node.state))
            {
                TState child = _info.ApplyAction(node.state, action);
                SearchNode<TState, TAction> frontierNode = null;
                bool isNodeInFrontier = frontierMap.TryGetValue(child, out frontierNode);
                if (!exploredSet.Contains(child) && !isNodeInFrontier)
                {
                    SearchNode<TState, TAction> searchNode = CreateSearchNode(node, action, child, toState);
                    frontier.Enqueue(searchNode, searchNode.f);
                    exploredSet.Add(child);
                }
                else if (isNodeInFrontier)
                {
                    SearchNode<TState, TAction> searchNode = CreateSearchNode(node, action, child, toState);
                    if (frontierNode.f > searchNode.f)
                    {
                        frontier.Replace(frontierNode, frontierNode.f, searchNode.f);
                    }
                }
            }
        }
    }

    private SearchNode<TState, TAction> CreateSearchNode(SearchNode<TState, TAction> node, TAction action, TState child,
        TState toState)
    {
        float cost = _info.ActualCost(node.state, action);
        float heuristic = _info.Heuristic(child, toState);
        return new SearchNode<TState, TAction>(node, node.g + cost, node.g + cost + heuristic, child, action);
    }

    private List<TAction> BuildSolution(SearchNode<TState, TAction> seachNode)
    {
        List<TAction> list = new List<TAction>();
        while (seachNode != null)
        {
            if (seachNode.action != null && !seachNode.action.Equals(_info.DefaultValue()))
            {
                list.Insert(0, seachNode.action);
            }
            seachNode = seachNode.parent;
        }
        return list;
    }

    #endregion

    #region Nested type: SearchNode

    private class SearchNode<TState2, TAction2> : IComparable<SearchNode<TState2, TAction2>>
    {
        #region Fields

        public readonly TAction2 action;
        public readonly float f; // estimate
        public readonly float g; // cost
        public readonly SearchNode<TState2, TAction2> parent;
        public readonly TState2 state;

        #endregion

        #region Other Members

        public SearchNode(SearchNode<TState2, TAction2> parent, float g, float f, TState2 state, TAction2 action)
        {
            this.parent = parent;
            this.g = g;
            this.f = f;
            this.state = state;
            this.action = action;
        }

        public override string ToString()
        {
            return "SN {f:" + f + ", state: " + state + " action: " + action + "}";
        }

        #endregion

        #region IComparable<ShortestPathGraphSearch<TState,TAction>.SearchNode<TState2,TAction2>> Members

        // Reverse sort order (smallest numbers first)
        public int CompareTo(SearchNode<TState2, TAction2> other)
        {
            return other.f.CompareTo(f);
        }

        #endregion
    }

    #endregion
}