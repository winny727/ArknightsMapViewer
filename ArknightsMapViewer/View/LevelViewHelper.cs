using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace ArknightsMapViewer
{
    public static class LevelViewHelper
    {
        public static TreeNode CreateRoutesNode(string name, List<Route> routes, Func<Route, RouteDrawer> routeDrawerCreater)
        {
            if (string.IsNullOrEmpty(name) || routes == null || routes.Count <= 0)
            {
                return null;
            }

            string title = name.TrimEnd('s'); // routes -> route
            TreeNode routesNode = new TreeNode(name);
            for (int i = 0; i < routes.Count; i++)
            {
                Route route = routes[i];
                TreeNode routeNode = routesNode.Nodes.Add($"{title} #{i}");
                routeNode.Tag = new RouteView()
                {
                    RouteIndex = i,
                    Route = route,
                    RouteDrawer = routeDrawerCreater?.Invoke(route),
                };
                if (route != null && route.checkPoints != null)
                {
                    routeNode.Nodes.Add($"startPosition: {route.startPosition}");
                    for (int j = 0; j < route.checkPoints.Count; j++)
                    {
                        routeNode.Nodes.Add($"checkPoint #{j} {route.checkPoints[j].ToSimpleString()}");
                    }
                    routeNode.Nodes.Add($"endPosition: {route.endPosition}");
                }
            }
            return routesNode;
        }

        public static TreeNode CreateWavesNode(
            LevelData levelData,
            Func<Route, RouteDrawer> routeDrawerCreater,
            Func<Predefine.PredefineInst, PredefineDrawer> predefineDrawerCreater,
            out List<ISpawnAction> spawnActions,
            out List<PredefineView> predefineViews,
            out Dictionary<string, int> totalWeightDict
            )
        {
            int spawnIndex = 0;
            float spawnTime = 0;
            spawnActions = new List<ISpawnAction>();
            predefineViews = new List<PredefineView>();
            totalWeightDict = new Dictionary<string, int>();

            if (levelData == null || levelData.waves == null)
            {
                return null;
            }

            TreeNode wavesNode = new TreeNode(nameof(levelData.waves));
            for (int i = 0; i < levelData.waves.Count; i++)
            {
                Wave wave = levelData.waves[i];
                TreeNode waveNode = wavesNode.Nodes.Add($"wave #{i}");
                waveNode.Tag = wave;

                int waveSpawnIndex = 0;
                spawnTime += wave.preDelay;
                for (int j = 0; j < wave.fragments.Count; j++)
                {
                    Fragment fragment = wave.fragments[j];
                    TreeNode fragmentNode = waveNode.Nodes.Add($"fragment #{j}");
                    fragmentNode.Tag = fragment;

                    float fragmentSpawnTime = 0;
                    spawnTime += fragment.preDelay;
                    for (int k = 0; k < fragment.actions.Count; k++)
                    {
                        Action action = fragment.actions[k];
                        TreeNode actionNode = fragmentNode.Nodes.Add($"#{k} {action.ToSimpleString()}");
                        if (action.actionType == ActionType.SPAWN)
                        {
                            if (levelData.routes != null && action.routeIndex >= 0 && action.routeIndex < levelData.routes.Count)
                            {
                                Route route = levelData.routes[action.routeIndex];
                                RouteDrawer routeDrawer = routeDrawerCreater?.Invoke(route);
                                actionNode.Tag = new SpawnActionView()
                                {
                                    Action = action,
                                    Drawer = routeDrawer,
                                    IsExtraRoute = false,
                                };

                                for (int n = 0; n < action.count; n++)
                                {
                                    DbData enemyData = null;
                                    levelData.enemyDbRefs?.TryGetValue(action.key, out enemyData);
                                    EnemySpawnView enemySpawnView = new EnemySpawnView()
                                    {
                                        EnemyKey = action.key,
                                        EnemyData = enemyData,
                                        SpawnTime = spawnTime + action.preDelay + n * action.interval,
                                        TotalSpawnIndex = spawnIndex,
                                        Route = route,
                                        RouteIndex = action.routeIndex,
                                        TotalWave = levelData.waves.Count,
                                        WaveIndex = i,
                                        SpawnIndexInWave = waveSpawnIndex,
                                        HiddenGroup = action.hiddenGroup,
                                        RandomSpawnGroupKey = action.randomSpawnGroupKey,
                                        RandomSpawnGroupPackKey = action.randomSpawnGroupPackKey,
                                        Weight = action.weight,
                                        BlockFragment = action.blockFragment,
                                        BlockWave = !action.dontBlockWave,
                                        MaxTimeWaitingForNextWave = wave.maxTimeWaitingForNextWave,
                                        RouteDrawer = routeDrawer,
                                    };
                                    spawnActions.Add(enemySpawnView);

                                    if (enemySpawnView.SpawnTime > fragmentSpawnTime)
                                    {
                                        fragmentSpawnTime = enemySpawnView.SpawnTime;
                                    }
                                }
                            }
                        }
                        else if (action.actionType == ActionType.ACTIVATE_PREDEFINED)
                        {
                            if (levelData.predefines != null)
                            {
                                bool isCard = false;
                                Predefine.PredefineInst predefine = null;

                                if (!string.IsNullOrEmpty(action.key))
                                {
                                    predefine =
                                        levelData.predefines.characterInsts?.Find(x => (x.alias ?? x.inst.characterKey) == action.key) ??
                                        levelData.predefines.tokenInsts?.Find(x => (x.alias ?? x.inst.characterKey) == action.key);

                                    if (predefine == null)
                                    {
                                        predefine =
                                            levelData.predefines.characterCards?.Find(x => (x.alias ?? x.inst.characterKey) == action.key) ??
                                            levelData.predefines.tokenCards?.Find(x => (x.alias ?? x.inst.characterKey) == action.key);
                                        isCard = predefine != null;
                                    }
                                }

                                PredefineDrawer predefineDrawer = predefineDrawerCreater?.Invoke(predefine);
                                actionNode.Tag = new PredefineActionView()
                                {
                                    Action = action,
                                    IsCard = isCard,
                                    Drawer = predefineDrawer,
                                };

                                //TODO 单个action内装置是否有多个？
                                CharacterData characterData = null;
                                if (predefine != null)
                                {
                                    GlobalDefine.CharacterTable.TryGetValue(predefine.inst.characterKey, out characterData);
                                }

                                PredefineView predefineView = new PredefineView()
                                {
                                    Predefine = predefine,
                                    PredefineKey = action.key,
                                    IsCard = isCard,
                                    PredefineData = characterData,
                                    ActivateTime = spawnTime + action.preDelay,
                                    TotalWave = levelData.waves.Count,
                                    WaveIndex = i,
                                    SpawnIndexInWave = waveSpawnIndex,
                                    HiddenGroup = action.hiddenGroup,
                                    RandomSpawnGroupKey = action.randomSpawnGroupKey,
                                    RandomSpawnGroupPackKey = action.randomSpawnGroupPackKey,
                                    Weight = action.weight,
                                    PredefineDrawer = predefineDrawer,
                                };
                                predefineViews.Add(predefineView);
                                spawnActions.Add(predefineView);

                                if (predefineView.ActivateTime > fragmentSpawnTime)
                                {
                                    fragmentSpawnTime = predefineView.ActivateTime;
                                }
                            }
                        }
                        else
                        {
                            actionNode.Tag = action;
                        }

                        if (!string.IsNullOrEmpty(action.randomSpawnGroupKey))
                        {
                            if (!totalWeightDict.ContainsKey(action.randomSpawnGroupKey))
                            {
                                totalWeightDict.Add(action.randomSpawnGroupKey, 0);
                            }
                            totalWeightDict[action.randomSpawnGroupKey] += action.weight;
                        }

                        waveSpawnIndex++;
                        spawnIndex++;
                    }
                    spawnTime = fragmentSpawnTime;
                }
                spawnTime += wave.postDelay;
            }
            return wavesNode;
        }

        public static TreeNode CreateBranchsNode(string name, Dictionary<string, Branch> branches, List<Route> extraRoutes,
            Func<Route, RouteDrawer> routeDrawerCreater)
        {
            if (branches == null || extraRoutes == null)
            {
                return null;
            }

            TreeNode branchesNode = new TreeNode(name);
            foreach (var item in branches)
            {
                string key = item.Key;
                List<Fragment> phases = item.Value.phases;
                TreeNode brancheNode = branchesNode.Nodes.Add(key);
                for (int i = 0; i < phases.Count; i++)
                {
                    Fragment phase = phases[i];
                    TreeNode phaseNode = brancheNode.Nodes.Add($"phase #{i}");
                    phaseNode.Tag = phase;

                    for (int j = 0; j < phase.actions.Count; j++)
                    {
                        Action action = phase.actions[j];
                        TreeNode actionNode = phaseNode.Nodes.Add($"#{j} {action.ToSimpleString()}");
                        if (action.actionType == ActionType.SPAWN && extraRoutes != null && action.routeIndex >= 0 && action.routeIndex < extraRoutes.Count)
                        {
                            Route extraRoute = extraRoutes[action.routeIndex];
                            actionNode.Tag = new SpawnActionView()
                            {
                                Action = action,
                                Drawer = routeDrawerCreater?.Invoke(extraRoute),
                                IsExtraRoute = true,
                            };
                        }
                        else
                        {
                            actionNode.Tag = action;
                        }
                    }
                }
            }
            return branchesNode;
        }

        public static TreeNode CreatePredefinesNode(string name, Predefine predefines, Func<string, PredefineView> getPredefineView,
            Func<Predefine.PredefineInst, PredefineDrawer> predefineDrawerCreater)
        {
            if (string.IsNullOrEmpty(name) || predefines == null)
            {
                return null;
            }

            TreeNode predefinesNode = new TreeNode(name);
            InsertPredefineNode(predefinesNode, predefines.characterInsts, getPredefineView, predefineDrawerCreater);
            InsertPredefineNode(predefinesNode, predefines.tokenInsts, getPredefineView, predefineDrawerCreater);
            InsertPredefineNode(predefinesNode, predefines.characterCards, getPredefineView, predefineDrawerCreater);
            InsertPredefineNode(predefinesNode, predefines.tokenCards, getPredefineView, predefineDrawerCreater);

            return predefinesNode;
        }

        private static void InsertPredefineNode(TreeNode predefinesNode, List<Predefine.PredefineInst> predefineInsts,
            Func<string, PredefineView> getPredefineView,
            Func<Predefine.PredefineInst, PredefineDrawer> predefineDrawerCreater)
        {
            if (predefineInsts == null)
            {
                return;
            }

            for (int i = 0; i < predefineInsts.Count; i++)
            {
                Predefine.PredefineInst predefine = predefineInsts[i];
                string predefineKey = predefine.alias ?? predefine.inst.characterKey;
                PredefineView predefineView = getPredefineView?.Invoke(predefineKey);
                if (predefineView != null)
                {
                    TreeNode predefineNode = predefinesNode.Nodes.Add($"#{i} {predefineView.ToSimpleString(false)}");
                    predefineNode.Tag = predefineView;
                }
                else
                {
                    TreeNode predefineNode = predefinesNode.Nodes.Add($"#{i} {predefineKey}");
                    GlobalDefine.CharacterTable.TryGetValue(predefine.inst.characterKey, out CharacterData characterData);
                    predefineNode.Tag = new PredefineView()
                    {
                        Predefine = predefine,
                        PredefineKey = predefineKey,
                        PredefineData = characterData,
                        ActivateTime = -1,
                        PredefineDrawer = predefineDrawerCreater?.Invoke(predefine),
                    };
                }
            }
        }

        public static TreeNode CreateSpawnsNode(string name, List<ISpawnAction> spawnActions, Dictionary<string, int> totalWeightDict)
        {
            if (string.IsNullOrEmpty(name) || spawnActions == null || spawnActions.Count <= 0)
            {
                return null;
            }

            TreeNode spawnsNode = new TreeNode(name);
            SpawnView spawnView = new SpawnView()
            {
                SpawnsNode = spawnsNode,
                ShowPredefinedNodes = true,
                HideInvalidNodes = false,
            };
            spawnsNode.Tag = spawnView;
            spawnActions.Sort((a, b) => a.CompareTo(b));

            Dictionary<string, TreeNode> randomSpawnGroupDefaultNodes = new Dictionary<string, TreeNode>();
            for (int i = 0; i < spawnActions.Count; i++)
            {
                ISpawnAction spawnAction = spawnActions[i];
                TreeNode spawnNode = new TreeNode
                {
                    Tag = spawnAction
                };

                if (!string.IsNullOrEmpty(spawnAction.RandomSpawnGroupKey) && totalWeightDict.TryGetValue(spawnAction.RandomSpawnGroupKey, out int totalWeight))
                {
                    spawnAction.TotalWeight = totalWeight;
                }

                if (spawnAction is EnemySpawnView enemySpawnView)
                {
                    Route route = enemySpawnView.Route;
                    if (route != null && route.checkPoints != null)
                    {
                        spawnNode.Nodes.Add($"startPosition: {route.startPosition}");
                        for (int j = 0; j < route.checkPoints.Count; j++)
                        {
                            spawnNode.Nodes.Add($"checkPoint #{j} {route.checkPoints[j].ToSimpleString()}");
                        }
                        spawnNode.Nodes.Add($"endPosition: {route.endPosition}");
                    }
                }

                if (!string.IsNullOrEmpty(spawnAction.HiddenGroup))
                {
                    if (!spawnView.HiddenGroups.ContainsKey(spawnAction.HiddenGroup))
                    {
                        spawnView.HiddenGroups.Add(spawnAction.HiddenGroup, true);
                    }
                }

                if (!string.IsNullOrEmpty(spawnAction.RandomSpawnGroupKey))
                {
                    if (!spawnView.RandomSpawnGroupNodesDict.ContainsKey(spawnAction.RandomSpawnGroupKey))
                    {
                        spawnView.RandomSpawnGroupNodesDict.Add(spawnAction.RandomSpawnGroupKey, new List<TreeNode>());
                    }
                    if (!randomSpawnGroupDefaultNodes.ContainsKey(spawnAction.RandomSpawnGroupKey))
                    {
                        randomSpawnGroupDefaultNodes.Add(spawnAction.RandomSpawnGroupKey, spawnNode);
                    }
                    spawnView.RandomSpawnGroupNodesDict[spawnAction.RandomSpawnGroupKey].Add(spawnNode);
                }

                if (!string.IsNullOrEmpty(spawnAction.RandomSpawnGroupPackKey))
                {
                    if (!spawnView.RandomSpawnGroupPackNodesDict.ContainsKey(spawnAction.RandomSpawnGroupPackKey))
                    {
                        spawnView.RandomSpawnGroupPackNodesDict.Add(spawnAction.RandomSpawnGroupPackKey, new List<TreeNode>());
                    }
                    spawnView.RandomSpawnGroupPackNodesDict[spawnAction.RandomSpawnGroupPackKey].Add(spawnNode);
                }

                spawnNode.Name = Guid.NewGuid().ToString(); //唯一标识
                spawnView.SpawnNodesList.Add(spawnNode);
                spawnView.ValidSpawnNodes.Add(spawnNode, true);
            }

            foreach (var item in randomSpawnGroupDefaultNodes)
            {
                spawnView.UpdateRandomSpawnGroupNodes(item.Value);
            }
            spawnView.UpdateSpawnViewNodeColor();
            spawnView.UpdateNodes();

            return spawnsNode;
        }

        public static TreeNode CreateGroupsNode(string name, SpawnView spawnView)
        {
            if (string.IsNullOrEmpty(name) || spawnView == null)
            {
                return null;
            }

            TreeNode groupsNode = new TreeNode(name);

            //HiddenGroup
            if (spawnView.HiddenGroups != null)
            {
                TreeNode hiddenGroupsNode = groupsNode.Nodes.Add("hiddenGroups");
                foreach (var item in spawnView.HiddenGroups)
                {
                    string groupName = item.Key;
                    TreeNode hiddenGroupNode = hiddenGroupsNode.Nodes.Add(groupName);
                    foreach (TreeNode treeNode in spawnView.SpawnNodesList)
                    {
                        if (treeNode.Tag is ISpawnAction spawnAction)
                        {
                            if (spawnAction.HiddenGroup == groupName)
                            {
                                TreeNode clonedNode = (TreeNode)treeNode.Clone();
                                clonedNode.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
                                hiddenGroupNode.Nodes.Add(clonedNode);
                            }
                        }
                    }
                }
            }

            //RandomGroup
            if (spawnView.RandomSpawnGroupNodesDict != null)
            {
                TreeNode randomGroupsNode = groupsNode.Nodes.Add("randomGroups");
                Dictionary<string, int> packCountDict = new Dictionary<string, int>();
                foreach (var item in spawnView.RandomSpawnGroupNodesDict)
                {
                    string groupName = item.Key;
                    List<TreeNode> randomGroupNodes = item.Value;

                    //RandomSpawnGroupPackKey计数
                    packCountDict.Clear();
                    foreach (TreeNode treeNode in randomGroupNodes)
                    {
                        if (treeNode.Tag is ISpawnAction spawnAction)
                        {
                            if (!string.IsNullOrEmpty(spawnAction.RandomSpawnGroupPackKey) && spawnView.RandomSpawnGroupPackNodesDict != null &&
                                spawnView.RandomSpawnGroupPackNodesDict.TryGetValue(spawnAction.RandomSpawnGroupPackKey, out List<TreeNode> randomGroupPackNodes))
                            {
                                if (!packCountDict.ContainsKey(spawnAction.RandomSpawnGroupPackKey))
                                {
                                    packCountDict.Add(spawnAction.RandomSpawnGroupPackKey, 0);
                                }
                                packCountDict[spawnAction.RandomSpawnGroupPackKey]++;
                            }
                        }
                    }

                    TreeNode randomGroupNode = randomGroupsNode.Nodes.Add($"randomGroup #{randomGroupsNode.Nodes.Count} {groupName}");
                    foreach (TreeNode treeNode in randomGroupNodes)
                    {
                        string weightText = "";
                        if (treeNode.Tag is ISpawnAction spawnAction)
                        {
                            if (!string.IsNullOrEmpty(spawnAction.RandomSpawnGroupKey))
                            {
                                int weight = spawnAction.Weight;
                                int totalWeight = spawnAction.TotalWeight;
                                weightText = weight < totalWeight ? $"{weight}/{totalWeight}" : weight.ToString();
                            }

                            if (!string.IsNullOrEmpty(spawnAction.RandomSpawnGroupPackKey) && spawnView.RandomSpawnGroupPackNodesDict != null &&
                                spawnView.RandomSpawnGroupPackNodesDict.TryGetValue(spawnAction.RandomSpawnGroupPackKey, out List<TreeNode> randomGroupPackNodes)
                                )
                            {
                                if (packCountDict.Count > 1)
                                {
                                    TreeNode groupPackNode = new TreeNode($"#{randomGroupNode.Nodes.Count} {spawnAction.RandomSpawnGroupPackKey}:{weightText}");
                                    randomGroupNode.Nodes.Add(groupPackNode);
                                    foreach (TreeNode randomGroupPackNode in randomGroupPackNodes)
                                    {
                                        TreeNode clonedNode = (TreeNode)randomGroupPackNode.Clone();
                                        clonedNode.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
                                        groupPackNode.Nodes.Add(clonedNode);
                                    }
                                }
                                else
                                {
                                    foreach (TreeNode randomGroupPackNode in randomGroupPackNodes)
                                    {
                                        if (!randomGroupNode.Nodes.ContainsKey(randomGroupPackNode.Name))
                                        {
                                            TreeNode clonedNode = (TreeNode)randomGroupPackNode.Clone();
                                            clonedNode.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
                                            randomGroupNode.Nodes.Add(clonedNode);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                TreeNode clonedNode = (TreeNode)treeNode.Clone();
                                clonedNode.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
                                randomGroupNode.Nodes.Add((TreeNode)treeNode.Clone());
                            }
                        }
                    }
                }
            }
            return groupsNode;
        }
    }
}