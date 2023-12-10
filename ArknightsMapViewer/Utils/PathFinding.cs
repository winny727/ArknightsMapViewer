using System;
using System.Collections.Generic;

namespace ArknightsMapViewer
{
    public abstract class PathFinding
    {
        public bool[,] isBarrier;
        public abstract List<Vector2Int> GetPath(Vector2Int origin, Vector2Int destination);
    }

    #region A*寻路算法

    public class AStarPathFinding : PathFinding
    {
        //结点类
        public class Node
        {
            public Vector2Int position;
            public float cost;//该点到目标点的预计代价
            public float pre_cost;//从起点移动到该点的总代价
            public float nex_cost;//从该点到终点的预计代价
            public Node parent;//前置节点

            public Node(Vector2Int location)
            {
                this.position = location;
                parent = null;
            }
            //起点的结点（没有前置结点）
            public Node(Vector2Int location, Vector2Int destination)
            {
                this.position = location;
                cost = (destination - position).sqrMagnitude;
                parent = null;
            }
            //带有父节点（前置节点）的Node
            public Node(Vector2Int location, Vector2Int destination, Node parent)
            {
                this.position = location;
                //移动代价+1
                this.pre_cost = parent.pre_cost;
                nex_cost = (destination - position).sqrMagnitude;
                cost = pre_cost + nex_cost;
                this.parent = parent;
            }
        }

        public List<Node> GetAstarPath(Vector2Int origin, Vector2Int destination)
        {
            List<Node> open = new List<Node>();//待搜索的点
            List<Node> close = new List<Node>();//已经搜索过一次或多次的点
            List<Node> road = new List<Node>();

            int width = isBarrier.GetLength(0);
            int height = isBarrier.GetLength(1);

            Node current;
            int minLocation;
            float min;
            //开始搜索
            open.Add(new Node(origin, destination));
            do
            {
                //如果搜索完则无路径
                if (open.Count == 0) return road;

                //找出open中权值最小的结点移出open，加入close，并作为当前点
                minLocation = 0;
                min = open[0].cost;
                for (int i = 0; i < open.ToArray().Length; i++)
                {
                    minLocation = open[i].cost < min ? i : minLocation;
                    min = open[i].cost < min ? open[i].cost : min;
                }
                current = open[minLocation];
                open.RemoveAt(minLocation);
                close.Add(current);
                int index;//存储所有暂时需要保存的list中元素位置
                          //遍历当前点邻域，使所有点的前置节点都保证其代价更小
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        //去除左上右上左下右下
                        if (Math.Abs(i) + Math.Abs(j) == 2) continue;

                        //判断是否超出地图范围并排除本身
                        if (current.position.x + i >= width ||
                            current.position.x + i < 0 ||
                            current.position.y + j >= height ||
                            current.position.y + j < 0 || (i == 0 && j == 0))

                        {
                            continue;
                        }
                        //判断该处是否为路障
                        if (isBarrier[(int)current.position.x + i, (int)current.position.y + j])
                        {
                            continue;
                        }
                        //已经在close中则跳过
                        if (close.Exists(t => (t.position - new Vector2Int(current.position.x + i,
                            current.position.y + j)).sqrMagnitude == 0))

                        {
                            continue;
                        }
                        //open中是否已经存在，如果不存在，添加进open，如果存在，看所在路径是否需要更新
                        Node temp = new Node(new Vector2Int(current.position.x + i,
                            current.position.y + j), destination, current);

                        if (!open.Exists(t => (t.position - new Vector2Int(current.position.x + i,
                            current.position.y + j)).sqrMagnitude == 0))

                        {
                            open.Add(temp);
                        }
                        else
                        {
                            index = open.FindIndex(t => (t.position - new Vector2Int
                            (current.position.x + i, current.position.y + j)).sqrMagnitude == 0);

                            //如果以current点为父节点计算出来的代价比现有代价小，改变其前置结点
                            if (open[index].cost > temp.cost)
                            {
                                open.RemoveAt(index);
                                open.Add(temp);
                            }
                        }
                    }
                }
            } while ((current.position - destination).sqrMagnitude != 0);
            //将链中的结点提取出来
            road.Add(current);
            do
            {
                current = current.parent;
                road.Add(current);
            } while (current.parent != null);
            //倒置
            road.Reverse();

