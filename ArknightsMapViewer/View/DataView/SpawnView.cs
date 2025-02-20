using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ArknightsMapViewer
{
    public class SpawnView
    {
        public TreeNode SpawnsNode;
        public bool ShowPredefined;
        public Dictionary<string, bool> SpawnGroups = new Dictionary<string, bool>();

        private List<TreeNode> originNodes;

        public void UpdateNodes()
        {
            if (originNodes == null)
            {
                originNodes = new List<TreeNode>();
                foreach (TreeNode node in SpawnsNode.Nodes)
                {
                    originNodes.Add(node);
                }
            }

            SpawnsNode.Nodes.Clear();
            foreach (TreeNode node in originNodes)
            {
                if (node.Tag is ISpawnAction spawnAction)
                {
                    if (spawnAction is PredefineView && !ShowPredefined)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(spawnAction.HiddenGroup) &&
                        string.IsNullOrEmpty(spawnAction.RandomSpawnGroupKey) &&
                        string.IsNullOrEmpty(spawnAction.RandomSpawnGroupPackKey))
                    {
                        SpawnsNode.Nodes.Add(node);
                        node.Text = $"#{node.Index} {spawnAction.ToSimpleString()}";
                        continue;
                    }

                    if (CheckShowNode(spawnAction.HiddenGroup) ||
                        CheckShowNode(spawnAction.RandomSpawnGroupKey) ||
                        CheckShowNode(spawnAction.RandomSpawnGroupPackKey))
                    {
                        SpawnsNode.Nodes.Add(node);
                        node.Text = $"#{node.Index} {spawnAction.ToSimpleString()}";
                    }
                }
            }
        }

        private bool CheckShowNode(string groupName)
        {
            return !string.IsNullOrEmpty(groupName) && SpawnGroups.ContainsKey(groupName) && SpawnGroups[groupName];
        }
    }
}
