using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ArknightsMapViewer
{
    public class SpawnView
    {
        public TreeNode SpawnsNode;
        public List<TreeNode> SpawnNodesList = new List<TreeNode>();
        public Dictionary<string, List<TreeNode>> RandomSpawnGroupNodesDict = new Dictionary<string, List<TreeNode>>();
        public Dictionary<string, List<TreeNode>> RandomSpawnGroupPackNodesDict = new Dictionary<string, List<TreeNode>>();

        public bool NeedUpdateNodes { get; private set; }

        public bool ShowPredefinedNodes;
        public bool HideInvalidNodes;
        public Dictionary<string, bool> HiddenGroups = new Dictionary<string, bool>();
        public Dictionary<TreeNode, bool> ValidSpawnNodes = new Dictionary<TreeNode, bool>();

        public void UpdateNodes()
        {
            TreeNode selectedNode = SpawnsNode.TreeView?.SelectedNode;
            NeedUpdateNodes = false;
            SpawnsNode.Nodes.Clear();
            foreach (TreeNode treeNode in SpawnNodesList)
            {
                if (treeNode.Tag is ISpawnAction spawnAction)
                {
                    if (!ShowPredefinedNodes && spawnAction is PredefineView)
                    {
                        continue;
                    }

                    if (HideInvalidNodes && ValidSpawnNodes.TryGetValue(treeNode, out bool isValid) && !isValid)
                    {
                        continue;
                    }

                    string groupName = spawnAction.HiddenGroup;
                    if (string.IsNullOrEmpty(groupName))
                    {
                        treeNode.Text = $"#{SpawnsNode.Nodes.Count} {spawnAction.ToSimpleString()}";
                        SpawnsNode.Nodes.Add(treeNode);
                        continue;
                    }

                    if (!string.IsNullOrEmpty(groupName) && HiddenGroups.ContainsKey(groupName) && HiddenGroups[groupName])
                    {
                        treeNode.Text = $"#{SpawnsNode.Nodes.Count} {spawnAction.ToSimpleString()}";
                        SpawnsNode.Nodes.Add(treeNode);
                    }
                }
            }
            if (selectedNode != null && selectedNode.TreeView != null)
            {
                SpawnsNode.TreeView.SelectedNode = selectedNode;
            }
        }

        public void UpdateRandomSpawnGroupNodes(TreeNode selectedNode)
        {
            //互斥分组
            if (selectedNode?.Tag is ISpawnAction spawnAction)
            {
                string randomSpawnGroupKey = spawnAction.RandomSpawnGroupKey;
                if (!string.IsNullOrEmpty(spawnAction.RandomSpawnGroupPackKey) &&
                    RandomSpawnGroupPackNodesDict.TryGetValue(spawnAction.RandomSpawnGroupPackKey, out var treeNodes))
                {
                    foreach (TreeNode treeNode in treeNodes)
                    {
                        if (treeNode.Tag is ISpawnAction curSpawnAction)
                        {
                            if (string.IsNullOrEmpty(randomSpawnGroupKey) && !string.IsNullOrEmpty(curSpawnAction.RandomSpawnGroupKey))
                            {
                                randomSpawnGroupKey = curSpawnAction.RandomSpawnGroupKey;
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(randomSpawnGroupKey) && RandomSpawnGroupNodesDict.TryGetValue(randomSpawnGroupKey, out var nodeList))
                {
                    foreach (TreeNode treeNode in nodeList)
                    {
                        if (treeNode.Tag is ISpawnAction curSpawnAction)
                        {
                            bool isValid;
                            if (spawnAction != curSpawnAction && (string.IsNullOrEmpty(curSpawnAction.RandomSpawnGroupPackKey) ||
                                spawnAction.RandomSpawnGroupPackKey != curSpawnAction.RandomSpawnGroupPackKey))
                            {
                                isValid = false;
                            }
                            else
                            {
                                isValid = true;
                            }

                            if (ValidSpawnNodes.ContainsKey(treeNode) && ValidSpawnNodes[treeNode] != isValid)
                            {
                                ValidSpawnNodes[treeNode] = isValid;
                                NeedUpdateNodes = true;
                            }

                            if (!string.IsNullOrEmpty(curSpawnAction.RandomSpawnGroupPackKey) &&
                                RandomSpawnGroupPackNodesDict.TryGetValue(curSpawnAction.RandomSpawnGroupPackKey, out var packTreeNodes))
                            {
                                foreach (TreeNode packTreeNode in packTreeNodes)
                                {
                                    if (ValidSpawnNodes.ContainsKey(packTreeNode) && ValidSpawnNodes[packTreeNode] != isValid)
                                    {
                                        ValidSpawnNodes[packTreeNode] = isValid;
                                        NeedUpdateNodes = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void UpdateSpawnViewNodeColor()
        {
            foreach (var item in ValidSpawnNodes)
            {
                item.Key.ForeColor = item.Value ? Color.FromKnownColor(KnownColor.WindowText) : Color.Gray;
            }
        }
    }
}
