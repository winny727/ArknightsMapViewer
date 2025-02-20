using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ArknightsMapViewer
{
    public class SpawnView
    {
        public TreeNode SpawnsNode;
        public List<TreeNode> SpawnNodesList = new List<TreeNode>();

        public bool ShowPredefined;
        public Dictionary<string, bool> SpawnGroups = new Dictionary<string, bool>();

        public void UpdateNodes()
        {
            SpawnsNode.Nodes.Clear();
            foreach (TreeNode node in SpawnNodesList)
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
                        node.Text = $"#{SpawnsNode.Nodes.Count} {spawnAction.ToSimpleString()}";
                        SpawnsNode.Nodes.Add(node);
                        continue;
                    }

                    if (CheckShowNode(spawnAction.HiddenGroup) ||
                        CheckShowNode(spawnAction.RandomSpawnGroupKey) ||
                        CheckShowNode(spawnAction.RandomSpawnGroupPackKey))
                    {
                        node.Text = $"#{SpawnsNode.Nodes.Count} {spawnAction.ToSimpleString()}";
                        SpawnsNode.Nodes.Add(node);
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
