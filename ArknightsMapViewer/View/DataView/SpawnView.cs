using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ArknightsMapViewer
{
    public class SpawnView
    {
        public TreeNode SpawnsNode;
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
                if (node.Tag is EnemySpawnView enemySpawnView)
                {
                    if (string.IsNullOrEmpty(enemySpawnView.HiddenGroup) &&
                        string.IsNullOrEmpty(enemySpawnView.RandomSpawnGroupKey) &&
                        string.IsNullOrEmpty(enemySpawnView.RandomSpawnGroupPackKey))
                    {
                        SpawnsNode.Nodes.Add(node);
                        node.Text = $"#{node.Index} {enemySpawnView.ToSimpleString()}";
                        continue;
                    }

                    if (CheckShowNode(enemySpawnView.HiddenGroup) ||
                        CheckShowNode(enemySpawnView.RandomSpawnGroupKey) ||
                        CheckShowNode(enemySpawnView.RandomSpawnGroupPackKey))
                    {
                        SpawnsNode.Nodes.Add(node);
                        node.Text = $"#{node.Index} {enemySpawnView.ToSimpleString()}";
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
