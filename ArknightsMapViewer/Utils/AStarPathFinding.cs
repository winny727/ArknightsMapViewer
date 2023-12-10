using System;
using System.Collections.Generic;
using System.Drawing;

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

#region A*寻路算法

public class AStarPathFinding
{
    /// <summary>
    /// 地图宽度
    /// </summary>
    public int mapWidth;
    /// <summary>
    /// 地图高度
    /// </summary>
    public int mapHeight;
    /// <summary>
    /// 判断是否为路障
    /// </summary>
    public bool[,] isBarrier;

    public List<Node> GetAStarPath(Vector2Int origin, Vector2Int destination)
    {
        List<Node> open = new List<Node>();//待搜索的点
        List<Node> close = new List<Node>();//已经搜索过一次或多次的点
        List<Node> road = new List<Node>();
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
                    if (current.position.x + i >= mapWidth ||
                        current.position.x + i < 0 ||
                        current.position.y + j >= mapHeight ||
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
}

#endregion