            return road;
        }

        public override List<Vector2Int> GetPath(Vector2Int origin, Vector2Int destination)
        {
            List<Node> road = GetAstarPath(origin, destination);
            List<Vector2Int> result = new List<Vector2Int>();
            for (int i = 0; i < road.Count; i++)
            {
                result.Add(road[i].position);
            }
            return result;
        }
    }

    #endregion

    #region Dijkstra寻路算法

    public class DijkstraPathFinding : PathFinding
    {
        public class DijkstraInfo
        {
            public DijkstraInfo(int len)
            {
                Dists = new float[len];//从原点v到其他的各定点当前的最短路径长度
                Path = new int[len];//path[i]表示从原点到定点i之间最短路径的前驱节点
            }
            public float[] Dists { get; set; }
            public int[] Path { get; set; }
        }

        /// <summary>
        /// 获取图中两点的距离
        /// </summary>
        /// <param name="mgrap"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        public DijkstraInfo PathFindingByDijkstra(float[,] mgrap, int startIndex)
        {
            int len = mgrap.GetLength(0);//节点个数
            DijkstraInfo dijkstraInfo = new DijkstraInfo(len);
            int[] s = new int[len];   //选定的顶点的集合
            float mindis;
            int u;
            u = 0;
            for (int i = 0; i < len; i++)
            {
                dijkstraInfo.Dists[i] = mgrap[startIndex, i];       //距离初始化
                s[i] = 0;                        //s[]置空  0表示i不在s集合中
                if (mgrap[startIndex, i] < int.MaxValue)
                {      //路径初始化
                    dijkstraInfo.Path[i] = startIndex;
                }
                else
                {
                    dijkstraInfo.Path[i] = -1;
                }
            }
            s[startIndex] = 1;                  //源点编号v放入s中
            dijkstraInfo.Path[startIndex] = 0;
            for (int i = 0; i < len; i++)                //循环直到所有顶点的最短路径都求出
            {
                mindis = int.MaxValue;                    //mindis置最小长度初值
                for (int j = 0; j < len; j++)         //选取不在s中且具有最小距离的顶点u
                    if (s[j] == 0 && dijkstraInfo.Dists[j] < mindis)
                    {
                        u = j;
                        mindis = dijkstraInfo.Dists[j];
                    }
                s[u] = 1;                       //顶点u加入s中
                for (int j = 0; j < len; j++)
                {        //修改不在s中的顶点的距离
                    if (s[j] == 0)
                    {
                        if (mgrap[u, j] < int.MaxValue && dijkstraInfo.Dists[u] + mgrap[u, j] < dijkstraInfo.Dists[j])
                        {
                            dijkstraInfo.Dists[j] = dijkstraInfo.Dists[u] + mgrap[u, j];
                            dijkstraInfo.Path[j] = u;
                        }
                    }
                }
            }

            return dijkstraInfo;
        }

        public override List<Vector2Int> GetPath(Vector2Int origin, Vector2Int destination)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            List<Vector2Int> nodeList = new List<Vector2Int>(); //所有节点列表
            int currentIndex = 0;
            int destinationIndex = -1;

            int width = isBarrier.GetLength(0);
            int height = isBarrier.GetLength(1);
            int maxLength = width * height;
            float[,] tempMatrix = new float[maxLength, maxLength];

            //遍历所有可以到达的点，生成带权无向图的三角邻接矩阵
            nodeList.Add(origin);
            do
            {
                Vector2Int current = nodeList[currentIndex];
                if ((current - destination).sqrMagnitude == 0)
                {
                    destinationIndex = currentIndex;
                }

                //遍历当前点邻域
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        //去除左上右上左下右下
                        if (Math.Abs(i) + Math.Abs(j) == 2)
                        {
                            continue;
                        }

                        //判断是否超出地图范围并排除本身
                        if (current.x + i >= width ||
                            current.x + i < 0 ||
                            current.y + j >= height ||
                            current.y + j < 0 || (i == 0 && j == 0))

                        {
                            continue;
                        }

                        //判断该处是否为路障
                        if (isBarrier[current.x + i, current.y + j])
                        {
                            continue;
                        }

                        Vector2Int temp = new Vector2Int(current.x + i, current.y + j);

                        //nodeList中是否已经存在，如果不存在，添加进nodeList
                        if (!nodeList.Exists(t => (t - temp).sqrMagnitude == 0))
                        {
                            nodeList.Add(temp);
                            tempMatrix[currentIndex, nodeList.Count - 1] = 1; //记录连接关系
                        }
                    }
                }
            }
            while (++currentIndex < nodeList.Count);

            //若到达不了目标点则返回
            if (destinationIndex < 0)
            {
                return path;
            }

            //地图连接区域的三角邻接矩阵表示
            int length = nodeList.Count;
            float[,] matrix = new float[length, length];
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    if (tempMatrix[i, j] > 0)
                    {
                        matrix[i, j] = tempMatrix[i, j];
                    }
                    else
                    {
                        matrix[i, j] = int.MaxValue;
                    }
                }
            }

            //Dijkstra求解从起点到各个点的距离和路径
            DijkstraInfo dijkstraInfo = PathFindingByDijkstra(matrix, 0);

            //从目标点根据各前驱节点获取路径
            int nodeIndex = destinationIndex;
            while (nodeIndex > 0)
            {
                path.Add(nodeList[nodeIndex]);
                nodeIndex = dijkstraInfo.Path[nodeIndex];
            }
            path.Add(nodeList[0]);
            path.Reverse();

            return path;
        }
    }
    #endregion
}
